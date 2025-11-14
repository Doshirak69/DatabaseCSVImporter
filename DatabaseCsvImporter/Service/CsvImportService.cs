using DatabaseCsvImporter.Helper;
using DatabaseCsvImporter.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace DatabaseCsvImporter.Service
{
    public class CsvImportService
    {
        private readonly DatabaseService _dbService;

        public CsvImportService(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public async Task<ImportResult> ImportFromCsv(string filePath, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var result = new ImportResult();

            var lines = await File.ReadAllLinesAsync(filePath, encoding);
            int lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    await ProcessLine(line);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportErrorModel
                    {
                        LineNumber = lineNumber,
                        Line = line,
                        ErrorMessage = ex.Message,
                        StackTrace = ex.StackTrace ?? string.Empty
                    });
                    result.ErrorCount++;
                }
            }

            return result;
        }

        private async Task ProcessLine(string line)
        {
            var dto = CsvParser.ParseLine(line);

            var employee = dto.ToEmployeeModel();

            var subdivisionTask = _dbService.GetOrCreateDepartmentIdAsync(employee.Department);
            var positionTask = _dbService.GetOrCreatePositionIdAsync(employee.Position);

            await Task.WhenAll(subdivisionTask, positionTask);

            await _dbService.GetOrCreateEmployeeIdAsync(employee);

            var cityTasks = employee.Cities.Select(city =>
                _dbService.UpsertCityStatusAsync(city, employee.Id));

            await Task.WhenAll(cityTasks);
            
        }

    }
}
