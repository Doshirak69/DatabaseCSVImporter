using DatabaseCsvImporter.Model;
using DatabaseCsvImporter.Model.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DatabaseCsvImporter.Helper
{
    public static class CsvParser
    {
        private const int MinCsvFields = 4;
        private const int FioPartsCount = 3;

        public static CsvRecordDTO ParseLine(string line)
        {

            var parts = line.Split(',');
            if (parts.Length < MinCsvFields)
                throw new FormatException($"Ожидалось минимум 4 поля, получено {parts.Length}");

            var result = new CsvRecordDTO();

            result.Department = GetLeafNodeName(parts[0].Trim());

            var fioText = parts[1].Trim();
            var fioParts = SplitBySpaces(fioText);

            if (fioParts.Count != FioPartsCount)
                throw new FormatException($"ФИО должно содержать 3 части (Имя Отчество Фамилия), получено: '{fioText}'");

            result.FirstName = fioParts[0]; 
            result.MiddleName = fioParts[1];
            result.LastName = fioParts[2];   

            result.Position = parts[2].Trim();

            var citiesRaw = string.Join(' ', parts.Skip(3).Select(p => p.Trim())).Trim();

            result.Cities = ParseCities(citiesRaw);

            return result;
        }
        private static List<string> SplitBySpaces(string text)
        {
            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private static readonly Regex CityPattern = new Regex(
             @"([А-Яа-яёЁ\s\-]+):(да|нет)",
             RegexOptions.Compiled);

        public static List<CityDTO> ParseCities(string line)
        {
            var cities = new List<CityDTO>();

            if (string.IsNullOrWhiteSpace(line))
                return cities;

            var cityMatches = CityPattern.Matches(line);

            foreach (Match match in cityMatches)
            {
                cities.Add(new CityDTO
                {
                    Name = match.Groups[1].Value.Trim(),
                    Status = match.Groups[2].Value.ToLower() == "да"
                });
            }
            return cities;
        }

        private static string GetLeafNodeName(string path)
        {
            var parts = path.Split('\\',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.LastOrDefault() ?? string.Empty;
        }
    }
}
