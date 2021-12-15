using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Support.V4.App;
using Android.Util;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MoreLinq;
using Stream = System.IO.Stream;
using System.Linq;
using TFlix.Interface;

namespace TFlix.Services
{
    [Service(Exported = false, Name = "com.toddy.tflix.DownloadService")]
    [IntentFilter(new string[] { "com.toddy.tflix.DownloadService" })]
    public class DownloadFilesService : IntentService, IOnPauseDownload, IOnCancelDownload
    {
        static readonly string TAG = "DownloadFilesService";
        public IBinder Binder { get; private set; }
        public static IOnPauseDownload OnPauseDownload { get; private set; }
        public static IOnCancelDownload OnCancelDownload { get; private set; }

        private const string CHANNEL_ID = "250801";
        private const int NOTIFICATION_ID = 70718;

        private static NotificationCompat.Builder Builder;
        private static RemoteViews BigRemoteView;
        private static RemoteViews SmallRemoteView;
        private static Intent BroadcastPauseResumeIntent;
        private static Intent BroadcastCancelIntent;
        private static PendingIntent PauseresumeDownloadPI;
        private static PendingIntent CancelDownloadPI;
        private static NotificationCompat.Action PauseresumeAction;
        private static NotificationCompat.Action CancelAction;
        private static HttpWebRequest download;

        private static NotificationManager notificationManager;

        private static bool ReCreateNotification;

        private static bool Stop;
        private static bool Pause;

        private static string _Path;
        private static long bytes_total = 1;
        private static long startRange;

        private static string URL;
        private static string FullTitle;
        private static string Thumb;
        private static string Show;
        private static string ShowThumb;
        private static int ShowSeason;
        private static int Ep;
        private static bool IsSubtitled;

        protected override void OnHandleIntent(Intent intent)
        {
            Log.Debug(TAG, "OnHandleIntent");
            if (OnPauseDownload == null)
                OnPauseDownload = this;
            if (OnCancelDownload == null)
                OnCancelDownload = this;

            try
            {
                URL = intent.Extras.GetString("DownloadURL");
                FullTitle = intent.Extras.GetString("FullTitle");
                Thumb = intent.Extras.GetString("Thumb");
                Show = intent.Extras.GetString("Show");
                ShowThumb = intent.Extras.GetString("ShowThumb");
                ShowSeason = intent.Extras.GetInt("ShowSeason");
                Ep = intent.Extras.GetInt("Ep");

                if (FullTitle.ToLower().Contains("legendado"))
                    IsSubtitled = true;
                else
                    IsSubtitled = false;

                Stop = false;
                Pause = false;

                ReCreateNotification = true;
                CreateNotification();
                DownloadFile();
            }
            catch (Exception e)
            {
                Console.WriteLine("Message: " + e.Message + " StackTrace: " + e.StackTrace);

                if (!e.Message.Contains("The request was aborted"))
                {
                    var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                       .SetSmallIcon(Android.Resource.Drawable.StatSysDownloadDone)
                       .SetContentText("Falha no download.")
                       .SetStyle(new NotificationCompat.DecoratedCustomViewStyle())
                       .SetColor(Android.Graphics.Color.ParseColor("#FFD80C0C"))
                       .SetPriority((int)NotificationPriority.Low)
                       .SetOngoing(false)
                       .SetOnlyAlertOnce(true);

                    notificationManager.Notify(NOTIFICATION_ID, builder.Build());
                }
            }
        }

        public override void OnCreate()
        {
            base.OnCreate();
            Log.Debug(TAG, "OnCreate");
        }

        public override IBinder OnBind(Intent intent)
        {
            Log.Debug(TAG, "Onbind");
            notificationManager = (NotificationManager)GetSystemService(NotificationService);
            Binder = new DownloadFilesBinder(this);
            return Binder;
        }

        public override void OnDestroy()
        {
            notificationManager.CancelAll();
            Binder = null;
            base.OnDestroy();
        }

