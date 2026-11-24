using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using System.Numerics;

namespace BPSR_ZDPS
{
    /// <summary>
    /// Per-monitor DPI awareness handling.
    /// </summary>
    public static unsafe class DpiScaling
    {
        private static ImGuiContext* _context;
        private static ImGuiStyle _unscaledStyle;
        private static ImPlotStyle _unscaledPlotStyle;
        private static float _scale = 1.0f;
        private static Vector2 _currentWorkSize = new Vector2(float.MaxValue);
        private static bool IsMainContext => ImGui.GetCurrentContext().Handle == _context;

        public static float Scale => IsMainContext ? _scale : 1.0f;

        /// <summary>
        /// Initializes the theme's scaling.
        /// </summary>
        public static void Initialize(ImGuiContextPtr context)
        {
            _context = context.Handle;
            _unscaledStyle = *ImGui.GetStyle().Handle;
            _unscaledPlotStyle = *ImPlot.GetStyle().Handle;
        }

        /// <summary>
        /// Initializes the scaling of a window at an optional position.
        /// </summary>
        public static void BeginWindow(string windowName, Vector2 position = default)
        {
            if (!IsMainContext)
            {
                return;
            }

            float scale = SelectWindowMonitor(windowName, position != Vector2.Zero ? position : null);
            if (scale != _scale)
            {
                ApplyStyleScale(scale);
            }
        }

        /// <summary>
        /// Converts a size to unscaled.
        /// </summary>
        public static Vector2 ToUnscaled(Vector2 size)
        {
            return size / Scale;
        }

        /// <summary>
        /// Converts a size to scaled.
        /// </summary>
        public static Vector2 ToScaled(Vector2 size)
        {
            // work around shrinking window sizes due to ImGui float truncation
            return new Vector2(MathF.Round(size.X * Scale), MathF.Round(size.Y * Scale));
        }

        /// <summary>
        /// Converts a window size to scaled.
        /// </summary>
        public static Vector2 ToScaledWindowSize(Vector2 size)
        {
            return Vector2.Min(size * Scale, _currentWorkSize);
        }

        /// <summary>
        /// Converts a thickness to scaled.
        /// </summary>
        public static float ToScaledThickness(float thickness)
        {
            // truncate like ScaleAllSizes() to prevent blurriness
            return MathF.Truncate(thickness * Scale);
        }

        /// <summary>
        /// Gets the scale of the monitor at a position.
        /// </summary>
        public static float GetMonitorScale(Vector2 position)
        {
            return GetMonitor(position).DpiScale;
        }

        private static ImGuiPlatformMonitor GetMonitor(Vector2 position)
        {
            var monitors = ImGui.GetPlatformIO().Monitors;
            for (int i = 0; i < monitors.Size; i++)
            {
                var monitor = monitors[i];
                if (position.X >= monitor.MainPos.X && position.Y >= monitor.MainPos.Y &&
                    position.X < monitor.MainPos.X + monitor.MainSize.X && position.Y < monitor.MainPos.Y + monitor.MainSize.Y)
                {
                    return monitor;
                }
            }
            // primary monitor
            return monitors[0];
        }

        private static float SelectWindowMonitor(string windowName, Vector2? positionHint)
        {
            var window = ImGuiP.FindWindowByName(windowName);
            if (window.Handle != null && window.Viewport.Handle != null)
            {
                _currentWorkSize = GetMonitor(window.Pos).WorkSize;
                return window.Viewport.ImGuiViewport.DpiScale;
            }

            Vector2? position = positionHint;
            if (position == null && window.Handle != null)
            {
                // use last position of closed windows
                position = window.Pos;
            }

            // use main viewport for windows without a position
            var monitor = GetMonitor(position ?? ImGui.GetMainViewport().Pos);
            _currentWorkSize = monitor.WorkSize;
            return monitor.DpiScale;
        }

        private static void ApplyStyleScale(float scale)
        {
            // use unscaled as source to prevent rounding errors from continuous scaling
            var scaledStyle = _unscaledStyle;
            ImGui.ScaleAllSizes(&scaledStyle, scale);

            // copy only sizes to keep colors and other runtime style changes
            CopyStyleSizes(&scaledStyle, ImGui.GetStyle().Handle);
            ScalePlotStyleSizes(scale);
            _scale = scale;
        }

