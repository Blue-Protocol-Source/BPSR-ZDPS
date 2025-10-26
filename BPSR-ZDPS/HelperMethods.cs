using BPSR_ZDPS.DataTypes;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Zproto;

namespace BPSR_ZDPS
{
    public class HelperMethods
    {
        public static GLFWwindowPtr GLFWwindow;
        public static Dictionary<string, ImFontPtr> Fonts = new();

        public static class DataTables
        {
            public static MonsterTable Monsters = new MonsterTable();
            public static SkillTable Skills = new SkillTable();
        }

        public enum ProfessionId : int
        {
            Profession_Unknown = 0,
            Profession_Stormblade = 1,
            Profession_FrostMage = 2,
            // PurifyingAxe
            Profession_WindKnight = 4,
            Profession_VerdantOracle = 5,
            // UNK
            // UNK
            // ThunderHandCannon
            Profession_HeavyGuardian = 9,
            // DarkSpiritDance
            Profession_Marksman = 11,
            Profession_ShieldKnight = 12,
            Profession_BeatPerformer = 13,
        }

        public enum SubProfessionId : int
        {
            SubProfession_Unknown = 00_00_00,
            SubProfession_Iaido = 01_00_01,
            SubProfession_Moonstrike = 01_00_02,
            SubProfession_Icicle = 02_00_01,
            SubProfession_Frostbeam = 02_00_02,
            SubProfession_Vanguard = 04_00_01,
            SubProfession_Skyward = 04_00_02,
            SubProfession_Smite = 05_00_01,
            SubProfession_Lifebind = 05_00_02,
            SubProfession_Earthfort = 09_00_01,
            SubProfession_Block = 09_00_02,
            SubProfession_Wildpack = 11_00_01,
            SubProfession_Falconry = 11_00_02,
            SubProfession_Recovery = 12_00_01,
            SubProfession_Shield = 12_00_02,
            SubProfession_Dissonance = 13_00_01,
            SubProfession_Concerto = 13_00_02,
        }

        public static string GetProfessionNameFromId(int professionId) => professionId switch
        {
            0 => AppStrings.GetLocalized("Profession_Unknown"),
            1 => AppStrings.GetLocalized("Profession_Stormblade"),
            2 => AppStrings.GetLocalized("Profession_FrostMage"),
            3 => "Purifying Evilfire Axe", // PurifyingAxe
            4 => AppStrings.GetLocalized("Profession_WindKnight"),
            5 => AppStrings.GetLocalized("Profession_VerdantOracle"),
            8 => "Thunder Flash Hand Cannon", // ThunderHandCannon
            9 => AppStrings.GetLocalized("Profession_HeavyGuardian"),
            10 => "Dark Spirit Dance Ritual Blade", // DarkSpiritDance
            11 => AppStrings.GetLocalized("Profession_Marksman"),
            12 => AppStrings.GetLocalized("Profession_ShieldKnight"),
            13 => AppStrings.GetLocalized("Profession_BeatPerformer"),
            _ => ""
        };

        // This is our own made up SubProfessionId used to support cross-locale lookups
        // The numbering format below uses leading zeros and is: <ProfessionId>_<Reserved>_<SubProfessionIndex>
        public static string GetSubProfessionNameFromId(int subProfessionId) => subProfessionId switch
        {
            00_00_00 => AppStrings.GetLocalized("SubProfession_Unknown"),
            01_00_01 => AppStrings.GetLocalized("SubProfession_Iaido"),
            01_00_02 => AppStrings.GetLocalized("SubProfession_Moonstrike"),
            02_00_01 => AppStrings.GetLocalized("SubProfession_Icicle"),
            02_00_02 => AppStrings.GetLocalized("SubProfession_Frostbeam"),
            04_00_01 => AppStrings.GetLocalized("SubProfession_Vanguard"),
            04_00_02 => AppStrings.GetLocalized("SubProfession_Skyward"),
            05_00_01 => AppStrings.GetLocalized("SubProfession_Smite"),
            05_00_02 => AppStrings.GetLocalized("SubProfession_Lifebind"),
            09_00_01 => AppStrings.GetLocalized("SubProfession_Earthfort"),
            09_00_02 => AppStrings.GetLocalized("SubProfession_Block"),
            11_00_01 => AppStrings.GetLocalized("SubProfession_Wildpack"),
            11_00_02 => AppStrings.GetLocalized("SubProfession_Falconry"),
            12_00_01 => AppStrings.GetLocalized("SubProfession_Recovery"),
            12_00_02 => AppStrings.GetLocalized("SubProfession_Shield"),
            13_00_01 => AppStrings.GetLocalized("SubProfession_Dissonance"),
            13_00_02 => AppStrings.GetLocalized("SubProfession_Concerto"),
            _ => ""
        };

