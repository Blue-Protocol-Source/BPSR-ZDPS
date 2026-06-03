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
        public static IntPtr MainWindowPlatformHandleRaw;
        public static Dictionary<string, ImFontPtr> Fonts = new();
        public static Action? DeferredImGuiRenderAction = null;

        public static class DataTables
        {
            public static MonsterTable Monsters = new MonsterTable();
            public static SkillTable Skills = new SkillTable();
            public static TargetTable Targets = new TargetTable();
            public static SceneTable Scenes = new SceneTable();
            public static DungeonsTable Dungeons = new DungeonsTable();
            public static BuffTable Buffs = new BuffTable();
            public static ModTable Modules = new ModTable();
            public static ModEffectTable ModEffects = new ModEffectTable();
            public static ModLinkEffectTable ModLinkEffects = new ModLinkEffectTable();
            public static SkillFightLevelTable SkillFightLevels = new SkillFightLevelTable();
            public static SceneEventDungeonConfigTable SceneEventDungeonConfigs = new SceneEventDungeonConfigTable();
            public static FightAttrTable FightAttrs = new FightAttrTable();
            public static ItemTable Items = new ItemTable();
            public static EquipTable Equips = new EquipTable();
            public static EquipAttrLibTable EquipAttrLibs = new EquipAttrLibTable();
            public static EquipAttrSchoolLibTable EquipAttrSchoolLibs = new EquipAttrSchoolLibTable();
            public static EquipEnchantTable EquipEnchants = new EquipEnchantTable();
            public static EquipPerfectLibTable EquipPerfectLibs = new EquipPerfectLibTable();
            public static EquipBreakThroughTable EquipBreakThroughs = new EquipBreakThroughTable();
            public static DbmTable Dbms = new DbmTable();
            public static TempAttrTable TempAttrs = new TempAttrTable();
            public static AttrDescriptionTable AttrDescriptions = new AttrDescriptionTable();
            public static DummyTable Dummys = new DummyTable();
        }
    }
}
