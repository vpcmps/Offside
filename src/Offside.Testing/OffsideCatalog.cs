using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Offside.Testing;

/// <summary>
/// Assertions over a JSON message catalog, read directly rather than through an
/// <see cref="IErrorMessageResolver"/>.
/// </summary>
/// <remarks>
/// <para>
/// The built-in resolver returns <see cref="Error.Code"/> when a catalog has no entry for it,
/// which makes "code missing" indistinguishable from "template equals the code". Reading the
/// JSON removes that ambiguity, and makes coverage assertions possible — which codes exist,
/// which template still has an unfilled token, whether a translated catalog kept up with the
/// default one.
/// </para>
/// <para>
/// These assertions belong in a test that runs in CI: a missing code is a runtime-only defect
/// otherwise, and it surfaces as an error message that reads like a machine key.
/// </para>
/// </remarks>
public sealed class OffsideCatalog
{
    private readonly Dictionary<string, string> _messages;

    private OffsideCatalog(string source, Dictionary<string, string> messages)
    {
        Source = source;
        _messages = messages;
    }

    /// <summary>Gets a description of where this catalog was read from, used in failure messages.</summary>
    public string Source { get; }

    /// <summary>Gets the codes defined by the catalog.</summary>
    public IReadOnlyCollection<string> Codes => _messages.Keys;

    /// <summary>Reads a catalog from a JSON file.</summary>
    /// <param name="path">The file path. Relative paths resolve against <see cref="AppContext.BaseDirectory"/>, matching <c>AddJsonFile</c>.</param>
    /// <returns>The loaded catalog.</returns>
    /// <exception cref="OffsideAssertionException">The file does not exist or does not parse.</exception>
    public static OffsideCatalog FromFile(string path)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        var resolved = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

        if (!File.Exists(resolved))
            throw AssertionEngine.Fail("Expected an error catalog at \"" + resolved + "\", but the file does not exist.");

