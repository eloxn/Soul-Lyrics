# Soul Lyrics — Complete Build Specification for Antigravity

Purpose: Use this document as the single source of truth for completing Soul Lyrics. Open the repository in Antigravity IDE and ask its coding agent to follow this spec, inspect the existing code, implement the milestones, run builds/tests, and update this document as work is completed.

## 1. Product vision

Build Soul Lyrics, a polished Windows desktop music player with expressive, synchronized lyrics inspired by animated lyric apps such as Verci. The defining feature is beautiful word-by-word lyrics that follow the song, including a floating overlay that can sit above other apps.

The app should be offline-first. Audio files, lyrics, generated alignments, settings, and the music library should remain on the user's computer. AI models should run locally after the user downloads them. Never upload the user's music or lyrics without an explicit, separate opt-in feature and clear consent.

### Target platform and user
- Windows 10/11, primarily Windows 11.
- Modern desktop experience, fluid animations, keyboard-friendly controls, high-DPI support, and responsive layouts.
- Designed for local music files and headphones/desktop use.
- Keep first launch simple. Advanced AI models must be optional downloads, not a blocker for basic playback.

## 2. Current repository and implementation status

The repository already contains an initial WPF/.NET 8 application:
- SoulLyrics/SoulLyrics.csproj: WPF app and NAudio dependency.
- SoulLyrics/App.xaml and App.xaml.cs: application resources and startup.
- SoulLyrics/MainWindow.xaml and MainWindow.xaml.cs: player UI, local audio playback, LRC parsing, line highlighting, and an initial Enhanced LRC word-highlight implementation.
- SoulLyrics/LyricsOverlay.xaml and LyricsOverlay.xaml.cs: always-on-top floating lyric window.
- SoulLyrics.sln: Visual Studio solution.
- samples/example-word-timed.lrc: sample Enhanced LRC lyrics.
- README.md: quick-start documentation.

Important: This is a starter implementation, not a verified release. Do not assume it compiles or works correctly. First inspect every existing source file, restore dependencies, build it, fix errors, and run tests. Preserve working features when refactoring.

## 3. Required workflow for the Antigravity agent

1. Read this file and the entire repository before editing.
2. Check installed .NET SDKs and the project target framework. Keep the app on a supported Windows desktop target; prefer .NET 8 unless there is a concrete compatibility reason to upgrade.
3. Run dotnet restore and dotnet build SoulLyrics.sln. Fix all compile errors and warnings that reveal real bugs.
4. Review the audio playback lifecycle, LRC parser, UI-thread usage, seek slider, overlay lifecycle, and end-of-track behavior.
5. Add automated tests for parsing and timing logic. Extract parsing/timing into a UI-independent class/library if that makes testing easier.
6. Implement the milestones below in order. Do not try to add every feature in one huge change.
7. After each milestone, build and test. If the environment cannot build a Windows WPF application, state that clearly and use the strongest available static checks and unit tests instead.
8. Keep changes small, maintainable, and documented. Avoid unrelated rewrites and unnecessary dependencies.
9. Update this file's checklist and README when features are actually implemented and verified. Never claim tests passed unless they were run.
10. Do not commit secrets, tokens, private audio, model binaries, or personal paths. Use GitHub Actions for CI if practical.

## 4. Core features and acceptance criteria

### A. Local music playback
- Open common local formats: MP3, WAV, FLAC, AIFF, WMA, AAC, and M4A where supported by the chosen decoder. Clearly report unsupported or corrupt files.
- Play, pause, stop, previous/next track when a queue exists, seek, volume, mute, elapsed time, and total duration.
- Prevent playback resources from leaking when changing tracks or closing the app.
- Correctly handle playback completion, repeated play after completion, seeking, missing files, and decoder exceptions.
- Maintain an internal playback position that can drive lyrics with stable timing.
- Support a queue and a simple local music library in a later milestone.
- Do not silently claim every codec works: document the actual decoder support and add a robust decoder strategy where needed.

