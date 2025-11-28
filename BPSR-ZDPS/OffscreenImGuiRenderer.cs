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

            // Init only the D3D11 backend
            ImGuiImplD3D11.SetCurrentContext(_ctx);
            if(!ImGuiImplD3D11.Init(Unsafe.BitCast<ComPtr<ID3D11Device1>, ID3D11DevicePtr>(_device), Unsafe.BitCast<ComPtr<ID3D11DeviceContext1>, ID3D11DeviceContextPtr>(_context)))
            {
                System.Diagnostics.Debug.WriteLine("Failed to init ImGui Impl D3D11");
            }

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
        public static ID3D11Texture2D* RenderToTexture(int width, int height, Action guiDraw)
        {
            if (!_initialized)
            {
                return null;
            }

            CreateRenderTarget(width, height);

            // Switch to this offscreen context
            ImGui.SetCurrentContext(_ctx);
            ImGuiImplD3D11.SetCurrentContext(_ctx);

            var io = ImGui.GetIO();
            io.DisplaySize = new Vector2(width, height);
            io.DisplayFramebufferScale = new Vector2(1.0f, 1.0f);

            ImGuiImplD3D11.NewFrame();
            ImGui.NewFrame();

            // Draw the ImGui UI that should appear in the texture (and any other processing happeing in this context)
            guiDraw?.Invoke();

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
                Width = width,
                Height = height,
                MinDepth = 0,
                MaxDepth = 1
            };
            _context->RSSetViewports(1, &vp);

            unsafe
            {
                //float* clear = stackalloc float[4] { 1f, 1f, 1f, 1f };
                //_context->ClearRenderTargetView(_rtv, clear);
                var color = new Vector4(1, 1, 1, 1);
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
    }

}
