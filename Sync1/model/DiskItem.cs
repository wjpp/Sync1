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
        public DateTime CreatedOnDiskUTC { get; set; }
        public DateTime UpdatedOnDiskUTC { get; set; }
        public string FileName { get; set; }
        public long Size { get; set; }
        public int Version { get; set; }
        public long FolderId { get; set; }
        public string Action { get; set; }
        public long WorkspaceId { get; set; }
        public Boolean Folder { get; set; }
        public string Path { get; set; }

        public bool EqualForGC(DiskItem other)
        {
            if (other == null) return false;

            if (this.Id == other.Id &&
                this.Created == other.Created &&
                this.Updated == other.Updated &&
                this.FileName == other.FileName &&
                this.Size == other.Size &&
                this.Version == other.Version &&
                this.FolderId == other.FolderId &&
                this.Action == other.Action &&
                this.WorkspaceId == other.WorkspaceId &&
                this.Folder == other.Folder )
            {
                return true;
            }

            return false;
        }

    }
}


