using System;
using System.Windows;
using System.Windows.Controls;

namespace Notatnik
{
    public partial class EdytujSzablonWindow : Window
    {
        private SzablonZadania Szablon;

        public EdytujSzablonWindow(SzablonZadania szablon)
        {
            InitializeComponent();
            Szablon = szablon;
            InitializeControls();
            LoadSzablonData();
        }

        private void InitializeControls()
        {
            KategoriaComboBox.ItemsSource = new string[] { "Praca", "Dom", "Hobby", "Zdrowie", "Finanse", "Inne" };
            PriorytetComboBox.ItemsSource = new string[] { "1 ☆", "2 ☆☆", "3 ☆☆☆", "4 ☆☆☆☆", "5 ☆☆☆☆☆" };
        }

        private void LoadSzablonData()
        {
            NazwaTextBox.Text = Szablon.NazwaSzablonu;
            TytulTextBox.Text = Szablon.Tytul;
            OpisTextBox.Text = Szablon.Opis;
            KategoriaComboBox.SelectedItem = Szablon.Kategoria;
            PriorytetComboBox.SelectedIndex = Szablon.Priorytet - 1;
            CzasTextBox.Text = Szablon.CzasTrwania.TotalHours.ToString();
        }

        private void ZapiszButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NazwaTextBox.Text))
            {
                MessageBox.Show("Nazwa szablonu jest wymagana.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Szablon.NazwaSzablonu = NazwaTextBox.Text;
            Szablon.Tytul = TytulTextBox.Text;
            Szablon.Opis = OpisTextBox.Text;
            Szablon.Kategoria = KategoriaComboBox.SelectedItem?.ToString() ?? "Inne";
            Szablon.Priorytet = PriorytetComboBox.SelectedIndex + 1;

            if (double.TryParse(CzasTextBox.Text, out double godziny))
            {
                Szablon.CzasTrwania = TimeSpan.FromHours(godziny);
            }

            this.DialogResult = true;
            this.Close();
        }

        private void AnulujButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}