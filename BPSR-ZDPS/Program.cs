using BPSR_ZDPS.Windows;
using Hexa.NET.GLFW;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.D3D11;
using Hexa.NET.ImGui.Backends.GLFW;
using Serilog;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BPSR_ZDPS.DataTypes;
using GLFWwindowPtr = Hexa.NET.GLFW.GLFWwindowPtr;
using Hexa.NET.ImPlot;

namespace BPSR_ZDPS
{
    internal class Program
    {
        private static MainWindow mainWindow;
        private static GLFWwindowPtr window;
        private static D3D11Manager manager;

        static void Main(string[] args)
        {            
            Settings.Load();

            var logBuilder = new LoggerConfiguration();
            logBuilder = logBuilder.MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Debug();

            logBuilder.WriteTo.Console(Serilog.Events.LogEventLevel.Error);

            if (Settings.Instance.LogToFile)
            {
                if (File.Exists("ZDPS_log.txt"))
                {
                    File.Copy("ZDPS_log.txt", "ZDPS_log_last_run.txt", true);
                    File.Delete("ZDPS_log.txt");
                }

                logBuilder = logBuilder.WriteTo.File("ZDPS_log.txt");
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            }

            Log.Logger = logBuilder.CreateLogger();

            Log.Information($"Starting ZDPS v{Utils.AppVersion}");

            DB.Init();

            GLFW.Init();

            GLFW.WindowHint(GLFW.GLFW_CLIENT_API, GLFW.GLFW_NO_API);

            GLFW.WindowHint(GLFW.GLFW_FOCUSED, 1);    // Make window focused on start
            GLFW.WindowHint(GLFW.GLFW_RESIZABLE, 1);  // Make window resizable
            GLFW.WindowHint(GLFW.GLFW_VISIBLE, 0); // Start window hidden so it can be nicely positioned first

            // TODO: Load these values from a settings file
            int windowWidth = 800;
            int windowHeight = 600;

            window = GLFW.CreateWindow(800, 600, "ZDPS", null, null);
            if (window.IsNull)
            {
                Console.WriteLine("Failed to create GLFW window.");
                GLFW.Terminate();
                return;
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            string iconAssemblyPath = "BPSR_ZDPS.Resources.MainWindowIcon.png";
            using (var iconStream = assembly.GetManifestResourceStream(iconAssemblyPath))
            {
                if (iconStream != null)
                {
                    SetWindowIcon(window, iconStream);
                }
            }

            // Center window initially
            var glfwMonitor = GLFW.GetPrimaryMonitor();
            var glfwVidMode = GLFW.GetVideoMode(glfwMonitor);
            GLFW.SetWindowPos(window, (glfwVidMode.Width - windowWidth) / 2, (glfwVidMode.Height - windowHeight) / 2);

            // TODO: Do we even actually need this if we use only imgui windows?
            //GLFW.ShowWindow(window);

            manager = new(window, false);

            HelperMethods.GLFWwindow = window;

            var guiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(guiContext);
            ImPlot.SetImGuiContext(guiContext);
            
            var guiPlotContext = ImPlot.CreateContext();
            ImPlot.SetCurrentContext(guiPlotContext);

            // Setup ImGui config.
            var io = ImGui.GetIO();

            // Disable imgui.ini file writing
            unsafe
            {
                io.IniFilename = null;
            }

            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;     // Enable Keyboard Controls
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;      // Enable Gamepad Controls
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;         // Enable Docking
            io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;       // Enable Multi-Viewport / Platform Windows
            io.ConfigViewportsNoAutoMerge = true; // If this is false, putting an ImGui window on top of an GLFW window will dock into it even if it's not shown
            io.ConfigViewportsNoTaskBarIcon = false;

            LoadFonts();

            ImGuiImplGLFW.SetCurrentContext(guiContext);
            if (!ImGuiImplGLFW.InitForOther(Unsafe.BitCast<GLFWwindowPtr, Hexa.NET.ImGui.Backends.GLFW.GLFWwindowPtr>(window), true))
            {
                Console.WriteLine("Failed to init ImGui Impl GLFW");
                GLFW.Terminate();
                return;
            }

            ImGuiImplD3D11.SetCurrentContext(guiContext);
            if (!ImGuiImplD3D11.Init(Unsafe.BitCast<ComPtr<ID3D11Device1>, ID3D11DevicePtr>(manager.Device), Unsafe.BitCast<ComPtr<ID3D11DeviceContext1>, ID3D11DeviceContextPtr>(manager.DeviceContext)))
            {
                Console.WriteLine("Failed to init ImGui Impl D3D11");
                GLFW.Terminate();
                return;
            }

            // Setup resizing.
            unsafe
            {
                GLFW.SetFramebufferSizeCallback(window, Window_Resized_Callback);
            }

            InitWindows();

            Theme.VSDarkTheme();

            // Windows 11 does not properly update the task bar icon when instructed to, so you have to tell it multiple times
            using (var iconStream = assembly.GetManifestResourceStream(iconAssemblyPath))
            {
                if (iconStream != null)
                {
                    SetWindowIcon(window, iconStream);
                }
            }

            unsafe
            {
                OffscreenImGuiRenderer.Initialize(manager, guiContext);
            }
            ImageHelper.SetDeviceManager(manager);
            ImageArchive.LoadBaseImages();

            // Main loop
            while (GLFW.WindowShouldClose(window) == 0)
            {
                // Poll for and process events
                GLFW.PollEvents();

                //var isMouseDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
                //GLFW.SwapInterval(isMouseDragging ? 0 : 1);

                var isMinimized = GLFW.GetWindowAttrib(window, GLFW.GLFW_ICONIFIED);
                if (isMinimized >= 1)
                {
                    System.Threading.Thread.Sleep(10);
                    continue;
                }

                ImGuiImplD3D11.NewFrame();
                ImGuiImplGLFW.NewFrame();
                ImGui.NewFrame();

                RenderWindowList();
                //ImGui.ShowDemoWindow();
                //ImPlot.ShowDemoWindow();

                ImGui.Render();
                ImGui.EndFrame();

                manager.SetTarget();

                manager.Clear(new(0, 0, 0, 1));

                ImGuiImplD3D11.RenderDrawData(ImGui.GetDrawData());

                if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
                {
                    ImGui.UpdatePlatformWindows();
                    ImGui.RenderPlatformWindowsDefault();
                }

                // We can present without vsync to run at double the normal framerate to have input be more responive
                // Double of framerate is controlled in the D3D11Manager by the SwapChain's BufferCount
                //manager.Present((uint)isMouseDragging ? 0 : 1, 0);

                manager.Present(1, 0);

                if (HelperMethods.DeferredImGuiRenderAction != null)
                {
                    HelperMethods.DeferredImGuiRenderAction.Invoke();
                    HelperMethods.DeferredImGuiRenderAction = null;
                }
            }

            Log.Information("ZDPS is beginning exit process.");

            // Save the current encounter to the database before exiting
            if (EncounterManager.Current != null)
            {
                EncounterManager.ShutdownManager();
            }

            DB.CloseAndSave();
            Settings.Save();
            MessageManager.StopCapturing();

            HotKeyManager.UnregisterAllHotKeys();
            //HotKeyManager.UnregisterHookProc();

            System.Diagnostics.Stopwatch writingTimeout = new();
            writingTimeout.Start();
            while (EntityCache.Instance.IsWritingFile)
            {
                // Spin until writing has finished
                System.Threading.Thread.Sleep(10);

                if (writingTimeout.IsRunning && writingTimeout.Elapsed.TotalSeconds >= 6)
                {
                    Log.Warning("EntityCache writing has taken too long during exit process! Forcing exit (this may corrupt the cache).");
                    break;
                }
            }
            writingTimeout.Stop();

            ImGuiImplD3D11.Shutdown();
            ImGuiImplD3D11.SetCurrentContext(null);
            ImGuiImplGLFW.Shutdown();
            ImGuiImplGLFW.SetCurrentContext(null);
            ImPlot.DestroyContext();
            ImGui.DestroyContext();
            manager.Dispose();

            // Clean up and terminate GLFW
            GLFW.DestroyWindow(window);
            GLFW.Terminate();

            Log.Information("ZDPS has successfully terminated all contexts. Performing final retention policy checks.");

            if (Settings.Instance.UseDatabaseForEncounterHistory && Settings.Instance.DatabaseRetentionPolicyDays > 0)
            {
                DB.ClearOldEncounters(Settings.Instance.DatabaseRetentionPolicyDays);
            }

            if (Settings.Instance.SaveEncounterReportToFile && Settings.Instance.ReportFileRetentionPolicyDays > 0)
            {
                var reportFiles = Directory.EnumerateFiles(Path.Combine("Reports"));
                foreach (var file in reportFiles)
                {
                    var fileExtension = Path.GetExtension(file);
                    if (!fileExtension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileNameWithoutExtension(file);

                    if (!fileName.StartsWith("Report_"))
                    {
                        continue;
                    }

                    var strippedName = fileName.Substring(7);

                    if (DateTime.TryParseExact(strippedName, "yyyy-MM-dd_HH-mm-ss-ff", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed))
                    {
                        // File name format matches being one of our generated Reports, it's finally safe to now attempt deleting it per the retention policy setting
                        var difference = DateTime.Now.Subtract(parsed);

                        if (difference.TotalDays > Settings.Instance.ReportFileRetentionPolicyDays)
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (IOException ioException)
                            {
                                Log.Error($"IO Error deleting Report file {file} per Retention Policy (Days = {Settings.Instance.ReportFileRetentionPolicyDays}, Difference = {difference}). Error: {ioException.Message}");
                            }
                            catch (UnauthorizedAccessException uaException)
                            {
                                Log.Error($"Unauthorized Access Error deleting Report file {file} per Retention Policy (Days = {Settings.Instance.ReportFileRetentionPolicyDays}, Difference = {difference}). Error: {uaException.Message}");
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Error deleting Report file {file} per Retention Policy (Days = {Settings.Instance.ReportFileRetentionPolicyDays}, Difference = {difference}). Error: {ex.Message}");
                            }
                        }
                    }
                }
            }

            Log.Information("ZDPS has cleanly exited.");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Log.Error($"Unhandled Exception:\n{ex?.Message}\nStack Trace:\n{ex?.StackTrace}");
        }

        static unsafe void Window_Resized_Callback(Hexa.NET.GLFW.GLFWwindow* window, int width, int height)
        {
            manager.Resize(width, height);
        }

        static unsafe void LoadFonts()
        {
            var io = ImGui.GetIO();
            var segoe = io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\segoeui.ttf", 18.0f);
            HelperMethods.Fonts.Add("Segoe", segoe);

            // Merging additional fonts into Segoe for multi-language support

            // Japanese character supporting font (this is a bit heavy to load into memory - 5MB)
            //ff = new FontFile("BPSR_ZDPS.Fonts.fot-seuratpron-m.otf");
            var ff = new FontFile("BPSR_ZDPS.Fonts.fot-seuratpron-m.otf", new GlyphRange(0x3000, 0x303F));
            var res = ff.BindToImGui(18.0f, true);
            ff.Dispose();

            // Chinese character supporting font (this is very heavy to load into memory - 16MB)
            ff = new FontFile("BPSR_ZDPS.Fonts.SourceHanSansSC-Regular.otf", new GlyphRange(0x4E00, 0x9FFF));
            res = ff.BindToImGui(18.0f, true);
            ff.Dispose();

            // Setting Segoe to be the default application font (though the other fonts will be used if their glyphs are required)
            ImGui.AddFontDefault(HelperMethods.Fonts["Segoe"].ContainerAtlas);

            // Note: Segoe-Bold will not support multi-language when it's used
            HelperMethods.Fonts.Add("Segoe-Bold", io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\segoeuib.ttf", 18.0f));

            ff = new FontFile("BPSR_ZDPS.Fonts.FAS.ttf", new GlyphRange(0x0021, 0xF8FF));
            res = ff.BindToImGui(18.0f);
            HelperMethods.Fonts.Add("FASIcons", res);
            ff.Dispose();

            // Windows 11 doesn't actually have this anymore so we can't rely on the system, we have to embed it
            //HelperMethods.Fonts.Add("Cascadia-Mono", io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\CascadiaMono.ttf", 18.0f));
            ff = new FontFile("BPSR_ZDPS.Fonts.CascadiaMono.ttf");
            res = ff.BindToImGui(18.0f);
            HelperMethods.Fonts.Add("Cascadia-Mono", res);
            ff.Dispose();

            // The below fonts are being merged into Cascadia-Mono

            // Japanese character supporting monospace font
            ff = new FontFile("BPSR_ZDPS.Fonts.CascadiaNextJP.wght.ttf");
            res = ff.BindToImGui(18.0f, true);
            ff.Dispose();

            // Chinese Simplified character supporting monospace font
            ff = new FontFile("BPSR_ZDPS.Fonts.CascadiaNextSC.wght.ttf");
            res = ff.BindToImGui(18.0f, true);
            ff.Dispose();

            // Chinese Traditional character supporting monospace font
            ff = new FontFile("BPSR_ZDPS.Fonts.CascadiaNextSC.wght.ttf");
            res = ff.BindToImGui(18.0f, true);
            ff.Dispose();
        }

        static unsafe void SetWindowIcon(GLFWwindowPtr window, string IconFilePath)
        {
            using (var stream = File.Open(IconFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                SetWindowIcon(window, stream);
            }
        }

        static unsafe void SetWindowIcon(GLFWwindowPtr window, Stream IconFileStream)
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

                GLFW.SetWindowIcon(window, 1, (GLFWimage*)imagesPtr);
                handle.Free();
            }
        }

        static void InitWindows()
        {
            mainWindow = new MainWindow();
        }

        static void RenderWindowList()
        {
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);
            ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, 0);
            ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, 0);
            mainWindow.Draw();
            ImGui.PopStyleColor(3);
        }
    }
}
