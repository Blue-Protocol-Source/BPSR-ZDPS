using Silk.NET.Direct3D11;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public unsafe static class TextureSaveUtil
    {
        public static void SaveTextureAsPng(
            ID3D11Device1* device,
            ID3D11DeviceContext1* context,
            ID3D11Texture2D* texture,
            string path)
        {
            // Get texture description
            Texture2DDesc desc;
            texture->GetDesc(&desc);

            // Create staging texture
            Texture2DDesc stagingDesc = desc;
            stagingDesc.Usage = Usage.Staging;
            stagingDesc.BindFlags = 0;
            stagingDesc.CPUAccessFlags = (uint)CpuAccessFlag.Read;
            stagingDesc.MiscFlags = 0;

            ID3D11Texture2D* staging = null;
            device->CreateTexture2D(&stagingDesc, null, &staging);

            // Copy GPU texture -> staging texture
            context->CopyResource((ID3D11Resource*)texture, (ID3D11Resource*)staging);

            // Map staging texture so we can read CPU memory
            MappedSubresource mapped;
            context->Map((ID3D11Resource*)staging, 0, Map.Read, 0, &mapped);

            int width = (int)desc.Width;
            int height = (int)desc.Height;
            int pitch = (int)mapped.RowPitch;

            // Create ImageSharp framebuffer
            using Image<Rgba32> img = new(width, height);

            byte* src = (byte*)mapped.PData;

            // Copy rows into ImageSharp
            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < height; y++)
                {
                    Span<Rgba32> destRow = accessor.GetRowSpan(y);
                    byte* srcRow = src + (y * pitch);

                    fixed (Rgba32* destPtr = destRow)
                    {
                        Rgba32* dest = destPtr;

                        for (int x = 0; x < width; x++)
                        {
                            byte b = srcRow[x * 4 + 0];
                            byte g = srcRow[x * 4 + 1];
                            byte r = srcRow[x * 4 + 2];
                            byte a = srcRow[x * 4 + 3];

                            dest[x] = new Rgba32(r, g, b, a);
                        }
                    }
                }
            });

            // Unmap D3D resource
            context->Unmap((ID3D11Resource*)staging, 0);
            staging->Release();

            // Save PNG
            img.Save(path, new PngEncoder());
        }

        public static Image<Rgba32> Texture2DToPng(ID3D11Device1* device, ID3D11DeviceContext1* context, ID3D11Texture2D* texture)
        {
            Texture2DDesc desc;
            texture->GetDesc(&desc);
            desc.BindFlags = 0;
            desc.CPUAccessFlags = (uint)CpuAccessFlag.Read;
            desc.Usage = Usage.Staging;
            desc.MiscFlags = 0;

            ID3D11Texture2D* staging = null;
            device->CreateTexture2D(&desc, null, &staging);
            context->CopyResource((ID3D11Resource*)staging, (ID3D11Resource*)texture);

            MappedSubresource mapped;
            context->Map((ID3D11Resource*)staging, 0, Map.Read, 0, &mapped);

            int width = (int)desc.Width;
            int height = (int)desc.Height;
            int pitch = (int)mapped.RowPitch;

            var img = new Image<Rgba32>(width, height);
            unsafe
            {
                byte* src = (byte*)mapped.PData;

                img.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < height; y++)
                    {
                        Span<Rgba32> destRow = accessor.GetRowSpan(y);
                        byte* srcRow = src + (y * pitch);

                        fixed (Rgba32* destPtr = destRow)
                        {
                            Rgba32* dest = destPtr;

                            for (int x = 0; x < width; x++)
                            {
                                byte b = srcRow[x * 4 + 0];
                                byte g = srcRow[x * 4 + 1];
                                byte r = srcRow[x * 4 + 2];
                                byte a = srcRow[x * 4 + 3];

                                dest[x] = new Rgba32(r, g, b, a);
                            }
                        }
                    }
                });
            }

            context->Unmap((ID3D11Resource*)staging, 0);
            staging->Release();

            return img;
        }
    }
}
