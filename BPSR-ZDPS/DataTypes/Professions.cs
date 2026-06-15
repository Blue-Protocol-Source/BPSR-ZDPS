using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static BPSR_ZDPS.DataTypes.Enums.Professions;

namespace BPSR_ZDPS.DataTypes
{
    public static class Professions
    {
        public static string GetProfessionIconPathFromId(int professionId)
        {
            switch ((EProfessionId)professionId)
            { 
                case EProfessionId.Profession_Stormblade:
                    return Path.Combine("Professions", "Profession_1");
                case EProfessionId.Profession_FrostMage:
                    return Path.Combine("Professions", "Profession_2");
                case EProfessionId.Profession_TwinStriker:
                    return Path.Combine("Professions", "Profession_3");
                case EProfessionId.Profession_WindKnight:
                    return Path.Combine("Professions", "Profession_4");
                case EProfessionId.Profession_VerdantOracle:
                    return Path.Combine("Professions", "Profession_5");
                case EProfessionId.Profession_HeavyGuardian:
                    return Path.Combine("Professions", "Profession_9");
                case EProfessionId.Profession_Marksman:
                    return Path.Combine("Professions", "Profession_11");
                case EProfessionId.Profession_ShieldKnight:
                    return Path.Combine("Professions", "Profession_12");
                case EProfessionId.Profession_BeatPerformer:
                    return Path.Combine("Professions", "Profession_13");
                default:
                    return "";
            }
        }

