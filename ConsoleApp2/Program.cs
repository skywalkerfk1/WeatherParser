using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WeatherApp
{
    public class WeatherData
    {
        public string City { get; set; }
        public int Temperature { get; set; } 
        public string Country { get; set; }

        public WeatherData(string city, int temperature, string country)
        {
            City = city;
            Temperature = temperature;
            Country = country;
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            string citiesUrl = "https://gistpad.com/raw/vk-task-14";

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                Console.WriteLine("Загрузка списка городов...");
                List<string> cities = new List<string>();

                try
                {
                    string fileContent = await httpClient.GetStringAsync(citiesUrl);
                    cities = fileContent
                        .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Distinct() 
                        .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при загрузке файла городов: {ex.Message}");
                    return;
                }

                List<WeatherData> weatherList = new List<WeatherData>();

                Console.WriteLine("Загрузка данных о погоде через API...");

                foreach (var city in cities)
                {
                    try
                    {
                        string encodedCity = Uri.EscapeDataString(city);
                        string apiUrl = $"https://wttr.in/{encodedCity}?format=j1";

                        string jsonResponse = await httpClient.GetStringAsync(apiUrl);

                        using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                        {
                            JsonElement root = doc.RootElement;

                            string tempStr = root
                                .GetProperty("current_condition")[0]
                                .GetProperty("temp_C")
                                .GetString();
                            int tempC = int.Parse(tempStr);

                            string country = root
                                .GetProperty("nearest_area")[0]
                                .GetProperty("country")[0]
                                .GetProperty("value")
                                .GetString();

                            weatherList.Add(new WeatherData(city, tempC, country));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Не удалось получить данные для города \"{city}\": {ex.Message}");
                    }
                }

                Console.WriteLine("\n--- Информация по городам ---");
                foreach (var item in weatherList)
                {
                    string sign = item.Temperature > 0 ? "+" : "";
                    Console.WriteLine($"{item.City}, {item.Country} {sign}{item.Temperature} °C");
                }

                Console.WriteLine("\n--- Статистика по странам ---");
                var groupedByCountry = weatherList.GroupBy(w => w.Country);

                foreach (var group in groupedByCountry)
                {
                    string country = group.Key;
                    int count = group.Count();
                    double avgTemp = group.Average(w => w.Temperature);
                    int roundedAvg = (int)Math.Round(avgTemp, MidpointRounding.AwayFromZero);
                    int minTemp = group.Min(w => w.Temperature);
                    int maxTemp = group.Max(w => w.Temperature);

                    string avgSign = roundedAvg > 0 ? "+" : "";
                    string minSign = minTemp > 0 ? "+" : "";
                    string maxSign = maxTemp > 0 ? "+" : "";

                    Console.WriteLine($"{country} — {count} cities, avg: {avgSign}{roundedAvg} °C, min: {minSign}{minTemp} °C, max: {maxSign}{maxTemp} °C");
                }
            }
        }
    }
}