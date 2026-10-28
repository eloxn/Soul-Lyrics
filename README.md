# Soul Lyrics

A Windows-first, offline-friendly animated lyrics player inspired by expressive, word-synchronized lyrics.

## Current MVP
- Native WPF desktop application targeting .NET 8 on Windows.
- Local audio playback with play, pause, stop, seek, and duration.
- Standard LRC line synchronization.
- Enhanced LRC word timestamps with per-word highlighting in the player and floating overlay.
- Dark desktop interface and always-on-top lyrics overlay.

## Requirements
- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 with the .NET desktop development workload (or the .NET CLI)

## Run
Open `SoulLyrics.sln` in Visual Studio, restore NuGet packages, and run the `SoulLyrics` project.

Or from the repository root:
```powershell
dotnet run --project SoulLyrics/SoulLyrics.csproj
```

## Word-by-word lyrics
Put a matching `.lrc` file next to your audio file, or choose **Load lyrics**. For example, `song.flac` can use `song.lrc`.

Ordinary LRC timestamps a whole line:
```text
[00:12.00]Hello world, how are you?
```

Enhanced LRC can timestamp each word:
```text
[00:12.00]<00:12.00>Hello <00:12.35>world, <00:12.70>how <00:12.90>are <00:13.05>you?
```

The player highlights the word whose timestamp is currently active. Word timing depends on the timestamps present in the lyric file. Ordinary LRC files still work, but can only synchronize by line.

A ready-to-edit example is in `samples/example-word-timed.lrc`.

## Roadmap
1. Improve word animation styles and overlay customization.
2. Optional local AI alignment/transcription component to generate word timestamps.
3. Music library, settings, system tray, and installer.

## Privacy
Audio playback and lyric parsing happen locally. This MVP does not upload audio to a service. The optional AI component is not implemented yet.
