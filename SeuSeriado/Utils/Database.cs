using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace SeuSeriado.Utils
{
    class Database
    {

        private static readonly string DatabaseFile = Application.Context.GetDatabasePath("Shows").AbsolutePath+".db3";


        [Table("Shows")]
        public class Shows
        {
            [PrimaryKey, AutoIncrement]
            public int ShowID { get; set; }

            public string Show { get; set; }
            public string ShowThumb { get; set; }
            public bool IsSubtitled { get; set; }
        }

        [Table("Episodes")]
        public class Episodes
        {
            public int ShowID { get; set; }

            public string EpThumbPath { get; set; }
            public int ShowSeason { get; set; }
            public int EP { get; set; }
            public int Progress { get; set; }
            public long Bytes { get; set; }
            public long TotalBytesEP { get; set; }
            public string Path { get; set; }
            public long Duration { get; set; }
        }

        public static void CreateDB()
        {
            var db = new SQLiteConnection(DatabaseFile);
            db.CreateTable<Shows>();
            db.CreateTable<Episodes>();
            
            db.Dispose();
            db.Close();
            db = null;
        }

        public static void InsertData(string epthumb, string show, string showThumb, int showSeason, int ep, long bytes, long totalBytesEp, bool isSubtitled, string path, long duration)
        {

            var db = new SQLiteConnection(DatabaseFile);

            var Show = new Shows
            {
                Show = show,
                ShowThumb = showThumb,
                IsSubtitled = isSubtitled
            };

            var x = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID);
            if (x.Count() == 0)
                db.Insert(Show);

            x = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID);

            if (db.Table<Episodes>().Where(row => row.Path == path).Count() == 0)
            {
                var Eps = new Episodes
                {
                    ShowID = Convert.ToInt32(x.First()),
                    Bytes = bytes,
                    EP = ep,
                    EpThumbPath = epthumb,
                    Progress = 0,
                    ShowSeason = showSeason,
                    TotalBytesEP = totalBytesEp,
                    Path = path,
                    Duration = duration
                };

                db.Insert(Eps);
            }
            db.Dispose();
            db.Close();
            db = null;
        }

        public static void DeleteItems()
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<Episodes>();

            int showID;

            foreach (var it in List.GetDownloads.Series.ToList())
            {
                foreach(var item in it.Episodes.Where(row => row.IsSelected == true))
                {
                    showID = db.Table<Shows>().Where(row => row.Show == it.Show).Select(row => row.ShowID).First();
                    try
                    {
                        System.IO.File.Delete(GetVideoPath(it.IsSubtitled, it.Show, item.EP, item.ShowSeason));
                    }
                    catch { }

                    db.Execute("DELETE FROM Episodes WHERE ShowID = "+ showID + " AND EP = "+ item.EP + " AND ShowSeason = "+ item.ShowSeason);

                    if (table.Where(row => row.ShowID == showID && row.ShowSeason == item.ShowSeason).Count() == 1)
                    {
                        db.Execute("DELETE FROM Episodes WHERE ShowID = " + showID);
                    }

                    if(table.Where(row => row.ShowID == showID).Count() == 0)
                    {
                        db.Execute("DELETE FROM Shows WHERE ShowID = " + showID );
                    }
                }
                ReadDB();
            }

            db.Dispose();
            db.Close();
            db = null;
        }

        public static void DeleteItem(bool isSubtitled, string show, int ep, int season)
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<Episodes>();

            try
            {
                var showID = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID).First();
                var bytes = table.Where(row => row.EP == ep && row.ShowSeason == season && row.ShowID == showID).Delete();
            }
            catch { }
        }

        public static void UpdateProgress(string show, int showSeason, int ep, int progress, long bytes, bool isSubtitled)
        {
            var db = new SQLiteConnection(DatabaseFile);
            int showID = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID).First();
            if(progress == -1)
                db.Execute("UPDATE Episodes SET Bytes = " + bytes + " WHERE ShowID = " + showID + " AND ShowSeason = " + showSeason + " AND EP = " + ep);
            else
                db.Execute("UPDATE Episodes SET Progress = " + progress + ", Bytes = " + bytes  + " WHERE ShowID = " + showID + " AND ShowSeason = " + showSeason + " AND EP = " + ep);
            
            db.Dispose();
            db.Close();
            db = null;
        }

        public static string GetVideoPath(bool isSubtitled, string show, int ep, int season)
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<Episodes>();

            var path = table.AsEnumerable().Select(row => row.Path).Where(row => row.Contains(show) && row.Contains(string.Format("Episódio {0:D2}", ep)) && row.Contains(string.Format("{0}ª Temporada", season)));

            if (isSubtitled)
                return path.Where(row => row.ToLower().Contains("legendado")).First();
            else
                return path.Where(row => row.ToLower().Contains("legendado") == false).First();
        }

        public static bool IsSeasonOnDB(int season, string show, bool isSubtitled)
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<Episodes>();
            try
            {
                var showID = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID).First();
                if (List.GetDownloads.Series != null || List.GetDownloads.Series.Count > 0)
                {
                    if (table.Any(x => x.ShowSeason == season && x.ShowID == showID))
                    {
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static long GetDownloadedBytes(bool isSubtitled, string show, int ep, int season)
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<Episodes>();

            //Set actual downloaded bytes
            try
            {
                long lenght = new System.IO.FileInfo(GetVideoPath(isSubtitled, show, ep, season)).Length;
                UpdateProgress(show, season, ep, -1, lenght, isSubtitled);
            }
            catch { }

            try
            {
                var showID = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID).First();
                var bytes = table.AsEnumerable().Where(row => row.EP == ep && row.ShowSeason == season && row.ShowID == showID).Select(row => row.Bytes).First();
                return bytes;
            }
            catch { }
            return 0;
        }

        public static long GetTotalBytes(bool isSubtitled, string show, int ep, int season)
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<Episodes>();

            try
            {
                var showID = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID).First();
                var bytes = table.AsEnumerable().Where(row => row.EP == ep && row.ShowSeason == season && row.ShowID == showID).Select(row => row.TotalBytesEP).First();
                return bytes;
            }
            catch { }
            return 0;
        }

        public static void ReadDB()
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<Shows>();
            int showID;

            if (List.GetDownloads.Series == null)
                List.GetDownloads.Series = new List<List.AllDownloads>();

                List.GetDownloads.Series.Clear();

                var Items = table.AsEnumerable().Select(row => new List.AllDownloads
                {
                    Show = row.Show,
                    ShowThumb = row.ShowThumb,
                    IsSubtitled = row.IsSubtitled
                });
                
                foreach (var Item in Items)
                {
                    showID = db.Table<Shows>().Where(row => row.Show == Item.Show && row.IsSubtitled == Item.IsSubtitled).Select(row => row.ShowID).First();
                    Item.Episodes = db.Table<Episodes>().AsEnumerable().Select(row2 => new List.Downloads
                    {
                        Bytes = row2.Bytes,
                        TotalBytesEP = row2.TotalBytesEP,
                        Progress = row2.Progress,
                        EP = row2.EP,
                        ShowSeason = row2.ShowSeason,
                        ShowID = row2.ShowID,
                        EpThumb = row2.EpThumbPath,
                        Duration = row2.Duration
                    }).Where(row => row.ShowID == showID).OrderBy(x => x.ShowSeason).OrderBy(x => x.EP).ToList();
                    List.GetDownloads.Series.Add(Item);
                }
            
            db.Dispose();
            db.Close();
            db = null;
        }

    }
}