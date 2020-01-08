using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Util;
using Android.Widget;
using ByteSizeLib;
using SeuSeriado.Network;
using System;
using System.IO;
using System.Net;

namespace SeuSeriado.Services
{
    [Service(Exported = true, Name = "com.toddy.tflix.DownloadService")]
    [IntentFilter(new string[] { "com.toddy.tflix.DownloadService" })]
    public class DownloadFilesService : Service
    {
        static readonly string TAG = "DownloadFilesService";
        public IBinder Binder { get; private set; }

        private const string CHANNEL_ID = "250801";
        private int NOTIFICATION_ID = Utils.NotificationID.ID();

        private string _Path;
        private string FullTitle;
        private string Thumb;
        private string Show;
        private string ShowThumb;
        private int ShowSeason;
        private int Ep;
        private long Duration;
        private bool IsSubtitled;

        private int PreviousPercentage = 0;

        private long bytes_total = 1;
        private long startRange;

        private NotificationCompat.Builder builder;
        private NotificationManager notificationManager;
        private RemoteViews notificationLayout;
        private RemoteViews smallNotificationLayout;


        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            return StartCommandResult.NotSticky;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            Log.Debug(TAG, "OnCreate");
        }

        public override IBinder OnBind(Intent intent)
        {
            int Pos;
            string temp;
            Log.Debug(TAG, "Onbind");

            Pos = intent.Extras.GetInt("DownloadSHOWID");
            Duration = Convert.ToInt64(intent.Extras.GetInt("DownloadEPDuration"));

            if (!intent.GetBooleanExtra("IsFromSearch", false))
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
            }

            Ep = int.Parse(temp);


            if (FullTitle.ToLower().Contains("legendado"))
                IsSubtitled = true;
            else
                IsSubtitled = false;

            DownloadFile(intent.Extras.GetString("DownloadURL"));

