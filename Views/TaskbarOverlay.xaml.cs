using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DesktopOrganizer
{
    public partial class TaskbarOverlay : Window
    {
        private const int GwlExStyle = -20;

        private const int WsExTransparent =
            0x00000020;

        private const int WsExToolWindow =
            0x00000080;

        private const int WsExNoActivate =
            0x08000000;

        private const uint SwpNoActivate =
            0x0010;

        private const uint SwpShowWindow =
            0x0040;

        private static readonly IntPtr HwndTopmost =
            new(-1);

        private IntPtr overlayHandle;

        private readonly DispatcherTimer positionTimer =
            new()
            {
                Interval =
                    TimeSpan.FromMilliseconds(75)
            };

        public TaskbarOverlay()
        {
            InitializeComponent();

            SourceInitialized +=
                TaskbarOverlay_SourceInitialized;

            Loaded +=
                TaskbarOverlay_Loaded;

            Closed +=
                TaskbarOverlay_Closed;

            positionTimer.Tick +=
                PositionTimer_Tick;
        }

        private void TaskbarOverlay_SourceInitialized(
            object? sender,
            EventArgs e)
        {
            overlayHandle =
                new WindowInteropHelper(this)
                    .Handle;

            int style =
                GetWindowLong(
                    overlayHandle,
                    GwlExStyle
                );

            SetWindowLong(
                overlayHandle,
                GwlExStyle,
                style |
                WsExTransparent |
                WsExToolWindow |
                WsExNoActivate
            );
        }

        private void TaskbarOverlay_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            UpdateOverlay();

            positionTimer.Start();
        }

        private void PositionTimer_Tick(
            object? sender,
            EventArgs e)
        {
            UpdateOverlay();
        }

        private void UpdateOverlay()
        {
            IntPtr taskbar =
                FindWindow(
                    "Shell_TrayWnd",
                    null
                );

            if (taskbar == IntPtr.Zero)
            {
                return;
            }

            if (!GetWindowRect(
                    taskbar,
                    out RectNative rect))
            {
                return;
            }

            SetWindowPos(
                overlayHandle,
                HwndTopmost,
                rect.Left,
                rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top,
                SwpNoActivate |
                SwpShowWindow
            );
        }

        private void TaskbarOverlay_Closed(
            object? sender,
            EventArgs e)
        {
            positionTimer.Stop();
        }

        [DllImport(
            "user32.dll",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern IntPtr FindWindow(
            string lpClassName,
            string? lpWindowName
        );

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        private static extern bool GetWindowRect(
            IntPtr hwnd,
            out RectNative rect
        );

        [DllImport(
            "user32.dll",
            EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong(
            IntPtr hwnd,
            int index
        );

        [DllImport(
            "user32.dll",
            EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong(
            IntPtr hwnd,
            int index,
            int newStyle
        );

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hwnd,
            IntPtr hwndInsertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags
        );

        [StructLayout(
            LayoutKind.Sequential)]
        private struct RectNative
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}