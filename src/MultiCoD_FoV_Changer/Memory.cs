using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace MultiCoD_FoV_Changer
{
#if WIN64
    using dword_ptr = UInt64;
#else
    using dword_ptr = UInt32;
#endif

    public class Memory
    {
        #region DLLImports

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(IntPtr hProc, dword_ptr lpBaseAddress, [Out] byte[] lpBuffer, int nSize, [Out] int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(IntPtr hProc, dword_ptr lpBaseAddress, out dword_ptr lpBuffer, int nSize, [Out] int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteProcessMemory(IntPtr hProc, dword_ptr lpBaseAddress, [In] byte[] lpBuffer, int nSize, [Out] int lpNumberOfBytesWritten);

        #endregion

        #region Variables

        byte dvarValueOffset;
        dword_ptr memReadRange;
        int pid;
        dword_ptr baseAddr;
        dword_ptr currDvarAddr;
        byte[] cVar;
        IntPtr hProc = IntPtr.Zero;

        #endregion

        public Memory(string cVar, int pid, dword_ptr baseAddr, dword_ptr memReadRange, byte dvarValueOffset)
        {
            this.cVar = Encoding.ASCII.GetBytes(cVar + '\0');
            this.pid = pid;
            this.baseAddr = baseAddr;
            this.memReadRange = memReadRange;
            this.dvarValueOffset = dvarValueOffset;

            const uint dwDesiredAccess = 0x0008 | 0x0010 | 0x0020; // PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE

            try
            {
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
        }

        ~Memory()
        {
            if (hProc != IntPtr.Zero)
            {
                CloseHandle(hProc);
            }
        }

        #region Methods

        public bool FindDvarAddress(ref dword_ptr dvarAddr, ref byte step)
        {
            bool isFound = false;

            if (hProc != IntPtr.Zero)
            {
                step = 1;
                byte[] buffer = new byte[memReadRange];

                try { ReadProcessMemory(hProc, baseAddr, buffer, (int)memReadRange, 0); }
                catch (Exception e) { step = 2; throw new Exception(String.Format("Failed to read process memory during a FindDvarAddress statement\nWin32 Error: {0}", e.Message)); }

                if (currDvarAddr == 0)
                {
                    step = 3;
                    int maxOffset = buffer.Length - cVar.Length;

                    for (int i = 0; i < maxOffset; i += sizeof(int))
                    {
                        bool match = true;
                        for (int j = 0; j < cVar.Length; j++)
                        {
                            if (buffer[i + j] != cVar[j])
                            {
                                match = false;
                                break;
                            }
                        }

                        if (match)
                        {
                            step = 4;
                            currDvarAddr = baseAddr + (dword_ptr)i;
                            break;
                        }
                    }
                }

                if (currDvarAddr > 0)
                {
                    int prevAddrIndex = (int)(dvarAddr - dvarValueOffset - baseAddr);
                    dword_ptr prevAddr = 0;

                    if (prevAddrIndex >= 0 && prevAddrIndex <= buffer.Length - sizeof(dword_ptr))
                    {
                        step = 5;
#if WIN64
                        prevAddr = BitConverter.ToUInt64(buffer, prevAddrIndex);
#else
                        prevAddr = BitConverter.ToUInt32(buffer, prevAddrIndex);
#endif
                    }

                    if (currDvarAddr == prevAddr)
                    {
                        step = 6;
                        isFound = true;
                    }
                    else
                    {
                        step = 7;
                        int startIndex = (int)(currDvarAddr - baseAddr);
                        int maxIndex = buffer.Length - sizeof(dword_ptr);

                        for (int i = startIndex; i <= maxIndex; i += sizeof(int))
                        {
#if WIN64
                            if (BitConverter.ToUInt64(buffer, i) == currDvarAddr)
#else
                            if (BitConverter.ToUInt32(buffer, i) == currDvarAddr)
#endif
                            {
                                step = 8;
                                dvarAddr = baseAddr + (dword_ptr)i + dvarValueOffset;
                                isFound = true;
                                break;
                            }
                        }
                    }
                }
            }

            return isFound;
        }

        private void ReadBytes(dword_ptr addr, int length, out byte[] buffer)
        {
            buffer = new byte[length];

            try
            {
                ReadProcessMemory(hProc, addr, buffer, length, 0);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to read process memory at Address: 0x{addr:X}\nWin32 Error: {e.Message}", e);
            }
        }

        private void WriteBytes(dword_ptr addr, byte[] bytes)
        {
            try
            {
                WriteProcessMemory(hProc, addr, bytes, bytes.Length, 0);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to write process memory at Address: 0x{addr:X}\nWin32 Error: {e.Message}", e);
            }
        }

        // Type-specific wrappers

        public float ReadFloat(dword_ptr addr)
        {
            ReadBytes(addr, sizeof(float), out byte[] buffer);
            return BitConverter.ToSingle(buffer, 0);
        }

        public void WriteFloat(dword_ptr addr, float val)
        {
            WriteBytes(addr, BitConverter.GetBytes(val));
        }

        public int ReadInt(dword_ptr addr)
        {
            ReadBytes(addr, sizeof(int), out byte[] buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        public void WriteInt(dword_ptr addr, int val)
        {
            WriteBytes(addr, BitConverter.GetBytes(val));
        }

        #endregion
    }
}
