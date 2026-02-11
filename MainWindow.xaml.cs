using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace gombaszedés
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int BoardSize = 8;
        private const int MushroomsCount = 5;
        private readonly Random _rnd = new();
        private List<int> _sequence = new();
        private Dictionary<int, int> _posToIndex = new();
        private int _currentIndex = 0;
        private int _bastyaPos = -1;

        private BitmapImage _mushroomImg;
        private BitmapImage _smileImg;
        private BitmapImage _sadImg;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                _mushroomImg = new BitmapImage(new Uri($"pack://application:,,,/{asm};component/Images/mushroom.png", UriKind.Absolute));
                _smileImg = new BitmapImage(new Uri($"pack://application:,,,/{asm};component/Images/smile.png", UriKind.Absolute));
                _sadImg = new BitmapImage(new Uri($"pack://application:,,,/{asm};component/Images/sad.png", UriKind.Absolute));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba képek betöltésénél: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                _mushroomImg = new BitmapImage();
                _smileImg = new BitmapImage();
                _sadImg = new BitmapImage();
            }

            BuildEmptyBoard();
        }

        private void BuildEmptyBoard()
        {
            BoardGrid.Children.Clear();
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    var btn = new Button
                    {
                        Tag = r * BoardSize + c,
                        Background = ((r + c) % 2 == 0) ? Brushes.White : Brushes.Gray,
                        BorderBrush = Brushes.Black,
                        Padding = new Thickness(0),
                    };
                    btn.Click += Square_Click;
                    BoardGrid.Children.Add(btn);
                }
            }
        }

        private async void Square_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            int pos = (int)btn.Tag;

            if (_sequence.Count == 0)
            {
                await ShowResultAsync(false);
                return;
            }

            if (!_posToIndex.TryGetValue(pos, out int idx))
            {
                await ShowResultAsync(false);
                return;
            }

            if (idx != _currentIndex)
            {
                await ShowResultAsync(false);
                return;
            }

            if (_bastyaPos >= 0)
            {
                try
                {
                    var prev = GetButtonAt(_bastyaPos);
                    prev.Content = null;
                }
                catch { }
            }

            btn.Content = new TextBlock { Text = "♖", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            _bastyaPos = pos;

            _posToIndex.Remove(pos);
            _currentIndex++;
            StatusText.Text = $"Következő: {_currentIndex} / {MushroomsCount}";

            await ShowResultAsync(true);

            if (_currentIndex >= MushroomsCount)
            {
                StatusText.Text = "Gratulálok!";
                MessageBox.Show("Megnyerted! Minden gomba összeszedve.", "Győzelem", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task ShowResultAsync(bool correct)
        {
            ResultImage.Source = correct ? _smileImg : _sadImg;
            await Task.Delay(700);
            ResultImage.Source = null;
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        private void StartNewGame()
        {
            BuildEmptyBoard();
            _sequence.Clear();
            _posToIndex.Clear();
            _currentIndex = 0;
            ResultImage.Source = null;

            bool ok = TryGeneratePath(out List<int> seq);
            if (!ok)
            {
                StatusText.Text = "Nem sikerült elhelyezni — próbáld újra.";
                return;
            }

            _sequence = seq;
            if (_bastyaPos >= 0)
            {
                var rookBtn = GetButtonAt(_bastyaPos);
                rookBtn.Content = new TextBlock { Text = "♖", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            }

            for (int i = 0; i < _sequence.Count; i++)
            {
                _posToIndex[_sequence[i]] = i;
                var btn = GetButtonAt(_sequence[i]);
                btn.Content = new Image { Source = _mushroomImg, Stretch = System.Windows.Media.Stretch.Uniform };
            }

            _currentIndex = 0;
            StatusText.Text = "Kezdés: kattints a 1. gombára (♖)";
        }

        private Button GetButtonAt(int pos)
        {
            return (Button)BoardGrid.Children[pos];
        }

        private bool TryGeneratePath(out List<int> mushrooms)
        {
            mushrooms = new List<int>();
            int maxAttempts = 300;
            int targetLength = MushroomsCount + 1;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int start = _rnd.Next(BoardSize * BoardSize);
                var path = new List<int> { start };
                if (ExtendPathRandomized(path, targetLength))
                {
                    if (path.Count >= targetLength)
                    {
                        _bastyaPos = path[0];
                        mushrooms = path.Skip(1).Take(MushroomsCount).ToList();
                        return true;
                    }
                }
            }
            return false;
        }

        private bool ExtendPathRandomized(List<int> path, int targetCount)
        {
            int maxSteps = 10000;
            int steps = 0;
            return Backtrack(path, ref steps, maxSteps, targetCount);
        }

        private bool Backtrack(List<int> path, ref int steps, int maxSteps, int targetCount)
        {
            if (steps++ > maxSteps) return false;
            if (path.Count >= targetCount) return true;

            var moves = GetLegalMoves(path.Last())
                        .Where(p => !path.Contains(p))
                        .OrderBy(_ => _rnd.Next()).ToList();

            foreach (var next in moves)
            {
                path.Add(next);
                if (Backtrack(path, ref steps, maxSteps, targetCount))
                    return true;
                path.RemoveAt(path.Count - 1);
            }
            return false;
        }

        private IEnumerable<int> GetLegalMoves(int pos)
        {
            int r = pos / BoardSize;
            int c = pos % BoardSize;
            return GetRookMoves(r, c);
        }

        private IEnumerable<int> GetRookMoves(int r, int c)
        {
            var list = new List<int>();
            for (int cc = 0; cc < BoardSize; cc++) if (cc != c) list.Add(r * BoardSize + cc);
            for (int rr = 0; rr < BoardSize; rr++) if (rr != r) list.Add(rr * BoardSize + c);
            return list;
        }

        private bool InBoard(int r, int c) => r >= 0 && r < BoardSize && c >= 0 && c < BoardSize;
    }
}