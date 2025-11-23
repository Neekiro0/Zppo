using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Notatnik
{
    public partial class Gra : Page
    {
        private const int Rows = 6;
        private const int Cols = 5;

        private List<List<Tile>> board;
        private int currentRow = 0;
        private int currentCol = 0;
        private string targetWord;
        private HashSet<string> validWords;
        public Gra()
        {
            InitializeComponent();
            InitializeBoard();
            LoadWordList();
            PickTargetWord();
        }

        // płytka
        public class Tile
        {
            public string Letter { get; set; }
            public Brush Background { get; set; } = Brushes.White;
        }

        // lista słów
        private void LoadWordList()
        {
            var words = new[]
            {
                "APPLE","BRAVE","CRANE","DANCE","EAGLE","FRAME","GHOST","HOUSE","INPUT","JEWEL",
                "KNIFE","LIGHT","MONEY","NIGHT","OCEAN","POINT","QUICK","RIVER","STONE","TIGER",
                "UNION","VIRUS","WATER","YOUTH","ZEBRA"
            };
            validWords = new HashSet<string>(words);
        }

        //Guzik nowa gra
        private void RefreshBoard()
        {
            BoardRows.Items.Refresh();
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            currentRow = 0;
            currentCol = 0;

            // reset planszy
            foreach (var row in board)
                foreach (var tile in row)
                {
                    tile.Letter = "";
                    tile.Background = Brushes.White;
                }

            RefreshBoard();

            PickTargetWord();
            EnableInput();
        }

        private void InitializeBoard()
        {
            board = new List<List<Tile>>();
            for (int r = 0; r < Rows; r++)
            {
                var row = new List<Tile>();
                for (int c = 0; c < Cols; c++)
                {
                    row.Add(new Tile { Letter = "", Background = Brushes.White });
                }
                board.Add(row);
            }
            BoardRows.ItemsSource = board;
        }        
        private void PickTargetWord()
        {
            var rnd = new Random();
            var list = validWords.ToList();
            targetWord = list[rnd.Next(list.Count)].ToUpper();
        }

        // Klawiatura
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(this);
            this.PreviewKeyDown += Gra_PreviewKeyDown;
        }

        private void EnableInput()
        {
            this.KeyDown += Gra_PreviewKeyDown;
        }
        private void DisableInput()
        {
            this.PreviewKeyDown -= Gra_PreviewKeyDown;
        }

        private void Gra_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                DoBackspace();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                SubmitGuess();
                e.Handled = true;
            }
            else
            {
                var ch = KeyToChar(e.Key);
                if (!string.IsNullOrEmpty(ch))
                {
                    AddLetter(ch);
                    e.Handled = true;
                }
            }
        }
        private void Key_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button b && b.Content is string s)
            {
                AddLetter(s.ToUpper());
            }
        }

        private void Backspace_Click(object sender, RoutedEventArgs e) => DoBackspace();

        private void Enter_Click(object sender, RoutedEventArgs e) => SubmitGuess();

        private string KeyToChar(Key k)
        {
            if (k >= Key.A && k <= Key.Z)
                return k.ToString().ToUpper();
            return null;
        }

        private void AddLetter(string letter)
        {
            if (currentCol >= Cols || currentRow >= Rows) return;
            board[currentRow][currentCol].Letter = letter;
            currentCol++;
            RefreshBoard();
        }

        private void DoBackspace()
        {
            if (currentCol > 0 && currentRow < Rows)
            {
                currentCol--;
                board[currentRow][currentCol].Letter = "";
                RefreshBoard();
            }
        }

        private void SubmitGuess()
        {
            if (currentCol != Cols) return;

            string guess = string.Concat(board[currentRow].Select(t => t.Letter)).ToUpper();

            if (!validWords.Contains(guess))
            {
                MessageBox.Show("Słowo nie znajduje się na liście słów.", "Nieprawidłowe słowo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            // Ocena i kolorowanie
            var targetChars = targetWord.ToCharArray();
            var guessChars = guess.ToCharArray();
            var colorResult = new Brush[Cols];

            for (int i = 0; i < Cols; i++)
            {
                if (guessChars[i] == targetChars[i])
                {
                    colorResult[i] = Brushes.LightGreen;
                    targetChars[i] = '*';
                }
            }

            for (int i = 0; i < Cols; i++)
            {
                if (colorResult[i] != null) continue;
                var idx = Array.IndexOf(targetChars, guessChars[i]);
                if (idx >= 0)
                {
                    colorResult[i] = Brushes.Gold;
                    targetChars[idx] = '*';
                }
                else
                {
                    colorResult[i] = Brushes.LightGray;
                }
            }

            for (int i = 0; i < Cols; i++)
            {
                board[currentRow][i].Background = colorResult[i];
            }
            RefreshBoard();


            // Wyniki
            if (guess == targetWord)
            {
                MessageBox.Show($"Brawo! Odgadłeś słowo: {targetWord}", "Wygrałeś", MessageBoxButton.OK, MessageBoxImage.Information);
                DisableInput();
                return;
            }

            currentRow++;
            currentCol = 0;

            if (currentRow >= Rows)
            {
                MessageBox.Show($"Koniec gry. Słowo to: {targetWord}", "Przegrana", MessageBoxButton.OK, MessageBoxImage.Information);
                DisableInput();
            }
        }
    }
}