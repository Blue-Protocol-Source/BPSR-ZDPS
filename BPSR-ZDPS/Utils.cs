using BPSR_ZDPS.DataTypes;
using Silk.NET.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Zproto;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Reflection;

namespace BPSR_ZDPS
{
    public static unsafe class Utils
    {
        // Note: This value MUST MATCH the "Data" folder name for the project resources that AppStrings.json and other files are in
        // Those files are copied to the output directory at build time with a matching path so if this mismatches then the app will not see them
        public const string DATA_DIR_NAME = "Data";
        public static readonly Guid D3DDebugObjectName = new(0x429b8c22, 0x9188, 0x4b0c, 0x87, 0x42, 0xac, 0xb0, 0xbf, 0x85, 0xc2, 0x00);

        public static Version AppVersion { get; set; } = typeof(Utils).Assembly.GetName().Version;

        internal static string? GetDebugName(void* target)
        {
            ID3D11DeviceChild* child = (ID3D11DeviceChild*)target;
            if (child == null)
            {
                return null;
            }

            uint len;
            Guid guid = D3DDebugObjectName;
            child->GetPrivateData(&guid, &len, null);
            if (len == 0)
            {
                return string.Empty;
            }

            byte* pName = (byte*)Marshal.AllocHGlobal((nint)len);
            child->GetPrivateData(&guid, &len, pName);
            string str = Encoding.UTF8.GetString(new Span<byte>(pName, (int)len));
            Marshal.FreeHGlobal((nint)pName);
            return str;
        }

        internal static void SetDebugName(void* target, string name)
        {
            ID3D11DeviceChild* child = (ID3D11DeviceChild*)target;
            if (child == null)
            {
                return;
            }

            Guid guid = D3DDebugObjectName;
            if (name != null)
            {
                byte* pName = (byte*)Marshal.StringToHGlobalAnsi(name);
                child->SetPrivateData(&guid, (uint)name.Length, pName);
                Marshal.FreeHGlobal((nint)pName);
            }
            else
            {
                child->SetPrivateData(&guid, 0, null);
            }
        }

        public static Guid* Guid(Guid guid)
        {
            return (Guid*)Unsafe.AsPointer(ref guid);
        }

        public static T2* Cast<T1, T2>(T1* t) where T1 : unmanaged where T2 : unmanaged
        {
            return (T2*)t;
        }

        public static byte* ToBytes(this string str)
        {
            return (byte*)Marshal.StringToHGlobalAnsi(str);
        }

        public static void ThrowHResult(this int code)
        {
            ResultCode resultCode = (ResultCode)code;
            if (resultCode != ResultCode.S_OK)
            {
                throw new D3D11Exception(resultCode);
            }
        }

        public static string BytesToString<T>(T number)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            double value = Convert.ToDouble(number);
            if (value == 0)
            {
                return "0" + suf[0];
            }

            double absoluteValue = Math.Abs(value);
            int place = Convert.ToInt32(Math.Floor(Math.Log(absoluteValue, 1024)));
            double shortNumber = Math.Round(absoluteValue / Math.Pow(1024, place), 2);

            string fmt = "";
            if (place > 0)
            {
                fmt = "N2";
            }
            return $"{(Math.Sign(value) * shortNumber).ToString(fmt)}{suf[place]}";
        }

        public static string NumberToShorthand<T>(T number)
        {
            string[] suf = { "", "K", "M", "B", "t", "q", "Q", "s", "S", "o", "n", "d", "U", "D", "T" };
            double value = Convert.ToDouble(number);
            if (value == 0)
            {
                return "0" + suf[0];
            }

            double absoluteValue = Math.Abs(value);
            int place = Convert.ToInt32(Math.Floor(Math.Log(absoluteValue, 1000)));
            double shortNumber = Math.Round(absoluteValue / Math.Pow(1000, place), 2);

            if (Settings.Instance.UseShortWidthNumberFormatting)
            {
                return place == 0 ? ((long)value).ToString() : shortNumber.ToString($"N2") + suf[place];
            }

            string fmt = "";
            if (place > 0)
            {
                fmt = "N2";
            }
            return $"{(Math.Sign(value) * shortNumber).ToString(fmt)}{suf[place]}";
        }

        public static EEntityType RawUuidToEntityType(long uuid) => (uuid & 0xFFFFL) switch
        {
            64 => EEntityType.EntMonster,
            128 => EEntityType.EntNpc,
            192 => EEntityType.EntSceneObject,
            320 => EEntityType.EntZone,
            640 => EEntityType.EntChar,
            704 => EEntityType.EntDummy,
            1024 => EEntityType.EntCollection, // Another one?
            32960 => EEntityType.EntSceneObject,
            33152 => EEntityType.EntBullet,
            33280 => EEntityType.EntNpc, // Another one?
            33472 => EEntityType.EntDummy,
            33664 => EEntityType.EntField,
            33792 => EEntityType.EntCollection,
            33984 => EEntityType.EntVehicle,
            34048 => EEntityType.EntToy,
            32832 => EEntityType.EntMonster, // Another one?
            _ => EEntityType.EntErrType,
        };

