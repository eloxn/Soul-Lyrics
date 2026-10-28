using Microsoft.Win32;
using NAudio.Wave;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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

    public MainWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
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

    private void LoadLrc(string path)
    {
        try
        {
            _lyrics.Clear();
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                var matches = System.Text.RegularExpressions.Regex.Matches(line, @"\[(\d{1,2}):(\d{2})(?:\.(\d{1,3}))?\]");
                if (matches.Count == 0) continue;
                var textStart = matches[^1].Index + matches[^1].Length;
                var text = line[textStart..].Trim();
                if (string.IsNullOrWhiteSpace(text)) continue;
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var minutes = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    var seconds = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    var fraction = match.Groups[3].Success ? match.Groups[3].Value : "0";
                    var milliseconds = int.Parse(fraction.PadRight(3, '0')[..3], CultureInfo.InvariantCulture);
                    _lyrics.Add(new LyricLine(TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(milliseconds), text));
                }
            }
            _lyrics.Sort((a, b) => a.Time.CompareTo(b.Time));
            RenderLyrics();
            StatusText.Text = $"LYRICS LOADED  ·  {_lyrics.Count} TIMED LINES";
            _overlay?.SetLyrics(_lyrics);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't read this lyrics file.\n\n{ex.Message}", "Soul Lyrics", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RenderLyrics()
    {
        LyricsLinesPanel.Children.Clear();
        if (_lyrics.Count == 0)
        {
            LyricsLinesPanel.Children.Add(new TextBlock
            {
                Text = "No timestamped lyrics loaded.",
                FontSize = 20,
                Foreground = new SolidColorBrush(Color.FromRgb(146, 146, 164)),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(8, 30, 8, 12)
            });
            return;
        }
        foreach (var lyric in _lyrics)
        {
            LyricsLinesPanel.Children.Add(new TextBlock
            {
                Text = lyric.Text, Tag = lyric, FontSize = 19,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(116, 116, 132)),
                TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(4, 9, 4, 9)
            });
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
            for (var i = 0; i < LyricsLinesPanel.Children.Count; i++)
            {
                if (LyricsLinesPanel.Children[i] is not TextBlock block) continue;
                block.Foreground = new SolidColorBrush(i == active ? Color.FromRgb(255, 92, 154) : Color.FromRgb(116, 116, 132));
                block.FontSize = i == active ? 23 : 19;
                block.FontWeight = i == active ? FontWeights.Bold : FontWeights.Medium;
            }
        }
        _overlay?.UpdatePosition(_reader.CurrentTime);
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

public sealed record LyricLine(TimeSpan Time, string Text);
