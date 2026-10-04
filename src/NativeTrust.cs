using System;
using System.Runtime.InteropServices;

namespace NorthStarCodeSigner {
// Read the native result rather than guessing from PowerShell's broad status
// categories or localized error messages. No trust store is changed.
public static class NativeTrust {
    public const int UntrustedRoot = unchecked((int)0x800B0109);
    public static bool AcceptableForSigning(int result) {
        return result == 0 || result == UntrustedRoot;
    }
    public static string Code(int result) { return "0x" + unchecked((uint)result).ToString("X8"); }
    [StructLayout(LayoutKind.Sequential)]
    struct FileInfo {
        public uint Size;
        public IntPtr Path;
        public IntPtr File;
        public IntPtr KnownSubject;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct TrustData {
        public uint Size;
        public IntPtr PolicyCallback;
        public IntPtr SipClient;
        public uint UiChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public IntPtr FileInfo;
        public uint StateAction;
        public IntPtr StateData;
        public IntPtr UrlReference;
        public uint ProviderFlags;
        public uint UiContext;
        public IntPtr SignatureSettings;
    }
    [DllImport("wintrust.dll", ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
    static extern int WinVerifyTrust(IntPtr window, ref Guid action, ref TrustData data);
    public static int Verify(string path) {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            throw new PlatformNotSupportedException("Native signature verification requires Windows.");
        var action = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");
        IntPtr name = Marshal.StringToCoTaskMemUni(path);
        IntPtr file = IntPtr.Zero;
        var data = new TrustData();
        try {
            var info = new FileInfo { Size = (uint)Marshal.SizeOf(typeof(FileInfo)), Path = name };
            file = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FileInfo)));
            Marshal.StructureToPtr(info, file, false);
            data.Size = (uint)Marshal.SizeOf(typeof(TrustData));
            data.UiChoice = 2; // WTD_UI_NONE
            data.UnionChoice = 1; // WTD_CHOICE_FILE: embedded PE signature
            data.FileInfo = file;
            data.StateAction = 1; // WTD_STATEACTION_VERIFY
            return WinVerifyTrust(new IntPtr(-1), ref action, ref data);
        } finally {
            try {
                if (data.StateData != IntPtr.Zero) {
                    data.StateAction = 2; // WTD_STATEACTION_CLOSE
                    WinVerifyTrust(new IntPtr(-1), ref action, ref data);
                }
            } finally {
                if (file != IntPtr.Zero) Marshal.FreeHGlobal(file);
                Marshal.FreeCoTaskMem(name);
            }
        }
    }
}
}
