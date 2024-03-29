﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Nio;
using TFlix.Dialog;
using Square.Picasso;
using Android.Gms.Ads;

namespace TFlix.Adapter
{
    class MainPage_SeriesAdapter : RecyclerView.Adapter
    {
        private Context context;
        private RecyclerView rec;
        private Fragments.MainPageFragment MainFragm;

        private const int AdRow = 0;
        private const int ShowRow = 1;

        public MainPage_SeriesAdapter(Context c, RecyclerView recyclerView, Fragments.MainPageFragment frag)
        {
            context = c;
            rec = recyclerView;
            MainFragm = frag;
        }

        public override int ItemCount { get { return List.GetMainPageSeries.Series.Count + ((int)List.GetMainPageSeries.Series.Count / 7); } }

        public override int GetItemViewType(int position)
        {
            if (position != 0)
                if (position % 7 == 0)
                    return AdRow;

            return ShowRow;
        }

        public class MainPage_SeriesAdapterHolder : RecyclerView.ViewHolder
        {
            public View mMain { get; set; }
            public ImageView Image { get; set; }
            public ImageView Synopsis { get; set; }
            public ImageView Download { get; set; }
            public TextView Title { get; set; }
            public TextView Updated { get; set; }

            public MainPage_SeriesAdapterHolder(View view) : base(view)
            {
                mMain = view;
            }
        }

        public class MainPage_SeriesAdsHolder : RecyclerView.ViewHolder
        {
            public View mMain { get; set; }
            public AdView Ads { get; set; }

            public MainPage_SeriesAdsHolder(View view) : base(view)
            {
                mMain = view;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == ShowRow)
            {
                View row = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.mainpage_row, parent, false);
                ImageView image = row.FindViewById<ImageView>(Resource.Id.thumbnail_mpg);
                ImageView synopsis = row.FindViewById<ImageView>(Resource.Id.info_mpg);
                ImageView download = row.FindViewById<ImageView>(Resource.Id.download_mpg);
                TextView title = row.FindViewById<TextView>(Resource.Id.title_mpg);
                TextView updated = row.FindViewById<TextView>(Resource.Id.updated_mpg);

                row.Click += Row_Click;
                download.Click += Download_Click;
                synopsis.Click += Synopsis_Click;

                image.SetScaleType(ImageView.ScaleType.CenterCrop);
                MainPage_SeriesAdapterHolder view = new MainPage_SeriesAdapterHolder(row) { Image = image, Title = title, Updated = updated, Synopsis = synopsis, Download = download };
                return view;
            }
            else
            {
                View row = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.adviewlayout, parent, false);
                AdView adView = row.FindViewById<AdView>(Resource.Id.adView);

