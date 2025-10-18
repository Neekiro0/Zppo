using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms.VisualStyles;
using System.Linq.Expressions;
using System.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.VisualBasic;


namespace Notatnik
{
    public partial class MainWindow : Window
    {
        private string currentDirectory =Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string currentFilePath = string.Empty;
        private bool isContentModified = false;
        private string originalContent = string.Empty;
        private Dictionary<string, List<string>> fileTags = new Dictionary<string, List<string>>();
        private const string TagsFileName = "file_tags.json";
        private readonly string tagsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), TagsFileName);
        private ObservableCollection<string> allTags = new ObservableCollection<string>();





        public MainWindow()
        {
            InitializeComponent();
            LoadFileList();

        }
        //Panel statusu wybranego pliku
        private void UpdateStatus()
        {
            if (string.IsNullOrEmpty(currentDirectory))
            {
                StatusText.Text = "Gotowe";
                EditStatusText.Text = " ";
            }
            else
            {
                StatusText.Text = $"Edytowanie: {Path.GetFileName(currentFilePath)}";
                EditStatusText.Text = isContentModified ? "Modyfikowane (nie zapisane)" : " " ;
            }
        }

        //System tagowania plików
        private void LoadTags()
        {
            if (File.Exists(tagsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(tagsFilePath);
                    var deserialized = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
                             ?? new Dictionary<string, List<string>>();
                    // Migracja kluczy z nazw plików do ścieżek pliku obecnego folderu
                    var migrated = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in deserialized)
                    {
                        try
                        {
                            if (Path.IsPathRooted(kv.Key))
                            {
                                migrated[kv.Key] = kv.Value;
                            }
                            else
                            {
                                // stary typ klucza - tylko nazwa pliku: próbuje przypisać do obecnego folderu
                                string candidate = Path.Combine(currentDirectory, kv.Key);
                                if (File.Exists(candidate))
                                {
                                    migrated[Path.GetFullPath(candidate)] = kv.Value;
                                }
                                else
                                {
                                    // zostaw oryginalny klucz,w celu zapobieżenia utracie danych, inne części kodu używają pełnych ścieżek
                                    // ignorowane dopóki użytkonik nie otaguje ponownie tych plików albo przyszła migracja nie znajdzie pliku.
                                    migrated[kv.Key] = kv.Value;
                                }
                            }
                        }
                        catch
                        {
                            // przy błędnej ścieżce pozostaw oryginalny klucz
                            migrated[kv.Key] = kv.Value;
                        }
                    }
                    fileTags = migrated;

                    TagFilterComboBox.Items.Clear();
                    var uniqueTags = fileTags.Values.SelectMany(tags => tags).Distinct().OrderBy(t => t);

                    foreach (var tag in uniqueTags)
                    {
                        TagFilterComboBox.Items.Add(tag);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Błąd podczas wczytywania tagów: {ex.Message}", "Błąd",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                TagFilterComboBox.Items.Clear();
            }
        }

        private void SaveTags()
        {
            try
            {
                string json = JsonSerializer.Serialize(fileTags);
                File.WriteAllText(tagsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd podczas zapisywania tagów: {ex.Message}", "Błąd",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateAllTags()
        {
            allTags.Clear();
            var uniqueTags = fileTags.Values.SelectMany(tags => tags).Distinct().OrderBy(t => t);
            foreach (var tag in uniqueTags)
            {
                allTags.Add(tag);
            }
        }

        private void FilterFilesByTag(string tag)
        {
            var filteredFiles = Directory.GetFiles(currentDirectory, "*.txt")
                .Where(fp => fileTags.ContainsKey(Path.GetFullPath(fp)) && fileTags[Path.GetFullPath(fp)].Contains(tag))
                .Select(Path.GetFileName)
                .ToArray();

            ListaTXT.ItemsSource = filteredFiles;

        }
        private void FilterAndSortFilesByTag(string tag)
        {
            try
            {
                var allFilePaths = Directory.GetFiles(currentDirectory, "*.txt")
                    .Select(fp =>Path.GetFullPath(fp))
                    .ToList();
                var filteredFiles = allFilePaths
                    .Select(filePath => new FileItem
                    {
                        FileName = Path.GetFileName(filePath),
                        Tags = fileTags.ContainsKey(filePath) ? fileTags[filePath] : new List<string>()
                    })
                    .Where(item => item.Tags.Contains(tag))
                    .OrderByDescending(item => item.Tags.Count)
                    .ThenBy(item => item.FileName)
                    .ToList();

                ListaTXT.ItemsSource = filteredFiles;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd podczas filtrowania plików: {ex.Message}", "Błąd",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CleanUnusedTags()
        {
            try
            {
                //usuń tylko tagi które są wybrane dla obecnego folderu i nie isnieją
                var filesInCurrent = new HashSet<string>(
                    Directory.GetFiles(currentDirectory, "*.txt").Select(Path.GetFullPath),
                    StringComparer.OrdinalIgnoreCase);

                var keysToRemove = fileTags.Keys
                .Where(key =>
                 {
                     try
                     {
                         string fullkey = Path.GetFullPath(key);
                         string keyDir = Path.GetDirectoryName(fullkey);
                         if (string.Equals(Path.GetFullPath(currentDirectory), keyDir, StringComparison.OrdinalIgnoreCase))
                         {
                             return !filesInCurrent.Contains(fullkey);
                         }
                         return false;
                     }
                     catch { return false;}
                }).ToList();
                foreach (var k in keysToRemove)
                {
                    fileTags.Remove(k);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd podczas czyszczenia tagów: {ex.Message}", "Błąd",
                                                 MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Zarządzanie odczytywanymi plikami
        private void LoadFileList() 
        {
            try
            {  
                LoadTags();

                var fileItems = Directory.GetFiles(currentDirectory, "*.txt")
                    .Select(filePath => 
                    {
                        string fullPath = Path.GetFullPath(filePath);
                        string fileName = Path.GetFileName(fullPath);
                        return new FileItem
                        {
                            FileName = fileName,
                            Tags = fileTags.ContainsKey(fullPath) ? fileTags[fullPath] : new List<string>()
                        };
                    })
                    .OrderByDescending(item => item.Tags.Count)
                    .ThenBy(item => item.FileName) 
                    .ToList();

                ListaTXT.ItemsSource = fileItems;

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error przy załadowaniu pliku: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool WriteFileContent(string filePath, string content)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(content);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error przy pisaniu zawartości pliku: {ex.Message}", "Error",
                  MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        private bool CheckForUnsavedChanges()
        {
            if (isContentModified)
            {
                var result = System.Windows.MessageBox.Show("Masz niezapisane zmiany. Odrzucić zmiany?",
                                          "Niezapisane zmiany",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                {
                    return false; 
                }
                else if (result == MessageBoxResult.No)
                {
                    return false;
                }

            }
            return true;
        }
        private bool CheckFileAccess(string filePath, FileAccess accessType)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, accessType, FileShare.None))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void TryUnlockFile(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    
                }
            }
            catch
            {
      
            }
        }

        private string ReadFileContent(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error przy czytaniu: {ex.Message}", "Error",
                      MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }

        //Funkcje przycisków
        private void ListaTXT_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaTXT.SelectedItem == null) return;



            if (!CheckForUnsavedChanges())
            {
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ListaTXT.SelectedItem = Path.GetFileName(currentFilePath);
                }));
                return;
            }
        
            string newFilePath = System.IO.Path.Combine(currentDirectory, ListaTXT.SelectedItem.ToString());
            try
            {
                using (var fs = new FileStream(newFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    currentFilePath = newFilePath;
                    originalContent = ReadFileContent(currentFilePath);
                    ContentTextBox.Text = originalContent;
                    isContentModified = false;
                    UpdateStatus();
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Windows.MessageBox.Show("Dostęp do pliku odmówiony. Proszę sprawdź uprawnienia.", "Brak dostępu",
                      MessageBoxButton.OK, MessageBoxImage.Error);
                ListaTXT.SelectedItem = Path.GetFileName(currentFilePath);
            }
            catch (IOException ioEx)
            {
                System.Windows.MessageBox.Show($"Error dostęp do pliku: {ioEx.Message}", "IO Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                ListaTXT.SelectedItem = Path.GetFileName(currentFilePath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error dostęp do pliku: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                ListaTXT.SelectedItem = Path.GetFileName(currentFilePath);
            }
        }
        private void Odswiezbt_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForUnsavedChanges())
                return;

            try
            {
                if (!string.IsNullOrEmpty(currentFilePath))
                {
                    TryUnlockFile(currentFilePath);
                }

                LoadFileList();
                ContentTextBox.Text = string.Empty;
                currentFilePath = string.Empty;
                originalContent = string.Empty;
                isContentModified = false;
                UpdateStatus();
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show($"Error przy odświeżaniu lisy: {ex.Message}", "Error",
              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            }

        private void Zmienfolderbt_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForUnsavedChanges())
                return ;

            var dialog = new FolderBrowserDialog
            {
                SelectedPath = currentDirectory
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {


                    Directory.GetFiles(dialog.SelectedPath);
                    currentDirectory = dialog.SelectedPath;
                    LoadFileList();
                    ContentTextBox.Text = string.Empty;
                    currentFilePath = string.Empty;
                    originalContent = string.Empty;
                    isContentModified = false;
                    UpdateStatus();
                }
                catch (UnauthorizedAccessException)
                {
                    System.Windows.MessageBox.Show("Dostęp do folderu został odmówiony.", "Dostęp odmówiony",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error przy dostępie do folderu: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }

        private void Usunbt_Click(object sender, RoutedEventArgs e)
        {
            if (ListaTXT.SelectedItems == null || ListaTXT.SelectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("Proszę wybierz plik(i) do usunięcia.", "Nie zaznaczono pliku", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedFiles = ListaTXT.SelectedItems.Cast<object>().ToList();

            var result = System.Windows.MessageBox.Show(
                $"Czy na pewno chcesz usunąć {selectedFiles.Count} plik(ów)?",
                "Potwierdź usunięcie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            int successCount = 0;
            foreach (var item in selectedFiles)
            {
                string fileName = item.ToString();
                string fullPath = Path.Combine(currentDirectory, fileName);

                try
                {
                    File.Delete(fullPath);

                    if (fileTags.ContainsKey(fileName))  // Fix: match on fileName, not fullPath
                    {
                        fileTags.Remove(fileName);
                    }

                    successCount++;
                }
                catch (UnauthorizedAccessException)
                {
                    System.Windows.MessageBox.Show($"Dostęp odmówiony: {fileName}", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException ioEx)
                {
                    System.Windows.MessageBox.Show($"Plik w użyciu: {fileName}\n{ioEx.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Błąd przy usuwaniu {fileName}:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (successCount > 0)
            {
                // 🧹 Clean up unused tags
                CleanUnusedTags();

                SaveTags();
                UpdateAllTags();
                LoadFileList();
                ContentTextBox.Text = string.Empty;
                currentFilePath = string.Empty;
                originalContent = string.Empty;
                isContentModified = false;
                UpdateStatus();

                System.Windows.MessageBox.Show($"{successCount} plik(ów) zostało usuniętych.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Nowybt_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForUnsavedChanges())
                return;

            string NazwaPliku = Microsoft.VisualBasic.Interaction.InputBox(
                "Podaj nazwe nowego pliku txt:",
                "Stwórz nowy plik txt",
                "NowyPlik");

            if (string.IsNullOrWhiteSpace(NazwaPliku))
                return;

            if (!NazwaPliku.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                NazwaPliku += ".txt";
            }

            string filePath = Path.Combine(currentDirectory, NazwaPliku);

            try
            {
                if (File.Exists(filePath))
                {
                    var result1 = System.Windows.MessageBox.Show($"Plik '{NazwaPliku}' już istnieje. Nadpisać?", "Plik istnieje", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result1 != MessageBoxResult.Yes)
                        return;
                }
                if (WriteFileContent(filePath, string.Empty))
                {
                   
                    var result = System.Windows.MessageBox.Show($"Plik '{NazwaPliku}' został utworzony pomyślnie.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadFileList();
                    ListaTXT.SelectedItem = NazwaPliku;
                    currentFilePath = filePath;
                    originalContent = string.Empty;
                    isContentModified = false;
                    UpdateStatus();
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Windows.MessageBox.Show("Dostęp odmówiony. Nie można tutaj stworzyć pliku.", "Access Denied",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error przy tworzeniu pliku: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            }

        private void Zapiszbt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                System.Windows.MessageBox.Show("Nie zaznaczono pliku do zapisania.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!CheckFileAccess(currentFilePath, FileAccess.Write))
            {
                System.Windows.MessageBox.Show("Nie zaznaczono pliku do zapisania.", "Error",
                      MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (WriteFileContent(currentFilePath, ContentTextBox.Text))
            {
                originalContent = ContentTextBox.Text;
                isContentModified = false;
                UpdateStatus();
                System.Windows.MessageBox.Show("Plik zapisany pomyślnie.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

            }
        }

        private void ContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isContentModified = ContentTextBox.Text != originalContent;
            UpdateStatus();
        }

        private void PokazWszystkiePlikiMenuItem_Click(object sender, RoutedEventArgs e)
        {

            LoadFileList(); 
            TagFilterComboBox.SelectedIndex = -1;
            TagFilterComboBox.Text = "Filtruj po tagu...";
        }

        private void TagFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            {
                if (TagFilterComboBox.SelectedItem == null) return;

                string selectedTag = TagFilterComboBox.SelectedItem.ToString();
                FilterAndSortFilesByTag(selectedTag);
            }
        }

        private void Otagujbt_Click(object sender, RoutedEventArgs e)
        {
                if (string.IsNullOrEmpty(currentFilePath))
                {
                System.Windows.MessageBox.Show("Proszę wybierz plik do otagowania.", "Brak pliku",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fileKey = Path.GetFullPath(currentFilePath);
                string currentTags = fileTags.ContainsKey(fileKey) ?
                    string.Join(", ", fileTags[fileKey]) : string.Empty;

                string newTags = Interaction.InputBox(
                    "Wprowadź tagi (oddzielone przecinkami):",
                    "Edytuj tagi",
                    currentTags);

                if (newTags != null)
                {
                    var tags = newTags.Split(',')
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Distinct()
                        .ToList();

                    if (tags.Count > 0)
                    {
                        fileTags[fileKey] = tags;
                    }
                    else
                    {
                        fileTags.Remove(fileKey);
                    }


                    UpdateAllTags();
                    SaveTags();
                    LoadFileList();
                }
            
        }
    }
    }


