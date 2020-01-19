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

namespace SeuSeriado.List
{
    public class MainPageSeries
    {
        public string Update { get; set; }
        public string Title { get; set; }
        public string Synopsis { get; set; }
        public string ImgLink { get; set; }
        public string IMG64 { get; set; }
        public string EPThumb { get; set; }
        public bool Downloading { get; set; }
        public bool Downloaded { get; set; }
        public bool AlreadyChecked { get; set; }
    }

    public class GetMainPageSeries
    {
        public static List<MainPageSeries> Series;
    }
}