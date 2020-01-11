using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Util;
using Android.Widget;
using ByteSizeLib;
using SeuSeriado.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Stream = System.IO.Stream;

namespace SeuSeriado.Services
{
    [Service(Exported = false, Name = "com.toddy.tflix.DownloadService")]
    [IntentFilter(new string[] { "com.toddy.tflix.DownloadService" })]
    public class DownloadFilesService : Service
    {
        static readonly string TAG = "DownloadFilesService";
        public IBinder Binder { get; private set; }

        private const string CHANNEL_ID = "250801";
        private int DownloadPos = 0;

        private List<bool> Stop = new List<bool>();
        private List<bool> Pause = new List<bool>();
        private List<NotificationCompat.Builder> Builders = new List<NotificationCompat.Builder>();
        private List<RemoteViews> BigRemoteView = new List<RemoteViews>();
        private List<RemoteViews> SmallRemoteView = new List<RemoteViews>();

        private NotificationManager notificationManager;

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            bool IsRequestingStop;
            bool IsRequestingPauseResume;

            try
            {
                IsRequestingStop = intent.Extras.GetBoolean("IsRequestingStop");
            }
            catch
            {
                IsRequestingStop = false;
            }
            
            try
            {
                IsRequestingPauseResume = intent.Extras.GetBoolean("IsRequestingPauseResume");
            }
            catch
            {
                IsRequestingPauseResume = false;
            }

