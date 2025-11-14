using DatabaseCsvImporter.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCsvImporter
{
    public class ImportResult
    {
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<ImportErrorModel> Errors { get; set; } = new List<ImportErrorModel>();

        public void PrintSummary()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("Результаты загрузки данных в базу данных:");
            Console.WriteLine(new string('=', 60));

            Console.WriteLine($"Успешно импортировано: {SuccessCount}");
            Console.WriteLine($"Ошибок: {ErrorCount}");

            if (ErrorCount > 0)
            {
                Console.WriteLine("\nДетали ошибок:");
                foreach (var error in Errors)
                {
                    Console.WriteLine($"  Строка {error.LineNumber}: {error.ErrorMessage}");
                    Console.WriteLine($"    Данные: {error.Line}");
                    if (!string.IsNullOrEmpty(error.StackTrace))
                    {
                        Console.WriteLine($"    StackTrace: {error.StackTrace.Split('\n').FirstOrDefault()}");
                    }
                }
            }

            Console.WriteLine(new string('=', 60));
        }
    }
}
