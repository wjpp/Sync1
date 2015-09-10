using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLite;
using Glasscubes.Drive.Model;

namespace Glasscubes.Drive.Util
{

    

    class DBHelper
    {
        const string dbName = "sync3.db";
        public SQLiteConnection db
        {
            get; set;
        }

        public DBHelper()
        {
            DbConnect(dbName);

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
                SetupDB(dbName);
            }
            
        }

        private void SetupDB(string databaseName)
        {

            db.DropTable<Workspace>();
            db.DropTable<DiskItem>();
            db.DropTable<Globals>();
            db.DropTable<GCAction>();

            db.CreateTable<Workspace>();
            db.CreateTable<DiskItem>();
            db.CreateTable<Globals>();
            db.CreateTable<GCAction>();

        }

        private void DbConnect(string databaseName)
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            db = new SQLiteConnection(System.IO.Path.Combine(folder, databaseName));
        }

    } }
