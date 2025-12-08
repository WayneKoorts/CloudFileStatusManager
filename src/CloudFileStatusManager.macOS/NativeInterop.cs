using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace CloudFileStatusManager.macOS;

/// <summary>
/// Native Objective-C runtime and Foundation framework interop for macOS.
/// Used to access NSFileManager and NSURL APIs for iCloud file status detection.
/// </summary>
internal static class NativeInterop
{
    private const string FoundationLib = "/System/Library/Frameworks/Foundation.framework/Foundation";
    private const string ObjCLib = "/usr/lib/libobjc.dylib";

    #region Objective-C Runtime

    [DllImport(ObjCLib, EntryPoint = "objc_getClass")]
    internal static extern IntPtr objc_getClass(string className);

    [DllImport(ObjCLib, EntryPoint = "sel_registerName")]
    internal static extern IntPtr sel_registerName(string selectorName);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern bool objc_msgSend_bool(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern bool objc_msgSend_bool(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern bool objc_msgSend_bool(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern bool objc_msgSend_bool(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

    #endregion

    #region Extended Attributes

    private const string LibSystem = "libSystem.dylib";

    /// <summary>
    /// Gets an extended attribute value from a file.
    /// </summary>
    [DllImport(LibSystem, SetLastError = true)]
    internal static extern long getxattr(
        [MarshalAs(UnmanagedType.LPStr)] string path,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        byte[] value,
        long size,
        uint position,
        int options);

    /// <summary>
    /// Removes an extended attribute from a file.
    /// </summary>
    [DllImport(LibSystem, SetLastError = true)]
    internal static extern int removexattr(
        [MarshalAs(UnmanagedType.LPStr)] string path,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        int options);

    #endregion

    #region Foundation Framework Constants

    // These are loaded dynamically from the Foundation framework
    private static IntPtr _foundationHandle;
    private static IntPtr _NSURLUbiquitousItemDownloadingStatusKey;
    private static IntPtr _NSURLUbiquitousItemDownloadingStatusNotDownloaded;
    private static IntPtr _NSURLUbiquitousItemDownloadingStatusDownloaded;
    private static IntPtr _NSURLUbiquitousItemDownloadingStatusCurrent;
    private static IntPtr _NSURLUbiquitousItemIsDownloadingKey;
    private static IntPtr _NSURLIsUbiquitousItemKey;
    private static IntPtr _NSURLUbiquitousItemDownloadRequestedKey;

    [DllImport("libdl.dylib")]
    private static extern IntPtr dlopen(string path, int mode);

    [DllImport("libdl.dylib")]
    private static extern IntPtr dlsym(IntPtr handle, string symbol);

    private const int RTLD_LAZY = 1;

    private static void EnsureFoundationLoaded()
    {
        if (_foundationHandle != IntPtr.Zero) return;

        _foundationHandle = dlopen(FoundationLib, RTLD_LAZY);
        if (_foundationHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to load Foundation framework");
        }

        _NSURLUbiquitousItemDownloadingStatusKey = GetSymbolPointer("NSURLUbiquitousItemDownloadingStatusKey");
        _NSURLUbiquitousItemDownloadingStatusNotDownloaded = GetSymbolPointer("NSURLUbiquitousItemDownloadingStatusNotDownloaded");
        _NSURLUbiquitousItemDownloadingStatusDownloaded = GetSymbolPointer("NSURLUbiquitousItemDownloadingStatusDownloaded");
        _NSURLUbiquitousItemDownloadingStatusCurrent = GetSymbolPointer("NSURLUbiquitousItemDownloadingStatusCurrent");
        _NSURLUbiquitousItemIsDownloadingKey = GetSymbolPointer("NSURLUbiquitousItemIsDownloadingKey");
        _NSURLIsUbiquitousItemKey = GetSymbolPointer("NSURLIsUbiquitousItemKey");
        _NSURLUbiquitousItemDownloadRequestedKey = GetSymbolPointer("NSURLUbiquitousItemDownloadRequestedKey");
    }

    private static IntPtr GetSymbolPointer(string symbolName)
    {
        var symbolPtr = dlsym(_foundationHandle, symbolName);
        if (symbolPtr == IntPtr.Zero) return IntPtr.Zero;
        return Marshal.ReadIntPtr(symbolPtr);
    }

    internal static IntPtr NSURLUbiquitousItemDownloadingStatusKey
    {
        get { EnsureFoundationLoaded(); return _NSURLUbiquitousItemDownloadingStatusKey; }
    }

    internal static IntPtr NSURLUbiquitousItemDownloadingStatusNotDownloaded
    {
        get { EnsureFoundationLoaded(); return _NSURLUbiquitousItemDownloadingStatusNotDownloaded; }
    }

    internal static IntPtr NSURLUbiquitousItemDownloadingStatusDownloaded
    {
        get { EnsureFoundationLoaded(); return _NSURLUbiquitousItemDownloadingStatusDownloaded; }
    }

    internal static IntPtr NSURLUbiquitousItemDownloadingStatusCurrent
    {
        get { EnsureFoundationLoaded(); return _NSURLUbiquitousItemDownloadingStatusCurrent; }
    }

    internal static IntPtr NSURLUbiquitousItemIsDownloadingKey
    {
        get { EnsureFoundationLoaded(); return _NSURLUbiquitousItemIsDownloadingKey; }
    }

    internal static IntPtr NSURLIsUbiquitousItemKey
    {
        get { EnsureFoundationLoaded(); return _NSURLIsUbiquitousItemKey; }
    }

    internal static IntPtr NSURLUbiquitousItemDownloadRequestedKey
    {
        get { EnsureFoundationLoaded(); return _NSURLUbiquitousItemDownloadRequestedKey; }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates an NSString from a C# string.
    /// </summary>
    internal static IntPtr CreateNSString(string str)
    {
        var nsStringClass = objc_getClass("NSString");
        var allocSel = sel_registerName("alloc");
        var initWithUTF8StringSel = sel_registerName("initWithUTF8String:");

        var allocated = objc_msgSend(nsStringClass, allocSel);
        var strPtr = Marshal.StringToHGlobalAnsi(str);
        try
        {
            return objc_msgSend(allocated, initWithUTF8StringSel, strPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(strPtr);
        }
    }

    /// <summary>
    /// Creates an NSURL from a file path string.
    /// </summary>
    internal static IntPtr CreateNSURLFromPath(string filePath)
    {
        var nsUrlClass = objc_getClass("NSURL");
        var fileURLWithPathSel = sel_registerName("fileURLWithPath:");
        var nsPath = CreateNSString(filePath);
        return objc_msgSend(nsUrlClass, fileURLWithPathSel, nsPath);
    }

    /// <summary>
    /// Converts an NSString to a C# string.
    /// </summary>
    internal static string? NSStringToString(IntPtr nsString)
    {
        if (nsString == IntPtr.Zero) return null;

        var utf8StringSel = sel_registerName("UTF8String");
        var utf8Ptr = objc_msgSend(nsString, utf8StringSel);
        return Marshal.PtrToStringAnsi(utf8Ptr);
    }

    /// <summary>
    /// Checks if two NSString/NSObject instances are equal.
    /// </summary>
    internal static bool NSObjectEquals(IntPtr obj1, IntPtr obj2)
    {
        if (obj1 == IntPtr.Zero || obj2 == IntPtr.Zero) return false;
        var isEqualSel = sel_registerName("isEqual:");
        return objc_msgSend_bool(obj1, isEqualSel, obj2);
    }

    /// <summary>
    /// Gets a boolean value from an NSNumber.
    /// </summary>
    internal static bool NSNumberToBool(IntPtr nsNumber)
    {
        if (nsNumber == IntPtr.Zero) return false;
        var boolValueSel = sel_registerName("boolValue");
        return objc_msgSend_bool(nsNumber, boolValueSel);
    }

    /// <summary>
    /// Gets the shared NSFileManager instance.
    /// </summary>
    internal static IntPtr GetDefaultFileManager()
    {
        var fileManagerClass = objc_getClass("NSFileManager");
        var defaultManagerSel = sel_registerName("defaultManager");
        return objc_msgSend(fileManagerClass, defaultManagerSel);
    }

    #endregion
}
