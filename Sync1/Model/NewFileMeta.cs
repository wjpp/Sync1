using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glasscubes.Drive.Model
{
    class NewFileMeta
    {
        public bool success { get; set; }
        public long globalRevision { get; set; }
        public long docId { get; set; }
        public int version { get; set; }
        public string checksum { get; set; }
        public DateTime created { get; set; }
        public DateTime updated { get; set; }
    }
}
