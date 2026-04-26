using System;
using System.Runtime.InteropServices;

namespace Avayomi.Utilities;

public static class WindowsNativeHelper
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    // ReSharper disable once InconsistentNaming
    private const int SW_RESTORE = 9;

    public static void RestoreAndFocus(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
            return;

        // Restore if minimized
        ShowWindow(windowHandle, SW_RESTORE);
        // Bring to front
        SetForegroundWindow(windowHandle);
    }
}
