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

namespace TFlix.List
{
    public class KeepWatching
    {
        public bool IsOnline { get; set; }
        public bool IsSubtitled { get; set; }
        public bool Downloading { get; set; }
        public long Duration { get; set; }
        public long TimeWatched { get; set; }
        public int Ep { get; set; }
        public int Season { get; set; }
        public string Show { get; set; }
        public string Thumb { get; set; }
        public string Synopsis { get; set; }
        public string Fulltitle { get; set; }
    }

    public class KeepWatchingList
    {
        public static List<KeepWatching> KeepWatching;
    }
}