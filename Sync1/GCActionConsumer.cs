using Glasscubes.Drive.Model;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Glasscubes.Drive.Util;
using RestSharp;

namespace Glasscubes.Drive
{
    class GCActionConsumer : ConnectsToGC
    {
        private SQLiteConnection db;

        public GCActionConsumer(SQLiteConnection db)
        {
            this.db = db;
        }

        public void process()
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

            DiskItem item = db.Find<DiskItem>(action.DiskItemId);
            if (item == null)
            {
                Console.Error.Write("cannot find diskitem!!");
                return;
            }

            var request = new RestRequest();
            request.Resource = "/rest/sync/delete";
            SetUpRequest(request);
            request.AddParameter("docId", action.DiskItemId);
            request.AddParameter("folder", item.Folder);

            var response = client.Execute<FileMeta>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                FileMeta meta = response.Data;

            }

        }
        private void DoNew(GCAction action)
        {
            throw new NotImplementedException();
        }

        
    }
}
