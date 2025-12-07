using BPSR_ZDPS.DataTypes;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public static class ImGuiEx
    {
        public static void TextAlignedProgressBar(float percent, string text, float alignment = 0.5f, float width = -1.0f, float height = 18)
        {
            var cursorPos = ImGui.GetCursorPos();

            ImGui.BeginGroup();
            var textSize = ImGui.CalcTextSize(text);
            float labelX = cursorPos.X + (width - textSize.X) * alignment;
            ImGui.ProgressBar(percent, new Vector2(width, height), "");
            var progSize = ImGui.GetItemRectSize();
            ImGui.SetCursorPos(new Vector2(labelX, cursorPos.Y + (ImGui.GetItemRectSize().Y - textSize.Y) * alignment));
            ImGui.Text(text);

            ImGui.EndGroup();
        }
    }
}
