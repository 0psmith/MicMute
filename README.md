# MicMute

MicMute is a small Windows tray app for toggling a selected microphone endpoint between muted and unmuted states.

## Build

This repository is intentionally dependency-free. It builds with the .NET Framework C# compiler that ships with Windows/Visual Studio Build Tools.

```powershell
.\build.ps1
```

The executable is written to:

```text
bin\MicMute.exe
```

Run after build:

```powershell
.\build.ps1 -Run
```

## Features

- Select the microphone endpoint to control, or use the current Windows default microphone.
- Configure a global keyboard hotkey with Ctrl, Alt, Shift, and Win modifiers.
- Configure mouse middle button, XButton1, or XButton2 shortcuts with optional modifiers.
- Show muted/unmuted state in the tray icon.
- Play a short system sound and show a compact overlay when the mute state changes.
- Keep state synchronized with Windows microphone mute changes by polling the selected endpoint.
- Optionally close the settings window to the tray instead of exiting.
