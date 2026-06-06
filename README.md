# Clean Your Setup

A small portable Windows utility that blocks keyboard and/or mouse input while you clean your desk, keyboard, mouse, and accessories.

The app is built for a simple problem: cleaning your setup without accidentally clicking, typing, opening windows, moving files, or triggering shortcuts.

## Features

* Block **keyboard only**
* Block **mouse only**
* Block **keyboard + mouse**
* Preset timers:

  * 30 seconds
  * 1 minute
  * 2 minutes
  * 5 minutes
* Custom cleaning time
* Full-screen cleaning overlay
* Visual countdown
* Last 5 seconds beep alert
* Spam-to-unlock safety system

  * Press **Esc**, **Space**, or **Enter** repeatedly to unlock early
* Portable one-file Windows build
* Dark VP-style interface

## Why this exists

Cleaning a keyboard or mouse while the computer is on can accidentally trigger clicks, shortcuts, file changes, browser actions, or app commands.

Clean Your Setup gives you a controlled cleaning mode where input is temporarily blocked and automatically restored when the timer ends.

## Safety design

Clean Your Setup does **not** disable Windows drivers or remove devices from Device Manager.

Instead, it uses a software-level input blocking approach designed for temporary cleaning sessions. This is safer because the app automatically unlocks after the timer finishes, and an emergency spam-to-unlock method is always available.

## Emergency unlock

During cleaning mode, repeatedly press one of these keys:

* `Esc`
* `Space`
* `Enter`

The overlay will show visual progress until the app unlocks.

## Requirements

### To run the portable `.exe`

* Windows 10 or Windows 11
* 64-bit Windows

No .NET installation is required when using the portable published build.

### To build from source

* Windows 10 or Windows 11
* .NET 8 SDK

## Run from source

```powershell
dotnet run
```

## Build normal release

```powershell
dotnet build -c Release
```

The output will be created in:

```txt
bin\Release\net8.0-windows\
```

## Build portable one-file `.exe`

Run:

```bat
build-portable-win-x64.bat
```

The portable executable will be created in:

```txt
bin\Release\net8.0-windows\win-x64\publish\CleanYourSetup.exe
```

You can copy that single `.exe` to another Windows x64 computer and run it without installing .NET.

## Usage

1. Open **Clean Your Setup**.
2. Choose what to block:

   * Keyboard + mouse
   * Keyboard only
   * Mouse only
3. Choose a preset time or enter a custom time.
4. Click **Start Cleaning Mode**.
5. Wait for the countdown.
6. Clean your setup.
7. The app automatically unlocks when the timer ends.

## Limitations

* The app is intended for normal desktop use, not Windows secure screens.
* Some system-level shortcuts or admin/security prompts may behave differently depending on Windows permissions.
* Controllers, drawing tablets, macro pads, Stream Decks, and other special accessories may not be blocked in every case.
* For unusual accessories, unplugging or powering them off is still recommended.

## Intended use

This app is meant for personal setup cleaning only.

Do not use it to interfere with someone else’s computer, shared workstation, public computer, or device without permission.

## Tech stack

* C#
* .NET 8
* Windows Forms
* Windows x64 portable publish

## License

This project is licensed under the MIT License.
