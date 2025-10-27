using BPSR_ZDPS.DataTypes;
using Silk.NET.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Zproto;

namespace BPSR_ZDPS
{
    public static unsafe class Utils
    {
        // Note: This value MUST MATCH the "Data" folder name for the project resources that AppStrings.json and other files are in
        // Those files are copied to the output directory at build time with a matching path so if this mismatches then the app will not see them
        public const string DATA_DIR_NAME = "Data";
        public static readonly Guid D3DDebugObjectName = new(0x429b8c22, 0x9188, 0x4b0c, 0x87, 0x42, 0xac, 0xb0, 0xbf, 0x85, 0xc2, 0x00);

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

        public static EEntityType RawUuidToEntityType(ulong uuid) => (uuid & 0xFFFFUL) switch
        {
            64 => EEntityType.EntMonster,
            128 => EEntityType.EntNpc,
            192 => EEntityType.EntSceneObject,
            320 => EEntityType.EntZone,
            640 => EEntityType.EntChar,
            704 => EEntityType.EntDummy,
            1024 => EEntityType.EntCollection, // Another one?
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
    }
}
