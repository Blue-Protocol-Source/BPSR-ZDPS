using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public static class Colors
    {
        public static Vector4 Dequantize(int r, int g, int b, int a = 255)
        {
            return new Vector4(r / 255.0f, g / 255.0f, b /255.0f, a / 255.0f);
        }

        public static Vector4 FromColor(System.Drawing.Color color)
        {
            return new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
        }

        public static Vector4 Transparent = new Vector4(1.0f, 1.0f, 1.0f, 0.0f);
        public static Vector4 Red = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        public static Vector4 Red_Transparent = new Vector4(1.0f, 0.0f, 0.0f, 0.75f);
        public static Vector4 LightRed = new Vector4(1.0f, 0.5f, 0.5f, 1.0f);
        public static Vector4 LightRed_Transparent = new Vector4(1.0f, 0.5f, 0.5f, 0.75f);
        public static Vector4 DarkRed = new Vector4(0.54510f, 0.0f, 0.0f, 1.0f);
        public static Vector4 DarkRed_Transparent = new Vector4(0.54510f, 0.0f, 0.0f, 0.75f);
        public static Vector4 Green = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        public static Vector4 Green_Transparent = new Vector4(0.0f, 1.0f, 0.0f, 0.75f);
        public static Vector4 LightGreen = new Vector4(0.56471f, 0.93333f, 0.56471f, 1.0f);
        public static Vector4 LightGreen_Transparent = new Vector4(0.56471f, 0.93333f, 0.56471f, 0.75f);
        public static Vector4 DarkGreen = new Vector4(0.0f, 0.39216f, 0.0f, 1.0f);
        public static Vector4 DarkGreen_Transparent = new Vector4(0.0f, 0.39216f, 0.0f, 0.75f);
        public static Vector4 Blue = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
        public static Vector4 Blue_Transparent = new Vector4(0.0f, 0.0f, 1.0f, 0.75f);
        public static Vector4 LightBlue = new Vector4(0.67843f, 0.84706f, 0.90196f, 1.0f);
        public static Vector4 LightBlue_Transparent = new Vector4(0.67843f, 0.84706f, 0.90196f, 0.75f);
        public static Vector4 SkyBlue = new Vector4(0.52941f, 0.80784f, 0.92157f, 1.0f);
        public static Vector4 SkyBlue_Transparent = new Vector4(0.52941f, 0.80784f, 0.92157f, 0.75f);
        public static Vector4 Yellow = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
        public static Vector4 Yellow_Transparent = new Vector4(1.0f, 1.0f, 0.0f, 0.75f);
        public static Vector4 LightYellow = new Vector4(1.0f, 1.0f, 0.87843f, 1.0f);
        public static Vector4 LightYellow_Transparent = new Vector4(1.0f, 1.0f, 0.87843f, 0.75f);
        public static Vector4 DimGray = new Vector4(0.41176f, 0.41176f, 0.41176f, 1.0f);
        public static Vector4 Gray = new Vector4(0.50196f, 0.50196f, 0.50196f, 1.0f);
        public static Vector4 White = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public static Vector4 Gold = new Vector4(1.0f, 0.84314f, 0.0f, 1.0f);
        public static Vector4 Gold_Transparent = new Vector4(1.0f, 0.84314f, 0.0f, 0.75f);
        public static Vector4 Goldenrod = new Vector4(0.85490f, 0.64706f, 0.12549f, 1.0f);
        public static Vector4 Goldenrod_Transparent = new Vector4(0.85490f, 0.64706f, 0.12549f, 0.75f);
        public static Vector4 OrangeRed = new Vector4(1.0f, 0.27059f, 0.0f, 1.0f);
        public static Vector4 OrangeRed_Transparent = new Vector4(1.0f, 0.27059f, 0.0f, 0.75f);
        public static Vector4 Wheat = new Vector4(0.96078f, 0.87059f, 0.70196f, 1.0f);
        public static Vector4 Orchid = new Vector4(0.85490f, 0.43922f, 0.83922f, 1.0f);
        public static Vector4 PaleVioletRed = new Vector4(0.85882f, 0.43922f, 0.57647f, 1.0f);
        public static Vector4 Pink = new Vector4(1.0f, 0.75294f, 0.79608f, 1.0f);
        public static Vector4 Magenta = new Vector4(1.00000f, 0.0f, 1.00000f, 1.0f);
        public static Vector4 Magenta_Transparent = new Vector4(1.00000f, 0.0f, 1.00000f, 0.75f);
        public static Vector4 DarkMagenta = new Vector4(0.54510f, 0.0f, 0.54510f, 1.0f);
    }
}
