using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPSR_ZDPS
{
    using Silk.NET.Direct3D11;
    using Silk.NET.DXGI;
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Backends.D3D11;
    using Silk.NET.Core.Native;
    using System.Runtime.CompilerServices;
    using System.Numerics;

    public unsafe static class OffscreenImGuiRenderer
    {
        public static ImGuiContextPtr OriginalContext { get; private set; }

        public static D3D11Manager? D3D11Manager { get; private set; } = null;

        private static ID3D11Device1* _device;
        private static ID3D11DeviceContext1* _context;

        private static ImGuiContextPtr _ctx;
        private static bool _initialized;

        private static ID3D11Texture2D* _texture;
        private static ID3D11RenderTargetView* _rtv;

        static OffscreenImGuiRenderer()
        {

        }

        public static void Initialize(D3D11Manager manager, ImGuiContextPtr originalContext)
        {
            D3D11Manager = manager;
            // Store the original (current) ImGui Context so we can return back to it internally after we swap to our own
            OriginalContext = originalContext;

            _device = manager.Device;
            _context = manager.DeviceContext;

            // Create isolated ImGui context
            _ctx = ImGui.CreateContext();
            ImGui.SetCurrentContext(_ctx);

            // Setup ImGui config.
            var io = ImGui.GetIO();

            // Disable imgui.ini file writing
            unsafe
            {
                io.IniFilename = null;
            }

            //io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;     // Enable Keyboard Controls
            //io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;      // Enable Gamepad Controls
            //io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;         // Enable Docking
            //io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;       // Enable Multi-Viewport / Platform Windows
            io.ConfigViewportsNoAutoMerge = true; // If this is false, putting an ImGui window on top of an GLFW window will dock into it even if it's not shown
            io.ConfigViewportsNoTaskBarIcon = false;

            LoadFonts();

            // Init only the D3D11 backend
            ImGuiImplD3D11.SetCurrentContext(_ctx);
            if(!ImGuiImplD3D11.Init(Unsafe.BitCast<ComPtr<ID3D11Device1>, ID3D11DevicePtr>(_device), Unsafe.BitCast<ComPtr<ID3D11DeviceContext1>, ID3D11DeviceContextPtr>(_context)))
            {
                System.Diagnostics.Debug.WriteLine("Failed to init ImGui Impl D3D11");
            }

            io.DisplaySize = new Vector2(800, 600);

            Theme.VSDarkTheme();

            _initialized = true;

            ImGui.SetCurrentContext(OriginalContext);
            ImGuiImplD3D11.SetCurrentContext(OriginalContext);
        }

        /// <summary>
        /// Renders the given ImGui action to a Texture. Note you may need to render multiple times to get the desired output.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="guiDraw"></param>
        /// <returns></returns>
        public static ID3D11Texture2D* RenderToTexture(Func<Vector2> guiDraw, int desiredWidth = 0, int desiredHeight = 0)
        {
            // TODO: Implement Desired Width and Height overrides (they are likely to be rarely used)

            if (!_initialized)
            {
                return null;
            }

            // Switch to this offscreen context
            ImGui.SetCurrentContext(_ctx);
            ImGuiImplD3D11.SetCurrentContext(_ctx);

            Vector2? windowSize = new Vector2();

            var io = ImGui.GetIO();
            // The Display Size must be large enough to contain the entire window, if it's smaller then the window will be cropped
            io.DisplaySize = new Vector2(4096, 4096);
            io.DisplayFramebufferScale = new Vector2(1.0f, 1.0f);

            // This cursed loop is done to render out a number of frames of the provided window in order for ImGui's auto layout system to fully complete before we use the result
            int iterations = 5;
            for (int i = 0; i < iterations; i++)
            {
                // TODO: Set the io.DisplaySize to the new WindowSize if window size is larger than current display size (hopefully never happens)

                ImGuiImplD3D11.NewFrame();
                ImGui.NewFrame();

                // Draw the ImGui UI that should appear in the texture (and any other processing happeing in this context)
                windowSize = guiDraw?.Invoke();
                if (windowSize == null)
                {
                    windowSize = new Vector2();
                }

                if (i < iterations)
                {
                    ImGui.Render();
                    ImGuiImplD3D11.RenderDrawData(ImGui.GetDrawData());
                }
            }

            CreateRenderTarget((int)windowSize.Value.X, (int)windowSize.Value.Y);

            ImGui.Render();

            // Bind our offscreen render target
            ID3D11RenderTargetView* oldRT = null;
            _context->OMGetRenderTargets(1, &oldRT, null);

            ID3D11RenderTargetView* rtv = _rtv;
            _context->OMSetRenderTargets(1, &rtv, null);

            Viewport vp = new()
            {
                TopLeftX = 0,
                TopLeftY = 0,
                Width = (int)windowSize.Value.X,
                Height = (int)windowSize.Value.Y,
                MinDepth = 0,
                MaxDepth = 1
            };
            _context->RSSetViewports(1, &vp);

            unsafe
            {
                //float* clear = stackalloc float[4] { 1f, 1f, 1f, 1f };
                //_context->ClearRenderTargetView(_rtv, clear);
                var color = new Vector4(1, 1, 1, 0); // Set Alpha to 0 to make transparent
                _context->ClearRenderTargetView(_rtv, (float*)&color);
            }

            // Render the ImGui draw lists into our RenderTarget and Texture
            ImGuiImplD3D11.RenderDrawData(ImGui.GetDrawData());

            // We do this just to make ImGui happy by calling these
            if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                ImGui.UpdatePlatformWindows();
                ImGui.RenderPlatformWindowsDefault();
            }

            _context->Flush();

            // Restore previous render target
            _context->OMSetRenderTargets(1, &oldRT, null);
            if (oldRT != null) oldRT->Release();

            ImGui.SetCurrentContext(OriginalContext);
            ImGuiImplD3D11.SetCurrentContext(OriginalContext);

            return _texture;
        }

        private static void CreateRenderTarget(int width, int height)
        {
            if (_texture != null) { _texture->Release(); _texture = null; }
            if (_rtv != null) { _rtv->Release(); _rtv = null; }

            Texture2DDesc desc = new()
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.FormatR8G8B8A8Unorm,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Default,
                BindFlags = (uint)(BindFlag.RenderTarget | BindFlag.ShaderResource),
                CPUAccessFlags = 0,
                MiscFlags = 0
            };

            ID3D11Texture2D* tex = null;
            _device->CreateTexture2D(&desc, null, &tex);
            _texture = tex;

            ID3D11RenderTargetView* rtv = null;
            _device->CreateRenderTargetView((ID3D11Resource*)_texture, null, &rtv);
            _rtv = rtv;
        }

        public static void Release()
        {
            if (_texture != null) { _texture->Release(); _texture = null; }
            if (_rtv != null) { _rtv->Release(); _rtv = null; }
        }

        public static void Dispose()
        {
            Release();

            if (_initialized)
            {
                ImGuiImplD3D11.Shutdown();
                ImGui.DestroyContext(_ctx);
            }

            _initialized = false;
        }

        static unsafe void LoadFonts()
        {
            var io = ImGui.GetIO();
            var segoe = io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\segoeui.ttf", 18.0f);
            HelperMethods.Fonts.Add("Segoe_Offscreen", segoe);
            //ImGui.PushFont(HelperMethods.Fonts["Segoe"], 18.0f);

            // Windows 11 doesn't actually have this anymore so we can't rely on the system, we have to embed it
            //HelperMethods.Fonts.Add("Cascadia-Mono", io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\CascadiaMono.ttf", 18.0f));
            //ImGui.PushFont(HelperMethods.Fonts["Cascadia-Mono"], 18.0f);
            var ff = new FontFile("BPSR_ZDPS.Fonts.CascadiaMono.ttf");
            var res = ff.BindToImGui(18.0f);
            HelperMethods.Fonts.Add("Cascadia-Mono_Offscreen", res);
            ff.Dispose();

            //ImGui.AddFontDefault(HelperMethods.Fonts["Segoe"].ContainerAtlas);

            HelperMethods.Fonts.Add("Segoe-Bold_Offscreen", io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\segoeuib.ttf", 18.0f));
            //ImGui.PushFont(HelperMethods.Fonts["Segoe-Bold"], 18.0f);

            ff = new FontFile("BPSR_ZDPS.Fonts.FAS.ttf", new GlyphRange(0x0021, 0xF8FF));
            res = ff.BindToImGui(18.0f);
            HelperMethods.Fonts.Add("FASIcons_Offscreen", res);
            ff.Dispose();

            // Japanese character supporting font (this is a bit heavy to load into memory - 5MB)
            //ff = new FontFile("BPSR_ZDPS.Fonts.fot-seuratpron-m.otf");
            ff = new FontFile("BPSR_ZDPS.Fonts.fot-seuratpron-m.otf", new GlyphRange(0x3000, 0x303F));
            res = ff.BindToImGui(18.0f);
            HelperMethods.Fonts.Add("Seurat_Offscreen", res);
            ff.Dispose();

            // Chinese character supporting font (this is very heavy to load into memory - 16MB)
            ff = new FontFile("BPSR_ZDPS.Fonts.SourceHanSansSC-Regular.otf", new GlyphRange(0x4E00, 0x9FFF));
            res = ff.BindToImGui(18.0f);
            HelperMethods.Fonts.Add("SourceHanSans_Offscreen", res);
            ff.Dispose();
        }
    }

}
