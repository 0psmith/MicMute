# MicMute

MicMute is a small Windows tray app for toggling a selected microphone endpoint between muted and unmuted states. The app uses an English interface by default and stays out of the way in the system tray.

## Usage

Run `MicMute.exe` and open **Preferences** from the tray icon. From there you can:

- Select the microphone endpoint to control, or follow the current Windows default microphone.
- Record a global keyboard hotkey with Ctrl, Alt, Shift, and Win modifiers.
- Record mouse middle button, XButton1, or XButton2 shortcuts with optional modifiers.
- Choose whether closing the preferences window exits the app or returns it to the tray.
- Start MicMute automatically with Windows.
- Adjust the compact overlay position and opacity.

The tray icon shows the current muted/unmuted state. Double-click the tray icon to open Preferences, or use the tray menu to mute, unmute, configure, or exit.

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

## Version

The app version is managed in the root `VERSION` file. The build script embeds it into the generated executable metadata.

```powershell
.\build.ps1 -Version 1.2.3
```

## Features

- English-first preferences, tray menu, hotkey capture, notifications, and overlay text.
- Select the microphone endpoint to control, or use the current Windows default microphone.
- Configure a global keyboard hotkey with Ctrl, Alt, Shift, and Win modifiers.
- Configure mouse middle button, XButton1, or XButton2 shortcuts with optional modifiers.
- Show muted/unmuted state in the tray icon.
- Play a short system sound and show a compact overlay when the mute state changes.
- Keep state synchronized with Windows microphone mute changes by polling the selected endpoint.
- Optionally close the settings window to the tray instead of exiting.
