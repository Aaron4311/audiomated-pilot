<p align="center">
  <img src="assets/logo.png" alt="Audiomated Pilot logo" width="120" />
</p>

# Audiomated Pilot

A native Windows audio device manager built on the raw Core Audio COM API — no NAudio, no NirSoft/SoundVolumeView, no AutoHotkey. List, switch, save, restore, and watch your Playback, Recording, and Communications default devices, from the command line, a WPF GUI, global hotkeys, or a silent background watcher.

Built to permanently fix a specific, very concrete annoyance: **SteelSeries Sonar resetting the default communications device on every launch.** It grew from there into a full CLI + GUI + profile system + real-time auto-revert tool.

## Why

Windows' own Sound settings have no concept of "remember my devices and put them back if something else changes them." Sonar (and similar virtual-audio-driver software) re-asserts its own preferred routing on its own schedule, silently overriding whatever you picked. Audiomated Pilot watches for that and reverts it in real time, or lets you script the fix entirely.

## Architecture

```
Audio.Interop   →  raw COM only: IMMDeviceEnumerator, IMMDevice, IMMDeviceCollection,
                    IPropertyStore, IPolicyConfig, IMMNotificationClient
Audio.Core      →  all business logic: AudioManager facade, DeviceEnumerator,
                    ConfigurationService, ProfileService, EndpointWatcher
Audio.CLI       →  console front-end
Audio.GUI       →  WPF front-end
Audio.Service   →  background watcher host (Generic Host / BackgroundService)
```

Each layer only talks to the one below it. `Audio.CLI` and `Audio.GUI` never reference `Audio.Interop` directly — only `Audio.Core`'s public surface.

### Why WPF, not WinUI 3

WinUI 3 (unpackaged) needs the Windows App SDK bootstrapper and more moving parts for an app this size. WPF runs as a plain executable with zero packaging/identity overhead and was the more reliable choice to actually ship and verify.

### Why no classic Windows Service

A traditional SCM-registered Windows Service runs in Session 0, which is isolated from the interactive desktop session. The Core Audio default-endpoint APIs are **per user session** — a Session 0 service cannot reliably read or change the logged-in user's default device. Instead, `Audio.Service` is built as a windowless (`OutputType=WinExe`) console-less executable, launched via a **Startup-folder shortcut for the current user** (not a Windows Service, and not Task Scheduler — some machines silently refuse to create `ONLOGON`/`RL LIMITED` scheduled tasks even for a trivial executable, for reasons unrelated to this app; a Startup shortcut needs no Task Scheduler API call at all and runs in the same interactive session either way).

## Requirements

- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build

`IPolicyConfig` is an **undocumented** COM interface. The vtable layout used here is the Windows 7–10 variant, verified working on Windows 10 22H2 (build 19045). It has not been verified against Windows 11's differing vtable — if `--set-*` commands throw or misbehave on Windows 11, that's the likely cause.

## Installing

Download `AudiomatedPilotSetup-<version>.exe` from [Releases](../../releases) and run it. It:

