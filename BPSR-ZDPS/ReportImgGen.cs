using Hexa.NET.ImGui;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace BPSR_ZDPS
{
    public static class ReportImgGen
    {
        public static Image<Rgba32> CreateReportImg(Encounter encounter)
        {
            Vector2 windowSize = new Vector2(400, 800);
            unsafe
            {
                var tex = OffscreenImGuiRenderer.RenderToTexture((int)windowSize.X, (int)windowSize.Y, () =>
                {
                    ImGui.SetNextWindowPos(new Vector2(0, 0));
                    ImGui.SetNextWindowSize(windowSize);
                    ImGui.Begin("##HiddenWindow");
                    ImGui.Text("UWU Evie was here <3");
                    ImGui.End();
                });

                var img = TextureSaveUtil.Texture2DToPng(OffscreenImGuiRenderer.D3D11Manager.Device, OffscreenImGuiRenderer.D3D11Manager.DeviceContext, tex);
                return img;
            }
        }
    }
}
