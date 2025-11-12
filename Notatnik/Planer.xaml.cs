using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms; 
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Microsoft.VisualBasic;

namespace Notatnik
{
    public class PlanerItem
    {
        public DateTime Data { get; set; }
        public string Tytul { get; set; }
        public string Opis { get; set; }
        public string Poczucie { get; set; } 
        public bool CzyZrealizowane { get; set; }
        public string FormatowanaData => Data.ToShortDateString();
    }

    public partial class Planer : Page
    {
        private ObservableCollection<PlanerItem> AllTasks { get; set; }
        private const string TasksFileName = "planer_tasks.json";
        private readonly string tasksFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), TasksFileName);

        public Planer()
        {
            InitializeComponent();
            AllTasks = new ObservableCollection<PlanerItem>();
            LoadTasks();

            MainCalendar.SelectedDate = DateTime.Today;
            if (MainCalendar.SelectedDate.HasValue)
            {
                UpdateTaskList(MainCalendar.SelectedDate.Value);
            }

            TasksListView.MouseDoubleClick += TasksListView_MouseDoubleClick;
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
                Poczucie = "",
                CzyZrealizowane = false
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

                        if (!string.IsNullOrEmpty(task.Opis))
                        {
                            content.AppendLine($"    Opis: {task.Opis}");
                        }
                        if (!string.IsNullOrEmpty(task.Poczucie))
                        {
                            content.AppendLine($"    Poczucie: {task.Poczucie}");
                        }
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

                        if (yPos > page.Height - 60)
                        {
                            page = document.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            yPos = 40;
                        }

                        gfx.DrawString(taskLine, fontTask, XBrushes.Black, margin, yPos);
                        yPos += lineHeight - 5;

                        if (!string.IsNullOrEmpty(task.Opis))
                        {
                            gfx.DrawString($"Opis: {task.Opis}", fontDetails, XBrushes.Gray, margin + 20, yPos);
                            yPos += lineHeight - 5;
                        }
                        if (!string.IsNullOrEmpty(task.Poczucie))
                        {
                            gfx.DrawString($"Poczucie: {task.Poczucie}", fontDetails, XBrushes.DarkGray, margin + 20, yPos);
                            yPos += lineHeight - 5;
                        }

                        yPos += 10;
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