using Glasscubes.Drive.Model;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Glasscubes.Drive.Util;
using RestSharp;
using System.IO;

namespace Glasscubes.Drive
{
    class GCActionConsumer : ConnectsToGC
    {
        const int FUZZY_TIMESTAMP_DIFF_MS = 2000; 
        private SQLiteConnection db;
        public string rootDir { get; set; }

        public GCActionConsumer(SQLiteConnection db)
        {
            this.db = db;
        }

        public void Process()
        {
            var actions = db.Query<GCAction>("select * from GCAction");
            foreach(GCAction action in actions)
            {
                switch(action.Action)
                {
                    case GCAction.NEW:
                        DoNew(action);
                        break;
                    case GCAction.CHANGED:
                        DoChanged(action);
                        break;
                    case GCAction.DELETED:
                        doDelete(action);
                        break;
                    case GCAction.RENAMED:
                        DoRenamed(action);
                        break;
                }
                db.Delete(action);
            }
        }

        internal static bool FileOrDirectoryExists(string name)
        {
            return (Directory.Exists(name) || File.Exists(name));
        }

        private void DoRenamed(GCAction action)
        {
            //Does it still exist?
            if (!FileOrDirectoryExists(action.Path)) return;


            if (ThisIsFromMe(action))
            {
                return;
            }

            Object o = FindDiskItem(action);
            // We should already have this file in DB unless its a new folder thats just got renamed
            if (!Directory.Exists(action.Path) && o == null) return;

            if (o == null)
            {
                // new folder
                DoNew(action);
                return;
            }

            //        @GET
            // @Path("/rename")
            // @Produces(MediaType.APPLICATION_JSON)
            // @Transactional
            //public FileMetaUpdate rename(
            //        @QueryParam("apiId") String apiId,
            //        @QueryParam("key") String key,
            //        @QueryParam("docId") Long docId,
            //        @QueryParam("filename") String fileName,
            //        @QueryParam("folder") Boolean isFolder) {

            if (o is DiskItem)
            {
                DiskItem item = (DiskItem)o;
                var request = newReq("/rest/sync/rename");

                SetUpRequest(request);
                request.AddParameter("docId", item.Id);
                request.AddParameter("folder", item.Folder);
                request.AddParameter("filename", Path.GetFileName(action.Path));

                var response = client.Execute<NewFileMeta>(request);
                if (response.ResponseStatus == ResponseStatus.Completed)
                {
                    NewFileMeta meta = response.Data;
                    if (meta.success)
                    {
                        // what do if anything?
                        return;
                    }
                }
                Console.Error.Write("Could not rename file ? ", action.Path);
            }

            // we do nothing if workspace
            Console.Error.Write("Workspace NOT being renamed ? ", action.Path);
        }

        private void DoChanged(GCAction action)
        {

            //Does it still exist?
            if (!FileOrDirectoryExists(action.Path)) return; // can happen when creating a new folder that immediately gets a different name

            if (ThisIsFromMe(action))
            {
                return;
            }


            // We should already have this file in DB if not then it could be a new folder thats being renamed (it fires a change event as well)
            Object o = FindDiskItem(action);
            if (o == null) return;

            if (o is DiskItem)
            {
                DiskItem item = (DiskItem)o;

                // anything changed? (it may be a parent folder)
                if (item.Folder)
                {
                    return;
                } 


            }

            // we don't do anything for workspaces
        }

        private void doDelete(GCAction action)
        {

            // find DiskItem 
            var rs = from d in db.Table<DiskItem>()
                       where d.Path.Equals(action.Path)
                       select d;
            if (rs.Count() == 0)
            {
                Console.Error.Write("No matching Diskitem when deleting ?", action.Path);
                return;
            }
            DiskItem item = rs.FirstOrDefault();

            var request = newReq("/rest/sync/delete");
       
            SetUpRequest(request);
            request.AddParameter("docId", action.DiskItemId);
            request.AddParameter("folder", item.Folder);

            var response = client.Execute<DeleteFileMeta>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                DeleteFileMeta meta = response.Data;
                if (meta.success)
                {
                    // what do if anything?

                }
            }

        }
        private void DoNew(GCAction action)
        {

            //Does it still exist?
            if (!FileOrDirectoryExists(action.Path)) return; // can happen when creating a new folder that immediately gets a different name

            if (ThisIsFromMe(action))
            {
                return;
            }

            RestRequest request;
            FileAttributes attr = File.GetAttributes(action.Path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                request = newReq("/rest/sync/newFolder", Method.GET);
            }
            else
            {
                request = newReq("/rest/sync/new", Method.POST);
            }

           
            SetUpRequest(request);
            request.AddHeader("Content-Type", "multipart/form-data");

            request.AddParameter("apiId", apiId, ParameterType.QueryString);
            request.AddParameter("key", key, ParameterType.QueryString);

            DiskItem i = new DiskItem();
            Object o = FindParentDiskItem(action);
            if (o == null)
            {
                Console.Error.Write("Unknown parent DIR cannot sync ?", action.Path);
                return;
            }
            if (o is Workspace)
            {
                request.AddParameter("workspaceId", ((Workspace)o).Id, ParameterType.QueryString);
                i.WorkspaceId = ((Workspace)o).Id;
            } else if (o is DiskItem)
            {
                DiskItem f = (DiskItem)o;
                request.AddParameter("workspaceId", f.WorkspaceId, ParameterType.QueryString);
                request.AddParameter("folderId", f.Id, ParameterType.QueryString);
                i.FolderId = f.Id;
                i.WorkspaceId = f.WorkspaceId;
            }

           
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                request.AddParameter("folderName", Path.GetFileName(action.Path), ParameterType.QueryString);
                i.Folder = true;
            }
            else
            {
                request.AddFile("file", action.Path);
            }

