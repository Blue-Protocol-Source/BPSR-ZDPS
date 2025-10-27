using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.DataTypes
{
    public class SkillTable
    {
        public Dictionary<string, Skill> Data = new();
    }

    public class Skill
    {
        public int Id { get; set; }
        public string NameDesign { get; set; }
        public string Desc { get; set; }
        public string Name { get; set; }
        public int SkillLevelGroup { get; set; }
        public object SkillPreloadGroup { get; set; }
        public List<int> EffectIDs { get; set; }
        public int SkillType { get; set; }
        public int SlotPassiveType { get; set; }
        public bool FaceTarget { get; set; }
        public bool IsPreview { get; set; }
        public int TargetType { get; set; }
        public int SkillTargetRangeType { get; set; }
        public int SkillRangeType { get; set; }
        public int SkillSelectPointType { get; set; }
        public int SkillHatedType { get; set; }
        public int SkillDamType { get; set; }
        public int SwitchSkillId { get; set; }
        public bool IsAoe { get; set; }
        public string Icon { get; set; }
        public int NextSkillId { get; set; }
        public int SlotType { get; set; }
        public bool LongPressOpen { get; set; }
        public float LongPressTime { get; set; }
        public float ComboTakeEffectTime { get; set; }
        public bool CanPlayInSky { get; set; }
        public int SkySkillId { get; set; }
        public float PlayInSkyHeight { get; set; }
        public bool IsArmor { get; set; }
        public bool CanBeSilence { get; set; }
        public bool CantStiff { get; set; }
        public bool CantStiffBack { get; set; }
        public bool CantStiffDown { get; set; }
        public bool CantStiffAir { get; set; }
        public int UnbreakSkillPriority { get; set; }
        public int BreakSkillPriority { get; set; }
        public bool UseTotalDamageHud { get; set; }
        public bool WeaponReturn { get; set; }
        public float SkillRootShift { get; set; }
        public int CoolTimeType { get; set; }
        // TODO: Finish type

        public string GetIconName()
        {
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