                MainPage_SeriesAdsHolder view = new MainPage_SeriesAdsHolder(row) { Ads = adView };
                return view;
            }
        }

        public override long GetItemId(int position) { return base.GetItemId(position); }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder.ItemViewType == ShowRow)
            {
                if (position != 0)
                    position -= (int)position / 7;
                MainPage_SeriesAdapterHolder Holder = holder as MainPage_SeriesAdapterHolder;
                bool isSubtitled;

                var (Show, Season, Ep) = Utils.Utils.BreakFullTitleInParts(List.GetMainPageSeries.Series[position].Title);

                Holder.Title.Text = string.Format("{0} {1}x{2}", Show, Season, Ep);

                Holder.Updated.Text = List.GetMainPageSeries.Series[position].Update;

                Holder.Image.Post(() => Picasso.With(context).Load(List.GetMainPageSeries.Series[position].ImgLink).Into(Holder.Image));

                if (List.GetMainPageSeries.Series[position].Title.ToUpper().Contains("LEGENDADO") || List.GetMainPageSeries.Series[position].Title.ToUpper().Contains("SEM LEGENDA"))
                {
                    isSubtitled = true;
                    Holder.Title.Text += " Legendado";
                }
                else if(List.GetMainPageSeries.Series[position].Title.ToUpper().Contains("NACIONAL"))
                {
                    isSubtitled = false;
                    Holder.Title.Text += " Nacional";
                }
                else
                {
                    isSubtitled = false;
                    Holder.Title.Text += " Dublado";
                }

                if (!List.GetMainPageSeries.Series[position].AlreadyChecked)
                {
                    List.GetMainPageSeries.Series[position].Downloaded = Utils.Database.IsItemDownloaded(Season, Show, isSubtitled, Ep);
                    List.GetMainPageSeries.Series[position].AlreadyChecked = true;
                }

                if (List.GetMainPageSeries.Series[position].Downloaded)
                {
                    Holder.Download.SetColorFilter(Android.Graphics.Color.Argb(255, 255, 255, 255));
                    Holder.Download.SetImageDrawable(context.GetDrawable(Resource.Drawable.baseline_cloud_done_24));
                }
                else
                {
                    Holder.Download.SetColorFilter(null);

                    //Utils.Database.ReadDB();
                    try
                    {
                        var listIndex = List.GetDownloads.Series.FindIndex(x => x.Show == Show && x.IsSubtitled == isSubtitled);
                        var epIndex = List.GetDownloads.Series[listIndex].Episodes.FindIndex(x => x.ShowSeason == Season && x.EP == Ep);

                        List.GetMainPageSeries.Series[position].Downloading = List.GetDownloads.Series[listIndex].Episodes[epIndex].IsDownloading;
                    }
                    catch
                    {
                        List.GetMainPageSeries.Series[position].Downloading = false;
                    }

                    if (List.GetMainPageSeries.Series[position].Downloading)
                        Holder.Download.SetImageDrawable(context.GetDrawable(Resource.Drawable.baseline_cloud_download_on));
                    else
                        Holder.Download.SetImageDrawable(context.GetDrawable(Resource.Drawable.baseline_cloud_download_24));
                }
            }
            else
            {
                MainPage_SeriesAdsHolder Holder = holder as MainPage_SeriesAdsHolder;

                //var adRequest = new AdRequest.Builder().AddTestDevice("898E71950C45AB644AEFAC8F2CA3857D").Build();
                var adRequest = new AdRequest.Builder().Build();
                Holder.Ads.Post(() => Holder.Ads.LoadAd(adRequest));
            }
        }

        private void Synopsis_Click(object sender, EventArgs e)
        {
            try
            {
                View v = (View)sender;
                MainFragm.Loading.BringToFront();
                MainFragm.Loading.Visibility = ViewStates.Visible;
                int Pos = rec.GetChildAdapterPosition((View)v.Parent.Parent);
                if (Pos != 0)
                    Pos -= (int)Pos / 7;
                var fragmentMan = ((Android.Support.V7.App.AppCompatActivity)context).SupportFragmentManager.BeginTransaction();

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, ex) =>
                {
                    if (string.IsNullOrWhiteSpace(List.GetMainPageSeries.Series[Pos].Synopsis))
                        List.GetMainPageSeries.Series[Pos].Synopsis = Utils.Utils.GetSynopsis(Utils.Utils.VideoPageUrl(List.GetMainPageSeries.Series[Pos].Title));
                };
                worker.RunWorkerAsync();
                worker.RunWorkerCompleted += (s, ex) =>
                {
                    Synopsis synopsisDialog = new Synopsis();
                    synopsisDialog.SynopsisTitleString = List.GetMainPageSeries.Series[Pos].Title;
                    synopsisDialog.SynopsisContentString = List.GetMainPageSeries.Series[Pos].Synopsis;
                    synopsisDialog.Show(fragmentMan, "SDF");
                    MainFragm.Loading.Visibility = ViewStates.Gone;
                };
            }
            catch { }
        }

        private void Download_Click(object sender, EventArgs e)
        {
            try
            {
                View v = (View)sender;
                int Pos = rec.GetChildAdapterPosition((View)v.Parent.Parent);
                if (Pos != 0)
                    Pos -= (int)Pos / 7;

                if (!List.GetMainPageSeries.Series[Pos].Downloading && !List.GetMainPageSeries.Series[Pos].Downloaded)
                {
                    var holder = rec.FindViewHolderForAdapterPosition(Pos);
                    var dowload = holder.ItemView.FindViewById<ImageView>(Resource.Id.download_mpg);

                    Task.Run(() => Utils.Utils.DownloadVideo(context, List.GetMainPageSeries.Series[Pos].Title, true));
                    dowload.SetImageDrawable(context.GetDrawable(Resource.Drawable.baseline_cloud_download_on));
                }
            }
            catch { }
        }

        private void Row_Click(object sender, EventArgs e)
        {
            View v = (View)sender;
            int position = rec.GetChildAdapterPosition(v);
            if (position != 0)
                position -= (int)position / 7;

            Intent intent = new Intent(v.Context, typeof(Activities.Player));
            intent.PutExtra("Pos", position);
            intent.PutExtra("IsOnline", true);
            context.StartActivity(intent);
        }

    }
}