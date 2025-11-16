using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Notatnik
{
    public partial class StatystykiWindow : Window
    {
        public StatystykiWindow(ObservableCollection<PlanerItem> tasks)
        {
            InitializeComponent();
            PokazStatystyki(tasks);
        }

        private void PokazStatystyki(ObservableCollection<PlanerItem> tasks)
        {
            var ostatniTydzien = DateTime.Today.AddDays(-7);
            var ostatniMiesiac = DateTime.Today.AddDays(-30);

            var wszystkieZadania = tasks.Count;
            var zrealizowane = tasks.Count(t => t.CzyZrealizowane);
            var niezrealizowane = tasks.Count(t => !t.CzyZrealizowane);
            var zTygodnia = tasks.Count(t => t.Data >= ostatniTydzien);
            var zMiesiaca = tasks.Count(t => t.Data >= ostatniMiesiac);

            double procentZrealizowane = wszystkieZadania > 0 ? (zrealizowane * 100.0 / wszystkieZadania) : 0;
            double procentNiezrealizowane = wszystkieZadania > 0 ? (niezrealizowane * 100.0 / wszystkieZadania) : 0;

            var statystykiKategorii = tasks
                .GroupBy(t => t.Kategoria)
                .Select(g => new { Kategoria = g.Key ?? "Brak", Liczba = g.Count() })
                .OrderByDescending(x => x.Liczba);

            StatystykiTextBlock.Text = $@"STATYSTYKI OGÓLNE:
                Wszystkie zadania: {wszystkieZadania}
                Zrealizowane: {zrealizowane} ({procentZrealizowane:F1}%)
                Niezrealizowane: {niezrealizowane} ({procentNiezrealizowane:F1}%)
                Z ostatniego tygodnia: {zTygodnia}
                Z ostatniego miesiąca: {zMiesiaca}

                STATYSTYKI KATEGORII:
                ";

            foreach (var stat in statystykiKategorii)
            {
                StatystykiTextBlock.Text += $"{stat.Kategoria}: {stat.Liczba} zadań\n";
            }

            var wazneZadania = tasks
                .Where(t => !t.CzyZrealizowane && t.Priorytet >= 4)
                .OrderByDescending(t => t.Priorytet)
                .ThenBy(t => t.Data)
                .Take(10);

            if (wazneZadania.Any())
            {
                StatystykiTextBlock.Text += "\nNAJWAŻNIEJSZE ZADANIA:\n";
                foreach (var zadanie in wazneZadania)
                {
                    StatystykiTextBlock.Text += $"{zadanie.PriorytetText} {zadanie.Tytul} - {zadanie.Data:dd.MM.yyyy}\n";
                }
            }
        }

        private void ZamknijButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}