        private void CreateNotification()
        {
            Log.Info(TAG, "Notification Created With ID {0} AND Show {1}", NOTIFICATION_ID, Show);

            BroadcastPauseResumeIntent = new Intent(this, typeof(DownloadFileBroadcastListener));
            BroadcastCancelIntent = new Intent(this, typeof(DownloadFileBroadcastListener));

            BroadcastPauseResumeIntent.SetAction("com.toddy.tflix.PAUSERESUMEDOWNLOAD");
            BroadcastCancelIntent.SetAction("com.toddy.tflix.CANCELDOWNLOAD");

            PauseresumeDownloadPI = PendingIntent.GetBroadcast(this, Utils.RequestCode.ID(), BroadcastPauseResumeIntent, PendingIntentFlags.CancelCurrent);
            CancelDownloadPI = PendingIntent.GetBroadcast(this, Utils.RequestCode.ID(), BroadcastCancelIntent, PendingIntentFlags.CancelCurrent);

            PauseresumeAction = new NotificationCompat.Action(0, "Pausar", PauseresumeDownloadPI);
            CancelAction = new NotificationCompat.Action(0, "Cancelar", CancelDownloadPI);

            BigRemoteView = new RemoteViews(PackageName, Resource.Layout.download_notification);
            SmallRemoteView = new RemoteViews(PackageName, Resource.Layout.download_notification_smallview);

            BigRemoteView.SetTextViewText(Resource.Id.downloadn_title, Show);
            BigRemoteView.SetTextViewText(Resource.Id.downloadn_se, string.Format("T{0}:E{1}", ShowSeason, Ep));
            BigRemoteView.SetTextViewText(Resource.Id.downloadn_perc, string.Format("{0}% ({1} MB/{2})", 0, 0, Utils.Utils.Size(bytes_total)));
            BigRemoteView.SetProgressBar(Resource.Id.downloadn_progress, 100, 0, false);

            SmallRemoteView.SetTextViewText(Resource.Id.downloadn_titles, Show);
            SmallRemoteView.SetTextViewText(Resource.Id.downloadn_percs, string.Format("{0}%", 0));
            SmallRemoteView.SetProgressBar(Resource.Id.downloadn_progresss, 100, 0, false);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannel chan = new NotificationChannel(CHANNEL_ID, "DownloadContent", NotificationImportance.Low);
                chan.SetSound(null, null);
                chan.SetShowBadge(false);
                chan.EnableLights(false);
                chan.EnableVibration(false);
                notificationManager.CreateNotificationChannel(chan);
            }

            try
            {
                Utils.Database.ReadDB();

                var index = List.GetDownloads.Series.FindIndex(x => x.Show == Show && x.IsSubtitled == IsSubtitled);
                var epIndex = List.GetDownloads.Series[index].Episodes.FindIndex(x => x.EP == Ep && x.ShowSeason == ShowSeason);
                List.GetDownloads.Series[index].Episodes[epIndex].IsDownloading = true;
            }
            catch { }

            Builder = new NotificationCompat.Builder(this, CHANNEL_ID)
               .SetSmallIcon(Android.Resource.Drawable.StatSysDownload)
               .SetCustomBigContentView(BigRemoteView)
               .SetCustomContentView(SmallRemoteView)
               .AddAction(PauseresumeAction)
               .AddAction(CancelAction)
               .SetStyle(new NotificationCompat.DecoratedCustomViewStyle())
               .SetColor(Android.Graphics.Color.ParseColor("#FFD80C0C"))
               .SetPriority((int)NotificationPriority.Low)
               .SetOngoing(true)
               .SetOnlyAlertOnce(true);

            notificationManager.Notify(NOTIFICATION_ID, Builder.Build());
        }

