namespace BPSR_ZDPS;

using Hexa.NET.ImGui;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

public static unsafe class ImageHelper
{
    public static Dictionary<string, ImTextureRef> LoadedImages = [];
    public static Dictionary<string, ImTextureRef> KeyedImages = [];
    private static Dictionary<ulong, ulong> Textures = [];

    private static D3D11Manager? _manager = null;

    public static void SetDeviceManager(D3D11Manager manager)
    {
        _manager = manager;
    }

    public static ImTextureRef? LoadTexture(string filePath, string? key = null)
    {
        return LoadTexture(_manager.Device, _manager.DeviceContext, filePath, key);
    }

    public static ImTextureRef? LoadTexture(ID3D11Device1* device, ID3D11DeviceContext1* context, string filePath, string? key = null)
    {
        if (LoadedImages.TryGetValue(filePath, out var cachedRef))
            return cachedRef;

        // TODO: Change this so if it finds a local file, it loads it but if not, it search the internal assembly, and lastly a web request
        if (!File.Exists(filePath))
        {
            return null;
        }
        /*else if (System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceNames().Contains(filePath))
        {
            System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream(filePath);
        }
        else
        {
            // TODO: Attempt an WebRequest to get the image
        }*/

        if (device == null)
        {
            return null;
        }

        using Image<Rgba32> image = Image.Load<Rgba32>(filePath);
        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);

        var texDesc = new Texture2DDesc
        {
            Width = (uint)image.Width,
            Height = (uint)image.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.FormatR8G8B8A8Unorm,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            Usage = Usage.Immutable,
            BindFlags = (uint)BindFlag.ShaderResource,
            CPUAccessFlags = 0,
            MiscFlags = 0
        };

        GCHandle pinned = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            var initData = new SubresourceData
            {
                PSysMem = pinned.AddrOfPinnedObject().ToPointer(),
                SysMemPitch = (uint)(image.Width * 4),
                SysMemSlicePitch = 0
            };

            ID3D11Texture2D* texture = null;
            int hr = ((ID3D11Device*)device)->CreateTexture2D(&texDesc, &initData, &texture);
            Silk.NET.Core.Native.SilkMarshal.ThrowHResult(hr);

            ID3D11ShaderResourceView* srv = null;
            hr = ((ID3D11Device*)device)->CreateShaderResourceView((ID3D11Resource*)texture, null, &srv);
            Silk.NET.Core.Native.SilkMarshal.ThrowHResult(hr);

            Textures.TryAdd((ulong)srv, (ulong)texture);

            var texRef = new ImTextureRef(null, srv);
            LoadedImages.TryAdd(filePath, texRef);

            if (key != null)
            {
                KeyedImages.TryAdd(key, texRef);
            }

            return texRef;
        }
        finally
        {
            pinned.Free();
        }
    }

    public static ImTextureRef? GetTextureByKey(string key)
    {
        if (KeyedImages.TryGetValue(key, out ImTextureRef texRef))
        {
            return texRef;
        }

        return null;
    }

    public static void UnloadAllImages()
    {
        foreach (var texInfo in Textures)
        {
            ((ID3D11Texture2D*)texInfo.Value)->Release();
            ((ID3D11ShaderResourceView*)texInfo.Key)->Release();
        }

        LoadedImages.Clear();
        KeyedImages.Clear();
        Textures.Clear();
    }
}