        private static void CopyStyleSizes(ImGuiStyle* src, ImGuiStyle* dst)
        {
            // all fields affected by ImGuiStyle::ScaleAllSizes() in ImGui 1.92.9b
            dst->MainScale = src->MainScale;
            dst->WindowPadding = src->WindowPadding;
            dst->WindowRounding = src->WindowRounding;
            dst->WindowBorderSize = src->WindowBorderSize;
            dst->WindowMinSize = src->WindowMinSize;
            dst->WindowBorderHoverPadding = src->WindowBorderHoverPadding;
            dst->ChildRounding = src->ChildRounding;
            dst->ChildBorderSize = src->ChildBorderSize;
            dst->PopupRounding = src->PopupRounding;
            dst->PopupBorderSize = src->PopupBorderSize;
            dst->FramePadding = src->FramePadding;
            dst->FrameBorderSize = src->FrameBorderSize;
            dst->FrameRounding = src->FrameRounding;
            dst->ItemSpacing = src->ItemSpacing;
            dst->ItemInnerSpacing = src->ItemInnerSpacing;
            dst->CellPadding = src->CellPadding;
            dst->TouchExtraPadding = src->TouchExtraPadding;
            dst->IndentSpacing = src->IndentSpacing;
            dst->ColumnsMinSpacing = src->ColumnsMinSpacing;
            dst->ScrollbarSize = src->ScrollbarSize;
            dst->ScrollbarRounding = src->ScrollbarRounding;
            dst->ScrollbarPadding = src->ScrollbarPadding;
            dst->GrabMinSize = src->GrabMinSize;
            dst->GrabRounding = src->GrabRounding;
            dst->LogSliderDeadzone = src->LogSliderDeadzone;
            dst->ImageRounding = src->ImageRounding;
            dst->ImageBorderSize = src->ImageBorderSize;
            dst->TabRounding = src->TabRounding;
            dst->TabBorderSize = src->TabBorderSize;
            dst->TabMinWidthBase = src->TabMinWidthBase;
            dst->TabMinWidthShrink = src->TabMinWidthShrink;
            dst->TabCloseButtonMinWidthSelected = src->TabCloseButtonMinWidthSelected;
            dst->TabCloseButtonMinWidthUnselected = src->TabCloseButtonMinWidthUnselected;
            dst->TabBarBorderSize = src->TabBarBorderSize;
            dst->TabBarOverlineSize = src->TabBarOverlineSize;
            dst->TreeLinesSize = src->TreeLinesSize;
            dst->TreeLinesRounding = src->TreeLinesRounding;
            dst->MenuItemRounding = src->MenuItemRounding;
            dst->SelectableRounding = src->SelectableRounding;
            dst->DragDropTargetRounding = src->DragDropTargetRounding;
            dst->DragDropTargetBorderSize = src->DragDropTargetBorderSize;
            dst->DragDropTargetPadding = src->DragDropTargetPadding;
            dst->ColorMarkerSize = src->ColorMarkerSize;
            dst->InputTextCursorSize = src->InputTextCursorSize;
            dst->SeparatorSize = src->SeparatorSize;
            dst->SeparatorTextBorderSize = src->SeparatorTextBorderSize;
            dst->SeparatorTextPadding = src->SeparatorTextPadding;
            dst->DockingSeparatorSize = src->DockingSeparatorSize;
            dst->DisplayWindowPadding = src->DisplayWindowPadding;
            dst->DisplaySafeAreaPadding = src->DisplaySafeAreaPadding;
            dst->MouseCursorScale = src->MouseCursorScale;
        }

        private static void ScalePlotStyleSizes(float scale)
        {
            var dst = ImPlot.GetStyle().Handle;
            dst->PlotDefaultSize = _unscaledPlotStyle.PlotDefaultSize * scale;
            dst->PlotMinSize = _unscaledPlotStyle.PlotMinSize * scale;
            dst->MajorTickLen = _unscaledPlotStyle.MajorTickLen * scale;
            dst->MinorTickLen = _unscaledPlotStyle.MinorTickLen * scale;
            dst->PlotPadding = _unscaledPlotStyle.PlotPadding * scale;
            dst->LabelPadding = _unscaledPlotStyle.LabelPadding * scale;
            dst->LegendPadding = _unscaledPlotStyle.LegendPadding * scale;
            dst->LegendInnerPadding = _unscaledPlotStyle.LegendInnerPadding * scale;
            dst->LegendSpacing = _unscaledPlotStyle.LegendSpacing * scale;
            dst->MousePosPadding = _unscaledPlotStyle.MousePosPadding * scale;
            dst->AnnotationPadding = _unscaledPlotStyle.AnnotationPadding * scale;
            dst->DigitalPadding = _unscaledPlotStyle.DigitalPadding * scale;
            dst->DigitalSpacing = _unscaledPlotStyle.DigitalSpacing * scale;
            // truncate thicknesses to prevent blurriness
            dst->PlotBorderSize = MathF.Truncate(_unscaledPlotStyle.PlotBorderSize * scale);
            dst->MajorTickSize = Vector2.Truncate(_unscaledPlotStyle.MajorTickSize * scale);
            dst->MinorTickSize = Vector2.Truncate(_unscaledPlotStyle.MinorTickSize * scale);
            dst->MajorGridSize = Vector2.Truncate(_unscaledPlotStyle.MajorGridSize * scale);
            dst->MinorGridSize = Vector2.Truncate(_unscaledPlotStyle.MinorGridSize * scale);
        }
    }
}
