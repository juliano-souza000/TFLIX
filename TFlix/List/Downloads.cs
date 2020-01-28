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
    public class Downloads
    {
        public int ShowID { get; set; }

        public string EpThumb { get; set; }
        public string FullTitle { get; set; }
        public int EP { get; set; }
        public int ShowSeason { get; set; }
        private int _Progress { get; set; }
        public long Bytes { get; set; }
        public long TotalBytesEP { get; set; }
        public long Duration { get; set; }
        public bool IsSelected { get; set; }
        public long TimeWatched { get; set; }
        public bool IsPaused { get; set; }
        private bool _IsDownloading { get; set; }

        public bool IsDownloading
        {
            get
            {
                return _IsDownloading;
            }
            set
            {
                bool fireEvent;
                if (value != _IsDownloading)
                    fireEvent = true;
                else
                    fireEvent = false;
                _IsDownloading = value;
                if (fireEvent)
                    Event.Download.OnChangedStatus(this, this.ShowSeason, this.EP, this.ShowID);
            }
        }

        public int Progress
        {
            get
            {
                return _Progress;
            }
            set
            {
                _Progress = value;
                try
                {
                    if (IsDownloading)
                    {
                        if (Progress != 100)
                            Event.Progress.OnProgressUpdated(this, this.ShowSeason, this.EP, this.ShowID);
                        else
                            Event.Progress.OnProgressCompleted(this, this.ShowSeason, this.EP, this.ShowID);
                    }
                }
                catch { }
            }
        }
        
    }

    public class AllDownloads
    {
        public int ShowID { get; set; }

        public string ShowThumb { get; set; }
        public long TotalBytes { get; set; }
        public string Show { get; set; }
        public bool IsSubtitled { get; set; }
        public bool IsSelected { get; set; }
        public List<Downloads> Episodes { get; set; }
    }

    public class GetDownloads
    {

        public static List<AllDownloads> Series;
    }
}