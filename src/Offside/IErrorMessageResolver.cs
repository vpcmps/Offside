using System.Globalization;

namespace Offside;

public interface IErrorMessageResolver
{
    string GetMessage(Error error, CultureInfo culture);
}