        public static string GetProfessionNameFromId(int professionId) => professionId switch
        {
            0 => AppStrings.GetLocalized("Profession_Unknown"),
            1 => AppStrings.GetLocalized("Profession_Stormblade"),
            2 => AppStrings.GetLocalized("Profession_FrostMage"),
            3 => AppStrings.GetLocalized("Profession_TwinStriker"),
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
            03_00_01 => AppStrings.GetLocalized("SubProfession_FormlessExpertise"),
            03_00_02 => AppStrings.GetLocalized("SubProfession_CrimsonExpertise"),
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

        public static int GetProfessionIdFromSubProfessionId(int subProfessionId) => subProfessionId switch
        {
            (int)SubProfessionId.SubProfession_Unknown => (int)EProfessionId.Profession_Unknown,
            (int)SubProfessionId.SubProfession_Iaido => (int)EProfessionId.Profession_Stormblade,
            (int)SubProfessionId.SubProfession_Moonstrike => (int)EProfessionId.Profession_Stormblade,
            (int)SubProfessionId.SubProfession_Icicle => (int)EProfessionId.Profession_FrostMage,
            (int)SubProfessionId.SubProfession_Frostbeam => (int)EProfessionId.Profession_FrostMage,
            (int)SubProfessionId.SubProfession_FormlessExpertise => (int)EProfessionId.Profession_TwinStriker,
            (int)SubProfessionId.SubProfession_CrimsonExpertise => (int)EProfessionId.Profession_TwinStriker,
            (int)SubProfessionId.SubProfession_Vanguard => (int)EProfessionId.Profession_WindKnight,
            (int)SubProfessionId.SubProfession_Skyward => (int)EProfessionId.Profession_WindKnight,
            (int)SubProfessionId.SubProfession_Smite => (int)EProfessionId.Profession_VerdantOracle,
            (int)SubProfessionId.SubProfession_Lifebind => (int)EProfessionId.Profession_VerdantOracle,
            (int)SubProfessionId.SubProfession_Earthfort => (int)EProfessionId.Profession_HeavyGuardian,
            (int)SubProfessionId.SubProfession_Block => (int)EProfessionId.Profession_HeavyGuardian,
            (int)SubProfessionId.SubProfession_Wildpack => (int)EProfessionId.Profession_Marksman,
            (int)SubProfessionId.SubProfession_Falconry => (int)EProfessionId.Profession_Marksman,
            (int)SubProfessionId.SubProfession_Recovery => (int)EProfessionId.Profession_ShieldKnight,
            (int)SubProfessionId.SubProfession_Shield => (int)EProfessionId.Profession_ShieldKnight,
            (int)SubProfessionId.SubProfession_Dissonance => (int)EProfessionId.Profession_BeatPerformer,
            (int)SubProfessionId.SubProfession_Concerto => (int)EProfessionId.Profession_BeatPerformer,
            _ => (int)EProfessionId.Profession_Unknown
        };

        public static int GetTalentIdFromSubProfessionId(int subProfessionId) => subProfessionId switch
        {
            (int)SubProfessionId.SubProfession_Unknown => (int)ETalentId.Unknown,
            (int)EProfessionId.Profession_Stormblade => (int)ETalentId.Profession_Stormblade,
            (int)SubProfessionId.SubProfession_Iaido => (int)ETalentId.SubProfession_Iaido,
            (int)SubProfessionId.SubProfession_Moonstrike => (int)ETalentId.SubProfession_Moonstrike,
            (int)EProfessionId.Profession_FrostMage => (int)ETalentId.Profession_FrostMage,
            (int)SubProfessionId.SubProfession_Icicle => (int)ETalentId.SubProfession_Icicle,
            (int)SubProfessionId.SubProfession_Frostbeam => (int)ETalentId.SubProfession_Frostbeam,
            (int)SubProfessionId.SubProfession_FormlessExpertise => (int)ETalentId.SubProfession_FormlessExpertise,
            (int)SubProfessionId.SubProfession_CrimsonExpertise => (int)ETalentId.SubProfession_CrimsonExpertise,
            (int)EProfessionId.Profession_WindKnight => (int)ETalentId.Profession_WindKnight,
            (int)SubProfessionId.SubProfession_Vanguard => (int)ETalentId.SubProfession_Vanguard,
            (int)SubProfessionId.SubProfession_Skyward => (int)ETalentId.SubProfession_Skyward,
            (int)EProfessionId.Profession_VerdantOracle => (int)ETalentId.Profession_VerdantOracle,
            (int)SubProfessionId.SubProfession_Smite => (int)ETalentId.SubProfession_Smite,
            (int)SubProfessionId.SubProfession_Lifebind => (int)ETalentId.SubProfession_Lifebind,
            (int)EProfessionId.Profession_HeavyGuardian => (int)ETalentId.Profession_HeavyGuardian,
            (int)SubProfessionId.SubProfession_Earthfort => (int)ETalentId.SubProfession_Earthfort,
            (int)SubProfessionId.SubProfession_Block => (int)ETalentId.SubProfession_Block,
            (int)EProfessionId.Profession_Marksman => (int)ETalentId.Profession_Marksman,
            (int)SubProfessionId.SubProfession_Wildpack => (int)ETalentId.SubProfession_Wildpack,
            (int)SubProfessionId.SubProfession_Falconry => (int)ETalentId.SubProfession_Falconry,
            (int)EProfessionId.Profession_ShieldKnight => (int)ETalentId.Profession_ShieldKnight,
            (int)SubProfessionId.SubProfession_Recovery => (int)ETalentId.SubProfession_Recovery,
            (int)SubProfessionId.SubProfession_Shield => (int)ETalentId.SubProfession_Shield,
            (int)EProfessionId.Profession_BeatPerformer => (int)ETalentId.Profession_BeatPerformer,
            (int)SubProfessionId.SubProfession_Dissonance => (int)ETalentId.SubProfession_Dissonance,
            (int)SubProfessionId.SubProfession_Concerto => (int)ETalentId.SubProfession_Concerto,
            _ => (int)ETalentId.Unknown
        };

        public static int GetBaseProfessionIdBySkillId(int skillId) => skillId switch
        {
            1701 or 1705 or 1713 or 1714 or 1715 or 1716 or 1717 or 1718 or 1719 or 1720 or 1724 or 1728 or 1730 or 1731 => 1, // Stormblade
            1201 or 1210 or 1211 or 1239 or 1240 or 1241 or 1242 or 1243 or 1244 or 1245 or 1246 or 1248 => 2, // FrostMage
            1601 or 1605 or 1606 or 1607 or 1608 or 1609 or 1610 or 1611 or 1612 or 1613 or 1614 or 1615 or 1616 or 1617 or 1618 or 1621 => 3, // Twin Striker
            1401 or 1410 or 1418 or 1419 or 1420 or 1421 or 1422 or 1423 or 1424 or 1425 or 1426 or 1430 or 1431 => 4, // WindKnight
            1501 or 1507 or 1509 or 1518 or 1519 or 1520 or 1521 or 1522 or 1523 or 1524 or 1527 or 1528 or 1529 or 1531 => 5, // VerdantOracle
            1901 or 1907 or 1917 or 1922 or 1923 or 1924 or 1925 or 1926 or 1927 or 1930 or 1932 or 1936 or 1937 or 1938 or 1940 => 9, // HeavyGuardian
            2201 or 2209 or 2220 or 2222 or 2230 or 2231 or 2232 or 2233 or 2234 or 2235 or 2237 or 2238 => 11, // Marksman
            2401 or 2405 or 2406 or 2407 or 2408 or 2409 or 2410 or 2412 or 2414 or 2415 or 2419 or 2420 or 2421 => 12, // ShieldKnight
            2301 or 2306 or 2307 or 2308 or 2309 or 2310 or 2311 or 2312 or 2313 or 2314 or 2315 or 2316 or 2321 or 2335 or 2336 => 13, // BeatPerformer
            _ => 0
        };

        public static SubProfessionId GetSubProfessionIdBySkillId(int skillId) => skillId switch
        {
            0 => SubProfessionId.SubProfession_Unknown,
            1714 or 1734 => SubProfessionId.SubProfession_Iaido, // 1714 = Core Skill: Iaido Slash, 179908 = spec skill?, 1724 = spec skill Thunder Cut?
            1715 or 1740 or 1741 or 179906 => SubProfessionId.SubProfession_Moonstrike, // 44701 = Core Skill: Moon Blade
            120901 or 120902 => SubProfessionId.SubProfession_Icicle,
            1241 => SubProfessionId.SubProfession_Frostbeam,
            35107 or 35108 or 35109 or 160102 => SubProfessionId.SubProfession_FormlessExpertise,
            1606 or 1621 or 1622 or 1623 => SubProfessionId.SubProfession_CrimsonExpertise,
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
            2321 or 2335 => SubProfessionId.SubProfession_Dissonance, // 2306 = Core Skill: Amplified Beat, 2362 maybe works?
            2301 or 2336 or 2361 or 55302 => SubProfessionId.SubProfession_Concerto, // 2307 = Core Skill: Healing Beat
            _ => SubProfessionId.SubProfession_Unknown
        };

        public static string GetSubProfessionNameBySkillId(int skillId) => skillId switch
        {
            0 => AppStrings.GetLocalized("SubProfession_Unknown"),
            1714 or 1734 or 1739 or 179908 => AppStrings.GetLocalized("SubProfession_Iaido"),
            1715 or 1740 or 1741 or 179906 => AppStrings.GetLocalized("SubProfession_Moonstrike"),
            120901 or 120902 => AppStrings.GetLocalized("SubProfession_Icicle"),
            1241 => AppStrings.GetLocalized("SubProfession_Frostbeam"),
            35107 or 35108 or 35109 or 160102 => AppStrings.GetLocalized("SubProfession_FormlessExpertise"),
            1606 or 1621 or 1622 or 1623 => AppStrings.GetLocalized("SubProfession_CrimsonExpertise"),
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

        // NEW COLORS
        public static Vector4 ProfessionColors(string professionName)
        {
            if (professionName == AppStrings.GetLocalized("Profession_Unknown"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#6c757d")); // Profession_Unknown
            }
            else if (professionName == AppStrings.GetLocalized("Profession_Stormblade"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#805AA3")); // Profession_Stormblade
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Iaido"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#6A4A87")); // SubProfession_Iaido
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Moonstrike"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#523A69")); // SubProfession_Moonstrike
            }
            else if (professionName == AppStrings.GetLocalized("Profession_FrostMage"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#3D56C2")); // Profession_FrostMage
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Frostbeam"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#32469F")); // SubProfession_Frostbeam
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Icicle"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#27377C")); // SubProfession_Icicle
            }
            else if (professionName == AppStrings.GetLocalized("Profession_TwinStriker"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#DC9617")); // Profession_TwinStriker
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_FormlessExpertise"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#C48618")); // SubProfession_FormlessExpertise
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_CrimsonExpertise"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#AC7619")); // SubProfession_CrimsonExpertise
            }
            else if (professionName == AppStrings.GetLocalized("Profession_WindKnight"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#6D8B8D")); // Profession_WindKnight
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Skyward"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#627C7E")); // SubProfession_Skyward
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Vanguard"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#576E6F")); // SubProfession_Vanguard
            }
            else if (professionName == AppStrings.GetLocalized("Profession_VerdantOracle"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#5A8D65")); // Profession_VerdantOracle
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Lifebind"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#517E5B")); // SubProfession_Lifebind
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Smite"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#486F51")); // SubProfession_Smite
            }
            else if (professionName == AppStrings.GetLocalized("Profession_HeavyGuardian"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#BE9B1C")); // Profession_HeavyGuardian
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Earthfort"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#AB8C1C")); // SubProfession_Earthfort
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Block"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#997D1C")); // SubProfession_Block
            }
            else if (professionName == AppStrings.GetLocalized("Profession_Marksman"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#D1218C")); // Profession_Marksman
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Falconry"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#BA227D")); // SubProfession_Falconry
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Wildpack"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#A4226E")); // SubProfession_Wildpack
            }
            else if (professionName == AppStrings.GetLocalized("Profession_ShieldKnight"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#CFD212")); // Profession_ShieldKnight
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Recovery"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#BABD17")); // SubProfession_Recovery
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Shield"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#A6A819")); // SubProfession_Shield
            }
            else if (professionName == AppStrings.GetLocalized("Profession_BeatPerformer"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#9C5353")); // Profession_BeatPerformer
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Concerto"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#894848")); // SubProfession_Concerto
            }
            else if (professionName == AppStrings.GetLocalized("SubProfession_Dissonance"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#6B3838")); // SubProfession_Dissonance
            }

            return new Vector4();
        }
