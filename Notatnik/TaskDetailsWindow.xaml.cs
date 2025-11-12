using System;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace Notatnik
{
    public partial class TaskDetailsWindow : Window
    {
        private PlanerItem OriginalTask { get; set; }
        private PlanerItem EditedTask { get; set; }

        public TaskDetailsWindow(PlanerItem task)
        {
            InitializeComponent();

            OriginalTask = task;
            EditedTask = new PlanerItem
            {
                Data = task.Data,
                Tytul = task.Tytul,
                Opis = task.Opis,
                Poczucie = task.Poczucie,
                CzyZrealizowane = task.CzyZrealizowane
            };

            TytulTextBox.Text = EditedTask.Tytul;
            OpisTextBox.Text = EditedTask.Opis;
            PoczucieComboBox.Text = EditedTask.Poczucie;
            ZrealizowaneCheckBox.IsChecked = EditedTask.CzyZrealizowane;

            DataDatePicker.SelectedDate = EditedTask.Data.Date;
            TimeTextBox.Text = EditedTask.Data.ToString("HH:mm");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TytulTextBox.Text))
            {
                MessageBox.Show("Tytuł zadania nie może być pusty.", "Błąd Walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(TimeTextBox.Text, out TimeSpan time))
            {
                MessageBox.Show("Nieprawidłowy format czasu. Użyj formatu HH:mm (np. 14:30).", "Błąd Walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DataDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Proszę wybrać datę.", "Błąd Walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EditedTask.Tytul = TytulTextBox.Text.Trim();
            EditedTask.Opis = OpisTextBox.Text ?? string.Empty;
            EditedTask.Poczucie = PoczucieComboBox.Text ?? string.Empty;
            EditedTask.CzyZrealizowane = ZrealizowaneCheckBox.IsChecked ?? false;

            DateTime selectedDate = DataDatePicker.SelectedDate.Value;
            EditedTask.Data = selectedDate.Date.Add(time);

            OriginalTask.Data = EditedTask.Data;
            OriginalTask.Tytul = EditedTask.Tytul;
            OriginalTask.Opis = EditedTask.Opis;
            OriginalTask.Poczucie = EditedTask.Poczucie;
            OriginalTask.CzyZrealizowane = EditedTask.CzyZrealizowane;

            DialogResult = true;
        }
    }
}