        public enum EEntityType_Lua
        {
            EntErrType = 0,
            EntMonster = 1,
            EntNpc = 2,
            EntSceneObject = 3,
            EntZone = 5,
            EntBullet = 6,
            EntClientBullet = 7,
            EntPet = 8,
            EntChar = 10,
            EntDummy = 11,
            EntDrop = 12,
            EntField = 14,
            EntTrap = 15,
            EntCollection = 16,
            EntStaticObject = 18,
            EntVehicle = 19,
            EntToy = 19,
            EntCommunityHouse = 21,
            EntHouseItem = 22,
            EntCount = 23
        }

        // Converts UUID to EntityId (UID)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long UuidToEntityId(long uuid) => uuid >> 16;

        // Converts EntityId (UID) to UUID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long EntityIdToUuid(long uid, long entityType, bool isSummon, bool isClient) => uid << 16 | ((isSummon ? 1L : 0L) << 15) | ((isClient ? 1L : 0L) << 14) | (entityType << 6);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long UuidToEntityType(long uuid) => (uuid >> 6) & 31;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSummonByUuid(long uuid) => ((uuid >> 15) & 1) == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClientByUuid(long uuid) => ((uuid >> 14) & 1) == 1;

        // Checks if EntityId (UID) is an AI (Bot)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckIsAiByEntityId(long uid) => ((uid >> 10) & 1) != 0;

        public static bool IsCurrentPlatformWindowVisible()
        {
            if (ImGui.GetWindowViewport().PlatformHandle != null)
            {
                int isVisible = GLFW.GetWindowAttrib((GLFWwindowPtr)ImGui.GetWindowViewport().PlatformHandle, GLFW.GLFW_VISIBLE);
                if (isVisible > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static void SetCurrentPlatformWindowVisible()
        {
            GLFW.SetWindowAttrib((GLFWwindowPtr)ImGui.GetWindowViewport().PlatformHandle, GLFW.GLFW_VISIBLE, 1);
        }

        public static void SetWindowTopmost(ImGuiViewportPtr? viewport = null)
        {
            viewport = viewport ?? ImGui.GetWindowViewport();
            SetWindowTopmost((IntPtr)viewport.Value.PlatformHandleRaw);
        }
        
        public static void UnsetWindowTopmost(ImGuiViewportPtr? viewport = null)
        {
            viewport = viewport ?? ImGui.GetWindowViewport();
            UnsetWindowTopmost((IntPtr)viewport.Value.PlatformHandleRaw);
        }
        
        public static void SetWindowOpacity(float alpha, ImGuiViewportPtr? viewport = null)
        {
            viewport = viewport ?? ImGui.GetWindowViewport();
            GLFW.SetWindowOpacity((GLFWwindowPtr) viewport.Value.PlatformHandle, alpha);
        }
        
        public static void BringWindowToFront(ImGuiViewportPtr? viewport = null)
        {
            viewport = viewport ?? ImGui.GetWindowViewport();
            User32.SetForegroundWindow((IntPtr)viewport.Value.PlatformHandleRaw);
        }
        
        public static void MinimiseWindow(ImGuiViewportPtr? viewport = null)
        {
            viewport = viewport ?? ImGui.GetWindowViewport();
            User32.ShowWindow((IntPtr)viewport.Value.PlatformHandleRaw, User32.SW_MINIMIZE);
        }
        
        public static void SetWindowTopmost(IntPtr hWnd)
        {
            User32.SetWindowPos(hWnd, User32.HWND_TOPMOST, 0, 0, 0, 0, User32.SWP_NOMOVE | User32.SWP_NOSIZE);
        }
    
        public static void UnsetWindowTopmost(IntPtr hWnd)
        {
            User32.SetWindowPos(hWnd, User32.HWND_NOTOPMOST, 0, 0, 0, 0, User32.SWP_NOMOVE | User32.SWP_NOSIZE);
        }

        public static unsafe void SetCurrentWindowIcon()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string iconAssemblyPath = "BPSR_ZDPS.Resources.MainWindowIcon.png";
            using (var iconStream = assembly.GetManifestResourceStream(iconAssemblyPath))
            {
                if (iconStream != null)
                {
                    SetCurrentWindowIcon(iconStream);
                }
            }
        }

        static unsafe void SetCurrentWindowIcon(Stream IconFileStream)
        {
            using (Image<Rgba32> image = Image.Load<Rgba32>(IconFileStream))
            {
                // Convert image data to byte array
                byte[] pixels = new byte[image.Width * image.Height * 4];
                image.CopyPixelDataTo(pixels);

                // Allocate unmanaged memory for pixels
                IntPtr pixelsPtr = Marshal.AllocHGlobal(pixels.Length);
                Marshal.Copy(pixels, 0, pixelsPtr, pixels.Length);

                // Create GLFWimage structure
                GLFWimage iconImage = new GLFWimage
                {
                    Width = image.Width,
                    Height = image.Height,
                    Pixels = (byte*)pixelsPtr
                };

                // Create an array for GLFWimage structures (though we only have one currently)
                GLFWimage[] images = new GLFWimage[] { iconImage };

                // Pin the array to prevent garbage collection during the call
                GCHandle handle = GCHandle.Alloc(images, GCHandleType.Pinned);
                IntPtr imagesPtr = handle.AddrOfPinnedObject();

                GLFW.SetWindowIcon((GLFWwindowPtr)ImGui.GetWindowViewport().PlatformHandle, 1, (GLFWimage*)imagesPtr);
                handle.Free();
            }
        }
    }
}
