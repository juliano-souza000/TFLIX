using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using ByteSizeLib;
using SeuSeriado.Activities;

namespace SeuSeriado.Adapter
{
    class EpisodesAdapter : RecyclerView.Adapter
    {
        private Context context;
        private RecyclerView rec;

        private IOnUserSelectItems _IOnUserSelectItems;

        private const int ShowHeader = 1;
        private const int ShowRow = 0;

        private int ListPos;
        private bool _IsUserSelecting;
        public bool IsUserSelecting
        {
            get { return _IsUserSelecting; }

            set
            {
                _IsUserSelecting = value;
                if (value == false)
                {
                    for (int i = 0; i < List.GetDownloads.Series[ListPos].Episodes.Count; i++)
                    {
                        List.GetDownloads.Series[ListPos].Episodes[i].IsSelected = false;
                    }
                }
            }
        }

        public EpisodesAdapter(Context c, RecyclerView recyclerView, int posi, IOnUserSelectItems onUserSelectItems)
        {
            context = c;
            rec = recyclerView;
            ListPos = posi;
            _IOnUserSelectItems = onUserSelectItems;
        }

        public override int ItemCount { get { return List.GetDownloads.Series[ListPos].Episodes.Count; } }

        public class EpisodesAdapterHolder : RecyclerView.ViewHolder
        {
            public View mMain { get; set; }
            public ImageView Image { get; set; }
            public TextView Title { get; set; }
            public TextView EPMB { get; set; }
            public CheckBox Selector { get; set; }

            public EpisodesAdapterHolder(View view) : base(view)
            {
                mMain = view;
            }
        }

        public class EpisodesSeasonAdapterHolder : RecyclerView.ViewHolder
        {
            public View mMain { get; set; }
            public TextView Season { get; set; }

            public EpisodesSeasonAdapterHolder(View view) : base(view)
            {
                mMain = view;
            }
        }

        public override int GetItemViewType(int position)
        {
            if (List.GetDownloads.Series[ListPos].Episodes[position].ShowSeason != 0 && List.GetDownloads.Series[ListPos].Episodes[position].TotalBytesEP == 0 && List.GetDownloads.Series[ListPos].Episodes[position].EP == 0 && List.GetDownloads.Series[ListPos].Episodes[position].Duration == 0)
                return ShowHeader;
            else
                return ShowRow;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == ShowHeader)
            {
                View row = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.download_series_showrow, parent, false);
                TextView header = row.FindViewById<TextView>(Resource.Id.downloads_showheader);

                EpisodesSeasonAdapterHolder view = new EpisodesSeasonAdapterHolder(row) { Season = header };
                return view;
            }
            else
            {
                View row = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.download_series_row, parent, false);
                ImageView image = row.FindViewById<ImageView>(Resource.Id.thumbnail_dpg_ep);
                TextView title = row.FindViewById<TextView>(Resource.Id.title_dpg_ep);
                TextView epmb = row.FindViewById<TextView>(Resource.Id.episodes_dpg_ep);
                CheckBox selector = row.FindViewById<CheckBox>(Resource.Id.episodes_dpg_selector);

                selector.Clickable = false;

