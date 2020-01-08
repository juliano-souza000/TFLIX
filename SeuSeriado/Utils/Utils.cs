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
using Android.Views;
using Android.Widget;
using ByteSizeLib;
using Newtonsoft.Json;
using SeuSeriado.List;
using SeuSeriadoTest;

namespace SeuSeriado.Utils
{
    class Utils
    {
        public static byte[] GetImageBytes(Drawable d)
        {
            Bitmap bitmap = ((BitmapDrawable)d).Bitmap;
            var ms = new System.IO.MemoryStream();
            bitmap.Compress(Bitmap.CompressFormat.Png, 100, ms);
            return ms.ToArray();
        }

        public static string Size(long sizeInBytes)
        {
            var fileSize = ByteSize.FromBytes(Convert.ToDouble(sizeInBytes));

            if (fileSize.TeraBytes > 1)
            {
                return fileSize.ToString("TB");
            }
            else if (fileSize.GigaBytes > 1)
            {
                return fileSize.ToString("GB");
            }
            else if (fileSize.MegaBytes > 1)
            {
                return fileSize.ToString("MB");
            }
            else
            {
                return fileSize.ToString("B");
            }

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