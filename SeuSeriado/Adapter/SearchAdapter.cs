using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Square.Picasso;

namespace SeuSeriado.Adapter
{
    class SearchAdapter : RecyclerView.Adapter
    {
        Context context;
        RecyclerView rec;

        public SearchAdapter(Context c, RecyclerView recyclerView)
        {
            context = c;
            rec = recyclerView;
        }

        public override int ItemCount => List.GetSearch.Search.Count;

        public override long GetItemId(int position) { return base.GetItemId(position); }

        public class SearchAdapterHolder : RecyclerView.ViewHolder
        {
            public View mMain { get; set; }
            public ImageView Image { get; set; }
            public TextView Title { get; set; }

            public SearchAdapterHolder(View view) : base(view)
            {
                mMain = view;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View row = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.search_row, parent, false);
            ImageView image = row.FindViewById<ImageView>(Resource.Id.thumbnail_spg);
            TextView title = row.FindViewById<TextView>(Resource.Id.title_spg);

            row.Click += Row_Click;

            image.SetScaleType(ImageView.ScaleType.CenterCrop);
            SearchAdapterHolder view = new SearchAdapterHolder(row) { Image = image, Title = title };
            return view;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            SearchAdapterHolder Holder = holder as SearchAdapterHolder;
            Holder.Title.Text = List.GetSearch.Search[position].Title.Replace("ª Temporada Episódio ", "x").Replace("Online,", "").Replace("(SEASON FINALE)", "");
            Picasso.With(context).Load(List.GetSearch.Search[position].ImgLink).Into(Holder.Image, new Action(async () => { await Task.Run(() => List.GetSearch.Search[position].IMG64 = Base64.EncodeToString(Utils.Utils.GetImageBytes(Holder.Image.Drawable), Base64Flags.UrlSafe)); }), new Action(() => { }));
        }

        private void Row_Click(object sender, EventArgs e)
        {
            View v = (View)sender;
            Intent intent = new Intent(v.Context, typeof(Activities.Player));
            intent.PutExtra("Pos", rec.GetChildAdapterPosition(v));
            intent.PutExtra("IsOnline", true);
            intent.PutExtra("IsFromSearch", true);
            context.StartActivity(intent);
        }
    }
}