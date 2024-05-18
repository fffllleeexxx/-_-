using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Срез_Погода
{
    public partial class MainWindow : Window
    {
        private List<WeatherRecord> weatherRecords = new List<WeatherRecord>();
        private List<WeatherRecord> originalWeatherRecords = new List<WeatherRecord>();
        private const string FilePath = "weatherData.txt";

        public MainWindow()
        {
            InitializeComponent();
            LoadWeatherData();
            WeatherDataGrid.ItemsSource = weatherRecords;
            originalWeatherRecords = weatherRecords.ToList();
            FilterComboBox.ItemsSource = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newRecord = new WeatherRecord
                {
                    Day = DayTextBox.Text,
                    Date = DateTime.Parse(DateTextBox.Text),
                    Temperature = int.Parse(TemperatureTextBox.Text)
                };
                weatherRecords.Add(newRecord);
                originalWeatherRecords.Add(newRecord);
                UpdateWeatherDataGrid();
                UpdateStatistics();
                SaveWeatherData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении записи: {ex.Message}");
            }
        }

        private void UpdateStatistics()
        {
            if (weatherRecords.Count == 0)
            {
                StatisticsTextBlock.Text = "Нет данных для отображения статистики.";
                return;
            }

            var averageTemp = weatherRecords.Average(r => r.Temperature);
            var maxTemp = weatherRecords.Max(r => r.Temperature);
            var minTemp = weatherRecords.Min(r => r.Temperature);

            var temperatureGroups = weatherRecords.GroupBy(r => r.Temperature)
                                                  .Where(g => g.Count() > 1)
                                                  .Select(g => new { Temperature = g.Key, Count = g.Count() });

            var tempChanges = weatherRecords.Zip(weatherRecords.Skip(1), (a, b) => new { First = a, Second = b })
                                            .Count(pair => Math.Abs(pair.First.Temperature - pair.Second.Temperature) >= 10);

            StatisticsTextBlock.Text = $"Средняя температура: {averageTemp:F1}°\n" +
                                       $"Максимальная температура: {maxTemp}°\n" +
                                       $"Минимальная температура: {minTemp}°\n" +
                                       $"Одинаковая температура наблюдалась: {string.Join(", ", temperatureGroups.Select(g => $"{g.Temperature}° - {g.Count} раз"))}\n" +
                                       $"Аномальные изменения температуры: {tempChanges} раз";
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateWeatherDataGrid();
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateWeatherDataGrid();
        }

        private void UpdateWeatherDataGrid()
        {
            var filteredRecords = originalWeatherRecords.AsEnumerable();

            // Применяем фильтр
            if (FilterComboBox.SelectedItem != null)
            {
                var selectedDay = (DayOfWeek)FilterComboBox.SelectedItem;
                filteredRecords = filteredRecords.Where(r => Enum.TryParse(r.Day, out DayOfWeek day) && day == selectedDay);
            }

            // Применяем сортировку
            if (SortComboBox.SelectedItem != null)
            {
                var sortBy = (SortComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (sortBy == "По возрастанию")
                {
                    filteredRecords = filteredRecords.OrderBy(r => r.Temperature);
                }
                else if (sortBy == "По убыванию")
                {
                    filteredRecords = filteredRecords.OrderByDescending(r => r.Temperature);
                }
            }

            weatherRecords = filteredRecords.ToList();
            WeatherDataGrid.ItemsSource = null; 
            WeatherDataGrid.ItemsSource = weatherRecords; 
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            FilterComboBox.SelectedItem = null;
            SortComboBox.SelectedItem = null;
            UpdateWeatherDataGrid();
        }

        private void SaveWeatherData()
        {
            using (StreamWriter writer = new StreamWriter(FilePath))
            {
                foreach (var record in originalWeatherRecords)
                {
                    writer.WriteLine($"{record.Day};{record.Date:yyyy-MM-dd};{record.Temperature}");
                }
            }
        }

        private void LoadWeatherData()
        {
            if (File.Exists(FilePath))
            {
                using (StreamReader reader = new StreamReader(FilePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(';');
                        if (parts.Length == 3)
                        {
                            var record = new WeatherRecord
                            {
                                Day = parts[0],
                                Date = DateTime.Parse(parts[1]),
                                Temperature = int.Parse(parts[2])
                            };
                            weatherRecords.Add(record);
                        }
                    }
                }
                originalWeatherRecords = weatherRecords.ToList();
                UpdateWeatherDataGrid();
                UpdateStatistics();
            }
        }
    }

    public class WeatherRecord
    {
        public string Day { get; set; }
        public DateTime Date { get; set; }
        public int Temperature { get; set; }
    }
}