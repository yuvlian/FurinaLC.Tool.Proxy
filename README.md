# FurinaLC.Tool.Proxy

A simple Proxy for playing Private Servers. Forked from [FireflySR.Tool.Proxy](https://git.xeondev.com/YYHEggEgg/FireflySR.Tool.Proxy/releases).

## Usage

### Windows

1. Download the [latest prebuilt binary](https://github.com/yuvlian/FurinaLC.Tool.Proxy/releases/download/v2.0.1/FurinaLC.Tool.Proxy_win-x64.zip).
2. Run `FurinaLC.Tool.Proxy.exe`
3. When it asks to install a certificate, let it. We need it to decrypt HTTPS traffic.
4. It should automatically set itself as system proxy (and revert this when you close it) and you're ready to go.

**Note:**

  - If you're unable to browse the internet after using and closing this proxy, it might be caused by Guardian failed to revert proxy settings.

  - If that happens, simply disable proxy settings manually yourself (Win+R, then type Proxy). Or run the proxy app again and close it by clicking the big red X.

### Linux

Just use mitmproxy.

## Building From Source

### Windows

  ```sh
  dotnet publish FurinaLC.Tool.Proxy.csproj -r win-x64
  ```

### Linux

Once again, just use mitmproxy.

## Configuration

Self explanatory by looking at the config.json

## Credits

- [FireflySR.Tool.Proxy](https://git.xeondev.com/YYHEggEgg/FireflySR.Tool.Proxy/releases)
- [FreeLC](https://git.xeondev.com/Moux23333/FreeLC) & `FreeLC.Tool.Proxy`
- Rebooted `Titanium.Web.Proxy` [Unobtanium.Web.Proxy](https://github.com/svrooij/titanium-web-proxy.git)
