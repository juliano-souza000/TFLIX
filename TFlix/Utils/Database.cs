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
using MoreLinq;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace TFlix.Utils
{
    class Database
    {
        private static readonly string DatabaseFile = Application.Context.GetDatabasePath("Shows").AbsolutePath + ".db3";

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
            public string FullTitle { get; set; }
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

        public static void InsertData(string epthumb, string show, string showThumb, int showSeason, int ep, long bytes, long totalBytesEp, bool isSubtitled, string path, long duration, string fullTitle)
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
            var showID = Convert.ToInt32(x.First());

            if (db.Table<Episodes>().Where(row => row.ShowSeason == showSeason && row.EP == ep && row.ShowID == showID).Select(row => row.ShowID).Count() == 0)
            {
                var Eps = new Episodes
                {
                    ShowID = showID,
                    Bytes = bytes,
                    EP = ep,
                    EpThumbPath = epthumb,
                    Progress = 0,
                    ShowSeason = showSeason,
                    TotalBytesEP = totalBytesEp,
                    Path = path,
                    Duration = duration,
                    FullTitle = fullTitle
                };

                db.Insert(Eps);
            }

            Bookmark.InsertData(show, ep, showSeason, isSubtitled);

            db.Dispose();
            db.Close();
            db = null;
        }

        public static void DeleteItems()
        {
            var dict = List.GetDownloads.Series.ToDictionary(x => x);
            foreach (var it in dict.Values.ToList())
            {
                var dictIt = it.Episodes.ToDictionary(x => x);
                foreach (var item in dictIt.Values.Where(row => row.IsSelected == true).ToList())
                {
                    DeleteItem(it.IsSubtitled, it.Show, item.EP, item.ShowSeason);
                }
            }
            ReadDB();
        }

        public static void DeleteItem(bool isSubtitled, string show, int ep, int season)
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<Episodes>();

            var listPos = List.GetDownloads.Series.FindIndex(x => x.IsSubtitled == isSubtitled && x.Show == show);

            try
            {
                if (ep != 0)
                    System.IO.File.Delete(GetVideoPath(isSubtitled, show, ep, season));
            }
            catch { }

            try
            {
                var showID = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID).First();
                table.Where(row => row.EP == ep && row.ShowSeason == season && row.ShowID == showID).Delete();

                var epIndex = List.GetDownloads.Series[listPos].Episodes.FindIndex(x => x.EP == ep && x.ShowSeason == season && x.ShowID == showID);
                List.GetDownloads.Series[listPos].Episodes.RemoveAt(epIndex);

                if (table.Where(row => row.ShowID == showID).Count() == 0)
                {
                    db.Execute("DELETE FROM Shows WHERE ShowID = " + showID);
                    List.GetDownloads.Series.RemoveAt(listPos);
                }
            }
            catch { }

            try
            {
                var countOfItemsWithPosSeason = List.GetDownloads.Series[listPos].Episodes.FindAll(delegate (List.Downloads dl) { return dl.ShowSeason == season; }).Count;
                if (countOfItemsWithPosSeason == 1)
                    DeleteItem(isSubtitled, show, 0, season);
            }
            catch { }

            db.Dispose();
            db.Close();
            db = null;
        }

        public static void UpdateProgress(string show, int showSeason, int ep, int progress, long bytes, bool isSubtitled)
        {
            var db = new SQLiteConnection(DatabaseFile);
            int showID = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID).First();
            if (progress == -1)
                db.Execute("UPDATE Episodes SET Bytes = " + bytes + " WHERE ShowID = " + showID + " AND ShowSeason = " + showSeason + " AND EP = " + ep);
            else
                db.Execute("UPDATE Episodes SET Progress = " + progress + ", Bytes = " + bytes + " WHERE ShowID = " + showID + " AND ShowSeason = " + showSeason + " AND EP = " + ep);

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
                return path.Where(row => row.ToLower().Contains("legendado")).FirstOrDefault();
            else
                return path.Where(row => row.ToLower().Contains("legendado") == false).FirstOrDefault();
        }

        public static bool IsSeasonOnDB(int season, string show, bool isSubtitled)
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<Episodes>();
            try
            {
                var showID = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID).First();

                if (table.Where(x => x.ShowSeason == season && x.ShowID == showID).Count() == 0)
                {
                    return true;
                }

            }
            catch { }
            db.Dispose();
            db.Close();
            db = null;
            return false;
        }

        public static bool IsItemDownloaded(int season, string show, bool isSubtitled, int ep)
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<Episodes>();
            bool isItemDownloaded = false;

            try
            {
                var x = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID);

                if (x.Count() != 0)
                {
                    try
                    {
                        var showID = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID).First();
                        var progress = table.Where(row => row.EP == ep && row.ShowSeason == season && row.ShowID == showID).Select(row => row.Progress).First();
                        if (progress == 100)
                            isItemDownloaded = true;
                    }
                    catch { }
                }
            }
            catch { }

            db.Dispose();
            db.Close();
            db = null;
            return isItemDownloaded;
        }

        public static long GetDownloadedBytes(bool isSubtitled, string show, int ep, int season)
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<Episodes>();

            //Set actual downloaded bytes
            try
            {
                long length = new System.IO.FileInfo(GetVideoPath(isSubtitled, show, ep, season)).Length;
                UpdateProgress(show, season, ep, -1, length, isSubtitled);
            }
            catch { }

            try
            {
                var showID = db.Table<Shows>().Where(row => row.Show == show && row.IsSubtitled == isSubtitled).Select(row => row.ShowID).First();
                var bytes = table.AsEnumerable().Where(row => row.EP == ep && row.ShowSeason == season && row.ShowID == showID).Select(row => row.Bytes).First();
                return bytes;
            }
            catch { }
            db.Dispose();
            db.Close();
            db = null;
            return 0;
        }

        public static long GetTotalDownloadedBytes(bool isSubtitled, string show, int ep, int season)
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
            db.Dispose();
            db.Close();
            db = null;
            return 0;
        }

        public static void ReadDB()
        {
            try
            {
                var db = new SQLiteConnection(DatabaseFile);
                var table = db.Table<Shows>();
                int showID;
                if (List.GetDownloads.Series == null)
                    List.GetDownloads.Series = new List<List.AllDownloads>();

                //List.GetDownloads.Series.Clear();

                var Items = table.AsEnumerable().Select(row => new List.AllDownloads
                {
                    Show = row.Show,
                    ShowThumb = row.ShowThumb,
                    IsSubtitled = row.IsSubtitled,
                    ShowID = row.ShowID
                });

                foreach (var Item in Items)
                {
                    showID = db.Table<Shows>().Where(row => row.Show == Item.Show && row.IsSubtitled == Item.IsSubtitled).Select(row => row.ShowID).First();

                    try
                    {
                        Android.Media.MediaMetadataRetriever reader = new Android.Media.MediaMetadataRetriever();
                        var tempPath = db.Table<Episodes>().AsEnumerable().Where(row => row.ShowID == showID && row.Duration == 0 && row.Bytes >= 1048576).Select(row2 => row2.Path);
                        var tempVars = db.Table<Episodes>().AsEnumerable().Where(row => row.ShowID == showID && row.Duration == 0 && row.Bytes >= 1048576).Select(row2 => new List<(string Path, int Ep, int Season)> { (row2.Path, row2.EP, row2.ShowSeason) });

                        foreach (var it in tempVars)
                        {
                            foreach (var itn in it)
                            {
                                reader.SetDataSource(itn.Path);
                                var dur = long.Parse(reader.ExtractMetadata(Android.Media.MetadataKey.Duration));
                                reader.Release();
                                db.Execute("UPDATE Episodes SET Duration = " + dur + " WHERE ShowID = " + showID + " AND ShowSeason = " + itn.Season + " AND EP = " + itn.Ep);
                            }
                        }
                    }
                    catch { }

                    Item.Episodes = db.Table<Episodes>().AsEnumerable().Select(row2 => new List.Downloads
                    {
                        Bytes = row2.Bytes,
                        TotalBytesEP = row2.TotalBytesEP,
                        Progress = row2.Progress,
                        EP = row2.EP,
                        ShowSeason = row2.ShowSeason,
                        ShowID = row2.ShowID,
                        EpThumb = row2.EpThumbPath,
                        Duration = row2.Duration,
                        TimeWatched = Bookmark.GetBookmarkInMillisecond(Item.Show, row2.EP, row2.ShowSeason, Item.IsSubtitled),
                        FullTitle = row2.FullTitle
                    }).Where(row => row.ShowID == showID).OrderBy(x => x.ShowSeason).ThenBy(x => x.EP).ToList();

                    if (List.GetDownloads.Series.FindAll(x => x.ShowID == showID).Count != 0)
                    {
                        for (int i = 0; i < List.GetDownloads.Series.Count; i++)
                        {
                            foreach (var ep in Item.Episodes)
                            {
                                var counts = List.GetDownloads.Series[i].Episodes.FindAll(x => x.EP == ep.EP && x.ShowSeason == ep.ShowSeason).Count;
                                if (counts == 0 && List.GetDownloads.Series[i].Episodes.FindAll(x => x.ShowID == showID).Count != 0)
                                    List.GetDownloads.Series[i].Episodes.Add(ep);
                            }
                        }
                    }
                    else
                    {
                        List.GetDownloads.Series.Add(Item);
                    }
                }

                for(int i = 0;i < List.GetDownloads.Series.Count; i++)
                {
                    List.GetDownloads.Series[i].Episodes = List.GetDownloads.Series[i].Episodes.OrderBy(x => x.ShowSeason).ThenBy(x => x.EP).ToList();
                }

                db.Dispose();
                db.Close();
                db = null;
            }
            catch (SQLiteException e)
            {
                if (e.Result == SQLite3.Result.Locked || e.Result == SQLite3.Result.Busy)
                    ReadDB();
            }
        }

    }
}