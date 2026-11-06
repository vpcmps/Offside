using System.Globalization;

namespace Offside;

/// <summary>Resolves localized success messages.</summary>
public interface INoticeMessageResolver
{
    /// <summary>Resolves the notice in the requested culture.</summary>
    string GetMessage(Notice notice, CultureInfo culture);
}
