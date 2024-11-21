using Microsoft.Win32;
using Serilog;
using System.Diagnostics;

namespace FurinaLC.Tool.Proxy.Guardian
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("FurinaLC.Tool.Proxy.Guardian started.");

            if (args.Length != 1 || !int.TryParse(args[0], out var watchPid))
            {
                Log.Error("Usage: FurinaLC.Tool.Proxy.Guardian [watch-pid]");
                Environment.Exit(1);
                return;
            }

            Process proc;
            try
            {
                proc = Process.GetProcessById(watchPid);
                Log.Information("Guardian found process {ProcessName}:{ProcessId}.", proc.ProcessName, watchPid);
            }
            catch
            {
                Log.Warning("Failed to find process with PID {ProcessId}. Disabling system proxy.", watchPid);
                DisableSystemProxy();
                Environment.Exit(2);
                return;
            }

            while (!proc.HasExited)
            {
                await Task.Delay(1000);
            }

            Log.Information("Watched process {ProcessName}:{ProcessId} has exited.", proc.ProcessName, watchPid);
            DisableSystemProxy();
        }

        private static void DisableSystemProxy()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true);

                if (key == null)
                {
                    Log.Warning("Failed to open registry key for system proxy settings.");
                    return;
                }

                key.SetValue("ProxyEnable", 0);
                Log.Information("Guardian successfully disabled System Proxy.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Guardian failed to disable System Proxy.");
            }
        }
    }
}
