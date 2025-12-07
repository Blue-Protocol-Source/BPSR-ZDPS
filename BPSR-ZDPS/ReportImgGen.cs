using BPSR_ZDPS.DataTypes;
using BPSR_ZDPS.Windows;
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
            unsafe
            {
                var tex = OffscreenImGuiRenderer.RenderToTexture(() =>
                {
                    // Windows default to being offset in the viewport, this will bring it back to the top left corner
                    ImGui.SetNextWindowPos(new Vector2(0, 0));
                    EncounterReportWindow report = new EncounterReportWindow();
                    report.Open(encounter);

                    // This returns the window size to the function to use as the image rendering size
                    return report.Draw();
                });

                var img = TextureSaveUtil.Texture2DToPng(OffscreenImGuiRenderer.D3D11Manager.Device, OffscreenImGuiRenderer.D3D11Manager.DeviceContext, tex);

                if (Settings.Instance.SaveEncounterReportToFile)
                {
                    Directory.CreateDirectory("Reports");
                    img.SaveAsPng(Path.Combine("Reports", $"Report_{encounter.StartTime.ToString("yyyy-MM-dd_HH-mm-ss-ff")}.png"));
                }
                
                return img;
            }
        }
    }
}
