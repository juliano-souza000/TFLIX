﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace SeuSeriado.List
{
    public class Downloads
    {
        public int ShowID { get; set; }

        public string EpThumb { get; set; }
        public int EP { get; set; }
        public int ShowSeason { get; set; }
        private int _Progress { get; set; }
        public long Bytes { get; set; }
        public long TotalBytesEP { get; set; }
        public long Duration { get; set; }
        public bool IsSelected { get; set; }
        public long TimeWatched { get; set; }
        public DownloadInfo downloadInfo { get; set; }

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
                    if (downloadInfo.IsDownloading)
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

    public class DownloadInfo
    {
        public bool IsPaused { get; set; }
        public bool IsDownloading { get; set; }
        public bool IsFromSearch { get; set; }
        public string URL { get; set; }
        public string FullTitle { get; set; }
        public int ListPos { get; set; }
        public int NotificationID { get; set; }
        public int DownloadPos { get; set; }
    }

    public class GetDownloads
    {

        public static List<AllDownloads> Series;
    }
}