            if (!IsRequestingStop)
            {
                if (!IsRequestingPauseResume)
                {
                    int Pos;
                    string temp;
                    string URL = intent.Extras.GetString("DownloadURL");
                    bool IsFromSearch = intent.Extras.GetBoolean("IsFromSearch", false);

                    string FullTitle;
                    string Thumb;
                    string Show;
                    string ShowThumb;
                    int ShowSeason;
                    int Ep;
                    bool IsSubtitled;

                    int downloadPos = DownloadPos;
                    DownloadPos++;

                    Log.Debug(TAG, "OnStartCommand");

                    var NOTIFICATION_ID = Utils.RequestCode.ID();

                    Pos = intent.Extras.GetInt("DownloadSHOWID");

                    if (!IsFromSearch)
                    {
                        var thumbpath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/ShowThumbnail", List.GetMainPageSeries.Series[Pos].Title);
                        System.IO.File.WriteAllBytes(thumbpath, Base64.Decode(List.GetMainPageSeries.Series[Pos].IMG64, Base64Flags.UrlSafe));

                        Show = List.GetMainPageSeries.Series[Pos].Title.Substring(0, List.GetMainPageSeries.Series[Pos].Title.IndexOf('ª'));
                        ShowSeason = int.Parse(Show.Substring(Show.LastIndexOf(' ')));
                        Show = Show.Substring(0, Show.LastIndexOf(' '));
                        temp = List.GetMainPageSeries.Series[Pos].Title.Substring(List.GetMainPageSeries.Series[Pos].Title.LastIndexOf("Episódio"));
                        temp = temp.Substring(0, temp.LastIndexOf("Online")).Replace("Episódio", "");
                        FullTitle = List.GetMainPageSeries.Series[Pos].Title;
                        ShowThumb = thumbpath;
                        Thumb = List.GetMainPageSeries.Series[Pos].EPThumb;

                        List.GetMainPageSeries.Series[Pos].Downloading = true;
                    }
                    else
                    {
                        if (!Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/ShowThumbnail"))
                            Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/ShowThumbnail");
                        var thumbpath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/ShowThumbnail", List.GetSearch.Search[Pos].Title.Replace("Online,", "Online "));
                        System.IO.File.WriteAllBytes(thumbpath, Base64.Decode(List.GetSearch.Search[Pos].IMG64, Base64Flags.UrlSafe));

                        Show = List.GetSearch.Search[Pos].Title.Substring(0, List.GetSearch.Search[Pos].Title.IndexOf('ª'));
                        ShowSeason = int.Parse(Show.Substring(Show.LastIndexOf(' ')));
                        Show = Show.Substring(0, Show.LastIndexOf(' '));
                        temp = List.GetSearch.Search[Pos].Title.Substring(List.GetSearch.Search[Pos].Title.LastIndexOf("Episódio"));
                        temp = temp.Substring(0, temp.LastIndexOf("Online")).Replace("Episódio", "");
                        FullTitle = List.GetSearch.Search[Pos].Title.Replace("Online,", "Online ");
                        ShowThumb = thumbpath;
                        Thumb = List.GetSearch.Search[Pos].EPThumb;

                        List.GetSearch.Search[Pos].Downloading = true;
                    }

                    Ep = int.Parse(temp);

                    if (FullTitle.ToLower().Contains("legendado"))
                        IsSubtitled = true;
                    else
                        IsSubtitled = false;

                    Stop.Insert(downloadPos, false);
                    Pause.Insert(downloadPos, false);

                    DownloadFile(URL, NOTIFICATION_ID, FullTitle, Show, Ep, ShowSeason, IsSubtitled, Thumb, ShowThumb, downloadPos, IsFromSearch, Pos);
                }
                else
                {
                    bool pause = intent.Extras.GetBoolean("PauseResume");
                    int notificationID = intent.Extras.GetInt("StartID");
                    int downloadPos = intent.Extras.GetInt("DownloadPos");
                    int listPos = intent.Extras.GetInt("ListPos");
                    bool IsFromSearch = intent.Extras.GetBoolean("IsFromSearch", false);
                    var builder = Builders[downloadPos];

                    string URL = intent.Extras.GetString("URL");
                    string FullTitle = intent.Extras.GetString("FullTitle");
                    string Show = intent.Extras.GetString("Show");
                    int Ep = intent.Extras.GetInt("Ep");
                    int ShowSeason = intent.Extras.GetInt("ShowSeason");
                    bool IsSubtitled = intent.Extras.GetBoolean("IsSubtitled");
                    string Thumb = intent.Extras.GetString("Thumb");
                    string ShowThumb = intent.Extras.GetString("ShowThumb");

                    NotificationCompat.Action pauseresumeAction;
                    NotificationCompat.Action cancelAction;
                    Intent broadcastPauseResumeIntent = new Intent(this, typeof(DownloadFileBroadcastListener));
                    Intent broadcastCancelIntent = new Intent(this, typeof(DownloadFileBroadcastListener));

                    builder.MActions.Clear();

                    broadcastPauseResumeIntent.SetAction("com.toddy.tflix.PAUSERESUMEDOWNLOAD");
                    broadcastPauseResumeIntent.PutExtra("NotificationID", notificationID);
                    broadcastPauseResumeIntent.PutExtra("PauseResume", !pause);
                    broadcastPauseResumeIntent.PutExtra("DownloadPos", downloadPos);
                    broadcastPauseResumeIntent.PutExtra("ListPos", listPos);
                    broadcastPauseResumeIntent.PutExtra("IsFromSearch", IsFromSearch);

                    broadcastPauseResumeIntent.PutExtra("URL", URL);
                    broadcastPauseResumeIntent.PutExtra("FullTitle", FullTitle);
                    broadcastPauseResumeIntent.PutExtra("Show", Show);
                    broadcastPauseResumeIntent.PutExtra("Ep", Ep);
                    broadcastPauseResumeIntent.PutExtra("ShowSeason", ShowSeason);
                    broadcastPauseResumeIntent.PutExtra("IsSubtitled", IsSubtitled);
                    broadcastPauseResumeIntent.PutExtra("Thumb", Thumb);
                    broadcastPauseResumeIntent.PutExtra("ShowThumb", ShowThumb);

                    broadcastCancelIntent.SetAction("com.toddy.tflix.CANCELDOWNLOAD");
                    broadcastCancelIntent.PutExtra("NotificationID", notificationID);
                    broadcastCancelIntent.PutExtra("DownloadPos", downloadPos);
                    broadcastCancelIntent.PutExtra("ListPos", listPos);
                    broadcastCancelIntent.PutExtra("IsFromSearch", IsFromSearch);

                    PendingIntent pauseresumeDownloadPI = PendingIntent.GetBroadcast(this, Utils.RequestCode.ID(), broadcastPauseResumeIntent, PendingIntentFlags.CancelCurrent);
                    PendingIntent cancelDownloadPI = PendingIntent.GetBroadcast(this, Utils.RequestCode.ID(), broadcastCancelIntent, PendingIntentFlags.CancelCurrent);

                    Pause[downloadPos] = pause;

                    if (pause)
                    {
                        pauseresumeAction = new NotificationCompat.Action(0, "Continuar", pauseresumeDownloadPI);
                        builder.SetSmallIcon(Android.Resource.Drawable.StatSysDownloadDone);
                    }
                    else
                    {
                        pauseresumeAction = new NotificationCompat.Action(0, "Pausar", pauseresumeDownloadPI);
                        builder.SetSmallIcon(Android.Resource.Drawable.StatSysDownload);
                        //DownloadFile(URL, notificationID, FullTitle, Show, Ep, ShowSeason, IsSubtitled, Thumb, ShowThumb, downloadPos);
                    }

                    cancelAction = new NotificationCompat.Action(0, "Cancelar", cancelDownloadPI);

                    builder.AddAction(pauseresumeAction)
                                .AddAction(cancelAction);
                    notificationManager.Notify(notificationID, builder.Build());

                    Builders[downloadPos] = builder;

                    if(!pause)
                        DownloadFile(URL, notificationID, FullTitle, Show, Ep, ShowSeason, IsSubtitled, Thumb, ShowThumb, downloadPos, IsFromSearch, listPos);
                }
            }
            else
            {
                Stop[intent.Extras.GetInt("DownloadPos")] = true;
            }

            return StartCommandResult.NotSticky;
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


        private void CreateNotification(int NOTIFICATION_ID, string Show, int ShowSeason, int Ep, long bytes_total, int downloadPos, string URL, string FullTitle, bool IsSubtitled, string Thumb, string ShowThumb, bool IsFromSearch, int listPos)
        {
            Log.Info(TAG, "Notification Created With ID {0} AND Show {1}", NOTIFICATION_ID, Show);

            RemoteViews notificationLayout;
            RemoteViews smallNotificationLayout;

            Intent broadcastPauseResumeIntent = new Intent(this, typeof(DownloadFileBroadcastListener));
            Intent broadcastCancelIntent = new Intent(this, typeof(DownloadFileBroadcastListener));

            broadcastPauseResumeIntent.SetAction("com.toddy.tflix.PAUSERESUMEDOWNLOAD");
            broadcastPauseResumeIntent.PutExtra("NotificationID", NOTIFICATION_ID);
            broadcastPauseResumeIntent.PutExtra("PauseResume", true);
            broadcastPauseResumeIntent.PutExtra("DownloadPos", downloadPos);
            broadcastPauseResumeIntent.PutExtra("ListPos", listPos);
            broadcastPauseResumeIntent.PutExtra("IsFromSearch", IsFromSearch);

            broadcastPauseResumeIntent.PutExtra("URL", URL);
            broadcastPauseResumeIntent.PutExtra("FullTitle", FullTitle);
            broadcastPauseResumeIntent.PutExtra("Show", Show);
            broadcastPauseResumeIntent.PutExtra("Ep", Ep);
            broadcastPauseResumeIntent.PutExtra("ShowSeason", ShowSeason);
            broadcastPauseResumeIntent.PutExtra("IsSubtitled", IsSubtitled);
            broadcastPauseResumeIntent.PutExtra("Thumb", Thumb);
            broadcastPauseResumeIntent.PutExtra("ShowThumb", ShowThumb);

            broadcastCancelIntent.SetAction("com.toddy.tflix.CANCELDOWNLOAD");
            broadcastCancelIntent.PutExtra("NotificationID", NOTIFICATION_ID);
            broadcastCancelIntent.PutExtra("DownloadPos", downloadPos);
            broadcastCancelIntent.PutExtra("ListPos", listPos);
            broadcastCancelIntent.PutExtra("IsFromSearch", IsFromSearch);

            PendingIntent pauseresumeDownloadPI = PendingIntent.GetBroadcast(this, Utils.RequestCode.ID(), broadcastPauseResumeIntent, PendingIntentFlags.CancelCurrent);
            PendingIntent cancelDownloadPI = PendingIntent.GetBroadcast(this, Utils.RequestCode.ID(), broadcastCancelIntent, PendingIntentFlags.CancelCurrent);

            NotificationCompat.Action pauseresumeAction = new NotificationCompat.Action(0, "Pausar", pauseresumeDownloadPI);
            NotificationCompat.Action cancelAction = new NotificationCompat.Action(0, "Cancelar", cancelDownloadPI);

            notificationLayout = new RemoteViews(PackageName, Resource.Layout.download_notification);
            smallNotificationLayout = new RemoteViews(PackageName, Resource.Layout.download_notification_smallview);

            notificationLayout.SetTextViewText(Resource.Id.downloadn_title, Show);
            notificationLayout.SetTextViewText(Resource.Id.downloadn_se, string.Format("T{0}:E{1}", ShowSeason, Ep));
            notificationLayout.SetTextViewText(Resource.Id.downloadn_perc, string.Format("{0}% ({1} MB/{2})", 0, 0, Utils.Utils.Size(bytes_total)));
            notificationLayout.SetProgressBar(Resource.Id.downloadn_progress, 100, 0, false);

            smallNotificationLayout.SetTextViewText(Resource.Id.downloadn_titles, Show);
            smallNotificationLayout.SetTextViewText(Resource.Id.downloadn_percs, string.Format("{0}%", 0));
            smallNotificationLayout.SetProgressBar(Resource.Id.downloadn_progresss, 100, 0, false);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannel chan = new NotificationChannel(CHANNEL_ID, "DownloadContent", NotificationImportance.Low);
                chan.SetSound(null, null);
                chan.SetShowBadge(false);
                chan.EnableLights(false);
                chan.EnableVibration(false);
                notificationManager.CreateNotificationChannel(chan);
            }

            var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
               .SetSmallIcon(Android.Resource.Drawable.StatSysDownload)
               .SetCustomBigContentView(notificationLayout)
               .SetCustomContentView(smallNotificationLayout)
               .AddAction(pauseresumeAction)
               .AddAction(cancelAction)
               .SetStyle(new NotificationCompat.DecoratedCustomViewStyle())
               .SetColor(Android.Graphics.Color.ParseColor("#FFD80C0C"))
               .SetPriority((int)NotificationPriority.Low)
               .SetOngoing(true)
               .SetOnlyAlertOnce(true);

            Builders.Insert(downloadPos, builder);
            SmallRemoteView.Insert(downloadPos, smallNotificationLayout);
            BigRemoteView.Insert(downloadPos, notificationLayout);
            notificationManager.Notify(NOTIFICATION_ID, builder.Build());
            
        }


