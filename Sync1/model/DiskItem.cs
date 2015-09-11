using System;
using SQLite;

namespace Glasscubes.Drive.Model
{
    public class DiskItem 
    {
        [PrimaryKey]
        public long Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string FileName { get; set; }
        public long Size { get; set; }
        public int Version { get; set; }
        public long FolderId { get; set; }
        public string Action { get; set; }
        public long WorkspaceId { get; set; }
        public Boolean Folder { get; set; }
        public string Path { get; set; }
    }
}


