# Seiren Discord

Sync the hardware mute button on the Razer Seiren V3 Chroma with Discord's microphone mute on Windows. The bridge reads the microphone's mute state through Core Audio and checks Discord's mute switch through Windows UI Automation. It does not record audio or send commands to Microsoft Teams.

## Getting started

1. On 64-bit Windows, set the Seiren V3 Chroma as your default input device. Select that microphone in the stable Discord desktop app and set the app language to English.
2. Run **SeirenDiscord.exe**. It runs in the background.
3. Press the hardware mute button and check that Discord's microphone icon follows it. To stop the bridge, end **SeirenDiscord** in Task Manager.

To launch the bridge when you sign in, add a shortcut to the executable in the Windows Startup folder. The local log is at %LOCALAPPDATA%\SeirenDiscord\SeirenDiscord.log.

## Building from source

Run **build.ps1** in PowerShell on 64-bit Windows. It uses the Windows .NET Framework compiler and places **SeirenDiscord.exe** alongside the source.

## Limitations

Sync is one-way, from the microphone to Discord. A button press may be skipped if Discord is closed, deafened, or its microphone controls are unavailable. The bridge never unmutes Discord at startup. If you change your input device, select the Seiren again and restart the bridge. Discord updates may change how its microphone controls are exposed.
