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

            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "128", "Profession_1.png"), "Profession_1_128");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "128", "Profession_2.png"), "Profession_2_128");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "128", "Profession_3.png"), "Profession_3_128");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "128", "Profession_4.png"), "Profession_4_128");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "128", "Profession_5.png"), "Profession_5_128");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "128", "Profession_9.png"), "Profession_9_128");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "128", "Profession_11.png"), "Profession_11_128");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "128", "Profession_12.png"), "Profession_12_128");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "128", "Profession_13.png"), "Profession_13_128");

            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "Slim", "Profession_1.png"), "Profession_1_Slim");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "Slim", "Profession_2.png"), "Profession_2_Slim");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "Slim", "Profession_3.png"), "Profession_3_Slim");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "Slim", "Profession_4.png"), "Profession_4_Slim");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "Slim", "Profession_5.png"), "Profession_5_Slim");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "Slim", "Profession_9.png"), "Profession_9_Slim");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "Slim", "Profession_11.png"), "Profession_11_Slim");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "Slim", "Profession_12.png"), "Profession_12_Slim");
            ImageHelper.LoadTexture(Path.Combine(images, "Professions", "Slim", "Profession_13.png"), "Profession_13_Slim");
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
