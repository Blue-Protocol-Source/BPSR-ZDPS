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
    }
}
