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

namespace SeuSeriado.Event
{
    class ShowDownload
    {
        public static event EventHandler<StatusEventArgs> ChangedStatus;

        protected static void OnChangedStatus(object sender, StatusEventArgs e)
        {
            ChangedStatus?.Invoke(sender, e);
        }

        public static void OnChangedStatus(object sender, string fullTitle, bool isFromsearch)
        {
            OnChangedStatus(sender, new StatusEventArgs(fullTitle, isFromsearch));
        }
    }

    class StatusEventArgs : EventArgs
    {
        public StatusEventArgs(string fullTitle, bool isFromsearch)
        {
            FullTitle = fullTitle;
            IsFromsearch = isFromsearch;
        }

        public string FullTitle { get; }
        public bool IsFromsearch { get; }
    }
}