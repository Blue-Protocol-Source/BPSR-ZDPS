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
                                byte r = srcRow[x * 4 + 0];
                                byte g = srcRow[x * 4 + 1];
                                byte b = srcRow[x * 4 + 2];
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
