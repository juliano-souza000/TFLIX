using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using Square.Picasso;
using TFlix.Adapter;
using TFlix.Decoration;
using TFlix.Dialog;
using TFlix.Event;
using TFlix.List;

namespace TFlix.Fragments
{
    class MainPageFragment : Android.Support.V4.App.Fragment
    {
        private RecyclerView Series;
        private RecyclerView KeepWatchingSeries;
        public ProgressBar Loading;
        private MainPage_SeriesAdapter adapter;
        private KeepWatchingAdapter _KeepWatchingAdapter;
        private SwipeRefreshLayout _SwipeRefreshLayout;
        private NestedScrollView NestedScroll;
        public RelativeLayout KeepWatchingLayout;
        private FrameLayout Frame;

        //TopShowLayout
        private ImageView TopShowImage;
        private ImageView TopShowInfoBt;
        private TextView TopShowTitle;
        private RelativeLayout WatchButton;
        private RelativeLayout TopShowLayout;

        private RelativeLayout KeepWatchingHeaderLayout;
        private RelativeLayout RecentlyUpdateHeaderLayout;

        private int page = 1;
        private bool IsDownloading;

        private bool Internet;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (MainActivity.current == Xamarin.Essentials.NetworkAccess.Internet)
            {
                Internet = true;
                return inflater.Inflate(Resource.Layout.mainpage, null);
            }
            else
            {
                Internet = false;
                return inflater.Inflate(Resource.Layout.nointernet_layout, null);
            }
        }