- Installs to `%LOCALAPPDATA%\Programs\Audiomated Pilot` (no admin rights required)
- Adds a Start Menu shortcut for the GUI
- Registers the background watcher via a per-user Startup-folder shortcut (see [Background watcher](#background-watcher-auto-revert) for why it's a Startup shortcut and not a classic Windows Service or Task Scheduler task)
- Ships a clean uninstaller (Start Menu → Uninstall Audiomated Pilot) that also stops the watcher and removes the shortcut

No installer prerequisites — each executable is published self-contained, so the .NET 8 runtime doesn't need to be installed separately.

### Building the installer yourself

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`) in addition to the .NET 8 SDK:

```powershell
./installer/build.ps1 -Version "1.0.0"
```

This publishes `Audio.CLI`/`Audio.GUI`/`Audio.Service` self-contained (`win-x64`, single-file) and compiles `installer/AudiomatedPilot.iss`, producing `installer/output/AudiomatedPilotSetup-1.0.0.exe`. The GitHub Actions workflow in [`.github/workflows/release.yml`](.github/workflows/release.yml) runs this same script and attaches the result to a GitHub Release whenever a `v*` tag is pushed.

## Building from source

```powershell
dotnet build AudiomatedPilot.sln
```

Five projects, all targeting `net8.0-windows`: `Audio.Interop`, `Audio.Core`, `Audio.CLI`, `Audio.GUI`, `Audio.Service`.

## CLI usage

```
Audio.CLI --list
Audio.CLI --set-playback <index|name|partial-name>
Audio.CLI --set-recording <index|name|partial-name>
Audio.CLI --set-communications <index|name|partial-name> [playback|recording]
Audio.CLI --set-all <index|name|partial-name> [playback|recording]
Audio.CLI --save
Audio.CLI --apply | --restore
Audio.CLI --startup [delaySeconds]
Audio.CLI --watch
Audio.CLI --profile <name>
Audio.CLI --save-profile <name>
Audio.CLI --delete-profile <name>
Audio.CLI --list-profiles
```

**Device selection** accepts three forms:
- **Index** — the number shown by `--list` (requires the `playback`/`recording` hint for `--set-communications`/`--set-all`, since those two aren't inherently flow-scoped)
- **Exact name** — the full friendly name
- **Partial name** — any substring match against the friendly name (errors out, rather than guessing, if more than one device matches)

The optional `playback`/`recording` hint on `--set-communications`/`--set-all` disambiguates when a name or index could refer to a device on either side — some virtual audio drivers (SteelSeries Sonar included) expose a render endpoint and a capture endpoint with the *exact same friendly name*.

### Examples

```powershell
# List devices, with [Default] / [Default Communication] tags
Audio.CLI --list

# Switch by partial name
Audio.CLI --set-playback corsair
Audio.CLI --set-recording hyperx

# Fix the Sonar hijack: put Corsair back as the communications device
Audio.CLI --set-communications corsair playback

# Save current defaults, then restore them later (e.g. after a reboot)
Audio.CLI --save
Audio.CLI --apply

# Named profiles
Audio.CLI --save-profile Gaming
Audio.CLI --profile Gaming
Audio.CLI --list-profiles
Audio.CLI --delete-profile Gaming

# Run once at startup: wait 20s (let other audio drivers settle), then apply config.json
Audio.CLI --startup

# Watch for drift in real time and auto-revert against config.json (foreground, Ctrl+C to stop)
Audio.CLI --watch
```

`config.json` and `profiles.json` live in `%LOCALAPPDATA%\AudiomatedPilot\`, shared across the CLI, GUI, and background watcher — not next to each executable.

`config.json` shape:

```json
{
  "Playback": "CORSAIR VOID PRO Wireless Gaming Headset",
  "Recording": "HyperX DuoCast",
  "Communications": "CORSAIR VOID PRO Wireless Gaming Headset"
}
```

## GUI

`Audio.GUI` gives you radio-button pickers for Playback, Recording, and Communication, plus profile save/load/delete and config import/export.

- **Apply** — sets whichever devices are selected as defaults
- **Refresh Devices** — re-reads current state
- **Save Profile / Load Profile / Delete Profile** — named device combinations
- **Import / Export** — copy `profiles.json` to/from a file you choose

### Global hotkeys

While `Audio.GUI` is running (regardless of window focus):

| Hotkey | Profile |
|---|---|
| `Ctrl+Shift+1` | Gaming |
| `Ctrl+Shift+2` | Streaming |
| `Ctrl+Shift+3` | Meeting |

These apply a saved profile of the same name — create them first with `--save-profile Gaming` (etc.) or via the GUI's Save Profile button.

## Background watcher (auto-revert)

`Audio.Service` waits briefly at startup (letting other audio drivers settle), applies `config.json` once, then runs the same watch logic as `Audio.CLI --watch` — windowless, via [`IMMNotificationClient`](https://learn.microsoft.com/windows/win32/api/mmdeviceapi/nn-mmdeviceapi-immnotificationclient), a real-time COM callback, not polling. On any default-device change, it compares against `config.json` and reverts if something else changed it out from under you. The startup apply matters on its own: without it, a device Windows or a driver already picked *before* the watcher started would never get corrected, since watch mode only reacts to changes that happen after it's running.

To install it as a persistent, silent, per-user background process:

```powershell
dotnet publish src/Audio.Service/Audio.Service.csproj -c Release -o src/Audio.Service/publish

$exePath = "<repo>\src\Audio.Service\publish\Audio.Service.exe"
$shell   = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut("$([Environment]::GetFolderPath('Startup'))\Audiomated Pilot Watcher.lnk")
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = Split-Path $exePath
$shortcut.Save()
```

This drops a shortcut in your per-user Startup folder, which Windows launches at every logon in your own interactive session — no Task Scheduler involved. (An earlier version of the installer used a Task Scheduler task instead; some machines silently refuse to create `ONLOGON`/`RL LIMITED` tasks even for a trivial executable, for reasons unrelated to this app, so 1.0.2+ uses a Startup shortcut instead, which needs no special API or privilege.)

Remove it by deleting `Audiomated Pilot Watcher.lnk` from `shell:startup`.

## Project layout

```
AudiomatedPilot.sln
src/
  Audio.Interop/   COM interfaces, GUIDs, PROPVARIANT marshaling, native wrappers
  Audio.Core/      AudioManager, DeviceEnumerator, ConfigurationService, ProfileService, EndpointWatcher
  Audio.CLI/       Console front-end
  Audio.GUI/       WPF front-end + global hotkeys
  Audio.Service/   Background watcher host
```

## Roadmap status

| # | Milestone | Status |
|---|---|---|
| 1 | List Playback/Recording devices | ✅ |
| 2 | Switch default device via `IPolicyConfig` | ✅ |
| 3 | Index + partial-name matching | ✅ |
| 4 | `config.json` save/apply/restore | ✅ |
| 5 | Startup mode (delay + apply) | ✅ |
| 6 | Real-time watch + auto-revert | ✅ |
| 7 | Named profiles | ✅ |
| 8 | GUI | ✅ (WPF) |
| 9 | Global hotkeys | ✅ |
| 10 | Background watcher | ✅ (Task Scheduler, not SCM service) |

Not yet built: device add/remove events, Bluetooth/USB-specific handling, portable mode, winget packaging.

## Contributing

Issues and PRs welcome. Keep the layering rule intact — if you're touching `Audio.Interop`, you're touching COM only; business logic belongs in `Audio.Core`.

## License

[MIT](LICENSE)