        private void DownloadFile(string URL, int NOTIFICATION_ID, string FullTitle, string Show, int Ep, int ShowSeason, bool IsSubtitled, string Thumb, string ShowThumb, int downloadPos, bool IsFromSearch, int listPos)
        {
            long Duration = 0;
            long prevTotalBytes = 0;
            long bytes_total = 1;

            if (!Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series"))
                Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series");

            var _Path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series", FullTitle);

            var startRange = Utils.Database.GetDownloadedBytes(IsSubtitled, Show, Ep, ShowSeason);

            WebClient header = new WebClient();
            HttpWebRequest download = (HttpWebRequest)WebRequest.Create(URL);

            download.Method = "GET";
            download.Timeout = 200000;

            try
            {
                prevTotalBytes = Utils.Database.GetTotalBytes(IsSubtitled, Show, Ep, ShowSeason);
            }
            catch { }

            try
            {
                MediaMetadataRetriever reader = new MediaMetadataRetriever();
                header.OpenRead(URL);
                bytes_total = long.Parse(header.ResponseHeaders["Content-Length"]);

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
                File.Delete(_Path);
                startRange = 0;
            }

            download.AddRange(startRange);

            if (!Utils.Database.IsSeasonOnDB(ShowSeason, Show, IsSubtitled))
                Utils.Database.InsertData("", Show, "", ShowSeason, 0, 0, 0, IsSubtitled, "", 0);
            Utils.Database.InsertData(Thumb, Show, ShowThumb, ShowSeason, Ep, 0, bytes_total, IsSubtitled, System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series", FullTitle), Duration);


            if (downloadPos >= Builders.Count)
                CreateNotification(NOTIFICATION_ID, Show, ShowSeason, Ep, bytes_total, downloadPos, URL, FullTitle, IsSubtitled, Thumb, ShowThumb, IsFromSearch, listPos);


            download.BeginGetResponse(new AsyncCallback(result => PlayResponseAsync(result, NOTIFICATION_ID, startRange, _Path, bytes_total, Show, ShowSeason, Ep, IsSubtitled, downloadPos, IsFromSearch, listPos)), download);

        }

        private void DownloadFileCompleted(int NOTIFICATION_ID, string Show, int ShowSeason, int Ep, long bytes_total, bool IsSubtitled)
        {
            Utils.Database.UpdateProgress(Show, ShowSeason, Ep, 100, bytes_total, IsSubtitled);

            var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
               .SetSmallIcon(Android.Resource.Drawable.StatSysDownloadDone)
               .SetContentText("Download Completo!")
               .SetStyle(new NotificationCompat.DecoratedCustomViewStyle())
               .SetColor(Android.Graphics.Color.ParseColor("#FFD80C0C"))
               .SetPriority((int)NotificationPriority.Low)
               .SetOngoing(false)
               .SetOnlyAlertOnce(true);

            notificationManager.Notify(NOTIFICATION_ID, builder.Build());
        }

        private int DownloadProgressChanged(long received, int NOTIFICATION_ID, int PreviousPercentage, long bytes_total, string Show, int ShowSeason, int Ep, bool IsSubtitled, int downloadPos)
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
                    SmallRemoteView[downloadPos].SetTextViewText(Resource.Id.downloadn_percs, string.Format("{0}%", (int)(received / (bytes_total / 100))));
                    SmallRemoteView[downloadPos].SetProgressBar(Resource.Id.downloadn_progresss, 100, (int)(received / (bytes_total / 100)), false);

                    BigRemoteView[downloadPos].SetTextViewText(Resource.Id.downloadn_perc, string.Format("{0}% ({1}/{2})", (int)(received / (bytes_total / 100)), Utils.Utils.Size(received), Utils.Utils.Size(bytes_total)));
                    BigRemoteView[downloadPos].SetProgressBar(Resource.Id.downloadn_progress, 100, (int)(received / (bytes_total / 100)), false);

                    notificationManager.Notify(NOTIFICATION_ID, Builders[downloadPos].Build());
                }
                catch { }
            }

