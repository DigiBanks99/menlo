using System.Globalization;

namespace Menlo.Common.Extensions;

public static class CultureInfoExtensions
{
    public static CultureInfo GetCultureInfo(string cultureInfoName)
    {
        if (cultureInfoName != "en-ZA")
        {
            return CultureInfo.CreateSpecificCulture(cultureInfoName);
        }

        // necessary because Ubuntu 24.04 has the wrong configuration
        CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("en-ZA");
        cultureInfo.NumberFormat.NumberDecimalSeparator = ",";
        cultureInfo.NumberFormat.NumberGroupSeparator = " ";
        cultureInfo.NumberFormat.CurrencyDecimalSeparator = ",";
        cultureInfo.NumberFormat.CurrencyGroupSeparator = " ";

        return cultureInfo;
    }
}
