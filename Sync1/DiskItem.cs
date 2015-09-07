using System;
using SQLite;

namespace Sync1
{
    public class DiskItem 
    {
        [PrimaryKey]
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string FileName { get; set; }
        public int Size { get; set; }
        public int Version { get; set; }
        public int FolderId { get; set; }
        public string Action { get; set; }
        public int WorkspaceId { get; set; }
        public Boolean Folder { get; set; }
        public string Path { get; set; }
    }
}


