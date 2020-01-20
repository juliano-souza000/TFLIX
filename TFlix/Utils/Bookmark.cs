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
            public string Show { get; set; }
            public bool IsSubtitled { get; set; }
            public long BookmarkInMillisecond { get; set; }
        }

        public static void CreateDB()
        {
            var db = new SQLiteConnection(DatabaseFile);
            db.CreateTable<BookmarkStore>();

            db.Dispose();
            db.Close();
            db = null;
        }

        public static void InsertData(string show, int ep, int showSeason, bool isSubtitled)
        {
            var db = new SQLiteConnection(DatabaseFile);

            var bookmark = new BookmarkStore
            {
                Show = show,
                ShowSeason = showSeason,
                EP = ep,
                IsSubtitled = isSubtitled
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
            db.Execute("UPDATE BookmarkStore SET BookmarkInMillisecond = ? WHERE Show = ? AND IsSubtitled = ? AND ShowSeason = ? AND EP = ?", bookmarkInMillisecond, show, isSubtitled, showSeason, ep);
            db.Dispose();
            db.Close();
            db = null;
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