        private void DownloadFile()
        {
            long Duration = 0;
            long prevTotalBytes = 0;

            if (!Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series"))
                Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series");

            _Path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series", FullTitle);

            startRange = Utils.Database.GetDownloadedBytes(IsSubtitled, Show, Ep, ShowSeason);

            WebClient header = new WebClient();
            download = (HttpWebRequest)WebRequest.Create(URL);

            download.Method = "GET";
            download.Timeout = 200000;

            try
            {
                prevTotalBytes = Utils.Database.GetTotalDownloadedBytes(IsSubtitled, Show, Ep, ShowSeason);
                header.OpenRead(URL);
                bytes_total = long.Parse(header.ResponseHeaders["Content-Length"]);
            }
            catch { }
            if (ReCreateNotification)
                CreateNotification();

            try
            {
                MediaMetadataRetriever reader = new MediaMetadataRetriever();

                reader.SetDataSource(URL, new Dictionary<string, string>());
                Duration = long.Parse(reader.ExtractMetadata(MetadataKey.Duration));

                reader.Release();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                this.StopSelf();
            }

            if (prevTotalBytes != 0 && prevTotalBytes != bytes_total)
            {
                Utils.Database.DeleteItem(IsSubtitled, Show, Ep, ShowSeason);
                startRange = 0;
            }

            download.AddRange(startRange);

            if (!Utils.Database.IsSeasonOnDB(ShowSeason, Show, IsSubtitled))
                Utils.Database.InsertData("", Show, ShowThumb, ShowSeason, -1, 0, 0, IsSubtitled, "", 0, "");
            Utils.Database.InsertData(Thumb, Show, ShowThumb, ShowSeason, Ep, 0, bytes_total, IsSubtitled, System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series", FullTitle), Duration, FullTitle);

            try
            {
                Utils.Database.ReadDB();

                //var index = List.GetDownloads.Series.FindIndex(x => x.Show == Show && x.IsSubtitled == IsSubtitled);
                //var epIndex = List.GetDownloads.Series[index].Episodes.FindIndex(x => x.EP == Ep && x.ShowSeason == ShowSeason);
                //Activities.DetailedDownloads.ReloadDataset();
            }
            catch { }

            download.BeginGetResponse(new AsyncCallback(result => PlayResponseAsync(result, startRange, _Path, bytes_total)), download);

        }

        private void DownloadFileCompleted()
        {
            Utils.Database.UpdateProgress(Show, ShowSeason, Ep, 100, bytes_total, IsSubtitled);

            try
            {
                var index = List.GetDownloads.Series.FindIndex(x => x.Show == Show && x.IsSubtitled == IsSubtitled);
                var epIndex = List.GetDownloads.Series[index].Episodes.FindIndex(x => x.EP == Ep && x.ShowSeason == ShowSeason);
                List.GetDownloads.Series[index].Episodes[epIndex].Progress = 100;
                List.GetDownloads.Series[index].Episodes[epIndex].IsDownloading = false;
            }
            catch { }

            var bigRemoteView = new RemoteViews(PackageName, Resource.Layout.notification_download_completed_big);

            bigRemoteView.SetTextViewText(Resource.Id.big_content_completed_title, "Download Completo!");
            bigRemoteView.SetTextViewText(Resource.Id.big_content_completed_show, Show);
            bigRemoteView.SetTextViewText(Resource.Id.big_content_completed_se, string.Format("T{0}:E{1}", ShowSeason, Ep));

            Builder = new NotificationCompat.Builder(this, CHANNEL_ID)
               .SetSmallIcon(Resource.Drawable.baseline_mobile_friendly_24)
               .SetContentText("Download Completo!")
               .SetCustomBigContentView(bigRemoteView)
               .SetStyle(new NotificationCompat.DecoratedCustomViewStyle())
               .SetColor(Android.Graphics.Color.ParseColor("#FFD80C0C"))
               .SetPriority((int)NotificationPriority.Low)
               .SetOngoing(false)
               .SetOnlyAlertOnce(true);
            notificationManager.Cancel(NOTIFICATION_ID);
            notificationManager.Notify(Utils.RequestCode.ID(), Builder.Build());
        }

        private int DownloadProgressChanged(long received, int PreviousPercentage)
        {
            int percentage = (int)(received / (bytes_total / 100));

            //builder.SetOngoing(true);

            if (PreviousPercentage != percentage && received != bytes_total)
            {
                try
                {
                    Utils.Database.UpdateProgress(Show, ShowSeason, Ep, (int)(received / (bytes_total / 100)), received, IsSubtitled);
                }
                catch { }

                try
                {
                    var index = List.GetDownloads.Series.FindIndex(x => x.Show == Show && x.IsSubtitled == IsSubtitled);
                    var epIndex = List.GetDownloads.Series[index].Episodes.FindIndex(x => x.EP == Ep && x.ShowSeason == ShowSeason);

                    List.GetDownloads.Series[index].Episodes[epIndex].Progress = percentage;
                    List.GetDownloads.Series[index].Episodes[epIndex].Bytes = received;
                    List.GetDownloads.Series[index].Episodes[epIndex].IsDownloading = true;
                }
                catch { }

                try
                {
                    SmallRemoteView.SetTextViewText(Resource.Id.downloadn_percs, string.Format("{0}%", (int)(received / (bytes_total / 100))));
                    SmallRemoteView.SetProgressBar(Resource.Id.downloadn_progresss, 100, (int)(received / (bytes_total / 100)), false);

                    BigRemoteView.SetTextViewText(Resource.Id.downloadn_perc, string.Format("{0}% ({1}/{2})", (int)(received / (bytes_total / 100)), Utils.Utils.Size(received), Utils.Utils.Size(bytes_total)));
                    BigRemoteView.SetProgressBar(Resource.Id.downloadn_progress, 100, (int)(received / (bytes_total / 100)), false);

                    if (!Pause && !Stop)
                        notificationManager.Notify(NOTIFICATION_ID, Builder.Build());
                }
                catch { }
            }

            return (int)(received / (bytes_total / 100));
        }

        private void CancelDownload()
        {
            notificationManager.Cancel(NOTIFICATION_ID);
            Utils.Database.DeleteItem(IsSubtitled, Show, Ep, ShowSeason);
            Utils.Database.ReadDB();
        }

        private async void PlayResponseAsync(IAsyncResult asyncResult, long startRange, string _Path, long bytes_total)
        {
            long received = startRange;
            int PreviousPercentage = 0;

            HttpWebRequest webRequest = (HttpWebRequest)asyncResult.AsyncState;

            try
            {

                using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.EndGetResponse(asyncResult))
                {
                    byte[] buffer = new byte[1024];

                    using (FileStream fileStream = File.Open(_Path, FileMode.Append))
                    {
                        using (Stream input = webResponse.GetResponseStream())
                        {
                            int size = input.Read(buffer, 0, buffer.Length);
                            while (size > 0)
                            {

                                if (!File.Exists(_Path))
                                {
                                    Stop = true;
                                    break;
                                }

                                fileStream.Write(buffer, 0, size);
                                received += size;

                                try
                                {
                                    await Task.Run(() =>
                                    {
                                        PreviousPercentage = DownloadProgressChanged(received, PreviousPercentage);
                                    });
                                }
                                catch { }
                                size = input.Read(buffer, 0, buffer.Length);
                            }
                            input.Flush();
                            input.Dispose();
                            input.Close();
                        }
                    }
                    webResponse.Close();
                }

                if (!Stop)
                {
                    if (!Pause)
                    {
                        DownloadFileCompleted();
                    }
                }
                else
                {
                    CancelDownload();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Message: " + e.Message + " StackTrace: " + e.StackTrace);
                if (!e.Message.Contains("The request was aborted"))
                {
                    notificationManager.Cancel(NOTIFICATION_ID);

                    var bigRemoteView = new RemoteViews(PackageName, Resource.Layout.notification_download_completed_big);

                    bigRemoteView.SetTextViewText(Resource.Id.big_content_completed_title, "Falha no download.");
                    bigRemoteView.SetTextViewText(Resource.Id.big_content_completed_show, Show);
                    bigRemoteView.SetTextViewText(Resource.Id.big_content_completed_se, string.Format("T{0}:E{1}", ShowSeason, Ep));

                    Builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                       .SetSmallIcon(Resource.Drawable.baseline_error_outline_24)
                       .SetContentText("Falha no download.")
                       .SetCustomBigContentView(bigRemoteView)
                       .SetStyle(new NotificationCompat.DecoratedCustomViewStyle())
                       .SetColor(Android.Graphics.Color.ParseColor("#FFD80C0C"))
                       .SetPriority((int)NotificationPriority.Low)
                       .SetOngoing(false)
                       .SetOnlyAlertOnce(true);

                    notificationManager.Notify(Utils.RequestCode.ID(), Builder.Build());

                    try
                    {
                        var index = List.GetDownloads.Series.FindIndex(x => x.Show == Show && x.IsSubtitled == IsSubtitled);
                        var epIndex = List.GetDownloads.Series[index].Episodes.FindIndex(x => x.EP == Ep && x.ShowSeason == ShowSeason);
                        List.GetDownloads.Series[index].Episodes[epIndex].IsDownloading = false;
                    }
                    catch { }
                }
            }

            if (List.Queue.DownloadQueue.Count > 0 && !Pause)
            {
                await Task.Run(() => Utils.Utils.StartNextOnQueue(ApplicationContext));
            }
        }

        public void IOnPauseDownload(bool isFromNotification)
        {
            Pause = !Pause;
            Builder.MActions.Clear();

            ReCreateNotification = false;

            if (Pause)
            {
                PauseresumeAction = new NotificationCompat.Action(0, "Continuar", PauseresumeDownloadPI);
                Builder.SetSmallIcon(Android.Resource.Drawable.StatSysDownloadDone);
            }
            else
            {
                PauseresumeAction = new NotificationCompat.Action(0, "Pausar", PauseresumeDownloadPI);
                Builder.SetSmallIcon(Android.Resource.Drawable.StatSysDownload);
            }

            if (isFromNotification)
            {
                var index = List.GetDownloads.Series.FindIndex(x => x.Show == Show && x.IsSubtitled == IsSubtitled);
                var epIndex = List.GetDownloads.Series[index].Episodes.FindIndex(x => x.EP == Ep && x.ShowSeason == ShowSeason);

                List.GetDownloads.Series[index].Episodes[epIndex].IsPaused = Pause;
                Event.Progress.OnProgressPaused(this, ShowSeason, Ep, List.GetDownloads.Series[index].Episodes[epIndex].ShowID);
            }

            Builder.AddAction(PauseresumeAction)
                        .AddAction(CancelAction);
            notificationManager.Notify(NOTIFICATION_ID, Builder.Build());

            if (!Pause)
                download.BeginGetResponse(new AsyncCallback(result => PlayResponseAsync(result, startRange, _Path, bytes_total)), download);
            else
                download.Abort();
        }

        public void IOnCancelDownload()
        {
            try
            {
                Stop = true;
                download.Abort();
                CancelDownload();
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} {1}", e.Message, e.StackTrace);
            }
        }
    }

    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new string[] { "com.toddy.tflix.PAUSERESUMEDOWNLOAD", "com.toddy.tflix.CANCELDOWNLOAD" })]
    public class DownloadFileBroadcastListener : BroadcastReceiver
    {

        public override void OnReceive(Context context, Intent intent)
        {
            Console.WriteLine("Received click at: " + intent.Action);

            switch (intent.Action)
            {
                case "com.toddy.tflix.PAUSERESUMEDOWNLOAD":
                    Task.Run(() => DownloadFilesService.OnPauseDownload.IOnPauseDownload(true));
                    break;
                case "com.toddy.tflix.CANCELDOWNLOAD":
                    Task.Run(() => DownloadFilesService.OnCancelDownload.IOnCancelDownload());
                    break;
            }
        }
    }

    public class DownloadFilesBinder : Binder
    {
        public DownloadFilesBinder(DownloadFilesService service)
        {
            this.Service = service;
        }

        public DownloadFilesService Service { get; private set; }

    }
}