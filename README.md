# Soul Lyrics

A Windows-first, offline-friendly animated lyrics player inspired by expressive, word-synchronized lyrics.

## Current MVP
- Native WPF desktop application targeting .NET 8 on Windows.
- Open local audio files (MP3, WAV, FLAC, M4A where the installed decoder supports them).
- Play, pause, stop, seek, and view track duration.
- Load a matching .lrc lyric file and display timestamped lines in a separate always-on-top overlay.
- Dark interface with an accent color and a clean foundation for future word-level animation.

## Requirements
- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 with the .NET desktop development workload (or the .NET CLI)

## Run
Open SoulLyrics.sln in Visual Studio, restore NuGet packages, and run the SoulLyrics project.

From the project directory:
    dotnet restore
    dotnet run

## Lyrics
For a track named song.flac, Soul Lyrics looks for song.lrc in the same folder. You can also load an LRC file manually. Standard LRC files timestamp whole lines; true word-by-word timing requires word-timed lyrics or an alignment engine and is planned as a later phase.

## Roadmap
1. Core local playback and lyric overlay.
2. Word-timed lyric format and per-word animations.
3. Local AI alignment/transcription as an optional downloadable component.
4. Library, settings, system tray, and packaging.

## Privacy
Local audio playback and LRC parsing are performed on the device. This MVP does not upload audio to a service.