            var response = client.Execute<NewFileMeta>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                NewFileMeta meta = response.Data;
                if (meta.success)
                {
                    // insert new DiskItem
                    
                    i.Id = (long)meta.docId;
                    i.FileName = Path.GetFileName(action.Path);
                    i.Action = "ADD";
                    i.Created = meta.created;
                    i.Path = Path.GetFileName(action.Path);
                    if (!i.Folder)
                    {
                        FileInfo info = new FileInfo(action.Path);
                        i.Size = info.Length;
                    }
                    i.Version = 1;
                    db.Insert(i);
                }
            }

        }

        
        // We need to check if this GCAction event has occured from a user or from the DownloadMinitor (me) i.e. event arising from files being downloaded/deleted/rename etc on Glasscubes
        private bool ThisIsFromMe(GCAction action)
        {

            Object o = FindDiskItem(action);
            if (o == null) return false;
           
            if (o is DiskItem)
            {
                DiskItem di = (DiskItem)o;
                // Test to see if its the same file/folder or a different version
                FileInfo fi = new FileInfo(action.Path);
                if (di.Folder)
                {
                    // fuzzy timecheck
                    TimeSpan span = fi.LastAccessTimeUtc - di.UpdatedOnDiskUTC;
                    if (span.TotalMilliseconds < FUZZY_TIMESTAMP_DIFF_MS)
                    {
                        return true;
                    }
                }
                else
                {
                   
                    var length = fi.Length;
                    if (di.Size == length)
                    {
                        // fuzzy timecheck
                        TimeSpan span = fi.LastAccessTimeUtc - di.UpdatedOnDiskUTC;
                        if (span.Milliseconds < FUZZY_TIMESTAMP_DIFF_MS)
                        {
                            return true;
                        }
                    }
                }
            }

            
            if (o is Workspace)
            {
                 return true;
            }

            return false;
        }

        private Object FindDiskItem(GCAction action)
        {
            string path;
            if (action.Action == GCAction.RENAMED)
            {
                path = action.OldPath;
            }
            else
            {
                path = action.Path;
            }

            string[] directories = path.Split(Path.DirectorySeparatorChar);

            path = "";
            Workspace workspace = null;
            DiskItem item = null;
            bool matchRoot = true;
            bool matchWorkspace = false;
            bool matchFoldersOrFiles = false;
            foreach(string p in directories)
            {
                if (path == "")
                {
                    path = p + "\\";
                } else
                {
                    path = Path.Combine(path, p);
                }

                if (matchRoot && path == rootDir)
                {
                    matchRoot = false;
                    matchWorkspace = true;
                }
                else
                if (matchWorkspace)
                {
                    var wrs = from w in db.Table<Workspace>()
                              where w.Path.Equals(p)
                              select w;
                    if (wrs.Count() != 0)
                    {
                        workspace = wrs.FirstOrDefault<Workspace>();
                    }
                    // TODO no workspace?
                    matchWorkspace = false;
                    matchFoldersOrFiles = true;
                }
                else
                if (matchFoldersOrFiles)
                {
                    if (item == null)
                    {
                        IEnumerable<DiskItem> d = db.Query<DiskItem>("select * from DiskItem where Path = ? and WorkspaceId = ?", p, workspace.Id);
                        item = d.FirstOrDefault<DiskItem>();
                        if (item == null) return null; // means we could not find any match
                    }
                    else
                    {
                        IEnumerable<DiskItem> d = db.Query<DiskItem>("select * from DiskItem where Path = ? and WorkspaceId = ? and FolderId = ? ", p, workspace.Id, item.Id);
                        item = d.FirstOrDefault<DiskItem>();
                        if (item == null) return null;
                    }
                }
            }

            if (item != null) return item;
            if (workspace != null) return workspace;
           

            return null;
        }




        private Object FindParentDiskItem(GCAction action)
        {
            string[] directories = action.Path.Split(Path.DirectorySeparatorChar);

            string path = "";
            Workspace workspace = null;
            DiskItem parent = null;
            bool matchRoot = true;
            bool matchWorkspace = false;
            bool matchFoldersOrFiles = false;
            int index = 1;
            foreach (string p in directories)
            {
                if (path == "")
                {
                    path = p + "\\";
                }
                else
                {
                    path = Path.Combine(path, p);
                }

                if (matchRoot && path == rootDir)
                {
                    matchRoot = false;
                    matchWorkspace = true;
                }
                else
                if (matchWorkspace)
                {
                    var wrs = from w in db.Table<Workspace>()
                              where w.Path.Equals(p)
                              select w;
                    if (wrs.Count() != 0)
                    {
                        workspace = wrs.FirstOrDefault<Workspace>();
                    }
                    // TODO no workspace?
                    matchWorkspace = false;
                    matchFoldersOrFiles = true;
                }
                else
                if (matchFoldersOrFiles)
                {
                    if (parent == null)
                    {
                        IEnumerable<DiskItem> d = db.Query<DiskItem>("select * from DiskItem where Path = ? and WorkspaceId = ?", p, workspace.Id);
                        DiskItem it  = d.FirstOrDefault<DiskItem>();
                        if (it != null && index + 1 == directories.Length) parent = it;
                    }
                    else
                    {
                        IEnumerable<DiskItem> d = db.Query<DiskItem>("select * from DiskItem where Path = ? and WorkspaceId = ? and FolderId = ? ", p, workspace.Id, parent.Id);
                        DiskItem item = d.FirstOrDefault<DiskItem>();
                        if (item != null && index + 1 == directories.Length) parent = item;
                    }
                }
                index++;
            }

            if (parent != null) return parent;
            if (workspace != null) return workspace;


            return null;
        }
    }
}

