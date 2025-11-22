using BPSR_ZDPS.DataTypes.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class BuffTable
    {
        public Dictionary<string, Buff> Data = new();
    }

    public class Buff
    {
        public string Id { get; set; }
        public int Level { get; set; }
        public string NameDesign { get; set; }
        public string Note { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Desc { get; set; }
        public Enum.EBuffType? BuffType { get; set; }
        public Enum.EBuffPriority? BuffPriority { get; set; }
        public int TipsDescription { get; set; }
        public int Visible { get; set; }
        public List<int> RepeatAddRule { get; set; }
        public List<List<float>> DestroyParam { get; set; }
        public bool DeleteDead { get; set; }
        public bool DeleteOffline { get; set; }
        public bool DeleteChangeScene { get; set; }
        public bool DeleteChangeVisualLayer { get; set; }
        public bool DeleteWeaponChange { get; set; }
        public bool DeleteSourceDead { get; set; }
        public List<int> Tags { get; set; }
        public List<int> SpecialAttr { get; set; }
        public int BuffAbilityType { get; set; }
        public int BuffAbilitySubType { get; set; }
        public bool IsClientBuff { get; set; }
        public string ShowHUDIcon { get; set; }
        public int HudSwitch { get; set; }
        public int TimeRefreshType { get; set; }
        public int PlayType { get; set; }

        public string GetIconName()
        {
            // Prioritize the HUD Icon that users would normally see
            if (ShowHUDIcon != null && ShowHUDIcon.Length > 0)
            {
                int lastSeparator = ShowHUDIcon.LastIndexOf('/');
                if (lastSeparator != -1)
                {
                    return ShowHUDIcon.Substring(lastSeparator + 1);
                }
            }

            if (Icon != null && Icon.Length > 0)
            {
                int lastSeparator = Icon.LastIndexOf('/');
                if (lastSeparator != -1)
                {
                    return Icon.Substring(lastSeparator + 1);
                }
            }

            return Icon;
        }
    }
}
