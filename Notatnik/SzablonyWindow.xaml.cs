using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Notatnik
{
    public partial class SzablonyWindow : Window
    {
        private ObservableCollection<SzablonZadania> Szablony;

        public SzablonyWindow(ObservableCollection<SzablonZadania> szablony)
        {
            InitializeComponent();
            Szablony = szablony;
            SzablonyListView.ItemsSource = Szablony;
        }

        private void DodajSzablonButton_Click(object sender, RoutedEventArgs e)
        {
            var nowySzablon = new SzablonZadania
            {
                NazwaSzablonu = "Nowy szablon",
                Tytul = "",
                Opis = "",
                Kategoria = "Inne",
                Priorytet = 3,
                CzasTrwania = TimeSpan.FromHours(1)
            };

            var edytujWindow = new EdytujSzablonWindow(nowySzablon);
            edytujWindow.Owner = this;
            if (edytujWindow.ShowDialog() == true)
            {
                Szablony.Add(nowySzablon);
            }
        }

        private void EdytujSzablonButton_Click(object sender, RoutedEventArgs e)
        {
            if (SzablonyListView.SelectedItem is SzablonZadania szablon)
            {
                var edytujWindow = new EdytujSzablonWindow(szablon);
                edytujWindow.Owner = this;
                edytujWindow.ShowDialog();
                SzablonyListView.Items.Refresh();
            }
        }

        private void UsunSzablonButton_Click(object sender, RoutedEventArgs e)
        {
            if (SzablonyListView.SelectedItem is SzablonZadania szablon)
            {
                var result = MessageBox.Show($"Czy na pewno usunąć szablon: {szablon.NazwaSzablonu}?",
                    "Potwierdź usunięcie", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    Szablony.Remove(szablon);
                }
            }
        }

        private void ZamknijButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}