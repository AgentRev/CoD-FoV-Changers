using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace MW2_mp_fov
{
    class Memory
    {
        #region Constants

        const int READ = 0x10; // PROCESS_VM_READ
        const int WRITE = 0x28; // PROCESS_VM_OPERATION | PROCESS_VM_WRITE

        const uint searchTextRegion = 0x600000;
        const uint searchRegion = 0x800000;
        const uint searchRegionBefore = 0x200000;

        #endregion

        #region Variables

        byte searchRange;
        int pid;
        uint baseAddr;
        uint varAddr;
        byte[] cVar;

        #endregion

        #region Constructor

        public Memory(string cVar, int pid, uint baseAddr, byte searchRange)
        {
            this.cVar = Encoding.ASCII.GetBytes(cVar + '\0');
            this.pid = pid;
            this.baseAddr = baseAddr;
            this.searchRange = searchRange;
        }

        #endregion

        #region DLLImports

        [DllImport("kernel32.dll", EntryPoint = "OpenProcess")]
        public static extern uint OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcID);
        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        public static extern bool CloseHandle(uint hObject);
        [DllImport("kernel32.dll", EntryPoint = "ReadProcessMemory")]
        public static extern bool ReadProcessMemory(uint hProc, uint lpBaseAddress, byte[] buffer, uint size, uint lpNumberOfBytesRead);
        [DllImport("kernel32.dll", EntryPoint = "WriteProcessMemory")]
        public static extern bool WriteProcessMemory(uint hProc, uint lpBaseAddress, byte[] buffer, uint size, uint lpNumberOfBytesWritten);
        [DllImport("msvcrt.dll", EntryPoint = "strcmp")]
        public unsafe static extern int strcmp(byte* b1, byte[] b2);

        #endregion

        #region Methods

        public unsafe bool FindFoVOffset(ref uint pFoV, ref byte step)
        {
            uint hProc;
            bool isFound = false;

            try { hProc = OpenProcess(READ, false, pid); step = 1; }
            catch (Win32Exception e) { throw new Exception(String.Format("Failed to open the process handle during a FindFoVOffset statement;\n{0}", e.Message)); }

            if (hProc > 0)
            {
                uint start = pFoV - searchRegionBefore;

                if (varAddr == 0)
                {
                    step = 2;
                    byte[] buffer = new byte[searchTextRegion];

                    try { ReadProcessMemory(hProc, baseAddr, buffer, searchTextRegion, 0); }
                    catch (Win32Exception e) { step = 3; throw new Exception(String.Format("Failed to read process memory during the first iteration of a FindFoVOffset statement\nWin32 Error: {1}", e.Message)); }

                    fixed (byte* pBuffer = buffer)
                    {
                        for (uint i = 0; i < searchTextRegion; i += sizeof(uint))
                        {
                            step = 4;

                            if (strcmp(pBuffer + i, cVar) == 0)
                            {
                                step = 5;
                                varAddr = baseAddr + i;
                                break;
                            }
                        }
                    }
                }

                if (varAddr > 0)
                {
                    step = 6;
                    byte[] buffer = new byte[searchRegion];

                    try { ReadProcessMemory(hProc, start, buffer, searchRegion, 0); step = 7; }
                    catch (Win32Exception e) { step = 8; throw new Exception(String.Format("Failed to read process memory during the second iteration of a FindFoVOffset statement; Address = {0:X}\nWin32 Error: {1}", start, e.Message)); }

                    step = 9;

                    fixed (byte* pBuffer = buffer)
                    {
                        for (uint i = 0; i < searchRegion; i += sizeof(uint))
                        {
                            step = 10;

                            if (*(uint*)(pBuffer + i) == varAddr)
                            {
                                step = 11;
                                pFoV += i - searchRegionBefore + 0xC;
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

        public float ReadFloat(uint ptr)
        {
            byte[] buffer = new byte[sizeof(float)];
            uint hProc;

            try { hProc = OpenProcess(READ, false, pid); }
            catch (Win32Exception e) { throw new Exception(String.Format("Failed to open the process handle during a ReadFloat statement;\n{0}", e.Message)); }

            if (hProc > 0)
            {
                try { ReadProcessMemory(hProc, ptr, buffer, sizeof(float), 0); }
                catch (Win32Exception e) { throw new Exception(String.Format("Failed to read process memory during a ReadFloat statement; Address = {0:X}\nWin32 Error: {1}", ptr, e.Message)); }

                CloseHandle(hProc);
            }

            return BitConverter.ToSingle(buffer, 0); //* 65.0f;
        }

        public void WriteFloat(uint ptr, float val)
        {
            //val /= 65.0f;
            uint hProc;

            try { hProc = OpenProcess(WRITE, false, pid); }
            catch (Win32Exception e) { throw new Exception(String.Format("Failed to open the process handle during a WriteFloat statement;\n{0}", e.Message)); }

            if (hProc > 0)
            {
                try { WriteProcessMemory(hProc, ptr, BitConverter.GetBytes(val), sizeof(float), 0); }
                catch (Win32Exception e) { throw new Exception(String.Format("Failed to write to process memory during a WriteFloat statement; Address = {0:X}\nWin32 Error: {1}", ptr, e.Message)); }

                CloseHandle(hProc);
            }
        }

        #endregion
    }
}
