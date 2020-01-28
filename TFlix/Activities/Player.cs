using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using TFlix.Services;
using TFlix.Srt;
using Square.Picasso;
using Xamarin.Essentials;
using Encoding = System.Text.Encoding;
using Newtonsoft.Json;
using Android.Gms.Ads;
using Android.Gms.Ads.Formats;
using Android.Content.Res;

namespace TFlix.Activities
{
    [Activity(Label = "Player", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.KeyboardHidden | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class Player : Activity, ISurfaceHolderCallback,MediaPlayer.IOnPreparedListener, MediaPlayer.IOnSeekCompleteListener, UnifiedNativeAd.IOnUnifiedNativeAdLoadedListener
    {
        private WebClient request = new WebClient();
        private BackgroundWorker worker = new BackgroundWorker();
        private UnifiedNativeAd _NativeAd;

        //CustomMediaController
        private ImageView PlayPause;
        private ImageView Download;
        private ImageView HD;
        private ImageView Subtitles;
        private TextView EndTime;
        private TextView CurrentTime;
        private TextView PTitle;
        private SeekBar SeekBarPlayer;
        private RelativeLayout ControllerLayout;

        private TextView Subtitle;
        private ImageView Thumbnail;
        private ProgressBar Loading;
        private SurfaceView SSurfaceView;
        private ISurfaceHolder SSurfaceHolder;
        private MediaPlayer player;
        private FrameLayout _FrameLayout;
        private Xamarin.Essentials.NetworkAccess current;

        private System.Timers.Timer timer = new System.Timers.Timer();
        private System.Timers.Timer CloseControlTimer = new System.Timers.Timer();
        private System.Timers.Timer ResetClickCounter = new System.Timers.Timer();

        private bool HasAdFinished = false;

        private int Pos;
        private int x = 0;
        private int ClickCount = 0;
        private string response;
        private string VideoPlayerDataString;

        private bool ShowSubtitles = true;
        private bool IsFromSearch;
        private bool IsOnline;
        private bool IsPrepared;
        private bool IsSubtitled;
        private bool IsListenerEnabled = false;
        private string Show;
        private int Season;
        private int Ep;
        private long BookmarkInMillisecond;
        private string filePath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "TempSubtitle.srt");

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.VideoPlayer);
           
            Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            try
            {
                System.IO.File.WriteAllText(filePath,"");
            }
            catch { }

            Subtitle = (TextView)FindViewById(Resource.Id.player_subtitle);
            SSurfaceView = (SurfaceView)FindViewById(Resource.Id.player);
            Thumbnail = (ImageView)FindViewById(Resource.Id.player_thumbnail);
            Loading = (ProgressBar)FindViewById(Resource.Id.player_loading);
            _FrameLayout = (FrameLayout)FindViewById(Resource.Id.fl_adplaceholder);

            //+-----------------------------------------+
            //|Media Controller Declaration             |
            //+-----------------------------------------+
            //PlayPause is the play/pause button
            //Download is the download button
            //HD is the indicator for current video quality mode//Subtitles is the button for activating/deactivating subtitles
            //CurrentTime is the textview on left side of seekbar, indicates the current time of the show
            //Endtime is the textview on the right side of seekbar, indicates the total time of the show
            //PTitle is the title of the current show
            //SeekBarPlayer is the seekbar of the player
            //ControllerLayout: where all those things are.

            PlayPause = (ImageView)FindViewById(Resource.Id.mcontroller_playpause);
            Download = (ImageView)FindViewById(Resource.Id.mcontroller_download);
            HD = (ImageView)FindViewById(Resource.Id.mcontroller_hd);
            Subtitles = (ImageView)FindViewById(Resource.Id.mcontroller_subtitle);
            CurrentTime = (TextView)FindViewById(Resource.Id.mcontroller_currenttime);
            EndTime = (TextView)FindViewById(Resource.Id.mcontroller_endtime);
            PTitle = (TextView)FindViewById(Resource.Id.mcontroller_title);
            SeekBarPlayer = (SeekBar)FindViewById(Resource.Id.mcontroller_seekbar);
            ControllerLayout = (RelativeLayout)FindViewById(Resource.Id.mcontroller);

            SSurfaceHolder = SSurfaceView.Holder;
            SSurfaceHolder.AddCallback(this);

            Pos = Intent.Extras.GetInt("Pos");
            IsOnline = Intent.Extras.GetBoolean("IsOnline");

            ControllerLayout.Visibility = ViewStates.Gone;
            Loading.Visibility = ViewStates.Gone;
            Thumbnail.BringToFront();

            CloseControlTimer.AutoReset = true;
            CloseControlTimer.Interval = 2000;

            current = Connectivity.NetworkAccess;

            if (IsOnline)
                StartAd();

            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
            CloseControlTimer.Elapsed += CloseControlTimer_Elapsed;
            SeekBarPlayer.ProgressChanged += SeekBarPlayer_ProgressChanged;
            SeekBarPlayer.StartTrackingTouch += SeekBarPlayer_StartTrackingTouch;
            SeekBarPlayer.StopTrackingTouch += SeekBarPlayer_StopTrackingTouch;
            Subtitles.Click += Subtitles_Click;
            Download.Click += Download_Click;
            SSurfaceView.Touch += TouchEv;
            ControllerLayout.Touch += TouchEv;
            PlayPause.Click += PlayPause_Click;
        }

