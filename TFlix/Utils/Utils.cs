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
using Newtonsoft.Json;
using TFlix.List;
using TFlix.Services;
using SeuSeriadoTest;

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
            var Ep  = fullTitle.Substring(fullTitle.LastIndexOf("Episódio"));
            Ep = Ep.Substring(0, Ep.LastIndexOf("Online")).Replace("Episódio", "");

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
                using (var webClient = new WebClient())
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

        public static string GetSynopsis(string url)
        {
            string synopsis;
            WebClient client = new WebClient();
            var data = client.DownloadData(url);
            var dataString = Encoding.UTF8.GetString(data);

            dataString = dataString.Substring(dataString.IndexOf("<div id=\"sinopseshow\"") + 117);
            synopsis = dataString.Substring(0, dataString.IndexOf("</div>"));

            return synopsis;
        }

        public static async void DownloadVideo(bool IsFromSearch, int Pos, Context context)
        {
            string Url;
            byte[] Data;

            WebClient request = new WebClient();

            //AskPermission(context);

            if (!IsFromSearch)
                Url = VideoPageUrl(List.GetMainPageSeries.Series[Pos].Title);
            else
                Url = VideoPageUrl(List.GetSearch.Search[Pos].Title.Replace("Online,", "Online "));

            Data = request.DownloadData(Url);
            var response = Encoding.UTF8.GetString(Data);

            var thumbDrawable = await GetThumbnailDrawable(context, response);
            var VideoPlayerDataString = await GetVideoAsync(response, context);

            try
            {
                if (!System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail"))
                    System.IO.Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail");

                if (!IsFromSearch)
                {
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail", List.GetMainPageSeries.Series[Pos].Title), GetImageBytes(thumbDrawable));
                    List.GetMainPageSeries.Series[Pos].EPThumb = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail", List.GetSearch.Search[Pos].Title);
                }
                else
                {
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail", List.GetSearch.Search[Pos].Title), GetImageBytes(thumbDrawable));
                    List.GetSearch.Search[Pos].EPThumb = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail", List.GetSearch.Search[Pos].Title);
                }
            }
            catch { }

            //if (context.CheckSelfPermission(Android.Manifest.Permission.ReadExternalStorage) == Android.Content.PM.Permission.Granted
            //    && context.CheckSelfPermission(Android.Manifest.Permission.WriteExternalStorage) == Android.Content.PM.Permission.Granted)
            //{
                if (!System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles"))
                    System.IO.Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles");

                try
                {
                    if (!IsFromSearch)
                    {
                        System.IO.File.WriteAllText(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles", List.GetMainPageSeries.Series[Pos].Title + ".srt"), System.IO.File.ReadAllText(filePath));
                    }
                    else
                    {
                        System.IO.File.WriteAllText(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles", List.GetSearch.Search[Pos].Title.Replace("Online,", "Online ") + ".srt"), System.IO.File.ReadAllText(filePath));
                    }
                }
                catch { }

                Intent downloader = new Intent(context, typeof(DownloadFilesService));
                downloader.PutExtra("DownloadURL", VideoPlayerDataString);
                downloader.PutExtra("DownloadSHOWID", Pos);
                downloader.PutExtra("IsFromSearch", IsFromSearch);
                //downloader.PutExtra("DownloadEPDuration", player.Duration);

                if (serviceConnection == null)
                {
                    Intent serviceBinder = new Intent(context, typeof(DownloadFilesService));
                    serviceConnection = new DownloadFileServiceConnection();
                    context.ApplicationContext.BindService(serviceBinder, serviceConnection, Bind.AutoCreate);
                }

                ((Activity)context).Application.StartService(downloader);
            //}
            //else
            //{
            //    AskPermission(context);
            //}
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
                WebClient client = new WebClient();
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

        private static async Task<Drawable> GetThumbnailDrawable(Context context, string response)
        {
            Drawable d = null;
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
                var imageBitmap = await GetImageBitmapFromUrl(ImgURL);
                d = new BitmapDrawable(context.Resources, imageBitmap);
            }
            catch { }
            return d;
        }

        public static string VideoPageUrl(string title)
        {
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
            title = title.Replace(",", "");
            title = title.Replace(" (SEASON FINALE)", "");
            title = title.Replace("ª", "a");
            title = title.Replace("º", "o");
            title = title.Replace("- ", "");
            title = title.Replace(" ", "-");
            title = title.Replace("SEM-LEGENDA", "legendado");
            return "https://seuseriado.com/" + title.ToLower();
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

            var request = new WebClient();
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

            try
            {
                WebClient request = CloudFlareEvader.CreateBypassedWebClient("https://seuseriado.com/");

                if (string.IsNullOrWhiteSpace(search))
                    responseB = request.DownloadData("https://seuseriado.com/page/" + page);
                else
                    responseB = request.DownloadData("https://seuseriado.com/page/" + page + "/?s=" + search.Replace(" ", "+"));

                response = Encoding.UTF8.GetString(responseB);

                //Get Usefull part of the string
                try
                {
                    response = response.Substring(0, response.IndexOf("</div><nav class=\"herald-pagination\">"));
                } catch { }

                if (string.IsNullOrWhiteSpace(search))
                {
                    //Get Usefull part of the string
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


                    response = response.Replace("<span class=\"site-title h1\"><a href=\"https://seuseriado.com/\" rel=\"home\"><img class=\"herald-logo-mini\" src=\"https://seuseriado.com/wp-content/uploads/2017/04/LogoSeuSeriado135.png\" alt=\"SeuSeriado.com\"></a></span>", "");

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

                    response = response.Replace("<span class=\"site-title h1\"><a href=\"https://seuseriado.com/\" rel=\"home\"><img class=\"herald-logo-mini\" src=\"https://seuseriado.com/wp-content/uploads/2017/04/LogoSeuSeriado135.png\" alt=\"SeuSeriado.com\"></a></span>", "");

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
                    //response = response.Replace("Online,", "");
                    response = response.Replace("\"\"", "\",\"");
                }
            }
            catch { }

            return response;
        }
    }
}