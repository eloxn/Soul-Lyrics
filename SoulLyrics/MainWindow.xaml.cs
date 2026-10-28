using Microsoft.Win32;
using NAudio.Wave;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SoulLyrics;

public partial class MainWindow : Window
{
    private IWavePlayer? _output;
    private AudioFileReader? _reader;
    private readonly DispatcherTimer _timer;
    private readonly List<LyricLine> _lyrics = new();
    private LyricsOverlay? _overlay;
    private int _activeLine = -1;
    private readonly List<TextBlock> _lineBlocks = new();

    public MainWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(35) };
        _timer.Tick += (_, _) => UpdatePlaybackUi();
        _timer.Start();
    }

    private void OpenMusic_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose a local music file",
            Filter = "Audio files|*.mp3;*.wav;*.aiff;*.aif;*.wma;*.aac;*.m4a;*.flac|All files|*.*"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            StopAndDisposeAudio();
            _reader = new AudioFileReader(dialog.FileName);
            _output = new WaveOutEvent();
            _output.Init(_reader);
            _output.PlaybackStopped += (_, _) => Dispatcher.Invoke(() =>
            {
                if (_reader is not null && _reader.Position >= _reader.Length)
                    PlayPauseButton.Content = "▶  Play";
            });
            TrackTitleText.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
            TrackPathText.Text = dialog.FileName;
            DurationText.Text = FormatTime(_reader.TotalTime);
            SeekSlider.Value = 0;
            PlayPauseButton.Content = "Ⅱ  Pause";
            _output.Play();
            StatusText.Text = "PLAYING  ·  LOCAL AUDIO";
            var automaticLrc = Path.ChangeExtension(dialog.FileName, ".lrc");
            if (File.Exists(automaticLrc)) LoadLrc(automaticLrc);
            else
            {
                _lyrics.Clear();
                RenderLyrics();
                StatusText.Text = "PLAYING  ·  NO MATCHING .LRC FOUND";
            }
        }
        catch (Exception ex)
        {
            StopAndDisposeAudio();
            MessageBox.Show($"Couldn't open this audio file.\n\n{ex.Message}", "Soul Lyrics", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_output is null || _reader is null) { OpenMusic_Click(sender, e); return; }
        if (_output.PlaybackState == PlaybackState.Playing)
        {
            _output.Pause();
            PlayPauseButton.Content = "▶  Play";
            StatusText.Text = "PAUSED";
        }
        else
        {
            if (_reader.Position >= _reader.Length) _reader.Position = 0;
            _output.Play();
            PlayPauseButton.Content = "Ⅱ  Pause";
            StatusText.Text = "PLAYING  ·  LOCAL AUDIO";
        }
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        if (_output is null || _reader is null) return;
        _output.Stop();
        _reader.Position = 0;
        PlayPauseButton.Content = "▶  Play";
        UpdatePlaybackUi();
    }

    private void LoadLyrics_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Load timestamped lyrics", Filter = "LRC lyrics|*.lrc|Text files|*.txt|All files|*.*" };
        if (dialog.ShowDialog() == true) LoadLrc(dialog.FileName);
    }

    // Supports ordinary LRC line timestamps and Enhanced LRC word timestamps, e.g.
    // [00:12.00]<00:12.00>Hello <00:12.30>world
    private void LoadLrc(string path)
    {
        try
        {
            _lyrics.Clear();
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                var lineTags = Regex.Matches(line, @"\[(\d{1,2}):(\d{2})(?:[.](\d{1,3}))?\]");
                if (lineTags.Count == 0) continue;
                var baseTime = ParseTimestamp(lineTags[0].Groups[1].Value, lineTags[0].Groups[2].Value, lineTags[0].Groups[3].Value);
                var textStart = lineTags[^1].Index + lineTags[^1].Length;
                var body = line[textStart..].Trim();
                if (string.IsNullOrWhiteSpace(body)) continue;

                var wordTags = Regex.Matches(body, @"<((?:\d{1,2}):(?:\d{2})(?:[.](?:\d{1,3}))?)>");
                var words = new List<LyricWord>();
                if (wordTags.Count > 0)
                {
                    for (var i = 0; i < wordTags.Count; i++)
                    {
                        var tag = wordTags[i];
                        var wordStart = tag.Index + tag.Length;
                        var wordEnd = i + 1 < wordTags.Count ? wordTags[i + 1].Index : body.Length;
                        var wordText = body[wordStart..wordEnd];
                        if (string.IsNullOrEmpty(wordText)) continue;
                        var stamp = tag.Groups[1].Value.Split(':');
                        var sec = stamp[1].Split('.');
                        var fraction = sec.Length > 1 ? sec[1] : "0";
                        words.Add(new LyricWord(ParseTimestamp(stamp[0], sec[0], fraction), wordText));
                    }
                }
                var plainText = wordTags.Count > 0
                    ? Regex.Replace(body, @"<\d{1,2}:\d{2}(?:[.]\d{1,3})?>", "")
                    : body;
                _lyrics.Add(new LyricLine(baseTime, plainText, words));
            }
            _lyrics.Sort((a, b) => a.Time.CompareTo(b.Time));
            RenderLyrics();
            var wordCount = _lyrics.Sum(x => x.Words.Count);
            StatusText.Text = wordCount > 0
                ? $"WORD-TIMED LYRICS  ·  {_lyrics.Count} LINES / {wordCount} WORDS"
                : $"LINE-TIMED LYRICS  ·  {_lyrics.Count} LINES";
            _overlay?.SetLyrics(_lyrics);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't read this lyrics file.\n\n{ex.Message}", "Soul Lyrics", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static TimeSpan ParseTimestamp(string minutes, string seconds, string fraction)
    {
        var ms = int.Parse(string.IsNullOrEmpty(fraction) ? "0" : fraction.PadRight(3, '0')[..3], CultureInfo.InvariantCulture);
        return TimeSpan.FromMinutes(int.Parse(minutes, CultureInfo.InvariantCulture))
             + TimeSpan.FromSeconds(int.Parse(seconds, CultureInfo.InvariantCulture))
             + TimeSpan.FromMilliseconds(ms);
    }

    private void RenderLyrics()
    {
        LyricsLinesPanel.Children.Clear();
        _lineBlocks.Clear();
        if (_lyrics.Count == 0)
        {
            LyricsLinesPanel.Children.Add(new TextBlock
            {
                Text = "No timestamped lyrics loaded.", FontSize = 20,
                Foreground = new SolidColorBrush(Color.FromRgb(146, 146, 164)),
                TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(8, 30, 8, 12)
            });
            return;
        }
        foreach (var lyric in _lyrics)
        {
            var block = new TextBlock
            {
                Tag = lyric, FontSize = 19, FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(116, 116, 132)),
                TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(4, 9, 4, 9)
            };
            block.Text = lyric.Text;
            LyricsLinesPanel.Children.Add(block);
            _lineBlocks.Add(block);
        }
        _activeLine = -1;
    }

    private void UpdatePlaybackUi()
    {
        if (_reader is null) return;
        if (_reader.TotalTime.TotalSeconds > 0)
            SeekSlider.Value = Math.Clamp(_reader.CurrentTime.TotalSeconds / _reader.TotalTime.TotalSeconds * 100, 0, 100);
        CurrentTimeText.Text = FormatTime(_reader.CurrentTime);

        var active = -1;
        for (var i = 0; i < _lyrics.Count; i++)
        {
            if (_lyrics[i].Time <= _reader.CurrentTime) active = i;
            else break;
        }
        if (active != _activeLine)
        {
            _activeLine = active;
            for (var i = 0; i < _lineBlocks.Count; i++)
            {
                var block = _lineBlocks[i];
                block.Foreground = new SolidColorBrush(i == active ? Color.FromRgb(255, 92, 154) : Color.FromRgb(116, 116, 132));
                block.FontSize = i == active ? 23 : 19;
                block.FontWeight = i == active ? FontWeights.Bold : FontWeights.Medium;
            }
        }

        // Rebuild inline word runs only for the active line, allowing word-by-word highlighting.
        if (active >= 0 && active < _lineBlocks.Count)
            RenderActiveWords(_lineBlocks[active], _lyrics[active], _reader.CurrentTime);
        _overlay?.UpdatePosition(_reader.CurrentTime);
    }

    private static void RenderActiveWords(TextBlock block, LyricLine lyric, TimeSpan position)
    {
        if (lyric.Words.Count == 0) { block.Text = lyric.Text; return; }
        block.Inlines.Clear();
        for (var i = 0; i < lyric.Words.Count; i++)
        {
            var word = lyric.Words[i];
            var end = i + 1 < lyric.Words.Count ? lyric.Words[i + 1].Time : TimeSpan.MaxValue;
            var isActive = position >= word.Time && position < end;
            block.Inlines.Add(new Run(word.Text)
            {
                Foreground = new SolidColorBrush(isActive ? Color.FromRgb(255, 255, 255) : Color.FromRgb(255, 92, 154)),
                FontWeight = isActive ? FontWeights.ExtraBold : FontWeights.SemiBold
            });
        }
    }

    private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_reader is null || Mouse.LeftButton != MouseButtonState.Pressed) return;
        _reader.CurrentTime = TimeSpan.FromSeconds(_reader.TotalTime.TotalSeconds * SeekSlider.Value / 100);
    }

    private void ShowOverlay_Click(object sender, RoutedEventArgs e)
    {
        if (_overlay is null || !_overlay.IsVisible)
        {
            _overlay = new LyricsOverlay(_lyrics);
            _overlay.Show();
        }
        else _overlay.Activate();
    }

    private static string FormatTime(TimeSpan time) => $"{(int)time.TotalMinutes}:{time.Seconds:00}";

    private void StopAndDisposeAudio()
    {
        try { _output?.Stop(); } catch { }
        _output?.Dispose();
        _reader?.Dispose();
        _output = null;
        _reader = null;
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _overlay?.Close();
        StopAndDisposeAudio();
        base.OnClosed(e);
    }
}

public sealed record LyricWord(TimeSpan Time, string Text);
public sealed record LyricLine(TimeSpan Time, string Text, List<LyricWord> Words);
