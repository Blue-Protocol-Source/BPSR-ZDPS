using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    public class FontFile: IDisposable
    {
        private byte[] data;
        private GCHandle handle;

        public string Path { get; }
        public GlyphRange[] Ranges { get; }
        public Vector2 GlyphOffset { get; } = new Vector2(0 , 0);
        public bool isValid { get; private set; } = false;

        public FontFile(string path, GlyphRange[] ranges = null, Vector2? glyphOffset = null)
        {
            Path = path;
            Ranges = ranges;

            if (glyphOffset != null)
            {
                GlyphOffset = (Vector2)glyphOffset;
            }

            ValidateAndLoad();
        }

        public FontFile(string path, Vector2? glyphOffset = null, GlyphRange[] ranges = null)
        {
            Path = path;
            Ranges = ranges;

            if (glyphOffset != null)
            {
                GlyphOffset = (Vector2)glyphOffset;
            }

            ValidateAndLoad();
        }

        public FontFile(string path, GlyphRange range, Vector2? glyphOffset = null)
        {
            Path = path;
            Ranges = new[] { range };

            if (glyphOffset != null)
            {
                GlyphOffset = (Vector2)glyphOffset;
            }

            ValidateAndLoad();
        }

        public FontFile(string path)
        {
            Path = path;

            ValidateAndLoad();
        }

        public unsafe ImFontPtr BindToImGui(float fontSize = 18.0f)
        {
            ImFontPtr result = null;

            ImFontConfig* fontConfig = ImGui.ImFontConfig();
            fontConfig->FontDataOwnedByAtlas = 0;

            ushort[] ranges = GetGlyphRanges();
            if (ranges == null || ranges.Length == 0)
            {
                result = ImGui.GetIO().Fonts.AddFontFromMemoryTTF((void*)GetPinnedData(), GetPinnedDataLength(), fontSize, fontConfig);
            }
            else
            {
                GCHandle rangeHandle = GCHandle.Alloc(ranges, GCHandleType.Pinned);
                result = ImGui.GetIO().Fonts.AddFontFromMemoryTTF((void*)GetPinnedData(), GetPinnedDataLength(), fontSize, fontConfig, (uint*)rangeHandle.AddrOfPinnedObject());
                rangeHandle.Free();
            }

            return result;
        }

        private object key = new object();
        private void ValidateAndLoad()
        {
            if (Path == "ImGui.Default")
            {
                isValid = true;
                return;
            }

            lock (key)
            {
                Stream stream;
                if (!TryGetStream(out stream) || stream == null || stream.Length == 0)
                {
                    return;
                }

                data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                handle = GCHandle.Alloc(data, GCHandleType.Pinned);

                isValid = true;
                stream.Dispose();
            }
        }

        private bool TryGetStream(out Stream stream)
        {
            stream = null;

            if (string.IsNullOrEmpty(Path))
            {
                return false;
            }

            try
            {
                if (File.Exists(Path))
                {
                    stream = new FileStream(Path, FileMode.Open, FileAccess.Read);
                    return true;
                }

                Assembly assembly = Assembly.GetExecutingAssembly();
                stream = assembly.GetManifestResourceStream(Path);
                if (stream != null)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        internal ushort[] GetGlyphRanges()
        {
            if (Ranges == null || Ranges.Length == 0)
            {
                return null;
            }

            int i = 0;
            ushort[] ret = new ushort[(Ranges.Length * 2) + 1];
            foreach (GlyphRange range in Ranges)
            {
                ret[i] = range.Min;
                ret[i + 1] = range.Max;
                i += 2;
            }

            return ret;
        }

        internal IntPtr GetPinnedData() => handle.AddrOfPinnedObject();

        internal int GetPinnedDataLength() => data?.Length ?? 0;

        public void Dispose()
        {
            handle.Free();
            data = [];
        }
    }

    public struct GlyphRange
    {
        public ushort Min;
        public ushort Max;

        public GlyphRange(ushort min, ushort max)
        {
            this.Min = min;
            this.Max = max;
        }
    }
}
