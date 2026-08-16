namespace Offside;

/// <summary>
/// A domain failure described by data instead of by an exception: a stable <see cref="Code"/>,
/// an <see cref="ErrorKind"/>, interpolation <see cref="Arguments"/>, and an optional <see cref="Field"/>.
/// </summary>
/// <remarks>
/// Instances are immutable and compare by value. They are created only through the static
/// factories on this type; the constructor is internal so every error carries a known shape.
/// The human-readable text lives in a JSON catalog keyed by <see cref="Code"/> — never in C#.
/// </remarks>
/// <example>
/// <code>
/// public Result&lt;Order&gt; Get(string id)
/// {
///     var order = _orders.Find(id);
///     return order is null
///         ? Result&lt;Order&gt;.Failure(Error.NotFound("order", id))
///         : Result&lt;Order&gt;.Success(order);
/// }
/// </code>
/// </example>
public sealed class Error : IEquatable<Error>
{
    /// <summary>
    /// The stable identifier of the failure, and the lookup key in the message catalogs
    /// (for example <c>not_found</c> or <c>order.already_shipped</c>).
    /// </summary>
    public string Code { get; }

    /// <summary>The failure species, which determines the HTTP status and the severity rank.</summary>
    public ErrorKind Kind { get; }

    /// <summary>
    /// A read-only snapshot of the values available to the message template, keyed by token name.
    /// A template token <c>{resource}</c> is filled from the <c>resource</c> entry.
    /// </summary>
    /// <remarks>Arguments are serialized into HTTP responses indirectly through the message; never place secrets here.</remarks>
    public IReadOnlyDictionary<string, object?> Arguments { get; }

    /// <summary>The name of the offending input field, when the failure is attributable to one. Otherwise <see langword="null"/>.</summary>
    public string? Field { get; }

    internal Error(
        string code,
        ErrorKind kind,
        IReadOnlyDictionary<string, object?> arguments,
        string? field)
    {
        Code = code;
        Kind = kind;
        Arguments = arguments;
        Field = field;
    }

    /// <summary>Creates a <see cref="ErrorKind.NotFound"/> error with code <c>not_found</c>.</summary>
    /// <param name="resource">The resource name, exposed to the template as <c>{resource}</c>.</param>
    /// <param name="id">The identifier that was looked up, exposed as <c>{id}</c>.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(string resource, object? id = null) =>
        Create("not_found", ErrorKind.NotFound, new { resource, id });

    /// <summary>Creates a <see cref="ErrorKind.Gone"/> error with code <c>gone</c>.</summary>
    /// <param name="resource">The resource name, exposed to the template as <c>{resource}</c>.</param>
    /// <param name="id">The identifier that was looked up, exposed as <c>{id}</c>.</param>
    /// <returns>The error.</returns>
    public static Error Gone(string resource, object? id = null) =>
        Create("gone", ErrorKind.Gone, new { resource, id });

    /// <summary>Creates a <see cref="ErrorKind.Conflict"/> error with code <c>conflict</c>.</summary>
    /// <param name="resource">The resource name, exposed to the template as <c>{resource}</c>.</param>
    /// <param name="reason">An optional reason, exposed as <c>{reason}</c>.</param>
    /// <returns>The error.</returns>
    public static Error Conflict(string resource, string? reason = null) =>
        Create("conflict", ErrorKind.Conflict, new { resource, reason });

    /// <summary>
    /// Creates a <see cref="ErrorKind.Validation"/> error attributed to <paramref name="field"/>.
    /// This is the only factory that sets <see cref="Field"/>.
    /// </summary>
    /// <param name="field">The offending field name. Also exposed to the template as <c>{field}</c>.</param>
    /// <param name="code">A specific catalog code. When omitted or blank, <c>validation</c> is used.</param>
    /// <param name="attemptedValue">The rejected value, exposed as <c>{attemptedValue}</c>.</param>
    /// <returns>The error.</returns>
    public static Error Validation(string field, string? code = null, object? attemptedValue = null)
    {
        var resolvedCode = string.IsNullOrWhiteSpace(code) ? "validation" : code!.Trim();
        return new Error(
            resolvedCode,
            ErrorKind.Validation,
            ErrorArgumentConverter.ToDictionary(new { field, attemptedValue }),
            field);
    }

    /// <summary>Creates a <see cref="ErrorKind.BadRequest"/> error with code <c>bad_request</c>.</summary>
    /// <param name="reason">An optional reason, exposed to the template as <c>{reason}</c>.</param>
    /// <returns>The error.</returns>
    public static Error BadRequest(string? reason = null) =>
        Create("bad_request", ErrorKind.BadRequest, new { reason });

    /// <summary>Creates an <see cref="ErrorKind.Unauthorized"/> error with code <c>unauthorized</c>.</summary>
    /// <param name="reason">An optional reason, exposed to the template as <c>{reason}</c>.</param>
    /// <returns>The error.</returns>
    public static Error Unauthorized(string? reason = null) =>
        Create("unauthorized", ErrorKind.Unauthorized, new { reason });

