using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class MemoryAPI
{
    #region DLLImports

    // --- STANDARD WIN32 API ---

    [DllImport("kernel32.dll")]
    private static extern bool IsWow64Process(IntPtr hProc, out bool isWow64);

    [DllImport("kernel32.dll")]
    public static extern bool ReadProcessMemory(IntPtr hProc, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, out UIntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    private static extern bool WriteProcessMemory(IntPtr hProc, UIntPtr lpBaseAddress, [In] byte[] lpBuffer, UIntPtr nSize, out UIntPtr lpNumberOfBytesWritten);

    // --- WOW64 INTERNAL APIS (For x64 Targets) ---

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION64
    {
        public long ExitStatus;
        public long PebBaseAddress; // Pointer to the 64-bit PEB
        public long AffinityMask;
        public long BasePriority;
        public long UniqueProcessId;
        public long InheritedFromUniqueProcessId;
    }

    [DllImport("ntdll.dll")]
    private static extern int NtWow64QueryInformationProcess64(IntPtr hProc, int processInformationClass, ref PROCESS_BASIC_INFORMATION64 processInformation, int processInformationLength, out int returnLength);

    [DllImport("ntdll.dll")]
    private static extern int NtWow64ReadVirtualMemory64(IntPtr hProc, ulong lpBaseAddress64, [Out] byte[] lpBuffer, ulong nSize, out ulong lpNumberOfBytesRead);

    [DllImport("ntdll.dll")]
    private static extern int NtWow64WriteVirtualMemory64(IntPtr hProc, ulong lpBaseAddress64, [In] byte[] lpBuffer, ulong nSize, out ulong lpNumberOfBytesWritten);

    #endregion

    public static bool IsTarget64Bit(IntPtr hProc)
    {
        // On a 64-bit OS, a 32-bit app returns true for IsWow64Process. A 64-bit native app returns false.
        if (IsWow64Process(hProc, out bool isWow64))
        {
            return !isWow64;
        }

        return false;
    }

    public static ulong GetBaseAddress(Process proc, IntPtr hProc)
    {
        if (!Environment.Is64BitProcess && IsTarget64Bit(hProc))
        {
            // TARGET IS 64-BIT: Use the WOW64 specific API

            PROCESS_BASIC_INFORMATION64 pbi = new PROCESS_BASIC_INFORMATION64();
            const int ProcessBasicInformation = 0;

            // 1. Get the 64-bit PEB address of the target process
            int status = NtWow64QueryInformationProcess64(hProc, ProcessBasicInformation, ref pbi, Marshal.SizeOf(pbi), out _);
            if (status != 0)
                throw new Exception($"NtWow64QueryInformationProcess64 failed with status: 0x{status:X}");

            // In a 64-bit PEB structure, ImageBaseAddress is at offset 16 (0x10)
            ulong imageBaseAddressOffset = (ulong)(pbi.PebBaseAddress + 16);
            byte[] buffer = new byte[8]; // 64-bit pointers require 8 bytes

            // 2. Read the actual 64-bit Base Address from the target process's memory space
            status = NtWow64ReadVirtualMemory64(hProc, imageBaseAddressOffset, buffer, (ulong)buffer.Length, out _);
            if (status != 0)
                throw new Exception($"NtWow64ReadVirtualMemory64 failed with status: 0x{status:X}");

            // Convert the 8-byte buffer to an unsigned 64-bit integer
            return BitConverter.ToUInt64(buffer, 0);
        }
        else
        {
            // We match the target, or we are 64-bit: use native API
            return (ulong)proc.MainModule.BaseAddress.ToInt64();
        }
    }

    public static bool ReadMemory(IntPtr hProc, ulong targetAddress, byte[] buffer)
    {
        if (!Environment.Is64BitProcess && IsTarget64Bit(hProc))
        {
            // TARGET IS 64-BIT: Use the WOW64 specific API
            int status = NtWow64ReadVirtualMemory64(hProc, targetAddress, buffer, (ulong)buffer.Length, out _);
            // (Note: For arbitrary buffers, you would adjust the signature to pass a byte array)
            return status == 0;
        }
        else
        {
            // We match the target, or we are 64-bit: use native API
            return ReadProcessMemory(hProc, (UIntPtr)targetAddress, buffer, (UIntPtr)buffer.Length, out _);
        }
    }

    public static bool WriteMemory(IntPtr hProc, ulong targetAddress, byte[] data)
    {
        if (!Environment.Is64BitProcess && IsTarget64Bit(hProc))
        {
            // TARGET IS 64-BIT: Use the WOW64 specific API
            int status = NtWow64WriteVirtualMemory64(hProc, targetAddress, data, (ulong)data.Length, out _);
            return status == 0;
        }
        else
        {
            // We match the target, or we are 64-bit: use native API
            return WriteProcessMemory(hProc, (UIntPtr)targetAddress, data, (UIntPtr)data.Length, out _);
        }
    }
}
