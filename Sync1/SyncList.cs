using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sync1
{
    class SyncList
    {
        public int LatestRevision { get; set; }
        public List<DiskItem> Items{ get; set; }
 
    }
}


