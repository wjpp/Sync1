﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.IO;

using RestSharp;
using RestSharp.Authenticators;
using SQLite;

using Glasscubes.Drive.Model;
using Glasscubes.Drive.UI;
using Glasscubes.Drive.Util;

namespace Glasscubes.Drive
{
    class DownloadMonitor : ConnectsToGC
    {

        
        public string rootDir { get; set; } 
       
       
        private SQLiteConnection db;
    

        public DownloadMonitor(SQLiteConnection dbIn)
        {
           
            db = dbIn;
            
        }

 

        public void Monitor()
        {
                  
            Globals global = null;
            try
            {
                global = db.Find<Globals>(1);
            }
            catch (SQLiteException e)
            {
            }
            if (global == null)
            {
                Init();
            }

           
            CheckForChanges();
           
        }

      

        private void CheckForChanges()
        {
            Globals global = null;
            try
            {
                global = db.Find<Globals>(1);
            }
            catch (SQLiteException e)
            {
            }
            if (global == null)
            {
                // this mean there was no files downloaded (as there is none on the account)
                global = new Globals();
                global.Id = 1;
                global.CurrentRevision = 0;
                db.Insert(global);
            }
            var request = newReq("/rest/sync/changes");
            
            SetUpRequest(request);
            request.AddParameter("rev", global.CurrentRevision);

            var response = client.Execute<SyncList>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                SyncList list = response.Data;
                List<DiskItem> items = list.Items;

                if (list.LatestRevision > global.CurrentRevision)
                {
                    global.CurrentRevision = list.LatestRevision;
                    db.Update(global);

                    foreach (DiskItem i in items)
                    {
                        if (DoesThisNeedUpdating(i))
                        {
                            switch (i.Action)
                            {
                                case "ADD":
                                    NewFileAdded(i);
                                    break;
                                case "DELETE":
                                    DeleteDiskItem(i);
                                    break;
                                case "UPDATED":
                                    UpdateDiskItem(i);
                                    break;
                                case "RENAME":
                                    DiskItemRenamed(i);
                                    break;
                            }
                        }
                    }
                }

            }
        }

        private bool DoesThisNeedUpdating(DiskItem i)
        {
            DiskItem di = db.Find<DiskItem>(i.Id);
            if (di == null)
            {
                return true;
            }

            switch (i.Action)
            {
                case "ADD":
                    return false;
                case "DELETE":
                    return true;
                case "UPDATED":
                    if (di.Updated != i.Updated ||
                      di.FileName != i.FileName ||
                      di.Version != i.Version || 
                      di.Size != i.Size)
                    {
                        return true;
                    }
                    return false;
                case "RENAME":
                    if (di.FileName != i.FileName)
                    {
                        return true;
                    }
                    return false;
            }


            return true;
        }

        private void DiskItemRenamed(DiskItem i)
        {
            try
            {
                DiskItem orig = db.Find<DiskItem>(i.Id);
                string root = GetRootPath(orig);
                string newFullPath = Path.Combine(root, i.FileName);
                if (i.Folder)
                {
                    Directory.Move(Path.Combine(root, orig.FileName), newFullPath);
                }
                else
                {
                    File.Move(Path.Combine(root, orig.FileName), newFullPath);
                }
                //i.Path = Path.Combine(root, i.FileName).ToString();
                i.Path = i.FileName;

                DateTime now = DateTime.Now;
                i.Updated = now;
                
                if (i.Folder)
                {
                    Directory.SetLastWriteTime(newFullPath, now);
                    i.UpdatedOnDiskUTC = Directory.GetLastWriteTimeUtc(newFullPath);
                }
                else
                {
                    File.SetLastWriteTime(newFullPath, now);
                    i.UpdatedOnDiskUTC = File.GetLastWriteTimeUtc(newFullPath);
                }
                db.Update(i);
            }
            catch (SQLiteException e)
            {
                //TODO
            }
        }

        private void UpdateDiskItem(DiskItem i)
        {
            DiskItem original = db.Find<DiskItem>(i.Id);
            DeleteDiskItem(original);
            NewFileAdded(i);
        }

        private void NewFileAdded(DiskItem i)
        {
            i.Updated = DateTime.Now;
            if (i.Folder)
            {
                string newPath = Path.Combine(GetRootPath(i), i.FileName);
                System.IO.Directory.CreateDirectory(newPath);
                // i.Path = newPath;
                i.Path = i.FileName;
                i.CreatedOnDiskUTC = Directory.GetCreationTimeUtc(newPath);
                i.UpdatedOnDiskUTC = Directory.GetLastAccessTimeUtc(newPath);

                db.Insert(i);
                
            }
            else
            {             
                db.Insert(i);
                DownLoadFile(i);
            }
        }

        private void DeleteDiskItem(DiskItem i)
        {
            db.Delete(i);
            var path = Path.Combine(GetRootPath(i), i.FileName);
            if (File.Exists(path) && !i.Folder)
            {
                File.Delete(path); 
            }
            if (Directory.Exists(path) && i.Folder)
            {
                Directory.Delete(path, true); 
            }
        }

        private void Init()
        {
            
            GetWorkspaces();
            GetInitFiles();
        }

        

