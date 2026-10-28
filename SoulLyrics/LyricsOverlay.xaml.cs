using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace SoulLyrics;

public partial class LyricsOverlay : Window
{
    private readonly List<LyricLine> _lyrics = new();

    public LyricsOverlay(IEnumerable<LyricLine> lyrics)
    {
        InitializeComponent();
        SetLyrics(lyrics);
        MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove();
        };
    }

    public void SetLyrics(IEnumerable<LyricLine> lyrics)
    {
        _lyrics.Clear();
        _lyrics.AddRange(lyrics.OrderBy(x => x.Time));
    }

    public void UpdatePosition(TimeSpan position)
    {
        var line = _lyrics.LastOrDefault(x => x.Time <= position);
        CurrentLyricText.Inlines.Clear();
        if (line is null)
        {
            CurrentLyricText.Inlines.Add(new Run("♪") { Foreground = new SolidColorBrush(Color.FromRgb(255, 92, 154)) });
            return;
        }

        if (line.Words.Count == 0)
        {
            CurrentLyricText.Inlines.Add(new Run(line.Text));
            return;
        }

        for (var i = 0; i < line.Words.Count; i++)
        {
            var word = line.Words[i];
            var end = i + 1 < line.Words.Count ? line.Words[i + 1].Time : TimeSpan.MaxValue;
            var active = position >= word.Time && position < end;
            CurrentLyricText.Inlines.Add(new Run(word.Text)
            {
                Foreground = new SolidColorBrush(active ? Colors.White : Color.FromRgb(255, 92, 154)),
                FontWeight = active ? FontWeights.ExtraBold : FontWeights.SemiBold
            });
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
