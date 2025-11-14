using DatabaseCsvImporter.Model.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCsvImporter.Model
{
    public class EmployeeModel
    {
        public Guid Id { get; set; }
        public required string FirstName { get; set; }
        public required string MiddleName { get; set; }
        public required string LastName { get; set; }
        public required DepartmentModel Department{ get; set; }
        public required PositionModel Position { get; set; }
        public List<CityModel> Cities { get; set; } = new List<CityModel>();

    }
}