        return Parse(resolved, File.ReadAllText(resolved));
    }

    /// <summary>Reads a catalog from JSON content.</summary>
    /// <param name="json">The catalog content — not a path.</param>
    /// <param name="source">An optional description used in failure messages.</param>
    /// <returns>The loaded catalog.</returns>
    /// <exception cref="OffsideAssertionException">The content does not parse.</exception>
    public static OffsideCatalog FromJson(string json, string? source = null)
    {
        if (json is null)
            throw new ArgumentNullException(nameof(json));

        return Parse(source ?? "(inline json)", json);
    }

    /// <summary>Reads a catalog from a stream.</summary>
    /// <param name="json">The stream holding the catalog content.</param>
    /// <param name="source">An optional description used in failure messages.</param>
    /// <returns>The loaded catalog.</returns>
    /// <exception cref="OffsideAssertionException">The content does not parse.</exception>
    public static OffsideCatalog FromStream(Stream json, string? source = null)
    {
        if (json is null)
            throw new ArgumentNullException(nameof(json));

        using var reader = new StreamReader(json, Encoding.UTF8);
        return Parse(source ?? "(stream)", reader.ReadToEnd());
    }

    /// <summary>Reads a catalog from an embedded resource.</summary>
    /// <param name="assembly">The assembly holding the resource.</param>
    /// <param name="resourceName">The fully qualified manifest resource name.</param>
    /// <returns>The loaded catalog.</returns>
    /// <exception cref="OffsideAssertionException">The resource does not exist or does not parse.</exception>
    public static OffsideCatalog FromAssembly(Assembly assembly, string resourceName)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));
        if (resourceName is null)
            throw new ArgumentNullException(nameof(resourceName));

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            var assemblyName = assembly.GetName().Name ?? assembly.FullName;
            throw AssertionEngine.Fail(
                "Expected an error catalog resource \"" + resourceName + "\" in assembly \"" + assemblyName +
                "\", but no such resource exists.");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return Parse(resourceName, reader.ReadToEnd());
    }

    /// <summary>Asserts that the catalog defines a code.</summary>
    /// <param name="code">The expected <see cref="Error.Code"/>.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="OffsideAssertionException">The catalog has no entry for the code.</exception>
    public OffsideCatalog ShouldDefine(string code)
    {
        if (code is null)
            throw new ArgumentNullException(nameof(code));

        if (!_messages.ContainsKey(code))
        {
            throw AssertionEngine.Fail(
                "Expected catalog " + Source + " to define \"" + code + "\", but it defines " +
                DescribeCodes(_messages.Keys) + ".");
        }

        return this;
    }

    /// <summary>Asserts that the catalog defines every one of these codes.</summary>
    /// <param name="codes">The expected codes.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="OffsideAssertionException">One or more codes are missing. The message names all of them.</exception>
    public OffsideCatalog ShouldDefineAll(params string[] codes)
    {
        if (codes is null)
            throw new ArgumentNullException(nameof(codes));

        var missing = codes.Where(code => !_messages.ContainsKey(code)).ToArray();
        if (missing.Length > 0)
        {
            throw AssertionEngine.Fail(
                "Expected catalog " + Source + " to define every requested code, but " +
                DescribeCodes(missing) + " " + (missing.Length == 1 ? "is" : "are") + " missing.");
        }

        return this;
    }

    /// <summary>
    /// Asserts that the catalog fully resolves an error: the code is defined, and every
    /// <c>{token}</c> in its template is filled by <see cref="Error.Arguments"/>.
    /// </summary>
    /// <param name="error">The error to resolve.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="OffsideAssertionException">The code is undefined, or a token was left unfilled.</exception>
    public OffsideCatalog ShouldResolve(Error error)
    {
        if (error is null)
            throw new ArgumentNullException(nameof(error));

        if (!_messages.TryGetValue(error.Code, out var template))
        {
            throw AssertionEngine.Fail(
                "Expected catalog " + Source + " to resolve " + ErrorFormatter.Describe(error) +
                ", but it defines no message for \"" + error.Code + "\".");
        }

        var message = ErrorMessageTemplate.Interpolate(template, error.Arguments);
        var unresolved = FindTokens(message);
        if (unresolved.Count > 0)
        {
            throw AssertionEngine.Fail(
                "Expected catalog " + Source + " to resolve " + ErrorFormatter.Describe(error) +
                ", but the message still has " + DescribeCodes(unresolved) + " unfilled: \"" + message + "\".");
        }

        return this;
    }

    /// <summary>Asserts <see cref="ShouldResolve"/> for several errors at once.</summary>
    /// <param name="errors">The errors to resolve.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="OffsideAssertionException">Any error fails to resolve.</exception>
    public OffsideCatalog ShouldResolveAll(params Error[] errors)
    {
        if (errors is null)
            throw new ArgumentNullException(nameof(errors));

        foreach (var error in errors)
            ShouldResolve(error);

        return this;
    }

    /// <summary>
    /// Asserts that this catalog defines exactly the same codes as another one — the check that
    /// keeps a translated catalog from silently drifting behind the default one.
    /// </summary>
    /// <param name="other">The catalog to compare against, usually the invariant one.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="OffsideAssertionException">Either catalog defines a code the other does not.</exception>
    public OffsideCatalog ShouldDefineSameCodesAs(OffsideCatalog other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        var missing = other._messages.Keys.Where(code => !_messages.ContainsKey(code)).ToArray();
        var extra = _messages.Keys.Where(code => !other._messages.ContainsKey(code)).ToArray();

        if (missing.Length == 0 && extra.Length == 0)
            return this;

        var message = new StringBuilder();
        message.Append("Expected catalog ").Append(Source).Append(" to define the same codes as ")
            .Append(other.Source).Append(", but");

        if (missing.Length > 0)
            message.Append(" it is missing ").Append(DescribeCodes(missing));

        if (missing.Length > 0 && extra.Length > 0)
            message.Append(" and");

        if (extra.Length > 0)
            message.Append(" it defines ").Append(DescribeCodes(extra)).Append(" which the other does not");

        message.Append('.');
        throw AssertionEngine.Fail(message.ToString());
    }

    private static OffsideCatalog Parse(string source, string json)
    {
        Dictionary<string, string>? messages;

        try
        {
            messages = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch (JsonException exception)
        {
            throw new OffsideAssertionException(
                "Expected catalog " + source + " to be a flat JSON object of \"code\": \"template\" pairs, but it could not be parsed: " +
                exception.Message,
                exception);
        }

        return new OffsideCatalog(source, messages ?? new Dictionary<string, string>());
    }

    private static List<string> FindTokens(string message)
    {
        var tokens = new List<string>();
        var start = message.IndexOf('{');

        while (start >= 0)
        {
            var end = message.IndexOf('}', start + 1);
            if (end < 0)
                break;

            tokens.Add(message.Substring(start, end - start + 1));
            start = message.IndexOf('{', end + 1);
        }

        return tokens;
    }

    private static string DescribeCodes(IEnumerable<string> codes)
    {
        var text = new StringBuilder();
        text.Append('[');

        var first = true;
        foreach (var code in codes.OrderBy(code => code, StringComparer.Ordinal))
        {
            if (!first)
                text.Append(", ");
            first = false;

            text.Append('"').Append(code).Append('"');
        }

        text.Append(']');
        return text.ToString();
    }
}
