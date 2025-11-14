using DatabaseCsvImporter.Model.DTO;
using DatabaseCsvImporter.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCsvImporter.Helper
{
    public static class DtoMapper
    {
        public static CityModel ToCityModel(this CityDTO dto)
        {
            return new CityModel
            {
                Name = dto.Name,
                Status = dto.Status
            };
        }
        public static PositionModel ToPositionModel(string positionName)
        {
            return new PositionModel
            {
                Name = positionName
            };
        }
        public static DepartmentModel ToDepartmentModel(string departmentName)
        {
            return new DepartmentModel
            {
                Name = departmentName
            };
        }

        public static EmployeeModel ToEmployeeModel(this CsvRecordDTO dto)
        {
            return new EmployeeModel
            {
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                Position = ToPositionModel(dto.Position),
                Department = ToDepartmentModel(dto.Department),
                Cities = dto.Cities.Select(c => c.ToCityModel()).ToList()
            };
        }
    }
}
