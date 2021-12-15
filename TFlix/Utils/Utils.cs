using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using ByteSizeLib;
using DnsClient;
using Newtonsoft.Json;
using TFlix.List;
using TFlix.Network;
using TFlix.Services;
using ToddyUtils;

namespace TFlix.Utils
{
    class Utils
    {
        public static string filePath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "TempSubtitle.srt");
        public static DownloadFileServiceConnection serviceConnection;

        public static float DPToPX(Context context, float px) { return px * context.Resources.DisplayMetrics.Density; }

        public static byte[] GetImageBytes(Drawable d)
        {
            Bitmap bitmap = ((BitmapDrawable)d).Bitmap;
            var ms = new System.IO.MemoryStream();
            bitmap.Compress(Bitmap.CompressFormat.Png, 100, ms);
            return ms.ToArray();
        }

        public static (string, int, int) BreakFullTitleInParts(string fullTitle)
        {
            var Show = fullTitle.Substring(0, fullTitle.IndexOf('ª'));
            var ShowSeason = int.Parse(Show.Substring(Show.LastIndexOf(' ')));
            Show = Show.Substring(0, Show.LastIndexOf(' '));
            var Ep = fullTitle.Substring(fullTitle.LastIndexOf("Episódio"));
            Ep = Ep.Substring(0, Ep.LastIndexOf("Online")).Replace("Episódio", "");
            if (Ep.StartsWith(' '))
                Ep = Ep.Remove(0,1);
            Ep = Ep.Substring(0, Ep.IndexOf(" "));
            return (Show, ShowSeason, int.Parse(Ep));
        }

        public static string Size(long sizeInBytes)
        {
            var fileSize = ByteSize.FromBytes(Convert.ToDouble(sizeInBytes));

            if (fileSize.TeraBytes > 1)
                return fileSize.ToString("TB");
            else if (fileSize.GigaBytes > 1)
                return fileSize.ToString("GB");
            else if (fileSize.MegaBytes > 1)
                return fileSize.ToString("MB");
            else if (fileSize.KiloBytes > 1)
                return fileSize.ToString("KB");
            else
                return fileSize.ToString("B");
        }

