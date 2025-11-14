using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCsvImporter.Model.DTO
{
    public class CityDTO
    {
        public required string Name { get; set; }
        public required bool Status { get; set; }
    }
}
