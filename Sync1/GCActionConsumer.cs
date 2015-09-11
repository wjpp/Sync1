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
        private SQLiteConnection db;

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
                        throw new NotImplementedException();
                        break;
                    case GCAction.DELETED:
                        doDelete(action);
                        break;
                    case GCAction.RENAMED:
                        throw new NotImplementedException();
                        break;
                }
                db.Delete(action);
            }
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
            var request = newReq("/rest/sync/new", Method.POST);
            SetUpRequest(request);
            request.AddHeader("Content-Type", "multipart/form-data");

            request.AddParameter("apiId", apiId, ParameterType.QueryString);
            request.AddParameter("key", key, ParameterType.QueryString);


            // try to find folder first
            DiskItem i = new DiskItem();
            string parent = Directory.GetParent(action.Path).FullName;
            var rs = from d in db.Table<DiskItem>()
                     where d.Path.Equals(parent)
                     select d;
            if (rs.Count() == 0)
            {
                // lets try and find workspace then
                var wrs = from d in db.Table<Workspace>()
                          where d.Path.Equals(parent)
                          select d;
                if (wrs.Count() == 0)
                {
                    Console.Error.Write("Unknown parent DIR cannot sync ?", parent);
                    return;
                }
                request.AddParameter("workspaceId", wrs.FirstOrDefault().Id, ParameterType.QueryString);
                i.Folder = true;
               
            }
            else
            {
                DiskItem f = rs.FirstOrDefault();
                request.AddParameter("workspaceId", f.WorkspaceId, ParameterType.QueryString);
                request.AddParameter("folderId", f.Id, ParameterType.QueryString);
                i.FolderId = f.Id;
                i.Folder = false;

            }


            FileAttributes attr = File.GetAttributes(action.Path);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                request.AddParameter("folderName", Path.GetFileName(action.Path), ParameterType.QueryString);
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
                    i.Path = action.Path;
                    FileInfo info = new FileInfo(action.Path);
                    i.Size = info.Length;
                    i.Version = 1;
                    db.Insert(i);
                }
            }

        }

    }
}
