using BPSR_ZDPS.Windows;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS.Meters
{
    public class MeterBase
    {
        public string Name = "";

        public virtual void Draw(MainWindow mainWindow) { }

        public static bool SelectableWithHintImage(string number, string name, string value, int profession)
        {
            var startPoint = ImGui.GetCursorPos();

            ImGui.AlignTextToFramePadding();

            float texSize = ImGui.GetItemRectSize().Y; // Most likely is 22
            float offset = ImGui.CalcTextSize(number).X + (ImGui.GetStyle().ItemSpacing.X * 2) + (texSize + 2);

            ImGui.SetCursorPosX(offset);
            bool ret = ImGui.Selectable(name, false, ImGuiSelectableFlags.SpanAllColumns);
            ImGui.SameLine();

            ImGui.SetCursorPos(startPoint);

            ImGui.TextUnformatted(number);
            ImGui.SameLine();

            var tex = ImageHelper.GetTextureByKey($"Profession_{profession}_Slim");

            if (tex == null)
            {
                ImGui.Dummy(new Vector2(texSize, texSize));
            }
            else
            {
                var roleColor = DataTypes.Professions.RoleTypeColors(DataTypes.Professions.GetRoleFromBaseProfessionId(profession));

                if (DataTypes.Settings.Instance.ColorClassIconsByRole)
                {
                    ImGui.ImageWithBg((ImTextureRef)tex, new Vector2(texSize, texSize), new Vector2(0, 0), new Vector2(1, 1), new Vector4(0, 0, 0, 0), roleColor);
                }
                else
                {
                    ImGui.Image((ImTextureRef)tex, new Vector2(texSize, texSize));
                }
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0.0f, ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(value).X));
            ImGui.TextUnformatted(value);

            return ret;
        }
    }
}
