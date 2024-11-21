namespace FurinaLC.Tool.Proxy
{
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Text;
    using System.Threading.Tasks;
    using Serilog;
    using Titanium.Web.Proxy;
    using Titanium.Web.Proxy.EventArguments;
    using Titanium.Web.Proxy.Models;

    internal class ProxyService
    {
        private readonly ProxyConfig _conf;
        private readonly string _webProxyHost;
        private readonly ProxyServer _webProxyServer;
        private readonly string _targetRedirectHost;
        private readonly int _targetRedirectPort;

        public ProxyService(string targetRedirectHost, int targetRedirectPort, ProxyConfig conf)
        {
            _conf = conf;
            _webProxyHost = "127.0.0.1";
            _webProxyServer = new ProxyServer();
            _webProxyServer.CertificateManager.EnsureRootCertificate();

            _webProxyServer.BeforeRequest += BeforeRequest;
            _webProxyServer.ServerCertificateValidationCallback += OnCertValidation;

            _targetRedirectHost = targetRedirectHost;
            _targetRedirectPort = targetRedirectPort;

            int port = conf.ProxyBindPort == 0 ? Random.Shared.Next(10000, 60000) : conf.ProxyBindPort;
            SetEndPoint(new ExplicitProxyEndPoint(IPAddress.Parse(_webProxyHost), port, true));

            Log.Information("Starting proxy server at {ProxyHost}:{BindPort}.", _webProxyHost, port);
            Log.Information("Proxy redirect target is {TargetHost}:{TargetPort}.", targetRedirectHost, targetRedirectPort);
        }

        private void SetEndPoint(ExplicitProxyEndPoint explicitEP)
        {
            explicitEP.BeforeTunnelConnectRequest += BeforeTunnelConnectRequest;

            _webProxyServer.AddEndPoint(explicitEP);
            _webProxyServer.Start();

            if (OperatingSystem.IsWindows())
            {
                _webProxyServer.SetAsSystemHttpProxy(explicitEP);
                _webProxyServer.SetAsSystemHttpsProxy(explicitEP);
                Log.Information("Proxy set as system HTTP/HTTPS proxy on Windows.");
            }
            else
            {
                Log.Warning("System-wide proxy settings are not supported on this OS.");
            }
        }

        public void Shutdown()
        {
            Log.Information("Shutting down proxy server.");
            _webProxyServer.Stop();
            _webProxyServer.Dispose();
            Log.Information("Proxy server shut down successfully.");
        }

        private Task BeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs args)
        {
            string hostname = args.HttpClient.Request.RequestUri.Host;
            Log.Debug("Received TunnelConnectRequest for hostname: {Hostname}", hostname);

            args.DecryptSsl = ShouldRedirect(hostname);
            if (args.DecryptSsl)
            {
                Log.Information("SSL decryption enabled for hostname: {Hostname}", hostname);
            }

            return Task.CompletedTask;
        }

        private Task OnCertValidation(object sender, CertificateValidationEventArgs args)
        {
            if (args.SslPolicyErrors == SslPolicyErrors.None)
            {
                Log.Debug("Certificate validation succeeded for: {CertificateSubject}", args.Certificate.Subject);
                args.IsValid = true;
            }
            else
            {
                Log.Warning("Certificate validation failed with errors: {Errors}", args.SslPolicyErrors);
            }

            return Task.CompletedTask;
        }

        private bool ShouldForceRedirect(string path)
        {
            foreach (var keyword in _conf.ForceRedirectOnUrlContains)
            {
                if (path.Contains(keyword))
                {
                    Log.Debug("Path {Path} matches force redirect keyword: {Keyword}", path, keyword);
                    return true;
                }
            }
            return false;
        }

        private bool ShouldBlock(Uri uri)
        {
            var path = uri.AbsolutePath;
            if (_conf.BlockUrls.Contains(path))
            {
                Log.Information("Blocking request to path: {Path}", path);
                return true;
            }
            return false;
        }

        private Task BeforeRequest(object sender, SessionEventArgs args)
        {
            string hostname = args.HttpClient.Request.RequestUri.Host;
            Log.Debug("Processing request to hostname: {Hostname}", hostname);

            if (ShouldRedirect(hostname) || ShouldForceRedirect(args.HttpClient.Request.RequestUri.AbsolutePath))
            {
                string requestUrl = args.HttpClient.Request.Url;
                Uri local = new Uri($"http://{_targetRedirectHost}:{_targetRedirectPort}/");

                Uri builtUrl = new UriBuilder(requestUrl)
                {
                    Scheme = local.Scheme,
                    Host = local.Host,
                    Port = local.Port
                }.Uri;

                string replacedUrl = builtUrl.ToString();
                if (ShouldBlock(builtUrl))
                {
                    Log.Warning("Blocking redirected URL: {ReplacedUrl}", replacedUrl);
                    args.Respond(new Titanium.Web.Proxy.Http.Response(Encoding.UTF8.GetBytes("Resource Blocked"))
                    {
                        StatusCode = 404,
                        StatusDescription = "Not Found",
                    }, true);
                    return Task.CompletedTask;
                }

                Log.Information("Redirecting URL from {OriginalUrl} to {ReplacedUrl}", requestUrl, replacedUrl);
                args.HttpClient.Request.Url = replacedUrl;
            }

            return Task.CompletedTask;
        }

        private bool ShouldRedirect(string hostname)
        {
            if (hostname.Contains(':'))
                hostname = hostname[..hostname.IndexOf(':')];

            foreach (string domain in _conf.AlwaysIgnoreDomains)
            {
                if (hostname.EndsWith(domain))
                {
                    Log.Debug("Hostname {Hostname} matches ignore domain: {Domain}", hostname, domain);
                    return false;
                }
            }

            foreach (string domain in _conf.RedirectDomains)
            {
                if (hostname.EndsWith(domain))
                {
                    Log.Debug("Hostname {Hostname} matches redirect domain: {Domain}", hostname, domain);
                    return true;
                }
            }

            return false;
        }
    }
}
