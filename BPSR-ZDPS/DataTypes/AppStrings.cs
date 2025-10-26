using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public static class AppStrings
    {
        public static string CurrentLocale { get; set; } = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        public static Dictionary<string, Dictionary<string, string>> Strings = new();

        public static string GetLocalized(string key, bool KeyIfEmptyValue = false)
        {
            Strings.TryGetValue(key, out var value);
            if (value.TryGetValue(CurrentLocale, out var localizedString))
            {
                return localizedString;
            }
            else
            {
                return key;
            }
        }
    }
}
