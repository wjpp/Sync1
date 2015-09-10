using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glasscubes.Drive.Model
{
    class FileMeta
    {

        private bool success {get; set;}
        private long globalRevision { get; set; }
        private long docId { get; set; }
        private long version { get; set; }
        private string checksum { get; set; }
        private string created { get; set; }
        private string updated { get; set; }
    }
}