        private void GetInitFiles()
        {
            // curl "http://localhost:8080/rest/sync/changes?key=a06055a0-7605-4aee-9048-981aa6ef41a0&apiId=11111&rev=0"
            var request = newReq("/rest/sync/changes");
           
            SetUpRequest(request);
            request.AddParameter("rev", 0);

            var response = client.Execute<SyncList>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                SyncList list = response.Data;
                List<DiskItem> items = list.Items;

                Globals g = new Globals();
                g.Id = 1;
                g.CurrentRevision = list.LatestRevision;
                db.Insert(g);

                foreach (DiskItem i in items)
                {
                    db.Insert(i);
                }

                var queryWork = db.Table<Workspace>();
                var workspaces = queryWork.Cast<Workspace>().ToDictionary(o => o.Id, o => o);

                // Create all folders
                var toplevel = db.Query<DiskItem>("select * from DiskItem where FolderId = ? and Folder = ?", 0, true);

                foreach (var item in toplevel)
                {
                    Workspace w = workspaces[(int)item.WorkspaceId];
                    string path = Path.Combine(rootDir, w.Name, item.FileName);
                    System.IO.Directory.CreateDirectory(path);
                    //item.Path = path;
                    item.Path = item.FileName;
                    item.UpdatedOnDiskUTC = Directory.GetLastWriteTimeUtc(path);
                    item.CreatedOnDiskUTC = Directory.GetCreationTimeUtc(path);
                    db.Update(item);

                    createChildFolders(item, path);
                }


                //Download all files

                var files = db.Query<DiskItem>("select * from DiskItem where  Folder = ?", false);
                foreach (DiskItem f in files)
                {
                    DownLoadFile(f);
                }

            }

        }

        private void DownLoadFile(DiskItem f)
        {
            var path = GetRootPath(f);
            var fullpath = Path.Combine(path, f.FileName);

            // conflict?
            if (File.Exists(fullpath))
            {
                bool conflict = true;
                int count = 1;
                while (conflict)
                {
                    string name = Path.GetFileNameWithoutExtension(f.FileName);
                    name = name + "(" + count + ")" + Path.GetExtension(f.FileName);
                    if (File.Exists(Path.Combine(path, name)))
                    {
                        count++;
                    }
                    else
                    {
                        fullpath = Path.Combine(path, name);
                        conflict = false;
                        f.FileName = name;
                    }
                }
            }

            // f.Path = fullpath;
            f.Path = f.FileName;

            db.Update(f);

            using (var client = new WebClient())
            {
                string url = server + "rest/sync/get?key=" + key + "&apiId=" + apiId + "&docId=" + f.Id;
                try {
                    client.DownloadFile(url, fullpath);
                    var length = new System.IO.FileInfo(fullpath).Length;
                    f.Size = length;
                    f.Updated = DateTime.Now;
                    f.UpdatedOnDiskUTC = File.GetLastWriteTimeUtc(fullpath);
                    f.CreatedOnDiskUTC = File.GetCreationTimeUtc(fullpath);
                    db.Update(f);
                }
                catch(WebException e)
                {
                    Console.Error.Write("Problem connecting to Glasscubes");
                    Console.Error.Write(e);
                }
            }
        }

        private string GetRootPath(DiskItem f)
        {
            string path;
            if (f.FolderId > 0)
            {
                DiskItem parent = db.Find<DiskItem>(f.FolderId);
                return GetRootPath(parent, parent.Path);
            }
            

            Workspace w = db.Find<Workspace>(f.WorkspaceId);
            path = Path.Combine(rootDir, w.Name);
            
            return path;
        }

        private string GetRootPath(DiskItem f, string path)
        {
            
            if (f.FolderId > 0)
            {
                DiskItem parent = db.Find<DiskItem>(f.FolderId);
                return GetRootPath(parent, Path.Combine(parent.Path, path));
            }
            
            Workspace w = db.Find<Workspace>(f.WorkspaceId);
            path = Path.Combine(rootDir, w.Name,  path);

            return path;
        }



        private void createChildFolders(DiskItem item, string path)
        {
            var folders = db.Query<DiskItem>("select * from DiskItem where FolderId = ? and Folder = ?", item.Id, true);
            foreach (var f in folders)
            {
                string newPath = Path.Combine(path, f.FileName);
                System.IO.Directory.CreateDirectory(newPath);
                //f.Path = newPath;
                f.Path = f.FileName;
                f.Updated = DateTime.Now;
                f.UpdatedOnDiskUTC = Directory.GetLastWriteTimeUtc(newPath);
                f.CreatedOnDiskUTC = Directory.GetCreationTimeUtc(newPath);
                db.Update(f);

                createChildFolders(f, newPath);
            }
        }



        protected void GetWorkspaces()
        {
            //  curl    "http://localhost:8080/rest/sync/workspaces?key=a06055a0-7605-4aee-9048-981aa6ef41a0&apiId=11111"                                                                                                                             
            var request = newReq("/rest/sync/workspaces");
     
            SetUpRequest(request);

            var response = client.Execute<List<Workspace>>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                List<Workspace> items = response.Data;
                foreach (Workspace w in items)
                {
                    System.IO.Directory.CreateDirectory(rootDir + "\\" + w.Name);
                    // w.Path = rootDir + "\\" + w.Name;
                    w.Path = w.Name;
                    db.Insert(w);
                }
            }

        }

        

        protected List<DiskItem> GetWorkspaceItems(int workspaceId)
        {
            //curl "http://localhost:8080/rest/sync/list?apiId=12345&key=3b0193dd-ebed-44e1-b30c-ba6430c15e78&workspaceId=115317"


            var request = newReq("/rest/sync/list");

            SetUpRequest(request);
            request.AddParameter("workspaceId", workspaceId);

            var response = client.Execute<List<DiskItem>>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                List<DiskItem> items = response.Data;
                return items;
            }

            return null;
        }

    }
}
