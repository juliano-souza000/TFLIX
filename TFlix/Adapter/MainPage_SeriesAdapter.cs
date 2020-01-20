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

namespace TFlix.Adapter
{
    class MainPage_SeriesAdapter : RecyclerView.Adapter
    {
        Context context;
        RecyclerView rec;

        public MainPage_SeriesAdapter(Context c, RecyclerView recyclerView)
        {
            context = c;
            rec = recyclerView;
        }

        public override int ItemCount { get { return List.GetMainPageSeries.Series.Count; } }

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

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
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

        public override long GetItemId(int position) { return base.GetItemId(position); }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            MainPage_SeriesAdapterHolder Holder = holder as MainPage_SeriesAdapterHolder;
            bool isSubtitled;

            var (Show, Season, Ep) = Utils.Utils.BreakFullTitleInParts(List.GetMainPageSeries.Series[position].Title);

            Holder.Title.Text = string.Format("{0} {1}x{2}", Show, Season, Ep);

            Holder.Updated.Text = List.GetMainPageSeries.Series[position].Update;

            Picasso.With(context).Load(List.GetMainPageSeries.Series[position].ImgLink).Into(Holder.Image, new Action(async () => { await Task.Run(() => List.GetMainPageSeries.Series[position].IMG64 = Base64.EncodeToString(Utils.Utils.GetImageBytes(Holder.Image.Drawable), Base64Flags.UrlSafe)); }), new Action(() => { }));

            if (List.GetMainPageSeries.Series[position].Title.Contains("LEGENDADO") || List.GetMainPageSeries.Series[position].Title.Contains("SEM LEGENDA"))
                isSubtitled = true;
            else
                isSubtitled = false;

            if (isSubtitled)
                Holder.Title.Text += " Legendado";
            else
                Holder.Title.Text += " Dublado";

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

                Utils.Database.ReadDB();
                try
                {
                    var listIndex = List.GetDownloads.Series.FindIndex(x => x.Show == Show && x.IsSubtitled == isSubtitled);
                    var epIndex = List.GetDownloads.Series[listIndex].Episodes.FindIndex(x => x.ShowSeason == Season && x.EP == Ep);

                    List.GetMainPageSeries.Series[position].Downloading = List.GetDownloads.Series[listIndex].Episodes[epIndex].downloadInfo.IsDownloading;
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

        private void Synopsis_Click(object sender, EventArgs e)
        {
            try
            {
                View v = (View)sender;
                int Pos = rec.GetChildAdapterPosition((View)v.Parent.Parent);
                var fragmentMan = ((Activity)context).FragmentManager.BeginTransaction();

                if (string.IsNullOrWhiteSpace(List.GetMainPageSeries.Series[Pos].Synopsis))
                    List.GetMainPageSeries.Series[Pos].Synopsis = Utils.Utils.GetSynopsis(Utils.Utils.VideoPageUrl(List.GetMainPageSeries.Series[Pos].Title));

                Synopsis synopsisDialog = new Synopsis();
                synopsisDialog.SynopsisTitleString = List.GetMainPageSeries.Series[Pos].Title;
                synopsisDialog.SynopsisContentString = List.GetMainPageSeries.Series[Pos].Synopsis;
                synopsisDialog.Show(fragmentMan, "SDF");
            }
            catch { }
        }

        private void Download_Click(object sender, EventArgs e)
        {
            try
            {
                View v = (View)sender;
                int Pos = rec.GetChildAdapterPosition((View)v.Parent.Parent);

                if (!List.GetMainPageSeries.Series[Pos].Downloading && !List.GetMainPageSeries.Series[Pos].Downloaded)
                {
                    var holder = rec.FindViewHolderForAdapterPosition(Pos);
                    var dowload = holder.ItemView.FindViewById<ImageView>(Resource.Id.download_mpg);

                    Utils.Utils.DownloadVideo(false, Pos, context);
                    dowload.SetImageDrawable(context.GetDrawable(Resource.Drawable.baseline_cloud_download_on));
                }
            }
            catch { }
        }

        private void Row_Click(object sender, EventArgs e)
        {
            View v = (View)sender;
            Intent intent = new Intent(v.Context, typeof(Activities.Player));
            intent.PutExtra("Pos", rec.GetChildAdapterPosition(v));
            intent.PutExtra("IsOnline", true);
            context.StartActivity(intent);
        }

    }
}