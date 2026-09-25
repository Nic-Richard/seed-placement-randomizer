using System.Runtime.InteropServices;

namespace SeedPlacement.App.Controls;

/// <summary>Reads the operating system's "reduce motion" preference once at startup.</summary>
public static partial class ReducedMotion
{
    public static bool IsRequested { get; } = Detect();

    private static bool Detect()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                // SPI_GETCLIENTAREAANIMATION reports the "Animation effects" switch in Accessibility settings.
                return SystemParametersInfo(0x1042, 0, out var enabled, 0) && !enabled;
            }
            if (OperatingSystem.IsMacOS())
            {
                var workspace = objc_msgSend(objc_getClass("NSWorkspace"), sel_registerName("sharedWorkspace"));
                return workspace != IntPtr.Zero &&
                    objc_msgSend_bool(workspace, sel_registerName("accessibilityDisplayShouldReduceMotion"));
            }
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
        }
        return false;
    }

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfo(uint action, uint param, [MarshalAs(UnmanagedType.Bool)] out bool value, uint winIni);

    [LibraryImport("/usr/lib/libobjc.dylib", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr objc_getClass(string name);

    [LibraryImport("/usr/lib/libobjc.dylib", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr sel_registerName(string name);

    [LibraryImport("/usr/lib/libobjc.dylib")]
    private static partial IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool objc_msgSend_bool(IntPtr receiver, IntPtr selector);
}