            Binder = new DownloadFilesBinder(this);
            return Binder;
        }

        public override void OnDestroy()
        {
            Binder = null;
            builder.SetOngoing(false);
            notificationManager.Notify(NOTIFICATION_ID, builder.Build());
            base.OnDestroy();
        }

        private void CreateNotification()
        {

            Intent broadcastPauseResumeIntent = new Intent(this, typeof(DownloadFileBroadcastListener));
            Intent broadcastCancelIntent = new Intent(this, typeof(DownloadFileBroadcastListener));

            broadcastPauseResumeIntent.PutExtra("BTN", 0);
            broadcastCancelIntent.PutExtra("BTN", 1);

            PendingIntent pauseresumeDownloadPI = PendingIntent.GetBroadcast(this, NOTIFICATION_ID, broadcastPauseResumeIntent, PendingIntentFlags.UpdateCurrent);
            PendingIntent cancelDownloadPI = PendingIntent.GetBroadcast(this, NOTIFICATION_ID, broadcastCancelIntent, PendingIntentFlags.UpdateCurrent);
            NotificationCompat.Action pauseresumeAction = new NotificationCompat.Action(0, "Pausar", pauseresumeDownloadPI);
            NotificationCompat.Action cancelAction = new NotificationCompat.Action(0, "Cancelar", cancelDownloadPI);

            notificationLayout = new RemoteViews(PackageName, Resource.Layout.download_notification);
            smallNotificationLayout = new RemoteViews(PackageName, Resource.Layout.download_notification_smallview);
            notificationManager = (NotificationManager)GetSystemService(NotificationService);

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


            builder = new NotificationCompat.Builder(this, CHANNEL_ID)
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
            
            notificationManager.Notify(NOTIFICATION_ID, builder.Build());
        }


        private void DownloadFile(string URL)
        {

            if (!Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series"))
                Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series");

            _Path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series", FullTitle);

            startRange = Utils.Database.GetDownloadedBytes(IsSubtitled, Show, Ep, ShowSeason);

            WebClient header = new WebClient();
            HttpWebRequest download = (HttpWebRequest)WebRequest.Create(URL);

            download.Method = "GET";
            download.Timeout = 200000;

            try
            {
                header.OpenRead(URL);
                bytes_total = long.Parse(header.ResponseHeaders["Content-Length"]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                this.StopSelf();
            }

            download.AddRange(startRange);

            if (!Utils.Database.IsSeasonOnDB(ShowSeason, Show, IsSubtitled))
                Utils.Database.InsertData("", Show, "", ShowSeason, 0, 0, 0, IsSubtitled, "", 0);
            Utils.Database.InsertData(Thumb, Show, ShowThumb, ShowSeason, Ep, 0, bytes_total, IsSubtitled, System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series", FullTitle), Duration);

            CreateNotification();

            download.BeginGetResponse(new AsyncCallback(PlayResponseAsync), download);
           
        }

        private void DownloadFileCompleted()
        {
            Utils.Database.UpdateProgress(Show, ShowSeason, Ep, 100, bytes_total, IsSubtitled);

            builder = new NotificationCompat.Builder(this, CHANNEL_ID)
               .SetSmallIcon(Android.Resource.Drawable.StatSysDownloadDone)
               .SetContentText("Download Completo!")
               .SetStyle(new NotificationCompat.DecoratedCustomViewStyle())
               .SetColor(Android.Graphics.Color.ParseColor("#FFD80C0C"))
               .SetPriority((int)NotificationPriority.Low)
               .SetOngoing(false)
               .SetOnlyAlertOnce(true);

            notificationManager.Notify(NOTIFICATION_ID, builder.Build());
            StopSelf();
        }

        private void DownloadProgressChanged(long received)
        {
            builder.SetOngoing(true);
            int percentage = (int)(received / (bytes_total / 100));

            if (PreviousPercentage != percentage && received != bytes_total)
            {
                try
                {
                    Utils.Database.UpdateProgress(Show, ShowSeason, Ep, (int)(received / (bytes_total / 100)), received, IsSubtitled);
                }
                catch { }

                smallNotificationLayout.SetTextViewText(Resource.Id.downloadn_percs, string.Format("{0}%", (int)(received / (bytes_total / 100)) ));
                smallNotificationLayout.SetProgressBar(Resource.Id.downloadn_progresss, 100, (int)(received / (bytes_total / 100)), false);

                notificationLayout.SetTextViewText(Resource.Id.downloadn_perc, string.Format("{0}% ({1}/{2})", (int)(received / (bytes_total / 100)),Utils.Utils.Size(received), Utils.Utils.Size(bytes_total)));
                notificationLayout.SetProgressBar(Resource.Id.downloadn_progress, 100, (int)(received / (bytes_total / 100)), false);

                notificationManager.Notify(NOTIFICATION_ID, builder.Build());
            }
            PreviousPercentage = (int)(received / (bytes_total / 100));
        }

        private void PlayResponseAsync(IAsyncResult asyncResult)
        {
            long received = startRange;

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
                            fileStream.Write(buffer, 0, size);
                            received += size;
                            DownloadProgressChanged(received);

                            size = input.Read(buffer, 0, buffer.Length);
                        }
                    }

                    fileStream.Flush();
                    fileStream.Close();
                    webResponse.Close();
                    DownloadFileCompleted();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: "+e.StackTrace);

                notificationManager.CancelAll();

                builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                   .SetSmallIcon(Android.Resource.Drawable.StatSysDownloadDone)
                   .SetContentText("Falha no download.")
                   .SetStyle(new NotificationCompat.DecoratedCustomViewStyle())
                   .SetColor(Android.Graphics.Color.ParseColor("#FFD80C0C"))
                   .SetPriority((int)NotificationPriority.Low)
                   .SetOngoing(false)
                   .SetOnlyAlertOnce(true);

                notificationManager.Notify(NOTIFICATION_ID, builder.Build());

                StopSelf();
            }
        }

    }

    [BroadcastReceiver]
    public class DownloadFileBroadcastListener : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Console.WriteLine("Received click at: " + intent.Extras.GetInt("BTN"));
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