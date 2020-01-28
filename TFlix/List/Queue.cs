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
    public class QueueList
    {
        public string FullTitle { get; set; }
        public string URL { get; set; }
        public string ShowThumb { get; set; }
        public string EpThumb { get; set; }
        public string Show { get; set; }
        public bool IsSubtitled { get; set; }
        public long Duration { get; set; }
        public int ShowSeason { get; set; }
        public int Ep { get; set; }
    }

    public class Queue
    {
        public static List<QueueList> DownloadQueue;
    }
}