/*
        public static Vector4 ProfessionColors(string professionName)
        {
            if (professionName == AppStrings.GetLocalized("Profession_Unknown"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#67AEF6"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_Stormblade") || professionName == AppStrings.GetLocalized("SubProfession_Iaido") || professionName == AppStrings.GetLocalized("SubProfession_Moonstrike"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#805AA3"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_FrostMage") || professionName == AppStrings.GetLocalized("SubProfession_Frostbeam") || professionName == AppStrings.GetLocalized("SubProfession_Icicle"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#7788D4"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_TwinStriker") || professionName == AppStrings.GetLocalized("SubProfession_FormlessExpertise") || professionName == AppStrings.GetLocalized("SubProfession_CrimsonExpertise"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#F5A614"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_WindKnight") || professionName == AppStrings.GetLocalized("SubProfession_Skyward") || professionName == AppStrings.GetLocalized("SubProfession_Vanguard"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#799A9C"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_VerdantOracle") || professionName == AppStrings.GetLocalized("SubProfession_Lifebind") || professionName == AppStrings.GetLocalized("SubProfession_Smite"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#639C70"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_HeavyGuardian") || professionName == AppStrings.GetLocalized("SubProfession_Earthfort") || professionName == AppStrings.GetLocalized("SubProfession_Block"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#537758"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_Marksman") || professionName == AppStrings.GetLocalized("SubProfession_Falconry") || professionName == AppStrings.GetLocalized("SubProfession_Wildpack"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#8E8b47"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_ShieldKnight") || professionName == AppStrings.GetLocalized("SubProfession_Recovery") || professionName == AppStrings.GetLocalized("SubProfession_Shield"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#9C9b75"));
            }
            else if (professionName == AppStrings.GetLocalized("Profession_BeatPerformer") || professionName == AppStrings.GetLocalized("SubProfession_Concerto") || professionName == AppStrings.GetLocalized("SubProfession_Dissonance"))
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#9C5353"));
            }

            // TODO: Add SubProfessions as their own entries to allow further coloring: DONE

            return new Vector4();
        }
*/
        public static Vector4 ProfessionColors(int professionId)
        {
            if (professionId == (int)EProfessionId.Profession_Unknown)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#6c757d")); // Profession_Unknown
            }
            else if (professionId == (int)EProfessionId.Profession_Stormblade)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#805AA3")); // Profession_Stormblade
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Iaido)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#6A4A87")); // SubProfession_Iaido
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Moonstrike)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#523A69")); // SubProfession_Moonstrike
            }
            else if (professionId == (int)EProfessionId.Profession_FrostMage)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#3D56C2")); // Profession_FrostMage
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Frostbeam)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#32469F")); // SubProfession_Frostbeam
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Icicle)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#27377C")); // SubProfession_Icicle
            }
            else if (professionId == (int)EProfessionId.Profession_TwinStriker)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#DC9617")); // Profession_TwinStriker
            }
            else if (professionId == (int)SubProfessionId.SubProfession_FormlessExpertise)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#C48618")); // SubProfession_FormlessExpertise
            }
            else if (professionId == (int)SubProfessionId.SubProfession_CrimsonExpertise)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#AC7619")); // SubProfession_CrimsonExpertise
            }
            else if (professionId == (int)EProfessionId.Profession_WindKnight)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#6D8B8D")); // Profession_WindKnight
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Skyward)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#627C7E")); // SubProfession_Skyward
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Vanguard)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#576E6F")); // SubProfession_Vanguard
            }
            else if (professionId == (int)EProfessionId.Profession_VerdantOracle)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#5A8D65")); // Profession_VerdantOracle
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Lifebind)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#517E5B")); // SubProfession_Lifebind
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Smite)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#486F51")); // SubProfession_Smite
            }
            else if (professionId == (int)EProfessionId.Profession_HeavyGuardian)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#BE9B1C")); // Profession_HeavyGuardian
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Earthfort)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#AB8C1C")); // SubProfession_Earthfort
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Block)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#997D1C")); // SubProfession_Block
            }
            else if (professionId == (int)EProfessionId.Profession_Marksman)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#D1218C")); // Profession_Marksman
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Falconry)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#BA227D")); // SubProfession_Falconry
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Wildpack)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#A4226E")); // SubProfession_Wildpack
            }
            else if (professionId == (int)EProfessionId.Profession_ShieldKnight)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#CFD212")); // Profession_ShieldKnight
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Recovery)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#BABD17")); // SubProfession_Recovery
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Shield)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#A6A819")); // SubProfession_Shield
            }
            else if (professionId == (int)EProfessionId.Profession_BeatPerformer)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#9C5353")); // Profession_BeatPerformer
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Concerto)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#894848")); // SubProfession_Concerto
            }
            else if (professionId == (int)SubProfessionId.SubProfession_Dissonance)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#6B3838")); // SubProfession_Dissonance
            }

            return new Vector4();
        }