### B. Lyrics sources and parser
- Automatically look for a matching .lrc file beside the selected audio file, and allow manual lyric-file selection.
- Parse standard LRC line timestamps, multiple timestamps on a line, optional fractional seconds, blank lines, and metadata tags.
- Parse Enhanced LRC word timestamps such as [00:12.00]<00:12.00>Hello <00:12.35>world!
- Preserve punctuation and whitespace. Do not lose spaces between adjacent word runs.
- Gracefully handle malformed timestamps, malformed tags, Unicode, Malayalam and other non-Latin scripts, and empty files.
- Keep lyrics sorted, deterministic, and safe when timestamps are duplicated or out of order.
- Separate parsing from WPF controls so it can be unit tested.
- If only line-timed lyrics exist, show line synchronization and clearly explain that true word-level timing needs Enhanced LRC or an alignment engine.

### C. Word-by-word synchronization and animation
- Synchronize word highlighting to playback time. The active word is the latest word whose timestamp is at or before the playback position, until the next word begins.
- Make line-level and word-level lyrics both work.
- Use smooth but restrained transitions: opacity, scale, color, or subtle glow. Avoid large distracting jumps.
- Avoid rebuilding all WPF inline elements every 35 ms. Update the active word only when it changes, or use a carefully designed animation layer.
- Seeking backward/forward must immediately update the active line and word.
- Handle gaps between lines, intros, outros, duplicate timestamps, and lines with no word timestamps.
- Provide animation styles such as Minimal, Karaoke, Glow, and Bounce in settings, with a reduced-motion option.
- Timing must remain stable under UI load; don't rely on expensive per-frame work.

### D. Floating lyrics overlay
- Separate, always-on-top, movable overlay.
- Adjustable position, width, font, font size, opacity, text alignment, background, and shadow/glow.
- Modes: compact current line, current line plus previous/next lines, and word-by-word karaoke.
- Optional click-through mode, with an obvious way to disable it so the user can move/close the overlay.
- Keep the overlay readable over light or dark backgrounds and handle multi-monitor/DPI changes.
- Provide a keyboard shortcut or tray menu item to show/hide the overlay.
- Correctly close/dispose the overlay and avoid creating duplicate windows.

### E. UI/UX
- Keep the current dark/pink visual direction as a starting point, but refine it into a coherent, premium-feeling music app.
- Add a clear empty state, loading state, error state, and no-lyrics state.
- Show track title and useful metadata where available; don't invent metadata.
- Include keyboard focus, accessible labels, tooltips, and high-DPI support.
- Use reusable styles/components rather than duplicating visual logic.
- Keep controls understandable and usable at different window sizes.
- Provide a settings page and consistent visual feedback for active playback, pause, loading, and errors.

## 5. Local AI lyrics: the major differentiator

Build an optional, local-only AI workflow that can produce word timestamps from a user's audio and lyrics. Do not promise perfect alignment. Music vocals, backing tracks, instrumental sections, rap, code-switching, and noisy mixes make alignment difficult.

### Desired user flow
1. User opens a local track.
2. If no word-timed lyrics exist, the app offers: Find lyrics, Import lyrics, or Generate word timing locally.
3. If the user imports plain text or line-timed lyrics, use those lyrics as the transcript for alignment.
4. If no transcript is available, optionally offer local speech/song transcription as a separate step; explain that sung vocals can be less accurate than speech.
5. The app checks whether the required model/runtime is installed. If not, show the model size, disk-space requirement, source/license, and a user-controlled download button.
6. Process the audio locally and show progress, cancellation, and errors.
7. Align the words to audio and create Enhanced LRC or an internal timestamp format.
8. Let the user preview and manually correct timings.
9. Save the result beside the audio or in the app's local data folder. Cache results so the same track is not repeatedly processed.

