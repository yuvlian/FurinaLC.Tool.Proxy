using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Serilog;

namespace FurinaLC.Tool.Proxy
{
    internal static class Program
    {
        private const string Title = "FurinaLC Proxy";
        private const string ConfigPath = "config.json";
        private const string ConfigTemplatePath = "config.tmpl.json";
        private const string GuardianPath = "tool/FurinaLC.Tool.Proxy.Guardian.exe";

        private static ProxyService s_proxyService = null!;
        private static bool s_clearupd = false;

        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("{Title} started.", Title);
            Log.Information("{Title} is a fork of https://git.xeondev.com/YYHEggEgg/FireflySR.Tool.Proxy/", Title);
            Console.Title = Title;

            _ = Task.Run(WatchGuardianAsync);

            try
            {
                CheckProxy();
                InitConfig();

                var conf = JsonSerializer.Deserialize(File.ReadAllText(ConfigPath), ProxyConfigContext.Default.ProxyConfig)
                           ?? throw new FileLoadException("Please configure 'config.json' correctly.");
                s_proxyService = new ProxyService(conf.DestinationHost, conf.DestinationPort, conf);

                Log.Information("Proxy server is running.");
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                Console.CancelKeyPress += OnProcessExit;

                Thread.Sleep(-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unexpected error occurred during application startup.");
                Environment.Exit(1);
            }
        }

        private static async Task WatchGuardianAsync()
        {
            var proc = StartGuardian();
            if (proc == null)
            {
                Log.Warning("Guardian start failed. Proxy settings might not revert after closing.");
                return;
            }

            Log.Information("Guardian started successfully.");

            while (!proc.HasExited)
            {
                await Task.Delay(1000);
            }

            Log.Warning("Guardian process exited.");
            OnProcessExit(null, null);
            Environment.Exit(0);
        }

        private static Process? StartGuardian()
        {
            if (!OperatingSystem.IsWindows())
            {
                Log.Information("Guardian is not supported on this OS.");
                return null;
            }

            try
            {
                return Process.Start(new ProcessStartInfo(GuardianPath, $"{Environment.ProcessId}")
                {
                    UseShellExecute = false,
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start Guardian process.");
                return null;
            }
        }

        private static void InitConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                Log.Information("Config file not found. Creating a new one from template.");
                File.Copy(ConfigTemplatePath, ConfigPath);
            }
        }

        private static void OnProcessExit(object? sender, EventArgs? args)
        {
            if (s_clearupd) return;
            s_proxyService?.Shutdown();
            s_clearupd = true;
        }

        public static void CheckProxy()
        {
            try
            {
                string? proxyInfo = GetProxyInfo();
                if (proxyInfo != null)
                {
                    Log.Warning("Detected another proxy: {ProxyInfo}. Please disable it.", proxyInfo);
                    Console.WriteLine("Press any key to continue if you have disabled other proxy.");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Unable to check system proxy.");
            }
        }

        public static string? GetProxyInfo()
        {
            try
            {
                IWebProxy proxy = WebRequest.GetSystemWebProxy();
                Uri? proxyUri = proxy.GetProxy(new Uri("https://www.example.com"));
                if (proxyUri == null) return null;

                string proxyInfo = $"{proxyUri.Host}:{proxyUri.Port}";
                return proxyInfo;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to retrieve proxy info.");
                return null;
            }
        }
    }
}
