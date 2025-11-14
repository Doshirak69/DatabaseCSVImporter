using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCsvImporter.Model
{
    public class ImportErrorModel
    {
        public int LineNumber { get; set; }
        public string Line { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;

    }
}
