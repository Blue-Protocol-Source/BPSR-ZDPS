using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public static class AppState
    {
        public static long PlayerUUID { get; set; } // Original raw UUID
        public static long PlayerUID { get; set; }  // Resolved UUID into UID
        public static string PlayerName { get; set; }
        public static int ProfessionId { get; set; }
        public static string ProfessionName { get; set; }
        public static string SubProfessionName { get; set; }

        public static int PlayerMeterPlacement { get; set; } // Current position on the active meter, 0 means not on it

        public static long PlayerTotalMeterValue { get; set; }
        public static bool NormalizeMeterContributions { get; set; }
        public static bool UseShortWidthNumberFormatting { get; set; }
    }
}