        protected override void OnSaveInstanceState(Bundle outState) { }

        protected override void OnStop()
        {
            base.OnStop();
        }

        protected override void OnResume()
        {
            if (!HasAdFinished && _NativeAd != null)
                _NativeAd.VideoController.Play();
            base.OnResume();
        }

        protected override void OnPause()
        {
            if (HasAdFinished)
                Pause();
            else
                if(_NativeAd != null)
                _NativeAd.VideoController.Pause();
            base.OnPause();
        }

        protected override void OnDestroy()
        {
            if(player.CurrentPosition >= player.Duration)
                Utils.Bookmark.UpdateBookmarkInMillisecond(Show, Ep, Season, IsSubtitled, 0);
            else
                Utils.Bookmark.UpdateBookmarkInMillisecond(Show, Ep, Season, IsSubtitled, player.CurrentPosition);

            try
            {
                player.Stop();
                player.Release();
            }
            catch { }

            if (_NativeAd != null)
                _NativeAd.Destroy();

            timer.Stop();
            CloseControlTimer.Stop();
            ResetClickCounter.Stop();
            player = null;
            timer = null;
            CloseControlTimer = null;
            ResetClickCounter = null;
            SeekBarPlayer.ProgressChanged -= SeekBarPlayer_ProgressChanged;
            SeekBarPlayer.StartTrackingTouch -= SeekBarPlayer_StartTrackingTouch;
            SeekBarPlayer.StopTrackingTouch -= SeekBarPlayer_StopTrackingTouch;
            Subtitles.Click -= Subtitles_Click;
            Download.Click -= Download_Click;
            SSurfaceView.Touch -= TouchEv;
            ControllerLayout.Touch -= TouchEv;
            PlayPause.Click -= PlayPause_Click;
            Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
            base.OnDestroy();
        }

