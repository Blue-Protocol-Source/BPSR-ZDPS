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
            ImGui.TextUnformatted(text);

            ImGui.EndGroup();
        }

        private static void DrawArc(float size, float max_angle_factor, float percentage, float thickness, Vector2 pos, ImTextureRef? imTextureRef = null, int texStretchLeft = 0, int texStretchRight = 0, bool isFilled = false)
        {
            var draw_list = ImGui.GetWindowDrawList();
            
            float x = pos.X;
            float y = pos.Y;

            const float ONE_DIV_360f = 1.0f / 360.0f;

            float a_min_factor;
            float a_max_factor;
            float a_max_factor_100percentage;
            a_min_factor = -1.5f + ((360 - max_angle_factor) * ONE_DIV_360f);
            a_max_factor_100percentage = (a_min_factor + 1.0f) * -1.0f;

            float a_factor_delta = (a_max_factor_100percentage - a_min_factor) * (percentage * 0.01f);
            a_max_factor = a_min_factor + a_factor_delta;

            if (imTextureRef != null)
            {
                //draw_list.AddImageRounded(imTextureRef.Value, pos + new Vector2(8, 0), pos + new Vector2(size) + new Vector2(-6, 0), new Vector2(0, 0), new Vector2(1, 1), ImGui.GetColorU32(Colors.White), size * 0.5f, ImDrawFlags.RoundCornersAll);
                draw_list.AddImageRounded(imTextureRef.Value, pos + new Vector2(texStretchLeft, 0), pos + new Vector2(size) + new Vector2(texStretchRight, 0), new Vector2(0, 0), new Vector2(1, 1), ImGui.GetColorU32(Colors.White), size * 0.5f, ImDrawFlags.RoundCornersAll);
            }

            if (isFilled)
            {
                draw_list.AddCircleFilled(new Vector2(x + size * 0.5f, y + size * 0.5f), size * 0.5f, ImGui.GetColorU32(ImGuiCol.ModalWindowDimBg));
            }

            // Path for background arc (dimmed arc)
            draw_list.PathArcTo(new Vector2(x + size * 0.5f, y + size * 0.5f), size * 0.5f - thickness * 0.5f, 3.141592f * a_min_factor, 3.141592f * a_max_factor_100percentage);
            draw_list.PathStroke(ImGui.GetColorU32(ImGuiCol.FrameBg), ImDrawFlags.None, thickness);
            
            // Path for progress filling (highlighted arc)
            draw_list.PathArcTo(new Vector2(x + size * 0.5f, y + size * 0.5f), size * 0.5f - thickness * 0.5f, 3.141592f * a_min_factor, 3.141592f * a_max_factor);
            draw_list.PathStroke(ImGui.GetColorU32(ImGuiCol.PlotHistogram), ImDrawFlags.None, thickness);
        }

        public static void ProgressBarArc(float size, float max_angle_factor, float percentage, Vector2 pos, float thickness, ImTextureRef? imTextureRef = null, int texStretchLeft = 0, int texStretchRight = 0, bool isFilled = false)
        {
            var windowPos = ImGui.GetWindowPos();

            DrawArc(size, max_angle_factor, percentage, thickness, new Vector2(pos.X + windowPos.X, pos.Y + windowPos.Y), imTextureRef, texStretchLeft, texStretchRight, isFilled);
        }

        public static void ProgressBarArc(float size, float max_angle_factor, float percentage, float thickness = 2.0f, ImTextureRef? imTextureRef = null, int texStretchLeft = 0, int texStretchRight = 0, bool isFilled = false)
        {
            var pos = ImGui.GetCursorScreenPos();
            ImGui.Dummy(new Vector2(size));

            DrawArc(size, max_angle_factor, percentage, thickness, pos, imTextureRef, texStretchLeft, texStretchRight, isFilled);
        }
    }
}
