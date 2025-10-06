using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notatnik
{
    public class FileItem
    {
        public string FileName { get; set; }
        public List<string> Tags { get; set; } = new List<string>();


        public override string ToString()
        {
            return FileName;
        }

    }
}