        private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            current = e.NetworkAccess;
            if (IsOnline)
            {
                switch (e.NetworkAccess)
                {
                    case Xamarin.Essentials.NetworkAccess.Internet:
                        Loading.Visibility = ViewStates.Gone;
                        Start();
                        break;
                    case Xamarin.Essentials.NetworkAccess.None:
                        if (_NativeAd != null)
                        {
                            _NativeAd.Destroy();
                            _FrameLayout.RemoveAllViews();
                        }
                        Loading.Visibility = ViewStates.Visible;
                        Loading.BringToFront();
                        Pause();
                        break;
                }
            }

        }

        private void Download_Click(object sender, EventArgs e)
        {
            bool download = false;
            if (IsFromSearch && !List.GetSearch.Search[Pos].Downloading && !List.GetSearch.Search[Pos].Downloaded)
                download = true;
            else if (!IsFromSearch && !List.GetMainPageSeries.Series[Pos].Downloading && !List.GetMainPageSeries.Series[Pos].Downloaded)
                download = true;

            if (download)
            {
                Pause();
                DownloadVideo();
            }

        }

        private void DownloadVideo()
        {
            try
            {
                if (!System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail"))
                    System.IO.Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail");

                if (!IsFromSearch)
                {
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail", List.GetMainPageSeries.Series[Pos].Title), Utils.Utils.GetImageBytes(Thumbnail.Drawable));
                    List.GetMainPageSeries.Series[Pos].EPThumb = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail", List.GetSearch.Search[Pos].Title);
                }
                else
                {
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail", List.GetSearch.Search[Pos].Title), Utils.Utils.GetImageBytes(Thumbnail.Drawable));
                    List.GetSearch.Search[Pos].EPThumb = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Thumbnail", List.GetSearch.Search[Pos].Title);
                }
            }
            catch { }

            if (CheckSelfPermission(Android.Manifest.Permission.ReadExternalStorage) == Android.Content.PM.Permission.Granted
                && CheckSelfPermission(Android.Manifest.Permission.WriteExternalStorage) == Android.Content.PM.Permission.Granted)
            {

                try
                {
                    if (!System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles"))
                        System.IO.Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles");

                    if (!IsFromSearch)
                    {
                        System.IO.File.WriteAllText(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles", List.GetMainPageSeries.Series[Pos].Title + ".srt"), System.IO.File.ReadAllText(filePath));
                    }
                    else
                    {
                        System.IO.File.WriteAllText(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles", List.GetSearch.Search[Pos].Title.Replace("Online,", "Online ")+ ".srt"), System.IO.File.ReadAllText(filePath));
                    }
                }
                catch { }

                Intent downloader = new Intent(this, typeof(DownloadFilesService));
                downloader.PutExtra("DownloadURL", VideoPlayerDataString);
                downloader.PutExtra("DownloadSHOWID", Pos);
                downloader.PutExtra("IsFromSearch", IsFromSearch);
                //downloader.PutExtra("DownloadEPDuration", player.Duration);

                if (Utils.Utils.serviceConnection == null)
                {
                    Intent serviceBinder = new Intent(this, typeof(DownloadFilesService));
                    Utils.Utils.serviceConnection = new DownloadFileServiceConnection();
                    Application.BindService(serviceBinder, Utils.Utils.serviceConnection, Bind.AutoCreate);
                }

                Application.StartService(downloader);
                

                player.Stop();
                player.Release();
                this.Finish();
               
            }
            else
            {
                Utils.Utils.AskPermission(this);
            }
        }

        private void Subtitles_Click(object sender, EventArgs e)
        {
           if(ShowSubtitles)
            {
                Subtitles.SetImageResource(Resource.Drawable.baseline_subtitles_off);
                ShowSubtitles = false;
            }
            else
            {
                Subtitles.SetImageResource(Resource.Drawable.baseline_subtitles_24);
                ShowSubtitles = true;
            }
        }

        private void PlayPause_Click(object sender, EventArgs e)
        {
            if (IsOnline)
            {
                if (player.IsPlaying)
                    Pause();
                else
                    if (current == Xamarin.Essentials.NetworkAccess.Internet)
                    Start();
            }
            else
            {
                if (player.IsPlaying)
                    Pause();
                else
                    Start();
            }
        }

        private void SeekBarPlayer_StartTrackingTouch(object sender, SeekBar.StartTrackingTouchEventArgs e) { Pause(); }

        private void SeekBarPlayer_StopTrackingTouch(object sender, SeekBar.StopTrackingTouchEventArgs e) { SeekTo(e.SeekBar.Progress); }

        private void SeekBarPlayer_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e) { CurrentTime.Text = StringForTime(e.Progress); }

        private void LoadVideoFromStorage(string path)
        {
            try
            {
                player.SetDataSource(this, Android.Net.Uri.FromFile(new Java.IO.File(path)));
                player.PrepareAsync();
                player.SetOnPreparedListener(this);
            }
            catch (Exception e) { Console.WriteLine(e.StackTrace); Finish(); }
        }

        private void TouchEv(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Down)
            {
                if (e.Event.DeviceId != 0)
                {
                    ClickCount++;

                    var metrics = Resources.DisplayMetrics;

                    if (!ResetClickCounter.Enabled)
                    {
                        ResetClickCounter.Enabled = true;
                        ResetClickCounter.Interval = 500;
                        ResetClickCounter.Start();
                        ResetClickCounter.Elapsed += (s, ex) =>
                        {
                            ClickCount = 0;
                            ResetClickCounter.Stop();
                            ResetClickCounter.Enabled = false;
                        };
                    }

                    if (ClickCount >= 2)
                    {
                        ClickCount = 0;

                        if (e.Event.RawX > (metrics.WidthPixels * 0.7))
                        {
                            //Right Side
                            SeekTo(player.CurrentPosition + (10 * 1000));
                        }
                        else if (e.Event.RawX < (metrics.WidthPixels * 0.3))
                        {
                            //Left Side
                            SeekTo(player.CurrentPosition - (10 * 1000));
                        }
                    }
                }

                if (IsPrepared)
                {
                    if (ControllerLayout.Visibility == ViewStates.Visible)
                    {
                        FadeControls(false);
                    }
                    else
                    {
                        ControllerLayout.Visibility = ViewStates.Visible;
                        ControllerLayout.BringToFront();
                        FadeControls(true);
                    }
                }
            }
        }

        private void CloseControlTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (player.IsPlaying)
            {
                if (ControllerLayout.Visibility == ViewStates.Visible)
                    ControllerLayout.DispatchTouchEvent(MotionEvent.Obtain(0,0, MotionEventActions.Down, 100, 100, 0.5f, 5, 0, 1, 1, 0, 0));
            }
        }

        private void FadeControls(bool FadeIn)
        {
            if(FadeIn)
            {

                CloseControlTimer.Enabled = true;
                CloseControlTimer.Start();

                var fadeinAnim = AnimationUtils.LoadAnimation(ApplicationContext, Resource.Animation.fadein);
                ControllerLayout.StartAnimation(fadeinAnim);

                fadeinAnim.AnimationEnd += (s, e) =>
                {
                    ControllerLayout.Visibility = ViewStates.Visible;
                };
            }
            else
            {
                CloseControlTimer.Stop();
                CloseControlTimer.Enabled = false;
                RunOnUiThread(() =>
                {
                    var fadeoutAnim = AnimationUtils.LoadAnimation(ApplicationContext, Resource.Animation.fadeout);
                    ControllerLayout.StartAnimation(fadeoutAnim);

                    fadeoutAnim.AnimationEnd += (s, e) =>
                    {
                        ControllerLayout.Visibility = ViewStates.Gone;
                        ControllerLayout.SetZ(0);
                    };
                });
            }

        }

        private void DownloadThumbnail(string Url)
        {
            try
            {
                byte[] Data = request.DownloadData(Url);
                response = Encoding.UTF8.GetString(Data);
                string ImgURL="";
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    try
                    {
                        ImgURL = response.Substring(response.IndexOf("https://player2.seuseriado.com/player/p.php?"));
                        ImgURL = ImgURL.Substring(0, ImgURL.IndexOf("\""));
                        if (ImgURL.Contains("https://imagecurl.com/"))
                            ImgURL = ImgURL.Substring(ImgURL.IndexOf("https://imagecurl.com/"));
                        else
                            ImgURL = ImgURL.Substring(ImgURL.IndexOf("https://image.tmdb.org"));
                        ImgURL = ImgURL.Substring(0, ImgURL.IndexOf("&prott"));
                        Console.WriteLine(Url);
                        Console.WriteLine(ImgURL);
                    }
                    catch
                    {
                        RunOnUiThread(() =>
                        {
                            Toast.MakeText(this, "Erro. Não foi possivel carregar a thumbnail.", ToastLength.Short).Show();
                            Thumbnail.Visibility = ViewStates.Gone;
                        });
                    }
                };
                worker.RunWorkerAsync();
                worker.RunWorkerCompleted += (s, e) =>
                {
                    try
                    {
                        Picasso.With(this).Load(ImgURL).Into(Thumbnail);
                    }
                    catch { }
                    Loading.Visibility = ViewStates.Visible;
                    Loading.BringToFront();
                };
            }
            catch
            {
                Thumbnail.Visibility = ViewStates.Gone;
                Toast.MakeText(this, "Erro. Não foi possivel carregar a thumbnail.", ToastLength.Short).Show();
            }
        }

        private void GetVideo()
        {
            string URL;
            byte[] VideoPlayerData;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
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
                        RunOnUiThread(() =>
                        {
                            Loading.Visibility = ViewStates.Visible;
                            Loading.BringToFront();
                        });
                        Utils.Utils.GetSubtitle(VideoPlayerDataString, true);
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

                        RunOnUiThread(() =>
                        {
                            try
                            {
                                HD.SetImageResource(Resource.Drawable.baseline_hd_on);
                                player.SetDataSource(this, Android.Net.Uri.Parse(VideoPlayerDataString));
                                player.PrepareAsync();
                                player.SetOnPreparedListener(this);
                            }
                            catch { }
                        });
                    }

                }
                catch
                {
                    try
                    {
                        URL = response.Substring(response.IndexOf("https://player2.seuseriado.com/player/p.php?"));
                        URL = URL.Substring(0, URL.IndexOf("\" frameborder=\"0\""));
                        VideoPlayerData = request.DownloadData(URL);
                        VideoPlayerDataString = Encoding.UTF8.GetString(VideoPlayerData);

                        Utils.Utils.GetSubtitle(VideoPlayerDataString, false);

                        if (VideoPlayerDataString.Contains("Error"))
                        {
                            RunOnUiThread(() =>
                            {
                                Toast.MakeText(this, "Esse título não está disponivel no momento. Por favor, tente novamente mais tarde.", ToastLength.Long).Show();
                                this.Finish();
                            });
                        }
                        else
                        {
                            RunOnUiThread(() =>
                            {
                                Loading.Visibility = ViewStates.Visible;
                                Loading.BringToFront();
                            });
                            VideoPlayerDataString = VideoPlayerDataString.Substring(0, VideoPlayerDataString.IndexOf("','SD'"));
                            VideoPlayerDataString = VideoPlayerDataString.Substring(VideoPlayerDataString.IndexOf("Play('"));
                            VideoPlayerDataString = VideoPlayerDataString.Replace("Play('", "");
                            RunOnUiThread(() =>
                            {
                                try
                                {
                                    HD.SetImageResource(Resource.Drawable.baseline_hd_off);
                                    player.SetDataSource(this, Android.Net.Uri.Parse(VideoPlayerDataString));
                                    player.PrepareAsync();
                                    player.SetOnPreparedListener(this);
                                }
                                catch { }
                            });
                        }
                    }
                    catch
                    {
                        RunOnUiThread(() =>
                        {
                            Toast.MakeText(this, "Esse título não está disponivel no momento. Por favor, tente novamente mais tarde.", ToastLength.Long).Show();
                            this.Finish();
                        });
                    }
                }
            };
            worker.RunWorkerAsync();
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height) { }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            player = new MediaPlayer();
            player.SetDisplay(SSurfaceHolder);
            player.BufferingUpdate += Player_BufferingUpdate;
            player.Info += Player_Info;
            player.Completion += Player_Completion;

            if (IsOnline)
            {
                if (!Intent.Extras.GetBoolean("IsFromSearch"))
                {
                    IsFromSearch = false;
                    PTitle.Text = List.GetMainPageSeries.Series[Pos].Title;
                    DownloadThumbnail(Utils.Utils.VideoPageUrl(List.GetMainPageSeries.Series[Pos].Title));
                    if (List.GetMainPageSeries.Series[Pos].Downloading)
                        Download.SetImageResource(Resource.Drawable.baseline_cloud_download_on);

                    (Show, Season, Ep) = Utils.Utils.BreakFullTitleInParts(List.GetMainPageSeries.Series[Pos].Title);

                    if (List.GetMainPageSeries.Series[Pos].Title.Contains("LEGENDADO") || List.GetMainPageSeries.Series[Pos].Title.Contains("SEM LEGENDA"))
                        IsSubtitled = true;
                    BookmarkInMillisecond = Utils.Bookmark.GetBookmarkInMillisecond(Show, Ep, Season, IsSubtitled);
                }
                else
                {
                    IsFromSearch = true;
                    PTitle.Text = List.GetSearch.Search[Pos].Title.Replace("Online,", "Online ");
                    DownloadThumbnail(Utils.Utils.VideoPageUrl(List.GetSearch.Search[Pos].Title.Replace("Online,", "Online ")));
                    if (List.GetSearch.Search[Pos].Downloading)
                        Download.SetImageResource(Resource.Drawable.baseline_cloud_download_on);

                    (Show, Season, Ep) = Utils.Utils.BreakFullTitleInParts(List.GetSearch.Search[Pos].Title);

                    if (List.GetSearch.Search[Pos].Title.Contains("LEGENDADO") || List.GetSearch.Search[Pos].Title.Contains("SEM LEGENDA"))
                        IsSubtitled = true;
                    BookmarkInMillisecond = Utils.Bookmark.GetBookmarkInMillisecond(Show, Ep, Season, IsSubtitled);
                }
                Utils.Bookmark.InsertData(Show, Ep, Season, IsSubtitled);
                GetVideo();
            }
            else
            {
                var path = Intent.Extras.GetString("VideoPath");

                PTitle.Text = path.Split('/').Last();

                var subPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles", PTitle.Text + ".srt");

                Download.Visibility = ViewStates.Gone;
                HD.Visibility = ViewStates.Gone;
                if (current == Xamarin.Essentials.NetworkAccess.Internet)
                {
                    try
                    {
                        var requests = new WebClient();
                        var url = Utils.Utils.VideoPageUrl(PTitle.Text);
                        var Data = requests.DownloadData(url);
                        var responses = Encoding.UTF8.GetString(Data);

                        var URL = responses.Substring(responses.IndexOf("https://player2.seuseriado.com/player/p.php?"));
                        URL = URL.Substring(0, URL.IndexOf("\" frameborder=\"0\""));
                        var VideoPlayerData = requests.DownloadData(URL);
                        VideoPlayerDataString = Encoding.UTF8.GetString(VideoPlayerData);

                        var length = Utils.Utils.GetSubtitle(VideoPlayerDataString, false, true);

                        if (!System.IO.Directory.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles"))
                            System.IO.Directory.CreateDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Subtitles");

                        if (!System.IO.File.Exists(subPath))
                            System.IO.File.Create(subPath).Close();

                        if (length < new System.IO.FileInfo(subPath).Length || new System.IO.FileInfo(subPath).Length == 0)
                        {
                            Utils.Utils.GetSubtitle(VideoPlayerDataString, false);
                            System.IO.File.WriteAllText(subPath, System.IO.File.ReadAllText(filePath));
                        }
                    }
                    catch { }
                }
                filePath = subPath;

                LoadVideoFromStorage(path);
            }

        }

        private void Player_Completion(object sender, EventArgs e)
        {
            timer.Stop();
            timer.Enabled = false;
        }

        private void Player_Error(object sender, MediaPlayer.ErrorEventArgs e)
        {
            switch (e.What)
            {
                case MediaError.Io:
                case MediaError.TimedOut:
                case MediaError.ServerDied:
                    Loading.Visibility = ViewStates.Visible;
                    Loading.BringToFront();
                    Pause();
                    break;
            }
        }

        private void Player_Info(object sender, Android.Media.MediaPlayer.InfoEventArgs e)
        {
            switch (e.What)
            {
                case MediaInfo.BufferingEnd:
                case MediaInfo.VideoRenderingStart:
                    Thumbnail.Visibility = ViewStates.Gone;
                    Loading.Visibility = ViewStates.Gone;
                    break;
                case MediaInfo.BufferingStart:
                    Loading.Visibility = ViewStates.Visible;
                    Loading.BringToFront();
                    break;
            }
        }

        private void Player_BufferingUpdate(object sender, MediaPlayer.BufferingUpdateEventArgs e)
        {
            double ratio = e.Percent / 100.0;
            int bufferingLevel = (int)(player.Duration * ratio);
            SeekBarPlayer.SecondaryProgress = bufferingLevel;
        }

        public void SurfaceDestroyed(ISurfaceHolder holder) { }

        public void OnPrepared(MediaPlayer mp)
        {
            IsPrepared = true;
            Thumbnail.Visibility = ViewStates.Gone;
            Loading.Visibility = ViewStates.Gone;

            SetSubtitles();

            player.SetOnSeekCompleteListener(this);

            if(current == Xamarin.Essentials.NetworkAccess.Internet && HasAdFinished || !IsOnline)
                Start();

            EndTime.Text = StringForTime(player.Duration);
            SeekBarPlayer.Max = player.Duration;

            if (!IsOnline)
            {
                var listPos = Intent.Extras.GetInt("ListPos");
                BookmarkInMillisecond = List.GetDownloads.Series[listPos].Episodes[Pos].TimeWatched;

                Show = List.GetDownloads.Series[listPos].Show;
                Ep = List.GetDownloads.Series[listPos].Episodes[Pos].EP;
                Season = List.GetDownloads.Series[listPos].Episodes[Pos].ShowSeason;
                IsSubtitled = List.GetDownloads.Series[listPos].IsSubtitled;

                Utils.Bookmark.InsertData(Show, Ep, Season, IsSubtitled);
            }

            if(!IsFromSearch)
            {
                List.GetMainPageSeries.Series[Pos].Downloaded = Utils.Database.IsItemDownloaded(Season, Show, IsSubtitled, Ep);
                List.GetMainPageSeries.Series[Pos].AlreadyChecked = true;

                if (List.GetMainPageSeries.Series[Pos].Downloaded)
                {
                    Download.SetColorFilter(Color.Argb(255, 255, 255, 255));
                    Download.SetImageDrawable(GetDrawable(Resource.Drawable.baseline_cloud_done_24));
                }
            }
            else
            {
                if (List.GetSearch.Search[Pos].Downloaded)
                {
                    Download.SetColorFilter(Color.Argb(255, 255, 255, 255));
                    Download.SetImageDrawable(GetDrawable(Resource.Drawable.baseline_cloud_done_24));
                }
            }

            PTitle.Text = PTitle.Text.Replace("Online ", "");

            SeekTo((int)BookmarkInMillisecond);
            if (current == Xamarin.Essentials.NetworkAccess.Internet && HasAdFinished || !IsOnline)
            {
                timer.Interval = 10;
                timer.Enabled = true;
                timer.AutoReset = true;
            }

            if (!IsOnline)
                Start();

            if (!IsListenerEnabled)
            {
                player.Error += Player_Error;
                timer.Elapsed += Timer_Elapsed;
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SeekBarPlayer.Progress = player.CurrentPosition;
            CurrentTime.Text = StringForTime(player.CurrentPosition);
        }

        private void Pause()
        {
            try
            {
                Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
                PlayPause.SetImageResource(Resource.Drawable.baseline_play_arrow_24);
                player.Pause();
                timer.Stop();
            }
            catch { }
        }

        private void SeekTo(int pos)
        {
            player.SeekTo(pos);
            CurrentTime.Text = StringForTime(player.CurrentPosition);
            try
            {
                GetSubtitleIndex();
            }
            catch { }
        }

        private void Start()
        {
            if ((IsOnline && current == Xamarin.Essentials.NetworkAccess.Internet) || !IsOnline)
            {
                Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                PlayPause.SetImageResource(Resource.Drawable.baseline_pause_24);
                player.Start();
                timer.Interval = 10;
                timer.Start();
            }
        }

        private void SetSubtitles()
        {
            string CurrCapt = "";
            BackgroundWorker subWorker = new BackgroundWorker();
            subWorker.DoWork += (s, e) =>
            {
                if (System.IO.File.OpenRead(filePath).Length > 1)
                {
                    try
                    {
                        var parser = new SrtParser();
                        var fileStream = System.IO.File.OpenRead(filePath);
                        var items = parser.ParseStream(fileStream, Encoding.UTF8);

                        do
                        {
                            if (player.CurrentPosition >= items[x].StartTime && player.CurrentPosition <= items[x].EndTime)
                            {
                                CurrCapt = "";
                                foreach (var line in items[x].Lines)
                                {
                                    if(line.StartsWith(' '))
                                        CurrCapt += line;
                                    else
                                        CurrCapt += " " + line;
                                }
                                
                                if (ShowSubtitles && !string.IsNullOrWhiteSpace(CurrCapt))
                                {
                                    RunOnUiThread(() =>
                                    {
                                        Subtitle.BringToFront();
                                        Subtitle.Visibility = ViewStates.Visible;

                                        if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                                            Subtitle.TextFormatted = Android.Text.Html.FromHtml(CurrCapt, Android.Text.FromHtmlOptions.ModeLegacy);
                                        else
#pragma warning disable CS0618 // Type or member is obsolete
                                            Subtitle.TextFormatted = Android.Text.Html.FromHtml(CurrCapt);
#pragma warning restore CS0618 // Type or member is obsolete
                                    });
                                }
                                else
                                {
                                    RunOnUiThread(() => { Subtitle.Visibility = ViewStates.Gone; });
                                }
                                System.Threading.Thread.Sleep(items[x].EndTime - items[x].StartTime);
                                RunOnUiThread(() => { Subtitle.Visibility = ViewStates.Gone; });
                                x++;
                            }

                        } while (player.CurrentPosition < player.Duration);
                    }
                    catch { }
                  }
                };
            subWorker.RunWorkerAsync();
        }

        private string StringForTime(int timeMs)
        {
            int totalSeconds = timeMs / 1000;

            int seconds = totalSeconds % 60;
            int minutes = (totalSeconds / 60) % 60;
            int hours = totalSeconds / 3600;

            if (hours > 0)
            {
                return hours.ToString("D2")+":"+minutes.ToString("D2")+":" + seconds.ToString("D2");
            }
            else
            {
                return minutes.ToString("D2") + ":" + seconds.ToString("D2");
            }
        }

        public void OnSeekComplete(MediaPlayer mp)
        {
            System.Threading.Thread.Sleep(200);
            //Console.WriteLine(StringForTime(player.CurrentPosition));
            if (current == Xamarin.Essentials.NetworkAccess.Internet && HasAdFinished || !IsOnline)
                Start();
        }

        private void GetSubtitleIndex()
        {
            var parser = new SrtParser();
            var fileStream = System.IO.File.OpenRead(filePath);
            var items = parser.ParseStream(fileStream, Encoding.UTF8);

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].StartTime >= player.CurrentPosition && items[i].EndTime <= player.CurrentPosition)
                {
                    x = i;
                    break;
                }
                else
                {

                    if (items[i].StartTime > player.CurrentPosition)
                    {
                        x = i;
                        break;
                    }

                }

            }
        }

        private void StartAd()
        {
            AdLoader.Builder builder = new AdLoader.Builder(this, GetString(Resource.String.ad_unit_video_id));
            builder.ForUnifiedNativeAd(this);
            VideoOptions videoOptions = new VideoOptions.Builder().SetStartMuted(false).Build();
            NativeAdOptions adOptions = new NativeAdOptions.Builder().SetVideoOptions(videoOptions).Build();

            builder.WithNativeAdOptions(adOptions);

            AdLoader adLoader = builder.WithAdListener(new VideoAdListener(this)).Build();

            adLoader.LoadAd(new AdRequest.Builder().Build());
            //adLoader.LoadAd(new AdRequest.Builder().AddTestDevice("898E71950C45AB644AEFAC8F2CA3857D").Build());
        }

        public void OnUnifiedNativeAdLoaded(UnifiedNativeAd ad)
        {
            // You must call destroy on old ads when you are done with them,
            // otherwise you will have a memory leak.
            if (_NativeAd != null)
            {
                _NativeAd.Destroy();
            }
            _NativeAd = ad;
            
            UnifiedNativeAdView adView = (UnifiedNativeAdView)LayoutInflater.Inflate(Resource.Layout.ad_unified, null);
            PopulateUnifiedNativeAdView(ad, adView);
            _FrameLayout.RemoveAllViews();
            _FrameLayout.AddView(adView);
            _FrameLayout.BringToFront();
        }

        private void PopulateUnifiedNativeAdView(UnifiedNativeAd nativeAd, UnifiedNativeAdView adView)
        {
            // Set the media view. Media content will be automatically populated in the media view once
            // adView.setNativeAd() is called.
            MediaView mediaView = (MediaView)adView.FindViewById(Resource.Id.ad_media);
            adView.MediaView = mediaView;

            // Set other ad assets.
            adView.HeadlineView = adView.FindViewById(Resource.Id.ad_headline);
            adView.CallToActionView = adView.FindViewById(Resource.Id.ad_call_to_action);
            adView.IconView = adView.FindViewById(Resource.Id.ad_app_icon);
            adView.PriceView = adView.FindViewById(Resource.Id.ad_price);
            adView.AdvertiserView = adView.FindViewById(Resource.Id.ad_advertiser);

            var skipAd = (RelativeLayout)adView.FindViewById(Resource.Id.skip_ad);

            skipAd.Click += (s, e) =>
            {
                HasAdFinished = true;
                _NativeAd.Destroy();
                _FrameLayout.RemoveAllViews();
                Start();
            };

            // The headline is guaranteed to be in every UnifiedNativeAd.
            ((TextView)adView.HeadlineView).Text = nativeAd.Headline;

            // These assets aren't guaranteed to be in every UnifiedNativeAd, so it's important to
            // check before trying to display them.
            if (nativeAd.CallToAction == null)
            {
                adView.CallToActionView.Visibility = ViewStates.Invisible;
            }
            else
            {
                adView.CallToActionView.Visibility = ViewStates.Visible;
                ((Button)adView.CallToActionView).Text = nativeAd.CallToAction;
            }

            if (nativeAd.Icon == null)
            {
                adView.IconView.Visibility = ViewStates.Gone;
            }
            else
            {
                ((ImageView)adView.IconView).SetImageDrawable(nativeAd.Icon.Drawable);
                adView.IconView.Visibility = ViewStates.Visible;
            }

            if (nativeAd.Price == null)
            {
                adView.PriceView.Visibility = ViewStates.Invisible;
            }
            else
            {
                adView.PriceView.Visibility = ViewStates.Visible;
                ((TextView)adView.PriceView).Text = nativeAd.Price;
            }

            if (nativeAd.Advertiser == null)
            {
                adView.AdvertiserView.Visibility = ViewStates.Invisible;
            }
            else
            {
                ((TextView)adView.AdvertiserView).Text = nativeAd.Advertiser;
                adView.AdvertiserView.Visibility = ViewStates.Visible;
            }

            // This method tells the Google Mobile Ads SDK that you have finished populating your
            // native ad view with this native ad. The SDK will populate the adView's MediaView
            // with the media content from this native ad.
            adView.SetNativeAd(nativeAd);

            // Get the video controller for the ad. One will always be provided, even if the ad doesn't
            // have a video asset.
            VideoController vc = nativeAd.VideoController;

            // Updates the UI to say whether or not this ad has a video asset.
            if (vc.HasVideoContent)
            {
                vc.SetVideoLifecycleCallbacks(new VideoCallback(this));
                Loading.Visibility = ViewStates.Gone;
                skipAd.PostDelayed(() => skipAd.Visibility = ViewStates.Visible, 5000);
                Console.WriteLine(string.Format("Video status: Ad contains a {0:F2} video asset.",vc.AspectRatio));
            }
            else
            {
                Console.WriteLine("Video status: Ad does not contain a video asset.");
                HasAdFinished = true;
                _NativeAd.Destroy();
                _FrameLayout.RemoveAllViews();
                Start();
            }
        }

        public class VideoAdListener : AdListener
        {
            Player player;

            public VideoAdListener(Player pl)
            {
                player = pl;
            }

            public override void OnAdFailedToLoad(int p0)
            {
                player.HasAdFinished = true;
                player.Loading.Visibility = ViewStates.Visible;
                player.Loading.BringToFront();
                try { player._NativeAd.Destroy(); } catch { }
                player._FrameLayout.RemoveAllViews();
                player.Start();
                base.OnAdFailedToLoad(p0);
            }
        }

        public class VideoCallback : VideoController.VideoLifecycleCallbacks
        {
            Player player;

            public VideoCallback(Player pl)
            {
                player = pl;
            }

            public override void OnVideoStart()
            {
                player.Loading.Visibility = ViewStates.Gone;
                base.OnVideoStart();
            }

            public override void OnVideoEnd()
            {
                player.HasAdFinished = true;
                player._NativeAd.Destroy();
                player._FrameLayout.RemoveAllViews();
                if(player.player != null)
                    player.Start();
                base.OnVideoEnd();
            }
        }

    }
}