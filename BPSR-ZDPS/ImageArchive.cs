using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public static class ImageArchive
    {
        private static D3D11Manager _manager;

        public static void LoadBaseImages(D3D11Manager manager)
        {
            _manager = manager;

            // Loads up our list of known 'required' images for basic features
            unsafe
            {
                string images = Path.Combine("Data", "Images");

                ImageHelper.LoadTexture(manager.Device, manager.DeviceContext, Path.Combine(images, "Profession_1.png"), "Profession_1");
                ImageHelper.LoadTexture(manager.Device, manager.DeviceContext, Path.Combine(images, "Profession_2.png"), "Profession_2");
                ImageHelper.LoadTexture(manager.Device, manager.DeviceContext, Path.Combine(images, "Profession_4.png"), "Profession_4");
                ImageHelper.LoadTexture(manager.Device, manager.DeviceContext, Path.Combine(images, "Profession_5.png"), "Profession_5");
                ImageHelper.LoadTexture(manager.Device, manager.DeviceContext, Path.Combine(images, "Profession_9.png"), "Profession_9");
                ImageHelper.LoadTexture(manager.Device, manager.DeviceContext, Path.Combine(images, "Profession_11.png"), "Profession_11");
                ImageHelper.LoadTexture(manager.Device, manager.DeviceContext, Path.Combine(images, "Profession_12.png"), "Profession_12");
                ImageHelper.LoadTexture(manager.Device, manager.DeviceContext, Path.Combine(images, "Profession_13.png"), "Profession_13");
            }
        }

        public static Hexa.NET.ImGui.ImTextureRef? LoadImage(string key)
        {
            unsafe
            {
                return ImageHelper.LoadTexture(_manager.Device, _manager.DeviceContext, Path.Combine("Data", "Images", $"{key}.png"), key);
            }
        }
    }
}
