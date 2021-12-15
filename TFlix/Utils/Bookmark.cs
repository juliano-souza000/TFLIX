using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace TFlix.Utils
{
    class Bookmark
    {
        private static readonly string DatabaseFile = Application.Context.GetDatabasePath("bookmark").AbsolutePath + ".db3";

        [Table("BookmarkStore")]
        public class BookmarkStore
        {
            [PrimaryKey, AutoIncrement]
            public int ID { get; set; }
            public int ShowSeason { get; set; }
            public int EP { get; set; }
            public string Fulltitle { get; set; }
            public string Show { get; set; }
            public string ShowThumb { get; set; }
            public bool IsSubtitled { get; set; }
            public bool IsOnline { get; set; }
            public long Duration { get; set; }
            public long BookmarkInMillisecond { get; set; }
            public DateTime LastUpdate { get; set; }
        }

        public static void CreateDB()
        {
            var db = new SQLiteConnection(DatabaseFile);
            db.CreateTable<BookmarkStore>();

            db.Dispose();
            db.Close();
            db = null;
        }

        public static void InsertData(string show, int ep, int showSeason, bool isSubtitled, string showThumb, bool isOnline, long duration, string fullTitle)
        {
            var db = new SQLiteConnection(DatabaseFile);

            var itemDownloaded = Database.IsItemDownloaded(showSeason, show, isSubtitled, ep);

            if (itemDownloaded)
                isOnline = false;


            var bookmark = new BookmarkStore
            {
                Show = show,
                ShowSeason = showSeason,
                EP = ep,
                IsSubtitled = isSubtitled,
                ShowThumb = showThumb,
                IsOnline = isOnline,
                Duration = duration,
                Fulltitle = fullTitle,
                LastUpdate = DateTime.Now
            };

            var x = db.Table<BookmarkStore>().Where(row => row.Show == show && row.ShowSeason == showSeason && row.EP == ep && row.IsSubtitled == isSubtitled).Select(row => row.ID);
            if (x.Count() == 0)
                db.Insert(bookmark);
            db.Dispose();
            db.Close();
            db = null;
        }

        public static void UpdateBookmarkInMillisecond(string show, int ep, int showSeason, bool isSubtitled, long bookmarkInMillisecond)
        {
            var db = new SQLiteConnection(DatabaseFile);
            db.Execute("UPDATE BookmarkStore SET BookmarkInMillisecond = ?, LastUpdate = ? WHERE Show = ? AND IsSubtitled = ? AND ShowSeason = ? AND EP = ?", bookmarkInMillisecond, DateTime.Now, show, isSubtitled, showSeason, ep);
            db.Dispose();
            db.Close();
            db = null;
        }

        public static void DeleteItem(bool isSubtitled, string show, int ep, int season)
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<BookmarkStore>();

            try
            {
                db.Execute("DELETE FROM BookmarkStore WHERE Show = ? AND IsSubtitled = ? AND ShowSeason = ? AND EP = ?", show, isSubtitled, season, ep);
                List.KeepWatchingList.KeepWatching.RemoveAt(List.KeepWatchingList.KeepWatching.FindIndex(x => x.Show == show && x.IsSubtitled == isSubtitled && x.Season == season && x.Ep == ep));
            }
            catch { }
            db.Dispose();
            db.Close();
            db = null;
        }

        public static void ReadDB()
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<BookmarkStore>();

            var Items = table.AsEnumerable().OrderBy(x => x.LastUpdate.Ticks).Select(row => new List.KeepWatching
            {
                Show = row.Show,
                Season = row.ShowSeason,
                Ep = row.EP,
                Thumb = row.ShowThumb,
                IsOnline = row.IsOnline,
                IsSubtitled = row.IsSubtitled,
                Duration = row.Duration,
                TimeWatched = row.BookmarkInMillisecond,
                Fulltitle = row.Fulltitle
            }).ToList();

            for(int i = 0; i < Items.Count; i++)
            {
                var itemDownloaded = Database.IsItemDownloaded(Items[i].Season, Items[i].Show, Items[i].IsSubtitled, Items[i].Ep);

                if (itemDownloaded)
                {
                    db.Execute("UPDATE BookmarkStore SET IsOnline = ? WHERE Show = ? AND IsSubtitled = ? AND ShowSeason = ? AND EP = ?", false, Items[i].Show, Items[i].IsSubtitled, Items[i].Season, Items[i].Ep);
                    Items[i].IsOnline = false;
                }
                else
                {
                    Items[i].IsOnline = true;
                    if (!Items[i].Thumb.StartsWith("http"))
                    {
                        Items.RemoveAt(i);
                        i--;
                    }
                }
            }

            List.KeepWatchingList.KeepWatching = Items;
        }

        public static long GetBookmarkInMillisecond(string show, int ep, int showSeason, bool isSubtitled)
        {
            var db = new SQLiteConnection(DatabaseFile);
            var table = db.Table<BookmarkStore>();

            long bookmarkInMillisecond = 0;

            try
            {
                bookmarkInMillisecond = table.Where(row => row.Show == show && row.IsSubtitled == isSubtitled && row.ShowSeason == showSeason && row.EP == ep).Select(row => row.BookmarkInMillisecond).First();
            }
            catch { }
            db.Dispose();
            db.Close();
            db = null;

            return bookmarkInMillisecond;
        }
    }
}