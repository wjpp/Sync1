using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glasscubes.Drive.Model
{
    class SyncList
    {
        public int LatestRevision { get; set; }
        public List<DiskItem> Items{ get; set; }
 
    }
}


