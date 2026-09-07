using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MultiCoD_FoV_Changer
{
#if WIN64
    using dword_ptr = UInt64;
#else
    using dword_ptr = UInt32;
#endif

    public static class Memory
    {
        #region DLLImports

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process(IntPtr hProc, out bool bIsX86);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(IntPtr hProc, dword_ptr lpBaseAddress, [Out] byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(IntPtr hProc, dword_ptr lpBaseAddress, out dword_ptr dwBuffer, int nSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteProcessMemory(IntPtr hProc, dword_ptr lpBaseAddress, [In] byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        #endregion

        #region Variables

        static dword_ptr baseAddr;
        static IntPtr hProc;
        static int ptrSize;
        static dword_ptr dvarValueOffset;

        #endregion

        #region Methods

        public static void Init(int pid, dword_ptr baseAddr)
        {
            Memory.baseAddr = baseAddr;

            if (hProc != IntPtr.Zero)
            {
                CloseHandle(hProc);
            }

            try
            {
                const uint dwDesiredAccess = 0x0008 | 0x0010 | 0x0020; // PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE
                hProc = OpenProcess(dwDesiredAccess, false, pid);
            }
            catch (Exception e)
            {
                throw new Exception($"OpenProcess failed.\n{e.Message}", e);
            }

            if (hProc == IntPtr.Zero)
            {
                throw new Exception("OpenProcess failed.");
            }

            try
            {
                IsWow64Process(hProc, out bool isX86);
                ptrSize = isX86 ? sizeof(Int32) : sizeof(Int64);
            }
            catch (Exception e)
            {
                throw new Exception($"IsWow64Process failed.\n{e.Message}", e);
            }
        }

        public static void Reset()
        {
            if (hProc != IntPtr.Zero)
            {
                CloseHandle(hProc);
            }

            hProc = IntPtr.Zero;
        }

        private static int ReadBytes(dword_ptr addr, int length, out byte[] buffer)
        {
            buffer = new byte[length];

            try
            {
                ReadProcessMemory(hProc, addr, buffer, length, out int lpNumberOfBytesRead);
                return lpNumberOfBytesRead;
            }
            catch (Exception e)
            {
                throw new Exception($"ReadProcessMemory failed for address: 0x{addr:X}\n{e.Message}", e);
            }
        }

        private static int WriteBytes(dword_ptr addr, byte[] bytes)
        {
            try
            {
                WriteProcessMemory(hProc, addr, bytes, bytes.Length, out int lpNumberOfBytesWritten);
                return lpNumberOfBytesWritten;
            }
            catch (Exception e)
            {
                throw new Exception($"WRiteProcessMemory failed for address: 0x{addr:X}\n{e.Message}", e);
            }
        }

        public static bool FindDvarAddress(string name, out dword_ptr addr, out int step)
        {
            addr = 0;
            step = 0;
            bool isFound = false;
            byte[] nameBytes = Encoding.ASCII.GetBytes(name + '\0');

            if (dvarValueOffset == 0 && name != "cg_fov")
            {
                throw new Exception("Must call FindDvarAddress for cg_fov first and foremost to compute the dvarValueOffset");
            }

            if (hProc != IntPtr.Zero)
            {
                step = 1;
                ReadBytes(baseAddr, Constants.c_memReadRange, out byte[] buffer);

                step = 2;
                dword_ptr dvarNameAddr = 0;
                int maxOffset = buffer.Length - nameBytes.Length;

                // Search dvar name
                for (int i = 0; i < maxOffset; i += sizeof(int))
                {
                    bool match = true;
                    for (int j = 0; j < nameBytes.Length; j++)
                    {
                        if (buffer[i + j] != nameBytes[j])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        step = 3;
                        dvarNameAddr = baseAddr + (dword_ptr)i;
                        break;
                    }
                }

                if (dvarNameAddr > 0)
                {
                    step = 4;
                    int startIndex = (int)(dvarNameAddr - baseAddr);
                    int maxIndex = buffer.Length - sizeof(dword_ptr);
                    var toUInt = (ptrSize == sizeof(int)) ? (Func<byte[], int, ulong>)((bytes, start) => BitConverter.ToUInt32(bytes, start)) : BitConverter.ToUInt64;

                    // Search dvar structure
                    for (int i = startIndex; i <= maxIndex; i += sizeof(int))
                    {
                        if (toUInt(buffer, i) == dvarNameAddr)
                        {
                            step = 5;
                            addr = baseAddr + (dword_ptr)i + dvarValueOffset;
                            isFound = true;
                            break;
                        }
                    }
                }

                // Figure out the value offset via dvar_t's "DvarValue reset" from cg_fov
                if (addr > 0 && dvarValueOffset == 0 && name == "cg_fov")
                {
                    step = 6;
                    int iDvarStruct = (int)(addr - baseAddr);

                    // "DvarValue reset" should normally always be within this range
                    for (int iValOffset = 0x3C; iValOffset >= 0x28; iValOffset -= sizeof(int))
                    {
                        if (BitConverter.ToSingle(buffer, iDvarStruct + iValOffset) == 65f)
                        {
                            step = 7;
                            dvarValueOffset = (dword_ptr)(iValOffset - 0x20);
                            addr += dvarValueOffset;
                            break;
                        }
                    }

                    if (dvarValueOffset == 0)
                    {
                        step = 8;
                        isFound = false; // failure
                    }
                }
            }

            return isFound;
        }

        // Type-specific wrappers

        public static float ReadFloat(dword_ptr addr)
        {
            ReadBytes(addr, sizeof(float), out byte[] buffer);
            return BitConverter.ToSingle(buffer, 0);
        }

        public static void WriteFloat(dword_ptr addr, float val)
        {
            WriteBytes(addr, BitConverter.GetBytes(val));
        }

        public static int ReadInt(dword_ptr addr)
        {
            ReadBytes(addr, sizeof(int), out byte[] buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static void WriteInt(dword_ptr addr, int val)
        {
            WriteBytes(addr, BitConverter.GetBytes(val));
        }

        #endregion
    }
}
