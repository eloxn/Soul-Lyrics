using System.Windows;

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
        CurrentLyricText.Text = line?.Text ?? "♪";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
