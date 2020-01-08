using System;
using System.Collections.Generic;
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

namespace SeuSeriado.Adapter
{
    class DownloadsAdapter : RecyclerView.Adapter
    {
        Context context;
        RecyclerView rec;

        public DownloadsAdapter(Context c, RecyclerView recyclerView)
        {
            context = c;
            rec = recyclerView;
        }

        public override int ItemCount { get { return List.GetDownloads.Series.Count; } }

        public class DownloadsAdapterHolder : RecyclerView.ViewHolder
        {
            public View mMain { get; set; }
            public ImageView Image { get; set; }
            public TextView Title { get; set; }
            public TextView EPMB { get; set; }

            public DownloadsAdapterHolder(View view) : base(view)
            {
                mMain = view;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View row = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.download_row, parent, false);
            ImageView image = row.FindViewById<ImageView>(Resource.Id.thumbnail_dpg);
            TextView title = row.FindViewById<TextView>(Resource.Id.title_dpg);
            TextView epmb = row.FindViewById<TextView>(Resource.Id.episodes_dpg);

            row.Click += Row_Click;

            image.SetScaleType(ImageView.ScaleType.CenterCrop);
            DownloadsAdapterHolder view = new DownloadsAdapterHolder(row) { Image = image, Title = title, EPMB = epmb };
            return view;
        }

        public override long GetItemId(int position) { return base.GetItemId(position); }

        public override async void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            DownloadsAdapterHolder Holder = holder as DownloadsAdapterHolder;

            List.GetDownloads.Series[position].TotalBytes = (List.GetDownloads.Series[position].Episodes.Sum(x => Convert.ToInt64(x.TotalBytesEP)));

            try
            {
                Holder.Image.SetImageBitmap(await GetBitmapFromStorageAsync(List.GetDownloads.Series[position].ShowThumb));
            }
            catch { }
            Holder.Title.Text = Regex.Replace(List.GetDownloads.Series[position].Show, @"\b([a-z])", m => m.Value.ToUpper());

            if (List.GetDownloads.Series[position].Episodes.Where(row => row.Duration > 0).Count() == 1)
                Holder.EPMB.Text = string.Format("{0} Episódio | {1}", List.GetDownloads.Series[position].Episodes.Where(row => row.Duration > 0).Count(), Utils.Utils.Size(List.GetDownloads.Series[position].TotalBytes));
            else
                Holder.EPMB.Text = string.Format("{0} Episódios | {1}", List.GetDownloads.Series[position].Episodes.Where(row => row.Duration > 0).Count(), Utils.Utils.Size(List.GetDownloads.Series[position].TotalBytes));

        }

        private void Row_Click(object sender, EventArgs e)
        {
            int pos = rec.GetChildLayoutPosition((View)sender);
            Intent intent = new Intent(context, typeof(Activities.DetailedDownloads));
            intent.PutExtra("ItemPos", pos);
            context.StartActivity(intent);
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