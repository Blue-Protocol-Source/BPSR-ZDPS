using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes.External
{
    public class BPTimerSettings
    {
        public bool ExternalBPTimerEnabled { get; set; } = false;
        public bool ExternalBPTimerIncludeCharacterId { get; set; } = false;
        public bool ExternalBPTimerFieldBossHpReportsEnabled { get; set; } = false;
    }
}
