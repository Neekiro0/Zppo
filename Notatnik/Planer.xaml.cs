using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Notatnik;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Notatnik
{
    public class PlanerItem
    {
        public DateTime Data { get; set; }
        public string Tytul { get; set; }
        public string Opis { get; set; }
        public string Nastroj { get; set; }
        public bool CzyZrealizowane { get; set; }
        public string Kategoria { get; set; }
        public int Priorytet { get; set; }
        public TimeSpan CzasTrwania { get; set; }
        public string FormatowanaData => Data.ToShortDateString();
        public string PriorytetText => new string('★', Priorytet) + new string('☆', 5 - Priorytet);

        public PlanerItem()
        {
            Kategoria = "Inne";
            Priorytet = 3;
            CzasTrwania = TimeSpan.FromHours(1);
        }
    }

    public class SzablonZadania
    {
        public string NazwaSzablonu { get; set; }
        public string Tytul { get; set; }
        public string Opis { get; set; }
        public string Kategoria { get; set; }
        public int Priorytet { get; set; }
        public TimeSpan CzasTrwania { get; set; }
    }

    public class Przypomnienie
    {
        public DateTime DataPrzypomnienia { get; set; }
        public string Tytul { get; set; }
        public bool CzyWyswietlone { get; set; }
    }

    public partial class Planer : Page
    {
        private ObservableCollection<PlanerItem> AllTasks { get; set; }
        private ObservableCollection<SzablonZadania> Szablony { get; set; }
        private ObservableCollection<Przypomnienie> Przypomnienia { get; set; }

        private const string TasksFileName = "planer_tasks.json";
        private const string TemplatesFileName = "planer_templates.json";
        private const string RemindersFileName = "planer_reminders.json";

        private readonly string tasksFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), TasksFileName);
        private readonly string templatesFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), TemplatesFileName);
        private readonly string remindersFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), RemindersFileName);

        private System.Windows.Controls.ComboBox KategoriaComboBox;
        private System.Windows.Controls.ComboBox PriorytetComboBox;
        private System.Windows.Controls.ComboBox StatusComboBox;
        private System.Windows.Controls.TextBox WyszukajTextBox;
        private System.Windows.Controls.Button FiltrujButton;
        private System.Windows.Controls.Button StatystykiButton;
        private System.Windows.Controls.Button SzablonyButton;
        private System.Windows.Controls.Button ExportIcsButton;

        public Planer()
        {
            InitializeComponent();
            AllTasks = new ObservableCollection<PlanerItem>();
            Szablony = new ObservableCollection<SzablonZadania>();
            Przypomnienia = new ObservableCollection<Przypomnienie>();

            LoadTasks();
            LoadSzablony();
            LoadPrzypomnienia();
            InitializeCustomControls();

            MainCalendar.SelectedDate = DateTime.Today;
            if (MainCalendar.SelectedDate.HasValue)
            {
                UpdateTaskList(MainCalendar.SelectedDate.Value);
            }

            TasksListView.MouseDoubleClick += TasksListView_MouseDoubleClick;

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(1);
            timer.Tick += (s, e) => SprawdzPrzypomnienia();
            timer.Start();
        }

        private void InitializeCustomControls()
        {
            var mainGrid = (System.Windows.Controls.Grid)Content;
            var mainStackPanel = mainGrid.Children
                .OfType<System.Windows.Controls.StackPanel>()
                .FirstOrDefault(sp => sp.Parent == mainGrid);

            if (mainStackPanel == null)
            {
                mainStackPanel = new System.Windows.Controls.StackPanel();
                mainGrid.Children.Clear();
                mainGrid.Children.Add(mainStackPanel);

                var calendar = mainGrid.Children.OfType<System.Windows.Controls.Calendar>().FirstOrDefault();
                if (calendar != null)
                {
                    mainGrid.Children.Remove(calendar);
                    mainStackPanel.Children.Add(calendar);
                }

                return;
            }

            var filterPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new System.Windows.Thickness(0, 10, 0, 10)
            };

            WyszukajTextBox = new System.Windows.Controls.TextBox
            {
                Width = 200,
                Margin = new System.Windows.Thickness(5),
                ToolTip = "Wyszukaj zadania..."
            };
            WyszukajTextBox.TextChanged += WyszukajTextBox_TextChanged;
            filterPanel.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Szukaj:",
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            });
            filterPanel.Children.Add(WyszukajTextBox);

            KategoriaComboBox = new System.Windows.Controls.ComboBox
            {
                Width = 120,
                Margin = new System.Windows.Thickness(5),
                ItemsSource = new ObservableCollection<string>
        {
            "Wszystkie", "Praca", "Dom", "Hobby", "Zdrowie", "Finanse", "Inne"
        },
                SelectedIndex = 0
            };
            filterPanel.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Kategoria:",
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            });
            filterPanel.Children.Add(KategoriaComboBox);

            PriorytetComboBox = new System.Windows.Controls.ComboBox
            {
                Width = 100,
                Margin = new System.Windows.Thickness(5),
                ItemsSource = new ObservableCollection<string>
        {
            "Wszystkie", "1 ☆", "2 ☆☆", "3 ☆☆☆", "4 ☆☆☆☆", "5 ☆☆☆☆☆"
        },
                SelectedIndex = 0
            };
            filterPanel.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Priorytet:",
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            });
            filterPanel.Children.Add(PriorytetComboBox);

            StatusComboBox = new System.Windows.Controls.ComboBox
            {
                Width = 120,
                Margin = new System.Windows.Thickness(5),
                ItemsSource = new ObservableCollection<string>
        {
            "Wszystkie", "Do zrobienia", "Zrealizowane"
        },
                SelectedIndex = 0
            };
            filterPanel.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Status:",
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            });
            filterPanel.Children.Add(StatusComboBox);

            FiltrujButton = new System.Windows.Controls.Button
            {
                Content = "Filtruj",
                Margin = new System.Windows.Thickness(5),
                Padding = new System.Windows.Thickness(10, 5, 10, 5)
            };
            FiltrujButton.Click += FiltrujButton_Click;
            filterPanel.Children.Add(FiltrujButton);

            int insertIndex = mainStackPanel.Children.IndexOf(SelectedDateTextBlock) + 1;
            mainStackPanel.Children.Insert(insertIndex, filterPanel);

            var additionalButtonsPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new System.Windows.Thickness(0, 10, 0, 10)
            };

            StatystykiButton = new System.Windows.Controls.Button
            {
                Content = "Statystyki",
                Margin = new System.Windows.Thickness(5),
                Padding = new System.Windows.Thickness(10, 5, 10, 5)
            };
            StatystykiButton.Click += StatystykiButton_Click;
            additionalButtonsPanel.Children.Add(StatystykiButton);

            SzablonyButton = new System.Windows.Controls.Button
            {
                Content = "Szablony",
                Margin = new System.Windows.Thickness(5),
                Padding = new System.Windows.Thickness(10, 5, 10, 5)
            };
            SzablonyButton.Click += SzablonyButton_Click;
            additionalButtonsPanel.Children.Add(SzablonyButton);

            ExportIcsButton = new System.Windows.Controls.Button
            {
                Content = "Export ICS",
                Margin = new System.Windows.Thickness(5),
                Padding = new System.Windows.Thickness(10, 5, 10, 5)
            };
            ExportIcsButton.Click += ExportIcsButton_Click;
            additionalButtonsPanel.Children.Add(ExportIcsButton);

            var mainButtonsPanel = mainStackPanel.Children
                .OfType<System.Windows.Controls.StackPanel>()
                .FirstOrDefault(sp => sp.Children.OfType<System.Windows.Controls.Button>().Any(b => b.Content.ToString() == "Dodaj Zadanie"));

            if (mainButtonsPanel != null)
            {
                int buttonsIndex = mainStackPanel.Children.IndexOf(mainButtonsPanel) + 1;
                mainStackPanel.Children.Insert(buttonsIndex, additionalButtonsPanel);
            }
            else
            {
                mainStackPanel.Children.Add(additionalButtonsPanel);
            }
        }
        private void LoadSzablony()
        {
            if (File.Exists(templatesFilePath))
            {
                try
                {
                    string json = File.ReadAllText(templatesFilePath);
                    var loadedTemplates = JsonSerializer.Deserialize<ObservableCollection<SzablonZadania>>(json);
                    if (loadedTemplates != null)
                    {
                        Szablony = loadedTemplates;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Błąd podczas wczytywania szablonów: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                Szablony.Add(new SzablonZadania
                {
                    NazwaSzablonu = "Spotkanie biznesowe",
                    Tytul = "Spotkanie",
                    Opis = "Spotkanie biznesowe",
                    Kategoria = "Praca",
                    Priorytet = 4,
                    CzasTrwania = TimeSpan.FromHours(1)
                });

                Szablony.Add(new SzablonZadania
                {
                    NazwaSzablonu = "Zakupy",
                    Tytul = "Zakupy",
                    Opis = "Zakupy tygodniowe",
                    Kategoria = "Dom",
                    Priorytet = 2,
                    CzasTrwania = TimeSpan.FromHours(2)
                });

                SaveSzablony();
            }
        }

        private void LoadPrzypomnienia()
        {
            if (File.Exists(remindersFilePath))
            {
                try
                {
                    string json = File.ReadAllText(remindersFilePath);
                    var loadedReminders = JsonSerializer.Deserialize<ObservableCollection<Przypomnienie>>(json);
                    if (loadedReminders != null)
                    {
                        Przypomnienia = loadedReminders;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Błąd podczas wczytywania przypomnień: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveSzablony()
        {
            try
            {
                string json = JsonSerializer.Serialize(Szablony);
                File.WriteAllText(templatesFilePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd podczas zapisywania szablonów: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePrzypomnienia()
        {
            try
            {
                string json = JsonSerializer.Serialize(Przypomnienia);
                File.WriteAllText(remindersFilePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd podczas zapisywania przypomnień: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TasksListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TasksListView.SelectedItem is PlanerItem selectedTask)
            {
                OpenEditDialog(selectedTask);
            }
        }

        private void LoadTasks()
        {
            if (File.Exists(tasksFilePath))
            {
                try
                {
                    string json = File.ReadAllText(tasksFilePath);
                    var loadedTasks = JsonSerializer.Deserialize<ObservableCollection<PlanerItem>>(json);
                    if (loadedTasks != null)
                    {
                        AllTasks = loadedTasks;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Błąd podczas wczytywania zadań: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveTasks()
        {
            try
            {
                string json = JsonSerializer.Serialize(AllTasks);
                File.WriteAllText(tasksFilePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd podczas zapisywania zadań: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTaskList(DateTime selectedDate)
        {
            SelectedDateTextBlock.Text = $"Zadania na dzień: {selectedDate.ToShortDateString()}";

            var tasksForDay = AllTasks.Where(t => t.Data.Date == selectedDate.Date)
                                     .OrderBy(t => t.Data)
                                     .ToList();
            TasksListView.ItemsSource = tasksForDay;
        }

        private void MainCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainCalendar.SelectedDate.HasValue)
            {
                UpdateTaskList(MainCalendar.SelectedDate.Value);
            }
        }

        private void OpenEditDialog(PlanerItem taskToEdit)
        {
            var detailsWindow = new TaskDetailsWindow(taskToEdit);
            if (detailsWindow.ShowDialog() == true)
            {
                SaveTasks();
                UpdateTaskList(taskToEdit.Data);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDate = MainCalendar.SelectedDate ?? DateTime.Today;

            var newItem = new PlanerItem
            {
                Data = selectedDate.Date.Add(DateTime.Now.TimeOfDay),
                Tytul = "",
                Opis = "",
                Nastroj = "",
                CzyZrealizowane = false,
                Kategoria = "Inne",
                Priorytet = 3,
                CzasTrwania = TimeSpan.FromHours(1)
            };

            var detailsWindow = new TaskDetailsWindow(newItem);

            if (detailsWindow.ShowDialog() == true)
            {
                AllTasks.Add(newItem);
                SaveTasks();
                UpdateTaskList(selectedDate);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListView.SelectedItem is PlanerItem selectedTask)
            {
                OpenEditDialog(selectedTask);
            }
            else
            {
                System.Windows.MessageBox.Show("Proszę wybierz zadanie do edycji.", "Brak zaznaczenia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksListView.SelectedItem is PlanerItem selectedTask)
            {
                var result = System.Windows.MessageBox.Show($"Czy na pewno usunąć zadanie: {selectedTask.Tytul}?", "Potwierdź usunięcie", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    AllTasks.Remove(selectedTask);
                    SaveTasks();
                    UpdateTaskList(MainCalendar.SelectedDate ?? DateTime.Today);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Proszę wybierz zadanie do usunięcia.", "Brak zaznaczenia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void WyszukajTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = WyszukajTextBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                if (MainCalendar.SelectedDate.HasValue)
                {
                    UpdateTaskList(MainCalendar.SelectedDate.Value);
                }
                return;
            }

            var filteredTasks = AllTasks.Where(t =>
                (t.Tytul != null && t.Tytul.ToLower().Contains(searchText)) ||
                (t.Opis != null && t.Opis.ToLower().Contains(searchText)) ||
                (t.Nastroj != null && t.Nastroj.ToLower().Contains(searchText)) ||
                (t.Kategoria != null && t.Kategoria.ToLower().Contains(searchText))
            ).OrderBy(t => t.Data);

            TasksListView.ItemsSource = filteredTasks.ToList();
        }
        private void FiltrujButton_Click(object sender, RoutedEventArgs e)
        {
            var filteredTasks = AllTasks.AsEnumerable();

            if (KategoriaComboBox.SelectedItem?.ToString() != "Wszystkie")
            {
                filteredTasks = filteredTasks.Where(t => t.Kategoria == KategoriaComboBox.SelectedItem?.ToString());
            }

            if (PriorytetComboBox.SelectedIndex > 0)
            {
                int selectedPriority = PriorytetComboBox.SelectedIndex;
                filteredTasks = filteredTasks.Where(t => t.Priorytet == selectedPriority);
            }

            if (StatusComboBox.SelectedIndex == 1)
            {
                filteredTasks = filteredTasks.Where(t => !t.CzyZrealizowane);
            }
            else if (StatusComboBox.SelectedIndex == 2)
            {
                filteredTasks = filteredTasks.Where(t => t.CzyZrealizowane);
            }

            TasksListView.ItemsSource = filteredTasks.OrderBy(t => t.Data).ToList();
        }

        private void SprawdzPrzypomnienia()
        {
            var teraz = DateTime.Now;
            var niezrealizowaneZadania = AllTasks
                .Where(t => !t.CzyZrealizowane && t.Data <= teraz.AddDays(1) && t.Data >= teraz)
                .OrderBy(t => t.Data);

            foreach (var zadanie in niezrealizowaneZadania)
            {
                if (!Przypomnienia.Any(p => p.Tytul == zadanie.Tytul && !p.CzyWyswietlone))
                {
                    Przypomnienia.Add(new Przypomnienie
                    {
                        DataPrzypomnienia = DateTime.Now,
                        Tytul = zadanie.Tytul,
                        CzyWyswietlone = false
                    });

                    PokazPowiadomienie(zadanie);
                    SavePrzypomnienia();
                }
            }
        }

        private void PokazPowiadomienie(PlanerItem zadanie)
        {
            var notificationWindow = new Window
            {
                Title = "Przypomnienie",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true
            };

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Nadchodzące zadanie:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10),
                TextAlignment = TextAlignment.Center
            });

            stackPanel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = zadanie.Tytul,
                Margin = new Thickness(10, 5, 10, 5),
                TextWrapping = TextWrapping.Wrap
            });

            stackPanel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = $"Data: {zadanie.Data:g}",
                Margin = new Thickness(10, 5, 10, 5)
            });

            if (!string.IsNullOrEmpty(zadanie.Kategoria))
            {
                stackPanel.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = $"Kategoria: {zadanie.Kategoria}",
                    Margin = new Thickness(10, 5, 10, 5)
                });
            }

            var closeButton = new System.Windows.Controls.Button { Content = "OK", Margin = new Thickness(10), Width = 80 };
            closeButton.Click += (s, e) =>
            {
                var przypomnienie = Przypomnienia.FirstOrDefault(p => p.Tytul == zadanie.Tytul && !p.CzyWyswietlone);
                if (przypomnienie != null)
                {
                    przypomnienie.CzyWyswietlone = true;
                    SavePrzypomnienia();
                }
                notificationWindow.Close();
            };

            var buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            buttonPanel.Children.Add(closeButton);
            stackPanel.Children.Add(buttonPanel);

            notificationWindow.Content = stackPanel;
            notificationWindow.Show();
        }

        private void StatystykiButton_Click(object sender, RoutedEventArgs e)
        {
            var statystykiWindow = new StatystykiWindow(AllTasks);
            statystykiWindow.Owner = Window.GetWindow(this);
            statystykiWindow.ShowDialog();
        }

        private void SzablonyButton_Click(object sender, RoutedEventArgs e)
        {
            var szablonyWindow = new SzablonyWindow(Szablony);
            szablonyWindow.Owner = Window.GetWindow(this);
            szablonyWindow.ShowDialog();
            SaveSzablony();
        }

        private void ExportIcsButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToIcs();
        }

        private void ExportToIcs()
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Plik iCalendar (*.ics)|*.ics",
                FileName = $"Planer_{DateTime.Today:yyyyMMdd}.ics",
                Title = "Eksportuj do kalendarza"
            };

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("BEGIN:VCALENDAR");
                    sb.AppendLine("VERSION:2.0");
                    sb.AppendLine("PRODID:-//Notatnik//Planer//PL");

                    foreach (var task in AllTasks)
                    {
                        sb.AppendLine("BEGIN:VEVENT");
                        sb.AppendLine($"SUMMARY:{EscapeIcsText(task.Tytul)}");
                        sb.AppendLine($"DESCRIPTION:{EscapeIcsText(task.Opis)}");
                        sb.AppendLine($"DTSTART:{task.Data:yyyyMMddTHHmmss}");
                        sb.AppendLine($"DTEND:{task.Data.Add(task.CzasTrwania):yyyyMMddTHHmmss}");
                        sb.AppendLine($"STATUS:{(task.CzyZrealizowane ? "CONFIRMED" : "TENTATIVE")}");
                        sb.AppendLine("END:VEVENT");
                    }

                    sb.AppendLine("END:VCALENDAR");
                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);

                    System.Windows.MessageBox.Show($"Zadania wyeksportowane do: {saveDialog.FileName}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Błąd podczas eksportu do ICS: {ex.Message}", "Błąd Eksportu", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string EscapeIcsText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");
        }

        private void SaveTasksToTextFile()
        {
            var saveDialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "Plik tekstowy (*.txt)|*.txt",
                FileName = $"Planer_{DateTime.Today:yyyyMMdd}.txt",
                Title = "Zapisz zadania planera jako plik tekstowy"
            };

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    var content = new StringBuilder();
                    content.AppendLine($"Planer - Zadania do {DateTime.Now:yyyy-MM-dd HH:mm}");
                    content.AppendLine("---------------------------------------------------");

                    var tasksToSave = AllTasks.OrderBy(t => t.Data).ToList();

                    foreach (var task in tasksToSave)
                    {
                        string status = task.CzyZrealizowane ? "[ZREALIZOWANE]" : "[NIEZREALIZOWANE]";
                        content.AppendLine($"{task.FormatowanaData} {status} - {task.Tytul}");
                        content.AppendLine($"    Kategoria: {task.Kategoria}");
                        content.AppendLine($"    Priorytet: {task.PriorytetText}");

                        if (!string.IsNullOrEmpty(task.Opis))
                        {
                            content.AppendLine($"    Opis: {task.Opis}");
                        }
                        if (!string.IsNullOrEmpty(task.Nastroj))
                        {
                            content.AppendLine($"    Poczucie: {task.Nastroj}");
                        }
                        content.AppendLine($"    Czas trwania: {task.CzasTrwania:hh\\:mm}");
                        content.AppendLine("---");
                    }

                    File.WriteAllText(saveDialog.FileName, content.ToString());
                    System.Windows.MessageBox.Show($"Zadania zapisane do: {saveDialog.FileName}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Błąd podczas zapisywania do TXT: {ex.Message}", "Błąd Zapisu", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveTasksToPdf()
        {
            var saveDialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "Plik PDF (*.pdf)|*.pdf",
                FileName = $"Planer_{DateTime.Today:yyyyMMdd}.pdf",
                Title = "Zapisz zadania planera jako plik PDF"
            };

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    PdfDocument document = new PdfDocument();
                    document.Info.Title = "Planer - Lista Zadań";

                    PdfPage page = document.AddPage();
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    XFont fontTitle = new XFont("Helvetica", 18);
                    XFont fontTask = new XFont("Helvetica", 12);
                    XFont fontDetails = new XFont("Helvetica", 10);
                    XFont fontSmall = new XFont("Helvetica", 9);

                    double yPos = 40;
                    double lineHeight = 20;
                    double margin = 40;

                    gfx.DrawString("Planer - Lista Zadań", fontTitle, XBrushes.Black, new XRect(margin, yPos, page.Width, lineHeight), XStringFormats.TopLeft);
                    yPos += lineHeight * 2;

                    var tasksToSave = AllTasks.OrderBy(t => t.Data).ToList();

                    foreach (var task in tasksToSave)
                    {
                        string status = task.CzyZrealizowane ? "✓" : "☐";
                        string taskLine = $"{status} {task.FormatowanaData}: {task.Tytul}";

                        if (yPos > page.Height - 80)
                        {
                            page = document.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            yPos = 40;
                        }

                        gfx.DrawString(taskLine, fontTask, task.CzyZrealizowane ? XBrushes.Green : XBrushes.Black, margin, yPos);
                        yPos += lineHeight - 5;

                        // Kategoria i priorytet
                        gfx.DrawString($"Kategoria: {task.Kategoria} | Priorytet: {task.PriorytetText}", fontSmall, XBrushes.DarkBlue, margin + 20, yPos);
                        yPos += lineHeight - 8;

                        if (!string.IsNullOrEmpty(task.Opis))
                        {
                            gfx.DrawString($"Opis: {task.Opis}", fontDetails, XBrushes.Gray, margin + 20, yPos);
                            yPos += lineHeight - 8;
                        }
                        if (!string.IsNullOrEmpty(task.Nastroj))
                        {
                            gfx.DrawString($"Poczucie: {task.Nastroj}", fontDetails, XBrushes.DarkGray, margin + 20, yPos);
                            yPos += lineHeight - 8;
                        }

                        gfx.DrawString($"Czas trwania: {task.CzasTrwania:hh\\:mm}", fontSmall, XBrushes.DarkGreen, margin + 20, yPos);
                        yPos += 15;
                    }

                    document.Save(saveDialog.FileName);
                    System.Windows.MessageBox.Show($"Zadania zapisane do: {saveDialog.FileName}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Błąd podczas zapisywania do PDF: {ex.Message}\nUpewnij się, że zainstalowano pakiet NuGet 'PDFsharp'.", "Błąd Zapisu", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveToTxtButton_Click(object sender, RoutedEventArgs e)
        {
            SaveTasksToTextFile();
        }

        private void SaveToPdfButton_Click(object sender, RoutedEventArgs e)
        {
            SaveTasksToPdf();
        }

        private void TasksListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveTasks();
        }
    }
}