        public static int GetBaseProfessionIdBySkillId(long skillId) => skillId switch
        {
            1701 or 1705 or 1713 or 1714 or 1715 or 1716 or 1717 or 1718 or 1719 or 1720 or 1724 or 1728 or 1730 or 1731 => 1, // Stormblade
            1201 or 1210 or 1211 or 1239 or 1240 or 1241 or 1242 or 1243 or 1244 or 1245 or 1246 or 1248 => 2, // FrostMage
            1401 or 1410 or 1418 or 1419 or 1420 or 1421 or 1422 or 1423 or 1424 or 1425 or 1426 or 1430 or 1431 => 4, // WindKnight
            1501 or 1507 or 1509 or 1518 or 1519 or 1520 or 1521 or 1522 or 1523 or 1524 or 1527 or 1528 or 1529 or 1531 => 5, // VerdantOracle
            1901 or 1907 or 1917 or 1922 or 1923 or 1924 or 1925 or 1926 or 1927 or 1930 or 1932 or 1936 or 1937 or 1938 or 1940 => 9, // HeavyGuardian
            2201 or 2209 or 2220 or 2222 or 2230 or 2231 or 2232 or 2233 or 2234 or 2235 or 2237 or 2238 => 11, // Marksman
            2401 or 2405 or 2406 or 2407 or 2408 or 2409 or 2410 or 2412 or 2414 or 2415 or 2419 or 2420 or 2421 => 12, // ShieldKnight
            2301 or 2306 or 2307 or 2308 or 2309 or 2310 or 2311 or 2312 or 2313 or 2314 or 2315 or 2316 or 2321 or 2335 or 2336 => 13, // BeatPerformer
            _ => 0
        };

        public static SubProfessionId GetSubProfessionIdBySkillId(long skillId) => skillId switch
        {
            0 => SubProfessionId.SubProfession_Unknown,
            1714 or 1734 or 1739 or 179908 => SubProfessionId.SubProfession_Iaido, // 1714 = Core Skill: Iaido Slash, 179908 = spec skill?, 1724 = spec skill Thunder Cut?
            44701 or 179906 => SubProfessionId.SubProfession_Moonstrike, // 44701 = Core Skill: Moon Blade
            120901 or 120902 => SubProfessionId.SubProfession_Icicle,
            1241 => SubProfessionId.SubProfession_Frostbeam,
            1405 or 1418 => SubProfessionId.SubProfession_Vanguard,
            1419 => SubProfessionId.SubProfession_Skyward,
            1518 or 1541 or 21402 => SubProfessionId.SubProfession_Smite,
            20301 => SubProfessionId.SubProfession_Lifebind,
            199902 => SubProfessionId.SubProfession_Earthfort,
            1930 or 1931 or 1934 or 1935 => SubProfessionId.SubProfession_Block,
            2292 or 1700820 or 1700825 or 1700827 => SubProfessionId.SubProfession_Wildpack,
            220112 or 2203622 or 220106 => SubProfessionId.SubProfession_Falconry,
            2405 => SubProfessionId.SubProfession_Recovery,
            2406 => SubProfessionId.SubProfession_Shield,
            2306 => SubProfessionId.SubProfession_Dissonance,
            2307 or 2361 or 55302 => SubProfessionId.SubProfession_Concerto,
            _ => SubProfessionId.SubProfession_Unknown
        };

        public static string GetSubProfessionNameBySkillId(long skillId) => skillId switch
        {
            0 => AppStrings.GetLocalized("SubProfession_Unknown"),
            1714 or 1734 => AppStrings.GetLocalized("SubProfession_Iaido"),
            44701 or 179906 => AppStrings.GetLocalized("SubProfession_Moonstrike"),
            120901 or 120902 => AppStrings.GetLocalized("SubProfession_Icicle"),
            1241 => AppStrings.GetLocalized("SubProfession_Frostbeam"),
            1405 or 1418 => AppStrings.GetLocalized("SubProfession_Vanguard"),
            1419 => AppStrings.GetLocalized("SubProfession_Skyward"),
            1518 or 1541 or 21402 => AppStrings.GetLocalized("SubProfession_Smite"),
            20301 => AppStrings.GetLocalized("SubProfession_Lifebind"),
            199902 => AppStrings.GetLocalized("SubProfession_Earthfort"),
            1930 or 1931 or 1934 or 1935 => AppStrings.GetLocalized("SubProfession_Block"),
            2292 or 1700820 or 1700825 or 1700827 => AppStrings.GetLocalized("SubProfession_Wildpack"),
            220112 or 2203622 or 220106 => AppStrings.GetLocalized("SubProfession_Falconry"),
            2405 => AppStrings.GetLocalized("SubProfession_Recovery"),
            2406 => AppStrings.GetLocalized("SubProfession_Shield"),
            2306 => AppStrings.GetLocalized("SubProfession_Dissonance"),
            2307 or 2361 or 55302 => AppStrings.GetLocalized("SubProfession_Concerto"),
            _ => ""
        };