    /// <summary>Creates a <see cref="ErrorKind.Forbidden"/> error with code <c>forbidden</c>.</summary>
    /// <param name="reason">An optional reason, exposed to the template as <c>{reason}</c>.</param>
    /// <returns>The error.</returns>
    public static Error Forbidden(string? reason = null) =>
        Create("forbidden", ErrorKind.Forbidden, new { reason });

    /// <summary>Creates a <see cref="ErrorKind.PreconditionFailed"/> error with code <c>precondition_failed</c>.</summary>
    /// <param name="reason">An optional reason, exposed to the template as <c>{reason}</c>.</param>
    /// <returns>The error.</returns>
    public static Error PreconditionFailed(string? reason = null) =>
        Create("precondition_failed", ErrorKind.PreconditionFailed, new { reason });

    /// <summary>Creates an <see cref="ErrorKind.Unprocessable"/> error with code <c>unprocessable</c>.</summary>
    /// <param name="reason">An optional reason, exposed to the template as <c>{reason}</c>.</param>
    /// <returns>The error.</returns>
    public static Error Unprocessable(string? reason = null) =>
        Create("unprocessable", ErrorKind.Unprocessable, new { reason });

    /// <summary>Creates a <see cref="ErrorKind.TooManyRequests"/> error with code <c>too_many_requests</c>.</summary>
    /// <param name="reason">An optional reason, exposed to the template as <c>{reason}</c>.</param>
    /// <returns>The error.</returns>
    public static Error TooManyRequests(string? reason = null) =>
        Create("too_many_requests", ErrorKind.TooManyRequests, new { reason });

    /// <summary>Creates an <see cref="ErrorKind.Unexpected"/> error with code <c>unexpected</c>.</summary>
    /// <param name="detail">
    /// Diagnostic text for logs. It is never sent to the client unless
    /// <c>ExposeExceptionDetails</c> is enabled, in which case it appears in the <c>debug</c> field.
    /// </param>
    /// <returns>The error.</returns>
    public static Error Unexpected(string? detail = null) =>
        Create("unexpected", ErrorKind.Unexpected, new { detail });

    /// <summary>
    /// Creates an error for a specific business rule, reusing an existing <see cref="ErrorKind"/>
    /// with a code of your own.
    /// </summary>
    /// <param name="code">The catalog key, for example <c>order.already_shipped</c>. Surrounding whitespace is trimmed.</param>
    /// <param name="kind">The kind that determines the HTTP status and severity.</param>
    /// <param name="arguments">An object or dictionary whose entries become <see cref="Arguments"/>.</param>
    /// <param name="field">The offending field name, when the rule is attributable to one.</param>
    /// <returns>The error.</returns>
    /// <exception cref="ArgumentException"><paramref name="code"/> is null, empty, or whitespace.</exception>
    /// <example>
    /// <code>
    /// Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId });
    /// </code>
    /// </example>
    public static Error Custom(
        string code,
        ErrorKind kind,
        object? arguments = null,
        string? field = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code must not be empty.", nameof(code));

        return new Error(
            code.Trim(),
            kind,
            ErrorArgumentConverter.ToDictionary(arguments),
            field);
    }

    private static Error Create(string code, ErrorKind kind, object arguments) =>
        new Error(code, kind, ErrorArgumentConverter.ToDictionary(arguments), field: null);

    /// <summary>
    /// Wraps this error in a <see cref="DomainException"/> — the escape hatch for boundaries
    /// that cannot return a <see cref="Result"/>.
    /// </summary>
    /// <returns>An exception carrying this error.</returns>
    public DomainException ToException() =>
        new DomainException(new[] { this });

    /// <summary>Determines whether this error has the same code, kind, field, and arguments as another.</summary>
    /// <param name="other">The error to compare with.</param>
    /// <returns><see langword="true"/> when the two errors are equivalent.</returns>
    public bool Equals(Error? other) =>
        other is not null
        && Code == other.Code
        && Kind == other.Kind
        && Field == other.Field
        && ArgumentsEqual(Arguments, other.Arguments);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Error);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = HashCode.Combine(Code, Kind, Field);
        var argumentsHash = 0;
        foreach (var pair in Arguments)
            argumentsHash ^= HashCode.Combine(pair.Key, pair.Value);
        return HashCode.Combine(hash, argumentsHash);
    }

    /// <summary>Compares two errors by value.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when both are null or equivalent.</returns>
    public static bool operator ==(Error? left, Error? right) => object.Equals(left, right);

    /// <summary>Compares two errors by value.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when the two errors differ.</returns>
    public static bool operator !=(Error? left, Error? right) => !object.Equals(left, right);

    private static bool ArgumentsEqual(
        IReadOnlyDictionary<string, object?> left,
        IReadOnlyDictionary<string, object?> right)
    {
        if (left.Count != right.Count) return false;
        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var value)) return false;
            if (!Equals(pair.Value, value)) return false;
        }
        return true;
    }
}
