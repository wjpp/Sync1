using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glasscubes.Drive.Model
{
    class DeleteFileMeta
    {
        public bool success { get; set; }
        public int globalRevision { get; set; }
        public object docId { get; set; }
        public object version { get; set; }
        public object checksum { get; set; }
        public object created { get; set; }
        public object updated { get; set; }
       
    }
}
