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
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using ByteSizeLib;
using Java.Lang;
using TFlix.Interface;
using Square.Picasso;

namespace TFlix.Adapter
{
    class DownloadsAdapter : RecyclerView.Adapter
    {
        private Context context;
        private RecyclerView rec;

        private IOnUserSelectItems _IOnUserSelectItems;

        private bool _IsUserSelecting;
        public bool IsUserSelecting
        {
            get { return _IsUserSelecting; }

            set
            {
                _IsUserSelecting = value;
                if (value == false)
                {
                    for (int i = 0; i < List.GetDownloads.Series.Count; i++)
                    {
                        for (int x = 0; x < List.GetDownloads.Series[i].Episodes.Count; x++)
                        {
                            List.GetDownloads.Series[i].Episodes[x].IsSelected = false;
                        }
                        List.GetDownloads.Series[i].IsSelected = false;
                    }
                }
            }
        }

        public DownloadsAdapter(Context c, RecyclerView recyclerView, IOnUserSelectItems onUserSelectItems)
        {
            context = c;
            rec = recyclerView;
            _IOnUserSelectItems = onUserSelectItems;
        }

        public override int ItemCount { get { return List.GetDownloads.Series.Count; } }

        public class DownloadsAdapterHolder : RecyclerView.ViewHolder
        {
            public View mMain { get; set; }
            public ImageView Image { get; set; }
            public ImageView SeeMore { get; set; }
            public TextView Title { get; set; }
            public TextView EPMB { get; set; }
            public TextView Type { get; set; }
            public CheckBox Selector { get; set; }

            public DownloadsAdapterHolder(View view) : base(view)
            {
                mMain = view;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View row = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.download_row, parent, false);
            ImageView image = row.FindViewById<ImageView>(Resource.Id.thumbnail_dpg);
            ImageView seeMore = row.FindViewById<ImageView>(Resource.Id.see_more);
            TextView title = row.FindViewById<TextView>(Resource.Id.title_dpg);
            TextView epmb = row.FindViewById<TextView>(Resource.Id.episodes_dpg);
            TextView type = row.FindViewById<TextView>(Resource.Id.type_dpg);
            CheckBox selector = row.FindViewById<CheckBox>(Resource.Id.download_selector);

            row.Click += Row_Click;
            row.LongClick += Row_LongClick;

            image.SetScaleType(ImageView.ScaleType.CenterCrop);
            DownloadsAdapterHolder view = new DownloadsAdapterHolder(row) { Image = image, Title = title, EPMB = epmb, Type = type, Selector = selector, SeeMore = seeMore };
            return view;
        }

        public override long GetItemId(int position) { return base.GetItemId(position); }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            DownloadsAdapterHolder Holder = holder as DownloadsAdapterHolder;

            List.GetDownloads.Series[position].TotalBytes = (List.GetDownloads.Series[position].Episodes.Sum(x => Convert.ToInt64(x.Bytes)));

            if (IsUserSelecting)
            {
                if (Holder.Selector.Visibility == ViewStates.Visible)
                    Holder.Selector.Checked = List.GetDownloads.Series[position].IsSelected;
                Holder.Selector.Visibility = ViewStates.Visible;
                Holder.SeeMore.Visibility = ViewStates.Gone;
            }
            else
            {
                Holder.Selector.Visibility = ViewStates.Gone;
                Holder.SeeMore.Visibility = ViewStates.Visible;
                Holder.Selector.Checked = false;
            }

            if (List.GetDownloads.Series[position].IsSelected)
            {
                Holder.Image.LayoutParameters.Width = (int)Utils.Utils.DPToPX(context, 70);
                Holder.Image.LayoutParameters.Height = (int)Utils.Utils.DPToPX(context, 70);
            }
            else
            {
                Holder.Image.LayoutParameters.Width = (int)Utils.Utils.DPToPX(context, 100);
                Holder.Image.LayoutParameters.Height = (int)Utils.Utils.DPToPX(context, 100);
            }
            Holder.Image.RequestLayout();

            if (List.GetDownloads.Series[position].IsSubtitled)
                Holder.Type.Text = "Legendado";
            else
                Holder.Type.Text = "Dublado";

            if (!string.IsNullOrWhiteSpace(List.GetDownloads.Series[position].ShowThumb))
            {
                try
                {
                    //Holder.Image.SetImageBitmap(await GetBitmapFromStorageAsync(List.GetDownloads.Series[position].ShowThumb));
                    Picasso.With(context).Load(new Java.IO.File(List.GetDownloads.Series[position].ShowThumb)).Into(Holder.Image);
                }
                catch { }
            }

