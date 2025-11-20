using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes.Enums
{
    public static class Professions
    {
        public enum ERoleType
        {
            None = 0,
            Tank = 1,
            Healer = 2,
            DPS = 3
        }

        public enum EProfessionId : int
        {
            Profession_Unknown = 0,
            Profession_Stormblade = 1,
            Profession_FrostMage = 2,
            Profession_TwinStriker = 3,
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
    }
}
