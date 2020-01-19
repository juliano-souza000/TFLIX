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
    class Progress
    {
        public static event EventHandler<ProgressEventArgs> Updated;
        public static event EventHandler<ProgressEventArgs> Completed;
        public static event EventHandler<ProgressEventArgs> Paused;

        protected static void OnProgressUpdated(object sender, ProgressEventArgs e)
        {
            Updated?.Invoke(sender, e);
        }

        public static void OnProgressUpdated(object sender, int showSeason, int ep, int showID)
        {
            OnProgressUpdated(sender, new ProgressEventArgs(showSeason, ep, showID));
        }

        protected static void OnProgressCompleted(object sender, ProgressEventArgs e)
        {
            Completed?.Invoke(sender, e);
        }

        public static void OnProgressCompleted(object sender, int showSeason, int ep, int showID)
        {
            OnProgressCompleted(sender, new ProgressEventArgs(showSeason, ep, showID));
        }

        protected static void OnProgressPaused(object sender, ProgressEventArgs e)
        {
            Paused?.Invoke(sender, e);
        }

        public static void OnProgressPaused(object sender, int showSeason, int ep, int showID)
        {
            OnProgressPaused(sender, new ProgressEventArgs(showSeason, ep, showID));
        }

    }

    class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(int showSeason, int ep, int showID)
        {
            ShowSeason = showSeason;
            EP = ep;
            ShowID = showID;
        }

        public int ShowSeason { get; }
        public int EP { get; }
        public int ShowID { get; }
    }
}