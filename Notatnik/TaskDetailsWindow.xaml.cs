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
                Nastroj = task.Nastroj,
                CzyZrealizowane = task.CzyZrealizowane,
                Kategoria = task.Kategoria,
                Priorytet = task.Priorytet,
                CzasTrwania = task.CzasTrwania
            };

            InitializeControls();
            LoadTaskData();
        }

        private void InitializeControls()
        {
            KategoriaComboBox.ItemsSource = new string[] { "Praca", "Dom", "Hobby", "Zdrowie", "Finanse", "Inne" };
            PriorytetComboBox.ItemsSource = new string[] { "1 ☆", "2 ☆☆", "3 ☆☆☆", "4 ☆☆☆☆", "5 ☆☆☆☆☆" };
            PoczucieComboBox.ItemsSource = new string[]
            {
                "", "Dobry", "Świetny", "Zmęczony", "Zestresowany", "Radosny",
                "Smutny", "Zły", "Spokojny", "Energrtyczny", "Obojętny"
            };
        }

        private void LoadTaskData()
        {
            TytulTextBox.Text = EditedTask.Tytul;
            OpisTextBox.Text = EditedTask.Opis;
            PoczucieComboBox.Text = EditedTask.Nastroj;
            ZrealizowaneCheckBox.IsChecked = EditedTask.CzyZrealizowane;

            DataDatePicker.SelectedDate = EditedTask.Data.Date;
            TimeTextBox.Text = EditedTask.Data.ToString("HH:mm");

            KategoriaComboBox.SelectedItem = EditedTask.Kategoria;
            PriorytetComboBox.SelectedIndex = EditedTask.Priorytet - 1;
            CzasTrwaniaTextBox.Text = EditedTask.CzasTrwania.TotalHours.ToString("0.0");
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

            if (!double.TryParse(CzasTrwaniaTextBox.Text, out double godziny) || godziny <= 0)
            {
                MessageBox.Show("Nieprawidłowy czas trwania. Wprowadź liczbę godzin (np. 1.5).", "Błąd Walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EditedTask.Tytul = TytulTextBox.Text.Trim();
            EditedTask.Opis = OpisTextBox.Text ?? string.Empty;
            EditedTask.Nastroj = PoczucieComboBox.Text ?? string.Empty;
            EditedTask.CzyZrealizowane = ZrealizowaneCheckBox.IsChecked ?? false;
            EditedTask.Kategoria = KategoriaComboBox.SelectedItem?.ToString() ?? "Inne";
            EditedTask.Priorytet = PriorytetComboBox.SelectedIndex + 1;
            EditedTask.CzasTrwania = TimeSpan.FromHours(godziny);

            DateTime selectedDate = DataDatePicker.SelectedDate.Value;
            EditedTask.Data = selectedDate.Date.Add(time);

            OriginalTask.Data = EditedTask.Data;
            OriginalTask.Tytul = EditedTask.Tytul;
            OriginalTask.Opis = EditedTask.Opis;
            OriginalTask.Nastroj = EditedTask.Nastroj;
            OriginalTask.CzyZrealizowane = EditedTask.CzyZrealizowane;
            OriginalTask.Kategoria = EditedTask.Kategoria;
            OriginalTask.Priorytet = EditedTask.Priorytet;
            OriginalTask.CzasTrwania = EditedTask.CzasTrwania;

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TimeTextBox.Text.Length == 2 && !TimeTextBox.Text.Contains(":"))
            {
                TimeTextBox.Text += ":";
                TimeTextBox.CaretIndex = TimeTextBox.Text.Length;
            }
        }

        private void CzasTrwaniaTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
            e.Handled = !regex.IsMatch(CzasTrwaniaTextBox.Text + e.Text);
        }
    }
}