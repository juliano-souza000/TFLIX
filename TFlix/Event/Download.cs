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
        public static event EventHandler<StatusEventArgs> ChangedStatus;

        protected static void OnChangedStatus(object sender, StatusEventArgs e)
        {
            ChangedStatus?.Invoke(sender, e);
        }

        public static void OnChangedStatus(object sender, string fullTitle)
        {
            OnChangedStatus(sender, new StatusEventArgs(fullTitle));
        }
    }

    class StatusEventArgs : EventArgs
    {
        public StatusEventArgs(string fullTitle)
        {
            FullTitle = fullTitle;
        }

        public string FullTitle { get; }
    }
}