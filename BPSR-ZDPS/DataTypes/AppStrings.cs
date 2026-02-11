using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public static class AppStrings
    {
        public static string CurrentLocale { get; set; } = "en";//CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        public static FrozenDictionary<string, Dictionary<string, string>> Strings = new Dictionary<string, Dictionary<string, string>>().ToFrozenDictionary();

        public static string GetLocalized(string key, bool KeyIfEmptyValue = false)
        {
            Strings.TryGetValue(key, out var value);
            if (value.TryGetValue(CurrentLocale, out var localizedString))
            {
                return localizedString;
            }
            else
            {
                if (KeyIfEmptyValue)
                {
                    return key;
                }
                else
                {
                    if (value.TryGetValue(CurrentLocale, out var enString))
                    {
                        return enString;
                    }
                    else
                    {
                        return key; // This probably should be an empty string at this point, but we'll use the key for now
                    }
                }
            }
        }
    }
}