        public static Vector4 ProfessionColors(string professionName)
        {
            if (professionName == AppStrings.GetLocalized("Profession_Unknown"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#67AEF6"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_Stormblade") || professionName == AppStrings.GetLocalized("SubProfession_Iaido") || professionName == AppStrings.GetLocalized("SubProfession_Moonstrike"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#70629c"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_FrostMage") || professionName == AppStrings.GetLocalized("SubProfession_Frostbeam") || professionName == AppStrings.GetLocalized("SubProfession_Icicle"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#79779c"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_WindKnight") || professionName == AppStrings.GetLocalized("SubProfession_Skyward") || professionName == AppStrings.GetLocalized("SubProfession_Vanguard"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#799a9c"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_VerdantOracle") || professionName == AppStrings.GetLocalized("SubProfession_Lifebind") || professionName == AppStrings.GetLocalized("SubProfession_Smite"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#639c70"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_HeavyGuardian") || professionName == AppStrings.GetLocalized("SubProfession_Earthfort") || professionName == AppStrings.GetLocalized("SubProfession_Block"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#537758"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_Marksman") || professionName == AppStrings.GetLocalized("SubProfession_Falconry") || professionName == AppStrings.GetLocalized("SubProfession_Wildpack"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#8e8b47"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_ShieldKnight") || professionName == AppStrings.GetLocalized("SubProfession_Recovery") || professionName == AppStrings.GetLocalized("SubProfession_Shield"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#9c9b75"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_BeatPerformer") || professionName == AppStrings.GetLocalized("SubProfession_Concerto") || professionName == AppStrings.GetLocalized("SubProfession_Dissonance"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#9c5353"));
            }

            // TODO: Add SubProfessions as their own entries to allow further coloring

            return new Vector4();
        }

        public static string GetBaseProfessionMainStatName(int professionId)
        {
            switch (professionId)
            {
                case (int)ProfessionId.Profession_Stormblade:
                case (int)ProfessionId.Profession_Marksman:
                    {
                        return "Agility";
                    }
                case (int)ProfessionId.Profession_FrostMage:
                case (int)ProfessionId.Profession_VerdantOracle:
                case (int)ProfessionId.Profession_BeatPerformer:
                    {
                        return "Intellect";
                    }
                case (int)ProfessionId.Profession_WindKnight:
                case (int)ProfessionId.Profession_ShieldKnight:
                case (int)ProfessionId.Profession_HeavyGuardian:
                    {
                        return "Strength";
                    }
                default:
                    return "";
            }
        }

        public static EEntityType RawUuidToEntityType(ulong uuid) => (uuid & 0xFFFFUL) switch
        {
            64 => EEntityType.EntMonster,
            128 => EEntityType.EntNpc,
            192 => EEntityType.EntSceneObject,
            320 => EEntityType.EntZone,
            640 => EEntityType.EntChar,
            1024 => EEntityType.EntCollection, // Another one?
            33152 => EEntityType.EntBullet,
            33280 => EEntityType.EntNpc, // Another one?
            33472 => EEntityType.EntDummy,
            33664 => EEntityType.EntField,
            33792 => EEntityType.EntCollection,
            33984 => EEntityType.EntVehicle,
            34048 => EEntityType.EntToy,
            32832 => EEntityType.EntMonster, // Another one?
            _ => EEntityType.EntErrType,
        };

        public static string NumberToShorthand<T>(T number)
        {
            string[] suf = { "", "K", "M", "B", "t", "q", "Q", "s", "S", "o", "n", "d", "U", "D", "T" };
            double value = Convert.ToDouble(number);
            if (value == 0)
            {
                return "0" + suf[0];
            }

            double absoluteValue = Math.Abs(value);
            int place = Convert.ToInt32(Math.Floor(Math.Log(absoluteValue, 1000)));
            double shortNumber = Math.Round(absoluteValue / Math.Pow(1000, place), 2);

            if (AppState.UseShortWidthNumberFormatting)
            {
                return place == 0 ? ((long)value).ToString() : shortNumber.ToString($"N2") + suf[place];
            }

            string fmt = "";
            if (place > 0)
            {
                fmt = "N2";
            }
            return $"{(Math.Sign(value) * shortNumber).ToString(fmt)}{suf[place]}";
        }
    }
}