                row.Click += Row_Click;
                row.LongClick += Row_LongClick;
                image.SetScaleType(ImageView.ScaleType.CenterCrop);
                EpisodesAdapterHolder view = new EpisodesAdapterHolder(row) { Image = image, Title = title, EPMB = epmb, Selector = selector };
                return view;
            }
        }

        public override long GetItemId(int position) { return base.GetItemId(position); }

        public override async void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder.ItemViewType == ShowHeader)
            {
                EpisodesSeasonAdapterHolder Holder = holder as EpisodesSeasonAdapterHolder;
                Holder.Season.Text = string.Format("Temporada {0}", List.GetDownloads.Series[ListPos].Episodes[position].ShowSeason);
            }
            else
            {
                EpisodesAdapterHolder Holder = holder as EpisodesAdapterHolder;

                if (IsUserSelecting)
                    Holder.Selector.Visibility = ViewStates.Visible;
                else
                {
                    Holder.Selector.Visibility = ViewStates.Gone;
                    Holder.Selector.Checked = false;
                }

                try
                {
                    Holder.Image.SetImageBitmap(await GetBitmapFromStorageAsync(List.GetDownloads.Series[ListPos].Episodes[position].EpThumb));
                }
                catch { }

                try
                {
                    Holder.Title.Text = string.Format("{0}.", List.GetDownloads.Series[ListPos].Episodes[position].EP);
                    Holder.EPMB.Text = string.Format("{0} | {1}", GetDuration(List.GetDownloads.Series[ListPos].Episodes[position].Duration), Utils.Utils.Size(List.GetDownloads.Series[ListPos].Episodes[position].TotalBytesEP));
                }
                catch { }
            }

            Console.WriteLine(string.Format("Temporada {0}", List.GetDownloads.Series[ListPos].Episodes[position].ShowSeason));
        }

        private void Row_LongClick(object sender, View.LongClickEventArgs e)
        {
            int Pos = rec.GetChildLayoutPosition((View)sender);
            RecyclerView.ViewHolder holder = rec.FindViewHolderForLayoutPosition(Pos);
            CheckBox selector = holder.ItemView.FindViewById<CheckBox>(Resource.Id.episodes_dpg_selector);

            if (!List.GetDownloads.Series[ListPos].Episodes[Pos].IsSelected)
                List.GetDownloads.Series[ListPos].Episodes[Pos].IsSelected = true;
            else
                List.GetDownloads.Series[ListPos].Episodes[Pos].IsSelected = false;

            _IOnUserSelectItems.IsUserSelecting(true);
            if (selector.Visibility != ViewStates.Visible)
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, ex) =>
                {
                    do { } while (selector.Visibility != ViewStates.Visible);
                };
                worker.RunWorkerAsync();
                worker.RunWorkerCompleted += (s, ex) =>
                {
                    selector.PerformClick();
                };
            }else
                selector.PerformClick();
            Console.WriteLine(List.GetDownloads.Series[ListPos].Episodes[Pos].TotalBytesEP);
        }

        private void Row_Click(object sender, EventArgs e)
        {
            int Pos = rec.GetChildLayoutPosition((View)sender);

            if (!IsUserSelecting)
            {
                _IOnUserSelectItems.IsUserSelecting(false);
                Intent intent = new Intent(context, typeof(Player));
                intent.PutExtra("Pos", ListPos);

                intent.PutExtra("VideoPath", Utils.Database.GetVideoPath(List.GetDownloads.Series[ListPos].IsSubtitled, List.GetDownloads.Series[ListPos].Show, List.GetDownloads.Series[ListPos].Episodes[Pos].EP, List.GetDownloads.Series[ListPos].Episodes[Pos].ShowSeason));
                intent.PutExtra("IsOnline", false);
                context.StartActivity(intent);
            }
            else
            {
                RecyclerView.ViewHolder holder = rec.FindViewHolderForLayoutPosition(Pos);
                CheckBox selector = holder.ItemView.FindViewById<CheckBox>(Resource.Id.episodes_dpg_selector);
                if (!List.GetDownloads.Series[ListPos].Episodes[Pos].IsSelected)
                    List.GetDownloads.Series[ListPos].Episodes[Pos].IsSelected = true;
                else
                    List.GetDownloads.Series[ListPos].Episodes[Pos].IsSelected = false;

                _IOnUserSelectItems.IsUserSelecting(true);
                selector.PerformClick();
            }
        }
        private string GetDuration(long timeInMillisec)
        {
           
            long sec = timeInMillisec / 1000;
            long min = sec / 60;

            if (min > 60)
            {
                return string.Format("{0} h", min/60);
            }
            else
                return string.Format("{0} min", min);
        }

        private async Task<Bitmap> GetBitmapFromStorageAsync(string path)
        {
            Bitmap bmp;
            byte[] data = File.ReadAllBytes(path);

            bmp = await BitmapFactory.DecodeByteArrayAsync(data, 0, data.Length);

            return bmp;
        }
    }

}