        public override void OnDestroyView()
        {
            Activity.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LayoutStable;
            Activity.Window.SetStatusBarColor(Color.Black);
            base.OnDestroyView();
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            if (Internet)
            {
                Series = (RecyclerView)view.FindViewById(Resource.Id.popular_series);
                KeepWatchingSeries = (RecyclerView)view.FindViewById(Resource.Id.keepwatching);
                Loading = (ProgressBar)view.FindViewById(Resource.Id.popular_series_loading);
                _SwipeRefreshLayout = (SwipeRefreshLayout)view.FindViewById(Resource.Id.swipeRefreshLayout);
                NestedScroll = (NestedScrollView)view.FindViewById(Resource.Id.nestedscroll);
                KeepWatchingLayout = (RelativeLayout)view.FindViewById(Resource.Id.keepwatching_lt);
                Frame = (FrameLayout)view.FindViewById(Resource.Id.error_frame);

                //TopShow Layout
                TopShowImage = (ImageView)view.FindViewById(Resource.Id.topshow_image);
                TopShowInfoBt = (ImageView)view.FindViewById(Resource.Id.watch_info_image);
                TopShowTitle = (TextView)view.FindViewById(Resource.Id.watch_title);
                WatchButton = (RelativeLayout)view.FindViewById(Resource.Id.watch_button);
                TopShowLayout = (RelativeLayout)view.FindViewById(Resource.Id.topshow_lt);

                KeepWatchingHeaderLayout = (RelativeLayout)view.FindViewById(Resource.Id.keepwatching_header_layout);
                RecentlyUpdateHeaderLayout = (RelativeLayout)view.FindViewById(Resource.Id.popular_series_header_layout);

                adapter = new MainPage_SeriesAdapter(Context, Series, this);
                _KeepWatchingAdapter = new KeepWatchingAdapter(Context, KeepWatchingSeries, this);

                TopShowLayout.LayoutParameters.Height = (int)(Context.Resources.DisplayMetrics.HeightPixels * 0.7f);
                TopShowLayout.RequestLayout();
                TopShowImage.RequestFocus();

                Loading.Visibility = ViewStates.Visible;

                try
                {
                    Utils.Bookmark.ReadDB();
                    KeepWatchingSeries.SetLayoutManager(new LinearLayoutManager(Context, 0, false));
                    KeepWatchingSeries.AddItemDecoration(new MarginItemDecoration((int)Utils.Utils.DPToPX(Context, 10)));

                    if (KeepWatchingList.KeepWatching != null && KeepWatchingList.KeepWatching.Count > 0)
                    {
                        RecentlyUpdateHeaderLayout.LayoutParameters.Height = ViewGroup.LayoutParams.WrapContent;
                        RecentlyUpdateHeaderLayout.RequestLayout();
                        KeepWatchingSeries.Post(() => KeepWatchingSeries.SetAdapter(_KeepWatchingAdapter));
                    }
                    else
                    {
                        KeepWatchingLayout.Visibility = ViewStates.Gone;
                    }

                }
                catch { }

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, ex) =>
                {
                    if (GetMainPageSeries.Series == null)
                        GetMainPageSeries.Series = JsonConvert.DeserializeObject<List<MainPageSeries>>(Utils.Utils.Download(page));
                };
                worker.RunWorkerAsync();

                Series.SetLayoutManager(new LinearLayoutManager(Context));
                Series.SetItemViewCacheSize(7);
                //Series.DrawingCacheEnabled = true;
                //Series.DrawingCacheQuality = DrawingCacheQuality.High;

                Series.AddItemDecoration(new DividerItemDecoration(Series.Context, DividerItemDecoration.Vertical));

                worker.RunWorkerCompleted += (s, ex) =>
                {
                    //Console.WriteLine("Done!");
                    if (GetMainPageSeries.Series != null)
                    {
                        string LanguageIdent;
                        Activity.Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(SystemUiFlags.LayoutStable | SystemUiFlags.LayoutFullscreen);
                        Activity.Window.SetStatusBarColor(Color.Transparent);
                        Frame.Visibility = ViewStates.Gone;
                        if (KeepWatchingList.KeepWatching != null && KeepWatchingList.KeepWatching.Count > 0)
                        {
                            KeepWatchingLayout.Visibility = ViewStates.Visible;
                            KeepWatchingSeries.Visibility = ViewStates.Visible;
                        }
                        TopShowLayout.Visibility = ViewStates.Visible;
                        RecentlyUpdateHeaderLayout.Visibility = ViewStates.Visible;
                        Series.Visibility = ViewStates.Visible;

                        var (Show, Season, Ep) = Utils.Utils.BreakFullTitleInParts(GetMainPageSeries.TopShow.Title);
                        if (GetMainPageSeries.TopShow.Title.ToLower().Contains("legendado") || GetMainPageSeries.TopShow.Title.ToLower().Contains("sem legenda"))
                            LanguageIdent = "Legendado";
                        else if (GetMainPageSeries.TopShow.Title.ToLower().Contains("nacional"))
                            LanguageIdent = "Nacional";
                        else
                            LanguageIdent = "Dublado";
                        
                        TopShowImage.Post(() => Picasso.With(Context).Load(GetMainPageSeries.TopShow.ImgLink).Into(TopShowImage));

                        TopShowTitle.Text = string.Format("{0} ● Temporada {1} ● Episódio {2} ● {3}", Show, Season, Ep, LanguageIdent);

                        Series.Post(() => Series.SetAdapter(adapter));
                    }
                    else
                    {
                        LayoutInflater li = LayoutInflater.From(Context);
                        View notfound = li.Inflate(Resource.Layout.mainpage_error, Frame, false);
                        Frame.AddView(notfound);
                        Frame.BringToFront();
                        Frame.Visibility = ViewStates.Visible;
                        KeepWatchingLayout.Visibility = ViewStates.Gone;
                        KeepWatchingSeries.Visibility = ViewStates.Gone;
                        TopShowLayout.Visibility = ViewStates.Gone;
                        RecentlyUpdateHeaderLayout.Visibility = ViewStates.Gone;
                        Series.Visibility = ViewStates.Gone;
                    }
                    Loading.Visibility = ViewStates.Gone;
                };
                if (TopShowLayout.Visibility == ViewStates.Visible)
                {
                    WatchButton.Click += WatchButton_Click;
                    TopShowInfoBt.Click += TopShowInfoBt_Click;
                }
                NestedScroll.ScrollChange += NestedScroll_ScrollChange;
                _SwipeRefreshLayout.Refresh += _SwipeRefreshLayout_Refresh;
            }
        }

        public override void OnResume()
        {
            Activity.RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait;
            Utils.Bookmark.ReadDB();
            if (List.KeepWatchingList.KeepWatching.Count > 0)
                KeepWatchingLayout.Visibility = ViewStates.Visible;
            _KeepWatchingAdapter.NotifyDataSetChanged();
            base.OnResume();
        }

        private void TopShowInfoBt_Click(object sender, EventArgs e)
        {
            try
            {
                Loading.Visibility = ViewStates.Visible;
                Loading.BringToFront();
                var fragmentMan = Activity.SupportFragmentManager.BeginTransaction();

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, ex) =>
                {
                    if (string.IsNullOrWhiteSpace(List.GetMainPageSeries.TopShow.Synopsis))
                        List.GetMainPageSeries.TopShow.Synopsis = Utils.Utils.GetSynopsis(Utils.Utils.VideoPageUrl(List.GetMainPageSeries.TopShow.Title));
                };
                worker.RunWorkerAsync();
                worker.RunWorkerCompleted += (s, ex) =>
                {
                    Synopsis synopsisDialog = new Synopsis();
                    synopsisDialog.SynopsisTitleString = List.GetMainPageSeries.TopShow.Title;
                    synopsisDialog.SynopsisContentString = List.GetMainPageSeries.TopShow.Synopsis;
                    synopsisDialog.Show(fragmentMan, "SDF");
                    Loading.Visibility = ViewStates.Gone;
                };
            }
            catch { }
        }

        private void WatchButton_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Context, typeof(Activities.Player));
            intent.PutExtra("IsFromTopShow", true);
            intent.PutExtra("IsOnline", true);
            StartActivity(intent);
        }

        private void _SwipeRefreshLayout_Refresh(object sender, EventArgs e)
        {
            bool error = false;
            page = 1;
            Frame.RemoveAllViews();

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, ex) =>
            {
                try { GetMainPageSeries.Series = JsonConvert.DeserializeObject<List<MainPageSeries>>(Utils.Utils.Download(page)); } catch { error = true; }
            };
            worker.RunWorkerAsync();

            worker.RunWorkerCompleted += (s, ex) =>
            {
                if (GetMainPageSeries.Series != null && !error)
                {
                    Frame.Visibility = ViewStates.Gone;
                    if (KeepWatchingList.KeepWatching != null && KeepWatchingList.KeepWatching.Count > 0)
                    {
                        KeepWatchingLayout.Visibility = ViewStates.Visible;
                        KeepWatchingSeries.Visibility = ViewStates.Visible;
                    }
                    TopShowLayout.Visibility = ViewStates.Visible;
                    RecentlyUpdateHeaderLayout.Visibility = ViewStates.Visible;
                    Series.Visibility = ViewStates.Visible;
                    adapter.NotifyDataSetChanged();
                }
                else
                {
                    LayoutInflater li = LayoutInflater.From(Context);
                    View notfound = li.Inflate(Resource.Layout.mainpage_error, Frame, false);
                    Frame.AddView(notfound);
                    Frame.BringToFront();
                    Frame.Visibility = ViewStates.Visible;
                    KeepWatchingLayout.Visibility = ViewStates.Gone;
                    KeepWatchingSeries.Visibility = ViewStates.Gone;
                    TopShowLayout.Visibility = ViewStates.Gone;
                    RecentlyUpdateHeaderLayout.Visibility = ViewStates.Gone;
                    Series.Visibility = ViewStates.Gone;
                }
                _SwipeRefreshLayout.Refreshing = false;
            };
        }

        private void NestedScroll_ScrollChange(object sender, NestedScrollView.ScrollChangeEventArgs e)
        {
            var max = Context.Resources.DisplayMetrics.HeightPixels * 0.7f;

            int percentage = (int)((e.ScrollY / max) * 100);

            var color = new Color(ContextCompat.GetColor(Context, Resource.Color.appBackgroundColor));

            color.A = (byte)((percentage / 100) * 255);
            Activity.Window.SetStatusBarColor(color);

            if (!NestedScroll.CanScrollVertically(1) && !IsDownloading && GetMainPageSeries.Series != null && Internet)
            {
                Loading.BringToFront();
                Loading.Visibility = ViewStates.Visible;
                try
                {
                    int posStarted = adapter.ItemCount;
                    List<MainPageSeries> newItems = null;

                    page++;
                    IsDownloading = true;

                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += (s, ex) =>
                    {
                        newItems = JsonConvert.DeserializeObject<List<MainPageSeries>>(Utils.Utils.Download(page));
                        GetMainPageSeries.Series.AddRange(newItems);
                    };
                    worker.RunWorkerAsync();
                    worker.RunWorkerCompleted += (s, ex) =>
                    {
                        Loading.Visibility = ViewStates.Gone;
                        IsDownloading = false;
                        if(newItems != null)
                            adapter.NotifyItemRangeInserted(posStarted, newItems.Count);
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Message: {0} Stacktrace: {1}", ex.Message, ex.StackTrace);
                    page--;
                    Loading.Visibility = ViewStates.Gone;
                }

            }
        }
    }
}