            Holder.Title.Text = Regex.Replace(List.GetDownloads.Series[position].Show, @"\b([a-z])", m => m.Value.ToUpper());

            if (List.GetDownloads.Series[position].Episodes.Where(row => row.Duration > 0).Count() == 1)
                Holder.EPMB.Text = string.Format("{0} Episódio | {1}", List.GetDownloads.Series[position].Episodes.Where(row => row.EP >= 0).Count(), Utils.Utils.Size(List.GetDownloads.Series[position].TotalBytes));
            else
                Holder.EPMB.Text = string.Format("{0} Episódios | {1}", List.GetDownloads.Series[position].Episodes.Where(row => row.EP >= 0).Count(), Utils.Utils.Size(List.GetDownloads.Series[position].TotalBytes));

        }

        public override void OnViewRecycled(Java.Lang.Object holder)
        {
            try
            {
                DownloadsAdapterHolder Holder = holder as DownloadsAdapterHolder;
                Holder.Image.SetImageDrawable(null);
                Picasso.With(context).CancelRequest(Holder.Image);
            }
            catch { }
            base.OnViewRecycled(holder);
        }

        private void Row_LongClick(object sender, View.LongClickEventArgs e)
        {
            try
            {
                int Pos = rec.GetChildLayoutPosition((View)sender);
                RecyclerView.ViewHolder holder = rec.FindViewHolderForAdapterPosition(Pos);
                CheckBox selector = holder.ItemView.FindViewById<CheckBox>(Resource.Id.download_selector);

                _IOnUserSelectItems.IsUserSelecting(true);
                if (selector.Visibility != ViewStates.Visible)
                {
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += (s, ex) =>
                    {
                        do
                        {
                            holder = rec.FindViewHolderForLayoutPosition(Pos);
                            selector = holder.ItemView.FindViewById<CheckBox>(Resource.Id.download_selector);
                        }
                        while (selector.Visibility != ViewStates.Visible);
                    };
                    worker.RunWorkerAsync();
                    worker.RunWorkerCompleted += (s, ex) =>
                    {
                        try
                        {
                            holder = rec.FindViewHolderForLayoutPosition(Pos);
                            var row = holder.ItemView;
                            row.SoundEffectsEnabled = false;
                            row.PerformClick();
                        }
                        catch { }
                    };
                }
                else
                {
                    var row = holder.ItemView;
                    row.SoundEffectsEnabled = false;
                    row.PerformClick();
                }
                //Console.WriteLine(List.GetDownloads.Series[ListPos].Episodes[Pos].TotalBytesEP);
            }
            catch { }
        }

        private void Row_Click(object sender, EventArgs e)
        {
            int Pos = rec.GetChildLayoutPosition((View)sender);
            //Console.WriteLine("Pos: " + Pos);
            if (!IsUserSelecting)
            {
                _IOnUserSelectItems.IsUserSelecting(false);
                Intent intent = new Intent(context, typeof(Activities.DetailedDownloads));
                intent.PutExtra("ItemPos", Pos);
                context.StartActivity(intent);
            }
            else
            {
                RecyclerView.ViewHolder holder = rec.FindViewHolderForLayoutPosition(Pos);
                CheckBox selector = holder.ItemView.FindViewById<CheckBox>(Resource.Id.download_selector);
                ImageView thumb = holder.ItemView.FindViewById<ImageView>(Resource.Id.thumbnail_dpg);
                ImageView seeMore = holder.ItemView.FindViewById<ImageView>(Resource.Id.see_more);

                seeMore.Visibility = ViewStates.Gone;

                List.GetDownloads.Series[Pos].IsSelected = !List.GetDownloads.Series[Pos].IsSelected;

                for (int x = 0; x < List.GetDownloads.Series[Pos].Episodes.Count; x++)
                {
                    List.GetDownloads.Series[Pos].Episodes[x].IsSelected = List.GetDownloads.Series[Pos].IsSelected;
                }

                _IOnUserSelectItems.IsUserSelecting(true);
                selector.PerformClick();

                if (List.GetDownloads.Series[Pos].IsSelected)
                {
                    thumb.LayoutParameters.Width = (int)Utils.Utils.DPToPX(context, 70);
                    thumb.LayoutParameters.Height = (int)Utils.Utils.DPToPX(context, 70);
                }
                else
                {
                    thumb.LayoutParameters.Width = (int)Utils.Utils.DPToPX(context, 100);
                    thumb.LayoutParameters.Height = (int)Utils.Utils.DPToPX(context, 100);
                }
                thumb.RequestLayout();
            }
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