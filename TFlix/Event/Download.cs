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

namespace TFlix.Event
{
    class Download
    {
        public static event EventHandler<ProgressEventArgs> ChangedStatus;

        protected static void OnChangedStatus(object sender, ProgressEventArgs e)
        {
            ChangedStatus?.Invoke(sender, e);
        }

        public static void OnChangedStatus(object sender, int showSeason, int ep, int showID)
        {
            OnChangedStatus(sender, new ProgressEventArgs(showSeason, ep, showID));
        }
    }
}