            return (int)(received / (bytes_total / 100));
        }

        private async void PlayResponseAsync(IAsyncResult asyncResult, int NOTIFICATION_ID, long startRange, string _Path, long bytes_total, string Show, int ShowSeason, int Ep, bool IsSubtitled, int downloadPos, bool IsFromSearch, int listPos)
        {
            long received = startRange;
            int PreviousPercentage = 0;

            HttpWebRequest webRequest = (HttpWebRequest)asyncResult.AsyncState;

            try
            {
                
                using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.EndGetResponse(asyncResult))
                {
                    byte[] buffer = new byte[1024];

                    FileStream fileStream = new FileStream(_Path, FileMode.Append);

                    using (Stream input = webResponse.GetResponseStream())
                    {
                        int size = input.Read(buffer, 0, buffer.Length);
                        while (size > 0)
                        {
                            if (Stop[downloadPos] || Pause[downloadPos])
                                break;
                            fileStream.Write(buffer, 0, size);
                            received += size;
                            
                            await Task.Run(() =>
                            { 
                                PreviousPercentage = DownloadProgressChanged(received, NOTIFICATION_ID, PreviousPercentage, bytes_total, Show, ShowSeason, Ep, IsSubtitled, downloadPos);
                            });

                            size = input.Read(buffer, 0, buffer.Length);
                        }
                        input.Close();
                    }

                    fileStream.Flush();
                    fileStream.Close();
                    webResponse.Close();

                    if (!Stop[downloadPos])
                    {
                        if (!Pause[downloadPos])
                        {
                            DownloadFileCompleted(NOTIFICATION_ID, Show, ShowSeason, Ep, bytes_total, IsSubtitled);
                            if (IsFromSearch)
                                List.GetSearch.Search[listPos].Downloading = false;
                            else
                                List.GetMainPageSeries.Series[listPos].Downloading = false;
                        }
                    }
                    else
                    {
                        notificationManager.Cancel(NOTIFICATION_ID);
                        Utils.Database.DeleteItem(IsSubtitled, Show, Ep, ShowSeason);
                        File.Delete(_Path);

                        if (IsFromSearch)
                            List.GetSearch.Search[listPos].Downloading = false;
                        else
                            List.GetMainPageSeries.Series[listPos].Downloading = false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: "+e.StackTrace);

                notificationManager.Cancel(NOTIFICATION_ID);

                Builders[downloadPos] = new NotificationCompat.Builder(this, CHANNEL_ID)
                   .SetSmallIcon(Android.Resource.Drawable.StatSysDownloadDone)
                   .SetContentText("Falha no download.")
                   .SetStyle(new NotificationCompat.DecoratedCustomViewStyle())
                   .SetColor(Android.Graphics.Color.ParseColor("#FFD80C0C"))
                   .SetPriority((int)NotificationPriority.Low)
                   .SetOngoing(false)
                   .SetOnlyAlertOnce(true);

                notificationManager.Notify(NOTIFICATION_ID, Builders[downloadPos].Build());
                if (IsFromSearch)
                    List.GetSearch.Search[listPos].Downloading = false;
                else
                    List.GetMainPageSeries.Series[listPos].Downloading = false;
            }
        }

    }


    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new string[] { "com.toddy.tflix.PAUSERESUMEDOWNLOAD", "com.toddy.tflix.CANCELDOWNLOAD" })]
    public class DownloadFileBroadcastListener : BroadcastReceiver
    {

        NotificationManager notificationManager;

        public override void OnReceive(Context context, Intent intent)
        {
            int ServiceNotificationID = 0;
            int downloadPos = 0;
            int listPos = 0;
            bool IsFromSearch = false;

            if (notificationManager == null)
                notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);

            Console.WriteLine("Received click at: " + intent.Action);

            try
            {
                ServiceNotificationID = intent.Extras.GetInt("NotificationID");
                downloadPos = intent.Extras.GetInt("DownloadPos");
                listPos = intent.Extras.GetInt("ListPos");
                IsFromSearch = intent.Extras.GetBoolean("IsFromSearch", false);
            }
            catch { }

            switch (intent.Action)
            {
                case "com.toddy.tflix.PAUSERESUMEDOWNLOAD":
                    bool pause = intent.Extras.GetBoolean("PauseResume");

                    string URL = intent.Extras.GetString("URL");
                    string FullTitle = intent.Extras.GetString("FullTitle");
                    string Show = intent.Extras.GetString("Show");
                    int Ep = intent.Extras.GetInt("Ep");
                    int ShowSeason = intent.Extras.GetInt("ShowSeason");
                    bool IsSubtitled = intent.Extras.GetBoolean("IsSubtitled");
                    string Thumb = intent.Extras.GetString("Thumb");
                    string ShowThumb = intent.Extras.GetString("ShowThumb");

                    Intent intentPauseResume = new Intent(context, typeof(DownloadFilesService));
                    intentPauseResume.PutExtra("IsRequestingPauseResume", true);
                    intentPauseResume.PutExtra("StartID", ServiceNotificationID);
                    intentPauseResume.PutExtra("PauseResume", pause);
                    intentPauseResume.PutExtra("DownloadPos", downloadPos);
                    intentPauseResume.PutExtra("ListPos", listPos);
                    intentPauseResume.PutExtra("IsFromSearch", IsFromSearch);

                    intentPauseResume.PutExtra("URL", URL);
                    intentPauseResume.PutExtra("FullTitle", FullTitle);
                    intentPauseResume.PutExtra("Show", Show);
                    intentPauseResume.PutExtra("Ep", Ep);
                    intentPauseResume.PutExtra("ShowSeason", ShowSeason);
                    intentPauseResume.PutExtra("IsSubtitled", IsSubtitled);
                    intentPauseResume.PutExtra("Thumb", Thumb);
                    intentPauseResume.PutExtra("ShowThumb", ShowThumb);

                    context.StartService(intentPauseResume);
                    break;
                case "com.toddy.tflix.CANCELDOWNLOAD":

                    Intent intentCancel = new Intent(context, typeof(DownloadFilesService));
                    intentCancel.PutExtra("IsRequestingStop", true);
                    intentCancel.PutExtra("StartID", ServiceNotificationID);
                    intentCancel.PutExtra("DownloadPos", downloadPos);
                    intentCancel.PutExtra("ListPos", listPos);
                    intentCancel.PutExtra("IsFromSearch", IsFromSearch);

                    context.StartService(intentCancel);
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