### Architecture recommendation
- Create clean interfaces such as ILyricsTranscriber and ILyricsAligner so engines can be swapped.
- Investigate Whisper.cpp or another maintained local inference runtime for optional transcription. Keep the engine behind an adapter and do not bundle large model files in Git.
- Investigate a forced-alignment engine suitable for music vocals for word-level timing. Do not assume speech-only forced alignment is reliable on songs. Compare viable options, licensing, Windows support, model sizes, CPU/GPU requirements, and whether each can run fully offline.
- Start with alignment from user-provided lyrics before attempting automatic lyric recognition. It is more controllable and avoids pretending song transcription is solved.
- Prefer a downloadable model manager with SHA-256 verification, explicit licenses, progress reporting, cancellation, and safe cleanup of partial downloads.
- If the selected model cannot use the RTX GPU reliably, provide a CPU fallback and explain the performance trade-off. Do not hard-code hardware assumptions.
- Make model downloads optional and store models outside the source repository.
- Do not use cloud APIs as a hidden fallback. Any future cloud feature must be explicitly opt-in, disabled by default, and clearly labeled.

### AI acceptance criteria
- Basic playback works without any AI runtime or model installed.
- Model installation and inference are optional.
- User audio is never uploaded.
- Progress, cancellation, failures, and missing dependencies are handled.
- Generated timestamps can be edited and saved.
- The UI describes output as machine-generated and potentially inaccurate.
- Model licensing and attribution are documented before distribution.

## 6. Suggested architecture

Keep the app small and understandable. Use the current WPF app initially; do not migrate frameworks without a strong reason.

Suggested organization (adapt to the existing project where appropriate):

    SoulLyrics/
      App.xaml
      MainWindow.xaml
      Views/
        PlayerView.xaml
        LibraryView.xaml
        SettingsView.xaml
        LyricsOverlay.xaml
      ViewModels/
        MainViewModel.cs
        PlayerViewModel.cs
        LyricsViewModel.cs
        SettingsViewModel.cs
      Models/
        Track.cs
        LyricLine.cs
        LyricWord.cs
        PlaybackState.cs
      Services/
        Audio/
          IAudioPlayer.cs
          NAudioPlayer.cs
        Lyrics/
          ILyricsParser.cs
          LrcParser.cs
          LyricsSynchronizer.cs
          LyricsStorage.cs
        Alignment/
          ILyricsAligner.cs
          ILyricsTranscriber.cs
          LocalAlignmentService.cs
          ModelManager.cs
        Library/
          MusicLibraryService.cs
        Settings/
          SettingsService.cs
      Resources/
      Helpers/
    SoulLyrics.Tests/
    samples/
    docs/

This is a proposed destination, not a mandate to create empty folders. Introduce abstractions when they improve testability or keep responsibilities separate. Avoid giant code-behind files over time, but refactor incrementally.

## 7. Persistence, privacy, and security
- Store preferences and library metadata locally using a small JSON store or SQLite; choose based on actual needs.
- Store generated lyrics locally, with a clear way to find, edit, export, and delete them.
- Do not modify original music files.
- Never run shell commands constructed from untrusted filenames without safe argument handling.
- Treat model downloads and archives as untrusted input; verify checksums, enforce size limits, and prevent path traversal during extraction.
- Do not include API keys or credentials in source control.
- Provide a clear privacy note and avoid telemetry by default.
- Respect licenses for models, libraries, and lyric sources. Do not scrape copyrighted lyrics from sources that prohibit it.

## 8. Testing strategy

Create unit tests for:
- Standard LRC timestamp parsing.
- Enhanced LRC parsing with multiple word timestamps.
- Multiple line timestamps on one line.
- Fractional timestamp normalization (for example .2, .20, .200).
- Unicode text, Malayalam text, punctuation, and whitespace preservation.
- Malformed tags and invalid timestamps.
- Out-of-order and duplicate timestamps.
- Active line/word selection at boundaries and after seeking backward.
- Empty lyrics and lyric files with metadata only.

