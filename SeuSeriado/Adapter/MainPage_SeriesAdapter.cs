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
using Square.Picasso;

namespace SeuSeriado.Adapter
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
            public TextView Title { get; set; }

            public MainPage_SeriesAdapterHolder(View view) : base(view)
            {
                mMain = view;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View row = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.mainpage_adapter, parent, false);
            ImageView image = row.FindViewById<ImageView>(Resource.Id.thumbnail_mpg);
            TextView title = row.FindViewById<TextView>(Resource.Id.title_mpg);
            row.LongClick += (s, e) =>
            {
                title.RequestFocus();
            };
            row.Click += Row_Click;
            image.SetScaleType(ImageView.ScaleType.CenterCrop);
            MainPage_SeriesAdapterHolder view = new MainPage_SeriesAdapterHolder(row) { Image = image, Title = title };
            return view;
        }

        public override long GetItemId(int position) { return base.GetItemId(position); }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            MainPage_SeriesAdapterHolder Holder = holder as MainPage_SeriesAdapterHolder;
            var title = List.GetMainPageSeries.Series[position].Title.Replace("Online,", "");
            title = title.Replace("Online ", "");
            title = title.Replace("ª Temporada Episódio ", "x");
            title = title.Replace(" LEGENDADO", "LEG");
            title = title.Replace(" DUBLADO", "DUB");
            Holder.Title.Text = title;
            Picasso.With(context).Load(List.GetMainPageSeries.Series[position].ImgLink).Into(Holder.Image, new Action(async () => { await Task.Run(()=>List.GetMainPageSeries.Series[position].IMG64 = Base64.EncodeToString(Utils.Utils.GetImageBytes(Holder.Image.Drawable), Base64Flags.UrlSafe)); }) , new Action(() => { }));

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