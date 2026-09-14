using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

public static class PInvokeAPI
{
    #region DLLImports

    // --- STANDARD WIN32 API ---

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION
    {
        public UIntPtr ExitStatus;
        public UIntPtr PebBaseAddress;
        public UIntPtr AffinityMask;
        public UIntPtr BasePriority;
        public UIntPtr UniqueProcessId;
        public UIntPtr InheritedFromUniqueProcessId;
    }

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtQueryInformationProcess(IntPtr hProcess, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtQueryInformationProcess(IntPtr hProcess, int processInformationClass, out UIntPtr processInformation, int processInformationLength, out int returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsWow64Process(IntPtr hProcess, out bool isWow64);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, out UIntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [In] byte[] lpBuffer, UIntPtr nSize, out UIntPtr lpNumberOfBytesWritten);

    // --- WOW64 INTERNAL APIS (For x64 Targets) ---

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION64
    {
        public ulong ExitStatus;
        public ulong PebBaseAddress;
        public ulong AffinityMask;
        public ulong BasePriority;
        public ulong UniqueProcessId;
        public ulong InheritedFromUniqueProcessId;
    }

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtWow64QueryInformationProcess64(IntPtr hProcess, int processInformationClass, out PROCESS_BASIC_INFORMATION64 processInformation, int processInformationLength, out int returnLength);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtWow64ReadVirtualMemory64(IntPtr hProcess, ulong lpBaseAddress, [Out] byte[] lpBuffer, ulong nSize, out ulong lpNumberOfBytesRead);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtWow64WriteVirtualMemory64(IntPtr hProcess, ulong lpBaseAddress, [In] byte[] lpBuffer, ulong nSize, out ulong lpNumberOfBytesWritten);

    #endregion

    #region Methods

    public static bool IsTargetProcess64Bit(IntPtr hProc)
    {
        if (Environment.Is64BitOperatingSystem && IsWow64Process(hProc, out bool isWow64))
        {
            return !isWow64;
        }

        return false;
    }

    public static ulong GetBaseAddress(IntPtr hProc)
    {
        bool isHost64 = Environment.Is64BitProcess;

        // 1. Determine if target is a 64-bit process
        bool isTarget64 = Environment.Is64BitOperatingSystem;
        if (isTarget64 && IsWow64Process(hProc, out bool isWow64))
        {
            isTarget64 = !isWow64;
        }

        byte[] buffer = new byte[sizeof(long)];

        // 32-bit Host -> 64-bit Target
        // Must use the NtWow64 API family to break out of 32-bit isolation
        if (!isHost64 && isTarget64)
        {
            var pbi64 = new PROCESS_BASIC_INFORMATION64();
            int status = NtWow64QueryInformationProcess64(hProc, 0 /*ProcessBasicInformation*/, out pbi64, Marshal.SizeOf(pbi64), out _);
            if (status != 0)
                throw new Exception($"NtWow64QueryInformationProcess64 failed: 0x{status:X8}");

            status = NtWow64ReadVirtualMemory64(hProc, pbi64.PebBaseAddress + 0x10, buffer, sizeof(long), out _);
            if (status != 0)
                throw new Exception($"NtWow64ReadVirtualMemory64 failed: 0x{status:X8}");
        }
        // 64-bit Host -> 32-bit Target (WoW64)
        // Must pass ProcessWow64Information (26) to request the 32-bit PEB pointer
        else if (isHost64 && !isTarget64)
        {
            int status = NtQueryInformationProcess(hProc, 26 /*ProcessWow64Information*/, out UIntPtr peb32Address, UIntPtr.Size, out _);
            if (status != 0)
                throw new Exception($"NtQueryInformationProcess(ProcessWow64Information) failed: 0x{status:X8}");

            if (!ReadProcessMemory(hProc, UIntPtr.Add(peb32Address, 0x08), buffer, (UIntPtr)sizeof(int), out _))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        // Native matching bitness (64-to-64 or 32-to-32)
        // Standard NtQueryInformationProcess (0) retrieves the matching PEB
        else
        {
            var pbi = new PROCESS_BASIC_INFORMATION();
            int status = NtQueryInformationProcess(hProc, 0 /*ProcessBasicInformation*/, ref pbi, Marshal.SizeOf(pbi), out _);
            if (status != 0)
                throw new Exception($"NtQueryInformationProcess failed: 0x{status:X8}");

            int offset = isTarget64 ? 0x10 : 0x08;
            var readSize = (UIntPtr)(isTarget64 ? sizeof(long) : sizeof(int));

            if (!ReadProcessMemory(hProc, UIntPtr.Add(pbi.PebBaseAddress, offset), buffer, readSize, out _))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return isTarget64 ? BitConverter.ToUInt64(buffer, 0) : BitConverter.ToUInt32(buffer, 0);
    }

    public static int ReadMemory(IntPtr hProc, ulong addr, byte[] buf, int len)
    {
        try
        {
            if (!Environment.Is64BitProcess && IsTargetProcess64Bit(hProc))
            {
                // 32 to 64
                NtWow64ReadVirtualMemory64(hProc, addr, buf, (ulong)len, out var bytesTotal);
                return (int)bytesTotal;
            }
            else
            {
                // 32 to 32 / 64 to 64 / 64 to 32
                ReadProcessMemory(hProc, (UIntPtr)addr, buf, (UIntPtr)len, out var bytesTotal);
                return (int)bytesTotal;
            }
        }
        catch (Exception e)
        {
            throw new Exception($"ReadMemory failed for address 0x{addr:X}\n{e}");
        }
    }

    public static int WriteMemory(IntPtr hProc, ulong addr, byte[] buf, int len)
    {
        try
        {
            if (!Environment.Is64BitProcess && IsTargetProcess64Bit(hProc))
            {
                // 32 to 64
                NtWow64WriteVirtualMemory64(hProc, addr, buf, (ulong)len, out var bytesTotal);
                return (int)bytesTotal;
            }
            else
            {
                // 32 to 32 / 64 to 64 / 64 to 32
                WriteProcessMemory(hProc, (UIntPtr)addr, buf, (UIntPtr)len, out var bytesTotal);
                return (int)bytesTotal;
            }
        }
        catch (Exception e)
        {
            throw new Exception($"WriteMemory failed for address 0x{addr:X}\n{e}");
        }
    }

    #endregion
}
