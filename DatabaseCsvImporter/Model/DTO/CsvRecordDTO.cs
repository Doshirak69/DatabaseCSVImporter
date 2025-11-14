using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCsvImporter.Model.DTO
{
    public class CsvRecordDTO
    {
        public string Department { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public List<CityDTO> Cities { get; set; } = new List<CityDTO>();
    }
}
