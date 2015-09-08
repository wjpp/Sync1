using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLite;

namespace Glasscubes.Drive.Model
{
    class Workspace
    {
        public bool WritePermission  { get; set; }
        public string Name { get; set; }
        [PrimaryKey]
        public int Id { get; set; }
    }
}
