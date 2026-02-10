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
        // sequence positions as single ints r*8 + c
        private List<int> _sequence = new();
        private Dictionary<int, int> _posToIndex = new();
        private int _currentIndex = 0;

        // images (pack URIs for Resource images placed in Images folder)
        private readonly BitmapImage _mushroomImg = new(new Uri("pack://application:,,,/Images/mushroom.png"));
        private readonly BitmapImage _smileImg = new(new Uri("pack://application:,,,/Images/smile.png"));
        private readonly BitmapImage _sadImg = new(new Uri("pack://application:,,,/Images/sad.png"));

        public MainWindow()
        {
            InitializeComponent();
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
                        Background = ((r + c) % 2 == 0) ? Brushes.Beige : Brushes.SaddleBrown,
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

            // If there is no active game
            if (_sequence.Count == 0)
            {
                await ShowResultAsync(false);
                return;
            }

            // Is clicked pos a mushroom?
            if (!_posToIndex.TryGetValue(pos, out int idx))
            {
                // not a mushroom
                await ShowResultAsync(false);
                return;
            }

            if (idx != _currentIndex)
            {
                // wrong mushroom in sequence
                await ShowResultAsync(false);
                return;
            }

            // correct
            // replace the image on that button with a smile image (and remove mapping)
            btn.Content = new Image { Source = _smileImg, Stretch = System.Windows.Media.Stretch.Uniform };
            _posToIndex.Remove(pos);
            _currentIndex++;
            StatusText.Text = $"Következő: {_currentIndex + 1} / {MushroomsCount}";

            await ShowResultAsync(true);

            if (_currentIndex >= MushroomsCount)
            {
                StatusText.Text = "Gratulálok — minden gomba összeszedve!";
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

            string piece = (PieceCombo.SelectedItem as ComboBoxItem)?.Content as string ?? "Rook";

            bool ok = TryGeneratePathForPiece(piece, out List<int> seq);
            if (!ok)
            {
                StatusText.Text = "Nem sikerült elhelyezni — próbáld újra.";
                return;
            }

            _sequence = seq;
            for (int i = 0; i < _sequence.Count; i++)
            {
                _posToIndex[_sequence[i]] = i;
                // place mushroom image on that button
                var btn = GetButtonAt(_sequence[i]);
                btn.Content = new Image { Source = _mushroomImg, Stretch = System.Windows.Media.Stretch.Uniform };
            }

            _currentIndex = 0;
            StatusText.Text = $"Kezdés: kattints a 1. gombára ({piece})";
        }

        private Button GetButtonAt(int pos)
        {
            // BoardGrid.Children order is row-major
            return (Button)BoardGrid.Children[pos];
        }

        private bool TryGeneratePathForPiece(string piece, out List<int> path)
        {
            path = new List<int>();
            int maxAttempts = 300;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // pick random start
                int start = _rnd.Next(BoardSize * BoardSize);
                path.Clear();
                path.Add(start);

                // depth-first randomized backtracking
                if (ExtendPathRandomized(piece, path))
                {
                    if (path.Count >= MushroomsCount)
                    {
                        path = path.Take(MushroomsCount).ToList();
                        return true;
                    }
                }
            }
            return false;
        }

        private bool ExtendPathRandomized(string piece, List<int> path)
        {
            // attempt to reach MushroomsCount by backtracking
            int maxSteps = 10000;
            int steps = 0;
            return Backtrack(path, piece, ref steps, maxSteps);
        }

        private bool Backtrack(List<int> path, string piece, ref int steps, int maxSteps)
        {
            if (steps++ > maxSteps) return false;
            if (path.Count >= MushroomsCount) return true;

            var moves = GetLegalMoves(piece, path.Last())
                        .Where(p => !path.Contains(p))
                        .OrderBy(_ => _rnd.Next()).ToList();

            // try each next
            foreach (var next in moves)
            {
                path.Add(next);
                if (Backtrack(path, piece, ref steps, maxSteps))
                    return true;
                path.RemoveAt(path.Count - 1);
            }
            return false;
        }

        private IEnumerable<int> GetLegalMoves(string piece, int pos)
        {
            int r = pos / BoardSize;
            int c = pos % BoardSize;
            switch (piece)
            {
                case "Rook":
                    return GetRookMoves(r, c);
                case "Bishop":
                    return GetBishopMoves(r, c);
                case "Queen":
                    return GetQueenMoves(r, c);
                case "Knight":
                    return GetKnightMoves(r, c);
                default:
                    return Enumerable.Empty<int>();
            }
        }

        private IEnumerable<int> GetRookMoves(int r, int c)
        {
            var list = new List<int>();
            for (int cc = 0; cc < BoardSize; cc++) if (cc != c) list.Add(r * BoardSize + cc);
            for (int rr = 0; rr < BoardSize; rr++) if (rr != r) list.Add(rr * BoardSize + c);
            return list;
        }

        private IEnumerable<int> GetBishopMoves(int r, int c)
        {
            var list = new List<int>();
            for (int dr = -BoardSize; dr <= BoardSize; dr++)
            {
                if (dr == 0) continue;
                int rr = r + dr;
                int cc = c + dr;
                if (InBoard(rr, cc)) list.Add(rr * BoardSize + cc);
                rr = r + dr;
                cc = c - dr;
                if (InBoard(rr, cc)) list.Add(rr * BoardSize + cc);
            }
            return list;
        }

        private IEnumerable<int> GetQueenMoves(int r, int c)
        {
            return GetRookMoves(r, c).Concat(GetBishopMoves(r, c));
        }

        private IEnumerable<int> GetKnightMoves(int r, int c)
        {
            int[] dr = { 2, 1, -1, -2, -2, -1, 1, 2 };
            int[] dc = { 1, 2, 2, 1, -1, -2, -2, -1 };
            var list = new List<int>();
            for (int i = 0; i < dr.Length; i++)
            {
                int rr = r + dr[i];
                int cc = c + dc[i];
                if (InBoard(rr, cc)) list.Add(rr * BoardSize + cc);
            }
            return list;
        }

        private bool InBoard(int r, int c) => r >= 0 && r < BoardSize && c >= 0 && c < BoardSize;
    }
}