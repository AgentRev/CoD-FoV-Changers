using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Ghosts_FoV_Changer
{
#if WIN64
    using dword_ptr = Int64;
#else
    using dword_ptr = Int32;
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

        #region Constants

        const uint READ = 0x0410; // PROCESS_VM_READ | PROCESS_QUERY_INFORMATION
        const uint WRITE = 0x0038; // PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ

        #endregion

        #region Variables

        byte pOffset;
        dword_ptr memReadRange;
        int pid;
        dword_ptr baseAddr;
        dword_ptr varAddr;
        byte[] cVar;

        #endregion

        public Memory(string cVar, int pid, dword_ptr baseAddr, dword_ptr memReadRange, byte pOffset)
        {
            this.cVar = Encoding.ASCII.GetBytes(cVar + '\0');
            this.pid = pid;
            this.baseAddr = baseAddr;
            this.memReadRange = memReadRange;
            this.pOffset = pOffset;
        }

        #region Methods

        public bool FindFoVOffset(ref dword_ptr pFoV, ref byte step)
        {
            IntPtr hProc;
            bool isFound = false;

            try { hProc = OpenProcess(READ, false, pid); step = 0; }
            catch (Exception e) { throw new Exception(String.Format("Failed to open the process handle during a FindFoVOffset statement;\n{0}", e.Message)); }

            if (hProc != IntPtr.Zero)
            {
                step = 2;
                byte[] buffer = new byte[memReadRange];

                try { ReadProcessMemory(hProc, baseAddr, buffer, (int)memReadRange, 0); }
                catch (Exception e) { step = 3; throw new Exception(String.Format("Failed to read process memory during a FindFoVOffset statement\nWin32 Error: {0}", e.Message)); }

                if (varAddr == 0)
                {
                    step = 4;
                    int maxOffset = buffer.Length - cVar.Length;

                    for (int i = 0; i < maxOffset; i += sizeof(Int32))
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
                            step = 5;
                            varAddr = baseAddr + i;
                            break;
                        }
                    }
                }

                if (varAddr > 0)
                {
                    int testIndex = (int)(pFoV - pOffset - baseAddr);
                    dword_ptr testCurrentPtr = 0;

                    if (testIndex >= 0 && testIndex <= buffer.Length - sizeof(dword_ptr))
                    {
                        step = 14;
#if WIN64
                        testCurrentPtr = BitConverter.ToInt64(buffer, testIndex);
#else
                        testCurrentPtr = BitConverter.ToInt32(buffer, testIndex);
#endif
                    }

                    if (testCurrentPtr == varAddr)
                    {
                        step = 15;
                        isFound = true;
                    }
                    else
                    {
                        step = 9;
                        int startIndex = (int)(varAddr - baseAddr);
                        int maxIndex = buffer.Length - sizeof(dword_ptr);

                        for (int i = startIndex; i <= maxIndex; i += sizeof(Int32))
                        {
#if WIN64
                            if (BitConverter.ToInt64(buffer, i) == varAddr)
#else
                            if (BitConverter.ToInt32(buffer, i) == varAddr)
#endif
                            {
                                step = 10;
                                pFoV = baseAddr + i + pOffset;
                                isFound = true;
                                break;
                            }
                        }
                    }
                }

                CloseHandle(hProc);
            }

            return isFound;
        }

        public float ReadFloat(dword_ptr ptr)
        {
            byte[] buffer = new byte[sizeof(float)];
            IntPtr hProc;

            try { hProc = OpenProcess(READ, false, pid); }
            catch (Exception e) { throw new Exception(String.Format("Failed to open the process handle during a ReadFloat statement;\n{0}", e.Message)); }

            if (hProc != IntPtr.Zero)
            {
                try { ReadProcessMemory(hProc, ptr, buffer, sizeof(float), 0); }
                catch (Exception e) { throw new Exception(String.Format("Failed to read process memory during a ReadFloat statement; Address = {0:X}\nWin32 Error: {1}", ptr, e.Message)); }

                CloseHandle(hProc);
            }

            return BitConverter.ToSingle(buffer, 0);
        }

        public void WriteFloat(dword_ptr ptr, float val)
        {
            IntPtr hProc;

            try { hProc = OpenProcess(WRITE, false, pid); }
            catch (Exception e) { throw new Exception(String.Format("Failed to open the process handle during a WriteFloat statement;\n{0}", e.Message)); }

            if (hProc != IntPtr.Zero)
            {
                try { WriteProcessMemory(hProc, ptr, BitConverter.GetBytes(val), sizeof(float), 0); }
                catch (Exception e) { throw new Exception(String.Format("Failed to write to process memory during a WriteFloat statement; Address = {0:X}\nWin32 Error: {1}", ptr, e.Message)); }

                CloseHandle(hProc);
            }
        }

        #endregion
    }
}
