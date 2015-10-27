using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLite;

namespace Glasscubes.Drive.Model
{
    class GCAction
    {
        public const string NEW = "NEW";
        public const string CHANGED = "CHANGED";
        public const string DELETED = "DELETED";
        public const string RENAMED = "RENAMED";

        [AutoIncrement,PrimaryKey]
        public int Id { get; set; } 
        public int DiskItemId { get; set; }
        public string Action { get; set; }
        public string Path { get; set; }
        public string OldPath { get; set; }
    }
}