/*
        public static Vector4 ProfessionColors(int professionId)
        {
            if (professionId == (int)EProfessionId.Profession_Unknown)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#67AEF6"));
            }
            else if (professionId == (int)EProfessionId.Profession_Stormblade || professionId == (int)SubProfessionId.SubProfession_Iaido || professionId == (int)SubProfessionId.SubProfession_Moonstrike)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#805AA3"));
            }
            else if (professionId == (int)EProfessionId.Profession_FrostMage || professionId == (int)SubProfessionId.SubProfession_Frostbeam || professionId == (int)SubProfessionId.SubProfession_Icicle)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#7788D4"));
            }
            else if (professionId == (int)EProfessionId.Profession_TwinStriker || professionId == (int)SubProfessionId.SubProfession_FormlessExpertise || professionId == (int)SubProfessionId.SubProfession_CrimsonExpertise)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#F5A614"));
            }
            else if (professionId == (int)EProfessionId.Profession_WindKnight || professionId == (int)SubProfessionId.SubProfession_Skyward || professionId == (int)SubProfessionId.SubProfession_Vanguard)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#799A9C"));
            }
            else if (professionId == (int)EProfessionId.Profession_VerdantOracle || professionId == (int)SubProfessionId.SubProfession_Lifebind || professionId == (int)SubProfessionId.SubProfession_Smite)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#639C70"));
            }
            else if (professionId == (int)EProfessionId.Profession_HeavyGuardian || professionId == (int)SubProfessionId.SubProfession_Earthfort || professionId == (int)SubProfessionId.SubProfession_Block)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#537758"));
            }
            else if (professionId == (int)EProfessionId.Profession_Marksman || professionId == (int)SubProfessionId.SubProfession_Falconry || professionId == (int)SubProfessionId.SubProfession_Wildpack)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#8E8b47"));
            }
            else if (professionId == (int)EProfessionId.Profession_ShieldKnight || professionId == (int)SubProfessionId.SubProfession_Recovery || professionId == (int)SubProfessionId.SubProfession_Shield)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#9C9b75"));
            }
            else if (professionId == (int)EProfessionId.Profession_BeatPerformer || professionId == (int)SubProfessionId.SubProfession_Concerto || professionId == (int)SubProfessionId.SubProfession_Dissonance)
            {
                return Colors.FromColor(ColorTranslator.FromHtml("#9C5353"));
            }

            // TODO: Add SubProfessions as their own entries to allow further coloring: DONE

            return new Vector4();
        }
*/
        public static string GetBaseProfessionMainStatName(int professionId)
        {
            switch (professionId)
            {
                case (int)EProfessionId.Profession_Stormblade:
                case (int)EProfessionId.Profession_Marksman:
                    {
                        return "Agility";
                    }
                case (int)EProfessionId.Profession_FrostMage:
                case (int)EProfessionId.Profession_VerdantOracle:
                case (int)EProfessionId.Profession_BeatPerformer:
                    {
                        return "Intellect";
                    }
                case (int)EProfessionId.Profession_TwinStriker:
                case (int)EProfessionId.Profession_WindKnight:
                case (int)EProfessionId.Profession_HeavyGuardian:
                case (int)EProfessionId.Profession_ShieldKnight:
                    {
                        return "Strength";
                    }
                default:
                    return "";
            }
        }



        public static ERoleType GetRoleFromBaseProfessionId(int professionId)
        {
            switch (professionId)
            {
                case (int)EProfessionId.Profession_Stormblade:
                case (int)EProfessionId.Profession_FrostMage:
                case (int)EProfessionId.Profession_TwinStriker:
                case (int)EProfessionId.Profession_WindKnight:
                case (int)EProfessionId.Profession_Marksman:
                    return ERoleType.DPS;
                case (int)EProfessionId.Profession_HeavyGuardian:
                case (int)EProfessionId.Profession_ShieldKnight:
                    return ERoleType.Tank;
                case (int)EProfessionId.Profession_VerdantOracle:
                case (int)EProfessionId.Profession_BeatPerformer:
                    return ERoleType.Healer;
                default:
                    return ERoleType.None;
            }
        }

        public static Vector4 RoleTypeColors(ERoleType roleType)
        {
            switch (roleType)
            {
                case ERoleType.DPS:
                    //return new Vector4(230 / 255f, 0, 0, 0.50f);
                    return new Vector4(227 / 255f, 36 / 255f, 36 / 255f, 0.50f);
                case ERoleType.Tank:
                    //return new Vector4(51 / 255f, 102 / 255f, 255 / 255f, 0.50f);
                    //return new Vector4(69 / 255f, 174 / 255f, 240 / 255f, 0.50f);
                    return new Vector4(17 / 255f, 136 / 255f, 212 / 255f, 0.50f);
                case ERoleType.Healer:
                    return new Vector4(0, 204 / 255f, 0, 0.50f);
                default:
                    return new Vector4(1, 1, 1, 1);
            }
        }
    }
}