Integration/manual tests:
- Open and play a short MP3 and WAV; test FLAC if the selected decoder supports it.
- Pause, resume, stop, seek, and play after the track finishes.
- Switch tracks repeatedly and close the app while playing.
- Load standard LRC, Enhanced LRC, a malformed LRC, and no lyric file.
- Open, move, hide, show, and close the floating overlay.
- Test different DPI settings and multiple monitors when available.
- Run with no AI model installed and confirm all basic features still work.
- Run AI alignment only after the local engine is implemented; verify cancellation, failure handling, and no network upload.

## 9. Packaging and release
- Provide a documented Release build command.
- Add a Windows build workflow (GitHub Actions) that restores, builds, and runs tests where supported.
- Produce a self-contained Windows build only after deciding the deployment model and testing it.
- Use a proper installer or packaged distribution; do not call a raw Debug build a release.
- Include third-party notices and model license/attribution where applicable.
- Never commit generated binaries or AI model weights to the repository unless explicitly required and licensed.

## 10. Prioritized implementation plan

### Phase 0 — Make the starter reliable
- [ ] Inspect the full repository.
- [ ] Build the current solution and fix compile/runtime issues.
- [ ] Ensure solution and project paths are correct.
- [ ] Add test project and move lyric parsing/timing into testable classes.
- [ ] Fix seek slider interactions, track switching, playback completion, and overlay lifecycle.
- [ ] Verify standard LRC and Enhanced LRC parsing, including whitespace.

### Phase 1 — Polished player and word animations
- [ ] Refine the player layout and states.
- [ ] Improve word highlighting without per-frame inline rebuilding.
- [ ] Add configurable animation styles and reduced-motion setting.
- [ ] Add overlay controls and robust multi-monitor positioning.
- [ ] Add keyboard shortcuts and volume control.

### Phase 2 — Local lyrics workflow
- [ ] Import plain text, LRC, and Enhanced LRC.
- [ ] Add an editor/preview to adjust lyric text and word timings.
- [ ] Save and cache generated lyrics locally.
- [ ] Add a clear missing-lyrics flow.

### Phase 3 — Optional offline AI
- [ ] Research maintained Windows-compatible transcription and alignment engines.
- [ ] Record a decision note comparing accuracy, music-vocal suitability, model sizes, licenses, CPU/GPU support, and offline behavior.
- [ ] Implement engine interfaces and a local model manager.
- [ ] Add local alignment from user-provided transcript first.
- [ ] Add optional transcription if technically viable.
- [ ] Add progress, cancellation, retries, model verification, and manual correction.

### Phase 4 — Library and distribution
- [ ] Build a persistent music library and queue.
- [ ] Add settings, tray controls, startup preference, and shortcuts.
- [ ] Add CI, Release packaging, installer, and third-party notices.
- [ ] Run a clean install test on a Windows machine.

## 11. Definition of done

Soul Lyrics is not done until:
- A clean checkout builds from documented instructions.
- Automated parser/timing tests pass.
- Local playback and standard/Enhanced LRC synchronization work.
- The overlay is stable and configurable.
- AI features are optional, local-only, and honestly described.
- A fresh Windows install can run the packaged app.
- README and this spec accurately describe what has been implemented and tested.

## 12. First task for Antigravity

Start with this exact task before adding new features:

> Read ANTIGRAVITY_BUILD_SPEC.md, inspect every file in the repository, run dotnet --info, dotnet restore, and dotnet build SoulLyrics.sln. Fix the existing project until it builds cleanly. Then add SoulLyrics.Tests and write tests for standard LRC and Enhanced LRC parsing. Extract the parser and timing selection into testable classes if necessary. Fix the known risk of rebuilding word inline elements every timer tick. Do not implement AI yet. Report the exact commands run, results, changed files, and any checks that could not be executed. Then proceed phase by phase, updating the checklist honestly.
