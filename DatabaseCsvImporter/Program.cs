using DatabaseCsvImporter;
using DatabaseCsvImporter.Service;
using System.Text;
using System;

namespace SimpleCsvImporter
{
    static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["PostgreSQL"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.WriteLine("Ошибка: строка подключения 'PostgreSQL' не найдена в app.config");
                return 1;
            }

            string csvPath;
            if (args.Length > 0)
            {
                csvPath = args[0];
            }
            else
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var appSettingPath = System.Configuration.ConfigurationManager.AppSettings["CsvFilePath"] ?? string.Empty;

                csvPath = Path.Combine(baseDirectory, appSettingPath); 

                if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
                {
                    Console.WriteLine($"Ошибка: Путь к CSV файлу не указан или файл не найден: '{csvPath}'");
                    return 1;
                }

            }

            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"Файл не найден: {csvPath}");
                return 2;
            }

            try
            {
                await using var dbService = new DatabaseService(connectionString);
                await dbService.OpenAsync();

                var importService = new CsvImportService(dbService);

                Console.WriteLine("Начало импорта данных...");
                var result = await importService.ImportFromCsv(csvPath, Encoding.UTF8);

                result.PrintSummary();
                return 0;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return 10;
            }
        }
    }
}