        public static async Task<Bitmap> GetImageBitmapFromUrl(string url)
        {
            Bitmap imageBitmap = null;
            try
            {
                using (var webClient = new DnsedWebClient(NameServer.GooglePublicDns, NameServer.GooglePublicDns2))
                {
                    var imageBytes = webClient.DownloadData(url);
                    if (imageBytes != null && imageBytes.Length > 0)
                    {
                        imageBitmap = await DecodeSampledBitmap(imageBytes, 150, 150);
                    }
                }

                return imageBitmap;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<Bitmap> DecodeSampledBitmap(byte[] bmp, int reqWidth, int reqHeight)
        {

            // First decode with inJustDecodeBounds=true to check dimensions
            BitmapFactory.Options options = new BitmapFactory.Options
            {
                InJustDecodeBounds = true,
                InPreferredConfig = Bitmap.Config.Rgb565
            };

            // Calculate inSampleSize
            options.InSampleSize = CalculateInSampleSize(options, reqWidth, reqHeight);

            // Decode bitmap with inSampleSize set
            options.InJustDecodeBounds = false;
            Bitmap scaled = await BitmapFactory.DecodeByteArrayAsync(bmp, 0, bmp.Length, options);
            return scaled;
        }

        private static int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
        {
            // Raw height and width of image
            int height = options.OutHeight;
            int width = options.OutWidth;
            int inSampleSize = 1;

            if (height > reqHeight || width > reqWidth)
            {

                int halfHeight = height / 2;
                int halfWidth = width / 2;

                while ((halfHeight / inSampleSize) > reqHeight
                        && (halfWidth / inSampleSize) > reqWidth)
                {
                    inSampleSize *= 2;
                }
            }

            return inSampleSize;
        }

        public static long AvailableInternalMemorySize()
        {
            Java.IO.File path = Android.OS.Environment.DataDirectory;
            StatFs stat = new StatFs(path.Path);
            long blockSize, availableBlocks;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr2)
            {
                blockSize = stat.BlockSizeLong;
                availableBlocks = stat.AvailableBlocksLong;
            }
            else
            {
#pragma warning disable 0618
                blockSize = stat.BlockSize;
                availableBlocks = stat.AvailableBlocks;
#pragma warning restore 0618
            }
            return availableBlocks * blockSize;
        }

        public static void StartNextOnQueue(Context context)
        {
            try
            {
                Queue.DownloadQueue.RemoveAt(0);
                if (Queue.DownloadQueue.Count > 0)
                {
                    Intent downloader = new Intent(context, typeof(DownloadFilesService));
                    downloader.PutExtra("IsFromQueue", true);
                    downloader.PutExtra("FullTitle", Queue.DownloadQueue[0].FullTitle);
                    downloader.PutExtra("Thumb", Queue.DownloadQueue[0].ShowThumb);
                    downloader.PutExtra("Show", Queue.DownloadQueue[0].Show);
                    downloader.PutExtra("ShowThumb", Queue.DownloadQueue[0].ShowThumb);
                    downloader.PutExtra("ShowSeason", Queue.DownloadQueue[0].ShowSeason);
                    downloader.PutExtra("Ep", Queue.DownloadQueue[0].Ep);
                    downloader.PutExtra("DownloadURL", Queue.DownloadQueue[0].URL);
                    //downloader.PutExtra("DownloadEPDuration", player.Duration);

                    if (serviceConnection == null)
                    {
                        Intent serviceBinder = new Intent(context, typeof(DownloadFilesService));
                        serviceConnection = new DownloadFileServiceConnection();
                        context.ApplicationContext.BindService(serviceBinder, serviceConnection, Bind.AutoCreate);
                    }

                    ((Application)context).StartService(downloader);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} {1}", e.Message, e.StackTrace);
            }
        }

        public static string GetSynopsis(string url)
        {
            string synopsis;
            DnsedWebClient client = new DnsedWebClient(NameServer.GooglePublicDns, NameServer.GooglePublicDns2);
            var data = client.DownloadData(url);
            var dataString = Encoding.UTF8.GetString(data);

            dataString = dataString.Substring(dataString.IndexOf("<div id=\"sinopseshow\"") + 117);
            synopsis = dataString.Substring(0, dataString.IndexOf("</div>"));

            return synopsis;
        }

        public static async void DownloadVideo(Context context, string FullTitle, bool GetDuration)
        {
            string Url = VideoPageUrl(FullTitle);
            byte[] Data;
            
            string ShowThumb;
            string Thumb;

            bool IsSubtitled;
            long duration = 0;
            long bytes_total = 0;

            DnsedWebClient request = new DnsedWebClient(NameServer.GooglePublicDns, NameServer.GooglePublicDns2);

            var (Show, ShowSeason, Ep) = BreakFullTitleInParts(FullTitle);
            //FullTitle = List.GetMainPageSeries.TopShow.Title;

            if (FullTitle.ToLower().Contains("legendado"))
                IsSubtitled = true;
            else
                IsSubtitled = false;

            if (Queue.DownloadQueue == null || Queue.DownloadQueue.Count == 0)
                CreateNotification(Show, ShowSeason, Ep, IsSubtitled, context);
            if (Queue.DownloadQueue == null)
                Queue.DownloadQueue = new List<QueueList>();

            Data = request.DownloadData(Url);
            var response = Encoding.UTF8.GetString(Data);

            var thumbBytes = await GetThumbnailBytes(response);
            var showThumbBytes = await request.DownloadDataTaskAsync(List.GetMainPageSeries.TopShow.ImgLink);
            var VideoPlayerDataString = await GetVideoAsync(response, context);

            try
            {
                if (!System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail"))
                    System.IO.Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail");

                if (!System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/ShowThumbnail"))
                    System.IO.Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/ShowThumbnail");

                Thumb = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail", FullTitle);
                try
                {
                    System.IO.File.WriteAllBytes(Thumb, thumbBytes);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                var thumbpath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/ShowThumbnail", Show);
                try
                {
                    if (!System.IO.File.Exists(thumbpath))
                        System.IO.File.WriteAllBytes(thumbpath, showThumbBytes);
                }
                catch { }

                //List.GetMainPageSeries.TopShow.Downloading = true;

                ShowThumb = thumbpath;

                if (!System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles"))
                    System.IO.Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles");

                try
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles", FullTitle + ".srt"), System.IO.File.ReadAllText(filePath));
                }
                catch { }

                DnsedWebClient header = new DnsedWebClient(NameServer.GooglePublicDns, NameServer.GooglePublicDns2);

                try
                {
                    header.OpenRead(VideoPlayerDataString);
                    bytes_total = long.Parse(header.ResponseHeaders["Content-Length"]);
                }
                catch { }

                if (GetDuration)
                {
                    try
                    {
                        Android.Media.MediaMetadataRetriever reader = new Android.Media.MediaMetadataRetriever();

                        reader.SetDataSource(VideoPlayerDataString, new Dictionary<string, string>());
                        duration = long.Parse(reader.ExtractMetadata(Android.Media.MetadataKey.Duration));

                        reader.Release();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.StackTrace);
                    }
                }

                if (!Database.IsSeasonOnDB(ShowSeason, Show, IsSubtitled))
                    Database.InsertData("", Show, ShowThumb, ShowSeason, -1, 0, 0, IsSubtitled, "", 0, "");
                Database.InsertData(Thumb, Show, ShowThumb, ShowSeason, Ep, 0, bytes_total, IsSubtitled, System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Series", FullTitle), duration, FullTitle);
                Queue.DownloadQueue.Add(new QueueList { URL = VideoPlayerDataString, FullTitle = FullTitle, EpThumb = Thumb, ShowThumb = ShowThumb, Duration = duration, IsSubtitled = IsSubtitled, Ep = Ep, Show = Show, ShowSeason = ShowSeason });


                if (Queue.DownloadQueue.Count == 1)
                {

                    Intent downloader = new Intent(context, typeof(DownloadFilesService));
                    downloader.PutExtra("DownloadURL", VideoPlayerDataString);
                    downloader.PutExtra("FullTitle", FullTitle);
                    downloader.PutExtra("Thumb", Thumb.Replace("Online,", "Online "));
                    downloader.PutExtra("Show", Show);
                    downloader.PutExtra("ShowThumb", ShowThumb);
                    downloader.PutExtra("ShowSeason", ShowSeason);
                    downloader.PutExtra("Ep", Ep);
                    //downloader.PutExtra("DownloadEPDuration", player.Duration);

                    if (serviceConnection == null)
                    {
                        Intent serviceBinder = new Intent(context, typeof(DownloadFilesService));
                        serviceConnection = new DownloadFileServiceConnection();
                        context.ApplicationContext.BindService(serviceBinder, serviceConnection, Bind.AutoCreate);
                    }

                    ((Activity)context).Application.StartService(downloader);
                }
            }
            catch { }
        }

        private static void CreateNotification(string Show, int ShowSeason, int Ep, bool IsSubtitled, Context context)
        {
            Log.Info("DownloadFilesService", "Notification Created With ID {0} AND Show {1}", 70718, Show);
            NotificationManager notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);

            var BroadcastPauseResumeIntent = new Intent(context, typeof(DownloadFileBroadcastListener));
            var BroadcastCancelIntent = new Intent(context, typeof(DownloadFileBroadcastListener));

            var PauseresumeDownloadPI = PendingIntent.GetBroadcast(context, RequestCode.ID(), BroadcastPauseResumeIntent, PendingIntentFlags.CancelCurrent);
            var CancelDownloadPI = PendingIntent.GetBroadcast(context, RequestCode.ID(), BroadcastCancelIntent, PendingIntentFlags.CancelCurrent);

            var PauseresumeAction = new NotificationCompat.Action(0, "Pausar", PauseresumeDownloadPI);
            var CancelAction = new NotificationCompat.Action(0, "Cancelar", CancelDownloadPI);

            var BigRemoteView = new RemoteViews(context.PackageName, Resource.Layout.download_notification);
            var SmallRemoteView = new RemoteViews(context.PackageName, Resource.Layout.download_notification_smallview);

            BigRemoteView.SetTextViewText(Resource.Id.downloadn_title, Show);
            BigRemoteView.SetTextViewText(Resource.Id.downloadn_se, string.Format("T{0}:E{1}", ShowSeason, Ep));
            BigRemoteView.SetTextViewText(Resource.Id.downloadn_perc, string.Format("{0}% ({1} MB/{2})", 0, 0, Size(0)));
            BigRemoteView.SetProgressBar(Resource.Id.downloadn_progress, 100, 0, false);

            SmallRemoteView.SetTextViewText(Resource.Id.downloadn_titles, Show);
            SmallRemoteView.SetTextViewText(Resource.Id.downloadn_percs, string.Format("{0}%", 0));
            SmallRemoteView.SetProgressBar(Resource.Id.downloadn_progresss, 100, 0, false);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannel chan = new NotificationChannel("250801", "DownloadContent", NotificationImportance.Low);
                chan.SetSound(null, null);
                chan.SetShowBadge(false);
                chan.EnableLights(false);
                chan.EnableVibration(false);
                notificationManager.CreateNotificationChannel(chan);
            }

            try
            {
                Database.ReadDB();

                var index = List.GetDownloads.Series.FindIndex(x => x.Show == Show && x.IsSubtitled == IsSubtitled);
                var epIndex = List.GetDownloads.Series[index].Episodes.FindIndex(x => x.EP == Ep && x.ShowSeason == ShowSeason);
                List.GetDownloads.Series[index].Episodes[epIndex].IsDownloading = true;
            }
            catch { }

            var Builder = new NotificationCompat.Builder(context, "250801")
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

            notificationManager.Notify(70718, Builder.Build());
        }

        public static long GetSubtitle(string URL, bool IsHDVideo, bool RequestFileSize = false)
        {
            string SubtitleURL = "";
            long length = 0;
            try
            {
                if (!IsHDVideo)
                {
                    try
                    {
                        SubtitleURL = URL.Substring(URL.IndexOf("<a href='"));
                        SubtitleURL = SubtitleURL.Substring(0, SubtitleURL.IndexOf("'>Baixar Legenda"));
                        SubtitleURL = SubtitleURL.Replace("<a href='", "");
                    }
                    catch
                    {
                        SubtitleURL = SubtitleURL.Substring(URL.IndexOf("seuseriado.com/player/legendas"));
                        SubtitleURL = SubtitleURL.Substring(0, SubtitleURL.IndexOf("','"));
                    }
                    SubtitleURL = SubtitleURL.Replace(" ", "%20");
                }
                else
                {
                    try
                    {
                        SubtitleURL = URL.Substring(0, URL.IndexOf(".srt") + 4);
                        SubtitleURL = SubtitleURL.Substring(SubtitleURL.IndexOf("'//seuseriado.com"));
                        SubtitleURL = SubtitleURL.Replace("'//", "");
                    }
                    catch { }
                }
                DnsedWebClient client = new DnsedWebClient(NameServer.GooglePublicDns, NameServer.GooglePublicDns2);
                try
                {
                    if (!RequestFileSize)
                    {
                        if (SubtitleURL.StartsWith("http"))
                            System.IO.File.WriteAllText(filePath, Encoding.UTF8.GetString(client.DownloadData(SubtitleURL)));
                        else
                            System.IO.File.WriteAllText(filePath, Encoding.UTF8.GetString(client.DownloadData("http://" + SubtitleURL)));
                    }
                    else
                    {
                        client.OpenRead("http://" + SubtitleURL);
                        length = long.Parse(client.ResponseHeaders["Content-Length"]);
                    }
                }
                catch { }
            }
            catch { }
            return length;
        }

        private static async Task<byte[]> GetThumbnailBytes(string response)
        {
            byte[] imageBytes = null;
            try
            {
                string ImgURL = "";
                try
                {
                    ImgURL = response.Substring(response.IndexOf("https://player2.seuseriado.com/player/p.php?"));
                    ImgURL = ImgURL.Substring(0, ImgURL.IndexOf("\""));
                    if (ImgURL.Contains("https://imagecurl.com/"))
                        ImgURL = ImgURL.Substring(ImgURL.IndexOf("https://imagecurl.com/"));
                    else
                        ImgURL = ImgURL.Substring(ImgURL.IndexOf("https://image.tmdb.org"));
                    ImgURL = ImgURL.Substring(0, ImgURL.IndexOf("&prott"));
                }
                catch { }
                using (var webClient = new DnsedWebClient(NameServer.GooglePublicDns, NameServer.GooglePublicDns2))
                {
                    imageBytes = await webClient.DownloadDataTaskAsync(ImgURL);
                    var idk = Encoding.UTF8.GetString(imageBytes);
                }
            }
            catch { }
            return imageBytes;
        }

        public static string VideoPageUrl(string title)
        {
            title = title.ToUpper();
            if(title.Contains("DUBLADO"))
                title = title.Substring(0, title.IndexOf("DUBLADO")+7);
            else if(title.Contains("LEGENDADO"))
                title = title.Substring(0, title.IndexOf("LEGENDADO")+9);
            else if (title.Contains("NACIONAL"))
                title = title.Substring(0, title.IndexOf("NACIONAL")+8);

            title = Regex.Replace(title, "[éèëêð]", "e");
            title = Regex.Replace(title, "[ÉÈËÊ]", "E");
            title = Regex.Replace(title, "[àâä]", "a");
            title = Regex.Replace(title, "[ÀÁÂÃÄÅ]", "A");
            title = Regex.Replace(title, "[àáâãäå]", "a");
            title = Regex.Replace(title, "[ÙÚÛÜ]", "U");
            title = Regex.Replace(title, "[ùúûüµ]", "u");
            title = Regex.Replace(title, "[òóôõöø]", "o");
            title = Regex.Replace(title, "[ÒÓÔÕÖØ]", "O");
            title = Regex.Replace(title, "[ìíîï]", "i");
            title = Regex.Replace(title, "[ÌÍÎÏ]", "I");
            title = Regex.Replace(title, @"\s+", " ");
            title = title.Replace(" (SEASON FINALE)", "");
            title = title.Replace("ª", "a");
            title = title.Replace("º", "o");
            title = title.Replace("&", "");
            title = title.Replace("- ", "");
            title = title.Replace(" ", "-");
            title = title.Replace("–", "-");
            title = title.Replace(",", "");
            title = title.Replace(".", "-");
            title = title.Replace(":", "-");
            title = title.Replace("SEM-LEGENDA", "legendado");
            title = Regex.Replace(title, @"-+", "-");

            return "http://seuseriado.com/" + title.ToLower();
        }

        public static void AskPermission(Context context)
        {
            if (context.CheckSelfPermission(Android.Manifest.Permission.ReadExternalStorage) != Android.Content.PM.Permission.Granted)
            {
                ActivityCompat.RequestPermissions((Activity)context, new string[] { Android.Manifest.Permission.ReadExternalStorage }, 87);
            }
            if (context.CheckSelfPermission(Android.Manifest.Permission.WriteExternalStorage) != Android.Content.PM.Permission.Granted)
            {
                ActivityCompat.RequestPermissions((Activity)context,new string[] { Android.Manifest.Permission.WriteExternalStorage }, 88);
            }
        }

        private static async Task<string> GetVideoAsync(string response, Context context)
        {
            return await Task.Run(() => GetVideo(response, context));
        }

        private static string GetVideo(string response, Context context)
        {
            string URL;
            string VideoPlayerDataString;
            byte[] VideoPlayerData;

            var request = new DnsedWebClient(NameServer.GooglePublicDns, NameServer.GooglePublicDns2);
            try
            {
                URL = response.Substring(response.IndexOf("http://player.seuseriado.com/player/p.php?"));
                URL = URL.Substring(0, URL.IndexOf("');\"><span style=\"color: #429bd6;\">"));
                VideoPlayerData = request.DownloadData(URL);
                VideoPlayerDataString = Encoding.UTF8.GetString(VideoPlayerData);

                if (VideoPlayerDataString.Contains("Error"))
                {
                    Console.WriteLine("SVideoPlayer {0}", "Error. Couldn't play HD video. Changing to SD Quality");
                    throw new InvalidOperationException("Playback Error");
                }
                else
                {
                    GetSubtitle(VideoPlayerDataString, true);
                    VideoPlayerDataString = VideoPlayerDataString.Substring(0, VideoPlayerDataString.IndexOf("','HD'"));
                    try
                    {
                        VideoPlayerDataString = VideoPlayerDataString.Substring(VideoPlayerDataString.IndexOf("'SD','"));
                        VideoPlayerDataString = VideoPlayerDataString.Replace("'SD','", "");
                    }
                    catch
                    {
                        VideoPlayerDataString = VideoPlayerDataString.Substring(VideoPlayerDataString.IndexOf("Play('"));
                        VideoPlayerDataString = VideoPlayerDataString.Replace("Play('", "");
                    }

                    if (VideoPlayerDataString.Contains("http://videoshare.club/"))
                    {
                        HttpWebRequest requests = (HttpWebRequest)WebRequest.Create(VideoPlayerDataString);
                        requests.AllowAutoRedirect = false;
                        HttpWebResponse responses = (HttpWebResponse)requests.GetResponse();
                        responses.Close();

                        VideoPlayerDataString = responses.Headers["location"];
                    }
                }
            }
            catch
            {
                URL = response.Substring(response.IndexOf("https://player2.seuseriado.com/player/p.php?"));
                URL = URL.Substring(0, URL.IndexOf("\" frameborder=\"0\""));
                VideoPlayerData = request.DownloadData(URL);
                VideoPlayerDataString = Encoding.UTF8.GetString(VideoPlayerData);

                GetSubtitle(VideoPlayerDataString, false);

                if (VideoPlayerDataString.Contains("Error"))
                {
                    Toast.MakeText(context, "Esse título não está disponivel no momento. Por favor, tente novamente mais tarde.", ToastLength.Long);
                }
                else
                {
                    VideoPlayerDataString = VideoPlayerDataString.Substring(0, VideoPlayerDataString.IndexOf("','SD'"));
                    VideoPlayerDataString = VideoPlayerDataString.Substring(VideoPlayerDataString.IndexOf("Play('"));
                    VideoPlayerDataString = VideoPlayerDataString.Replace("Play('", "");
                }
            }
            return VideoPlayerDataString;
        }

        public static string Download(int page, string search = "")
        {
            byte[] responseB;
            string response = "";
            string topShow = "";
            string link = "";
            bool IsHTTPS = false;

            try
            {
                var CheckHTTPS = new DnsedWebClient(NameServer.GooglePublicDns, NameServer.GooglePublicDns2);
                WebClient request = CloudFlareEvader.CreateBypassedWebClient("http://seuseriado.com");

                try
                {
                    CheckHTTPS.OpenRead("https://seuseriado.com").Close();
                    link = "https://seuseriado.com";
                }
                catch
                {
                    CheckHTTPS.OpenRead("http://seuseriado.com" ).Close();
                    link = "http://seuseriado.com";
                }

                if (string.IsNullOrWhiteSpace(search))
                    responseB = request.DownloadData(link + "/page/" + page);
                else
                    responseB = request.DownloadData(link + "/page/" + page + "/?s=" + search.Replace(" ", "+"));

                response = Encoding.UTF8.GetString(responseB);
                if(page == 1)
                    if(string.IsNullOrWhiteSpace(search))
                        topShow = response.Substring(response.IndexOf("Destaques</h2></div>"));

                if (IsHTTPS)
                    link = "https://seuseriado.com";
                else
                    link = "http://seuseriado.com";

                foreach (var header in request.ResponseHeaders)
                {
                    Console.WriteLine("{0}: {1}", header, request.ResponseHeaders[header.ToString()]);
                }

                try
                {
                    response = response.Substring(0, response.IndexOf("</div><nav class=\"herald-pagination\">"));
                } catch { }

                if (string.IsNullOrWhiteSpace(search))
                {
                    //Get Usefull part of the string
                    if (page == 1)
                    {
                        try
                        {
                            topShow = topShow.Substring(0, topShow.IndexOf("Mais Atualizações"));
                            topShow = topShow.Substring(topShow.IndexOf("Seja Premium! Veja as Vantagens</a></h2></div></div></article>") + 62);
                            while (topShow.Contains("<article "))
                            {
                                topShow = topShow.Remove(topShow.IndexOf("<article "), topShow.IndexOf("/\" title=") - topShow.IndexOf("<article ") + 3);
                            }
                            topShow = topShow.Replace("<img width=\"215\" height=\"161\" src=\"", "\"ImgLink\":\"");
                            while (topShow.Contains("class=\"attachment-herald"))
                            {
                                topShow = topShow.Remove(topShow.IndexOf("class=\"attachment-herald"), topShow.IndexOf("</article>") - topShow.IndexOf("class=\"attachment-herald") + 10);
                            }
                            topShow = topShow.Substring(0, topShow.IndexOf("</div>"));
                            topShow = topShow.Substring(0, topShow.IndexOf("title=", topShow.IndexOf("title=") + 1));
                            topShow = topShow.Replace("\">", "\",");
                            topShow = topShow.Replace("title=", "{\"Title\":");
                            topShow = topShow.Insert(topShow.Length, "}");
                            topShow = topShow.Replace("&#8211;", "");

                            List.GetMainPageSeries.TopShow = JsonConvert.DeserializeObject<MainPageSeries>(topShow.Replace("resize=215%2C161", ""));
                        }
                        catch { }
                    }

                    response = response.Substring(response.IndexOf("Mais Atualizações"));

                    //Start updated date conversion
                    response = response.Replace("<div class=\"entry-meta\"><div class=\"meta-item herald-date\"><span class=\"updated\">", "\"Update\":\"");
                    response = response.Replace("</span></div></div></div></div></article>", "\"},\n");

                    //Link of The img
                    response = response.Replace("<img width=\"300\" height=\"200\" src=", "\"ImgLink\":");
                    response = response.Replace("class=\"attachment-herald-lay-b1 size-herald-lay-b1 wp-post-image\" alt=\"\" />", ",");
                    do
                    {
                        response = response.Remove(response.IndexOf("<a href=\""), (response.IndexOf("/\"") - response.IndexOf("<a href=\"")) + 3);
                    } while (response.Contains("<a href=\""));

                    //Create Title JSON(Converting from Html)
                    response = response.Replace("<div class=\"herald-ovrld\">", "");
                    response = response.Replace("title=\"", "\n\"Title\":\"");
                    response = response.Replace(")\">", ")\",");
                    response = response.Replace("O\">", "O\",");
                    response = response.Replace("A\">", "A\",");
                    response = response.Replace("L\">", "L\",");
                    response = response.Replace("I\">", "I\",");
                    response = response.Replace("X\">", "X\",>");
                    response = response.Replace("o\">", "o\",");
                    response = response.Replace("a\">", "a\",");
                    response = response.Replace("l\">", "l\",");
                    response = response.Replace("i\">", "i\",");
                    response = response.Replace("x\">", "x\",>");
                    response = Regex.Replace(response, " PARTE \\d*\">", "\",>");
                    response = response.Replace("SEM LEGENDA", "LEGENDADO");
                    response = response.Replace("sem legendado", "LEGENDADO");

                    do
                    {
                        response = response.Remove(response.IndexOf("<span class=\"meta-category\">"), (response.IndexOf("</a></h2>") - response.IndexOf("<span class=\"meta-category\">")) + 9);
                    } while (response.Contains("<span class=\"meta-category\">"));

                    do
                    {
                        response = response.Remove(response.IndexOf("<article class=\"herald-lay-f herald-lay-f1"), (response.IndexOf("\"Title\":") - response.IndexOf("<article class=\"herald-lay-f herald-lay-f1")) + 1);
                    } while (response.Contains("<article class=\"herald-lay-f herald-lay-f1"));
                    response = response.Replace("Title\":\"", "{\"Title\":\"");

                    response = response.Replace("<div class=\"entry-header\">", "");
                    response = response.Replace("</a>", "");
                    response = response.Replace("</div>", "");


                    response = response.Replace("<span class=\"site-title h1\"><a href=\""+ link + "\" rel=\"home\"><img class=\"herald-logo-mini\" src=\""+ link + "/wp-content/uploads/2017/04/LogoSeuSeriado135.png\" alt=\"SeuSeriado.com\"></a></span>", "");

                    response = response.Replace("Mais Atualizações", "[");
                    response = response.Replace("</h2>", "");
                    response = response.Replace("<div class=\"row herald-posts row-eq-height \">", "");
                    response = response.Replace("&#8211;", "");
                    response = response.Replace("<", "");
                    response = response.Replace(">", "");
                    response = response.Substring(0, response.Length - 2);
                    response = response.Insert(response.Length, "]");
                }
                else
                {
                    response = response.Substring(response.IndexOf("Resultados Para : "));

                    //Start updated date conversion
                    response = response.Replace("<div class=\"entry-meta\"><div class=\"meta-item herald-date\"><span class=\"updated\">", "\"Update\":\"");
                    response = response.Replace("</span></div></div></div></div></article>", "\"},\n");

                    //Link of The img
                    response = response.Replace("<img width=\"300\" height=\"200\" src=", "\"ImgLink\":");
                    response = response.Replace("<img width=\"285\" height=\"200\" src=", "\"ImgLink\":");
                    response = response.Replace("class=\"attachment-herald-lay-b1 size-herald-lay-b1 wp-post-image\" alt=\"\" />", ",");
                    try
                    {
                        do
                        {
                            response = response.Remove(response.IndexOf("<a href=\""), (response.IndexOf("/\"") - response.IndexOf("<a href=\"")) + 3);
                        } while (response.Contains("<a href=\""));
                    }
                    catch { }

                    //Create Title JSON(Converting from Html)
                    response = response.Replace("<div class=\"herald-ovrld\">", "");
                    response = response.Replace("title=\"", "\n\"Title\":\"");
                    response = response.Replace(")\">", ")\",");
                    response = response.Replace("O\">", "O\",");
                    response = response.Replace("A\">", "A\",");
                    response = response.Replace("L\">", "L\",");
                    response = response.Replace("I\">", "I\",");
                    response = response.Replace("X\">", "X\",>");
                    response = response.Replace("o\">", "o\",");
                    response = response.Replace("a\">", "a\",");
                    response = response.Replace("l\">", "l\",");
                    response = response.Replace("i\">", "i\",");
                    response = response.Replace("x\">", "x\",>");
                    response = Regex.Replace(response, " PARTE \\d*\">", "\",>");
                    response = response.Replace("SEM LEGENDA", "LEGENDADO");

                    do
                    {
                        response = response.Remove(response.IndexOf("<span class=\"meta-category\">"), (response.IndexOf("</a></h2>") - response.IndexOf("<span class=\"meta-category\">")) + 9);
                    } while (response.Contains("<span class=\"meta-category\">"));

                    do
                    {
                        response = response.Remove(response.IndexOf("<article class=\"herald-lay-f herald-lay-f1"), (response.IndexOf("\"Title\":") - response.IndexOf("<article class=\"herald-lay-f herald-lay-f1")) + 1);
                    } while (response.Contains("<article class=\"herald-lay-f herald-lay-f1"));
                    response = response.Replace("Title\":\"", "{\"Title\":\"");

                    response = response.Replace("<div class=\"entry-header\">", "");
                    response = response.Replace("</a>", "");
                    response = response.Replace("</div>", "");

                    response = response.Replace("<div class=\"row row-eq-height herald-posts\">", "");

                    response = response.Replace("<span class=\"site-title h1\"><a href=\""+ link + "\" rel=\"home\"><img class=\"herald-logo-mini\" src=\""+ link + "/wp-content/uploads/2017/04/LogoSeuSeriado135.png\" alt=\"SeuSeriado.com\"></a></span>", "");

                    response = response.Replace(string.Format("Resultados Para : {0}", search), "[");
                    response = response.Replace("<h1>", "");
                    response = response.Replace("</h1>", "");
                    response = response.Replace("</h2>", "");
                    response = response.Replace("&#8211;", "");
                    response = response.Replace("<", "");
                    response = response.Replace(">", "");
                    response = response.Substring(0, response.LastIndexOf("\"},")+2);
                    //response = response.Substring(0, response.Length - 2);
                    response = response.Insert(response.Length, "]");
                    do
                    {
                        try
                        {
                            response = response.Remove(response.IndexOf("class="), response.IndexOf("/ \"") - response.IndexOf("class=") + 1);
                        }
                        catch { }
                    } while (response.Contains("class="));
                    response = response.Replace("  ", ",");

                    response = response.Replace("Online,", "Online ");

                    response = response.Replace("\"\"", "\",\"");
                    response = response.Replace("img src=", ",\"ImgLink\":");
                    response = response.Remove(response.IndexOf("alt="), response.LastIndexOf("/") - response.IndexOf("alt="));
                    var inx = response.LastIndexOf("/");
                    response = response.Insert(inx, ",");
                    response = response.Remove(inx + 1, 1);

                }
                response = response.Replace("resize=300%2C200", "");
                response = WebUtility.HtmlDecode(response);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} {1}", e.Message, e.StackTrace);
            }

            return response;
        }


    }
}