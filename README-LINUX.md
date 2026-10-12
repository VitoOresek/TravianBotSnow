# TravianBotSharp for Linux

Linux port of [TravianBotSharp](https://github.com/vinaghost/TravianBotSharp). The bot logic (`MainCore`) is upstream's code;
the Windows-only parts were replaced: the WPF window became an Avalonia window (`AvaloniaUI`), and Chrome / chromedriver
handling works on Linux.

Requirements: Ubuntu or Debian with a desktop session (local screen, or VNC / RDP on a VPS), on x86_64 or ARM64
(e.g. Oracle Cloud Ampere). Download `TravianBotSharp-linux-x64.tar.gz` or `TravianBotSharp-linux-arm64.tar.gz` to match
`uname -m` (`x86_64` or `aarch64`).

## Install

```bash
tar xzf TravianBotSharp-linux-<x64|arm64>.tar.gz -C ~
cd ~/TravianBotSharp
./install.sh          # Google Chrome, libraries, Microsoft fonts, menu + desktop shortcut (asks for sudo)
./run.sh              # or double click the TravianBotSharp desktop icon
```

`./install.sh --no-msfonts` skips the Microsoft fonts.
Run the bot from inside the desktop session. A plain SSH shell has no display, so the window can't open there.

Everything the bot stores stays in that folder: `TBS.db` (accounts and settings), `logs/`, `Data/Cache/` (one Chrome profile per
account and proxy), `Screenshots/`. To move the bot, copy the folder. To update, extract the new version over it.

The first start downloads a chromedriver matching your Chrome and patches it. When Chrome updates, the next start does it again.
Set `TBS_CHROME_PATH=/path/to/chrome` to use a Chrome binary that is not in the default place.

## Detection and safety measures

| Measure | Notes |
|---|---|
| Patched chromedriver | The `{window.cdc_…}` automation marker is removed from the binary on every start (ELF as well as PE). The bot always uses this patched driver; Selenium Manager is never asked for one. |
| Chrome automation flags hidden | `enable-automation` excluded, `AutomationControlled` blink feature off, so `navigator.webdriver` is `false`; uses `--remote-debugging-pipe` instead of a debugging port. |
| Consistent browser identity | Upstream's user-agent list is mostly Android/iPhone and old Chrome versions. The bot now keeps only the OS family of each access's agent (mobile and unknown agents become Windows). It then builds a desktop Chrome identity at the **installed** Chrome version, and overrides `navigator.platform`, `navigator.userAgentData` and the `Sec-CH-UA*` headers to match it. Brand order and GREASE values follow Chrome's own algorithm. |
| Fingerprint extensions | All-Fingerprint-Defender (canvas/WebGL/audio/font noise), Spoof-Timezone, WebRTC-Leak-Shield, loaded through WebDriver BiDi. |
| Windows fonts | `install.sh` installs the Microsoft core fonts, so the font list looks like the Windows identity. |
| Proxies per access | Proxy authentication through BiDi; each proxy is checked against travian.com before use; the least recently used access is picked. An access is not reused within the minimum sleep time. |
| Separate Chrome profile | One profile per server, account and proxy, so cookies stay with their identity. |
| Human-like timing | Random click delay (500–900 ms), task delay (1–1.5 s), and random intervals for farming, refresh and training. Clicks are real mouse actions; typing sends key events. |
| Work / sleep cycle | Works 10–12 h, closes Chrome, sleeps 8–10 h, reopens with the next access (all configurable). |
| Fail safe | An unknown page pauses the account and saves a screenshot instead of clicking blindly; the bot re-logs in when logged out; retries use exponential backoff with jitter. |
| Linux specifics | On ARM64 the identity is always overridden, because Chrome would otherwise report `aarch64`. `--disable-dev-shm-usage` (small `/dev/shm` on VPSes), `--password-store=basic` (no keyring prompt over RDP), `--no-sandbox` only when running as root, 1920×1080 window in headless mode. |

Known limits:
- The `navigator.platform` getter's `toString()` reads `function () { [native code] }`, where native Chrome shows the property name.
- Web workers report the real platform.
- WebGL reports Chrome's software renderer because of `--disable-gpu`. Upstream on Windows has the same behaviour.

## Building from source

```bash
dotnet test MainCore.Test                 # .NET 8 SDK
./scripts/publish-linux.sh                # -> dist/TravianBotSharp-linux-{x64,arm64}.tar.gz
```
