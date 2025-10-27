using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public static class ImageArchive
    {
        public static void LoadBaseImages()
        {
            // Loads up our list of known 'required' images for basic features
            string images = Path.Combine(Utils.DATA_DIR_NAME, "Images");

            ImageHelper.LoadTexture(Path.Combine(images, "Profession_1.png"), "Profession_1");
            ImageHelper.LoadTexture(Path.Combine(images, "Profession_2.png"), "Profession_2");
            ImageHelper.LoadTexture(Path.Combine(images, "Profession_4.png"), "Profession_4");
            ImageHelper.LoadTexture(Path.Combine(images, "Profession_5.png"), "Profession_5");
            ImageHelper.LoadTexture(Path.Combine(images, "Profession_9.png"), "Profession_9");
            ImageHelper.LoadTexture(Path.Combine(images, "Profession_11.png"), "Profession_11");
            ImageHelper.LoadTexture(Path.Combine(images, "Profession_12.png"), "Profession_12");
            ImageHelper.LoadTexture(Path.Combine(images, "Profession_13.png"), "Profession_13");
        }

        public static Hexa.NET.ImGui.ImTextureRef? LoadImage(string key)
        {
            unsafe
            {
                return ImageHelper.LoadTexture(Path.Combine(Utils.DATA_DIR_NAME, "Images", $"{key}.png"), key);
            }
        }
    }
}
