using System;

namespace Sync1
{
    public class DiskItem
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string FileName { get; set; }
        public int Size { get; set; }
        public int Version { get; set; }
        public int FolderId { get; set; }
        public string Action { get; set; }
        public int WorkspaceId { get; set; }
    }
}