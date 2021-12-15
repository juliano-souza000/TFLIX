using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Square.Picasso;
using TFlix.Dialog;
using TFlix.Views;
using PopupMenu = Android.Widget.PopupMenu;

namespace TFlix.Adapter
{
    class KeepWatchingAdapter : RecyclerView.Adapter
    {
        private Context context;
        private RecyclerView recycler;
        private Fragments.MainPageFragment MainFragm;

        public KeepWatchingAdapter(Context contx, RecyclerView rec, Fragments.MainPageFragment frag)
        {
            context = contx;
            recycler = rec;
            MainFragm = frag;
        }

        public override int ItemCount { get { return List.KeepWatchingList.KeepWatching.Count; } }

        public class KeepWatchingAdapterHolder : RecyclerView.ViewHolder
        {
            public View mMain { get; set; }
            public ImageView Image { get; set; }
            public ProgressBar Watched { get; set; }
            public TextView Title { get; set; }

            public KeepWatchingAdapterHolder(View view) : base(view)
            {
                mMain = view;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View row = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.keep_watching_row, parent, false);
            ImageView image = row.FindViewById<ImageView>(Resource.Id.image_kpg);
            ImageView info = row.FindViewById<ImageView>(Resource.Id.info_kpg);
            ProgressBar watched = row.FindViewById<ProgressBar>(Resource.Id.progress_kpg);
            TextView title = row.FindViewById<TextView>(Resource.Id.title_kpg);

            row.Click += Row_Click;
            info.Click += Info_Click;

            KeepWatchingAdapterHolder view = new KeepWatchingAdapterHolder(row) { Image = image, Title = title, Watched = watched};
            return view;
        }

        public override long GetItemId(int position) { return base.GetItemId(position); }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            KeepWatchingAdapterHolder Holder = holder as KeepWatchingAdapterHolder;

            Console.WriteLine(List.KeepWatchingList.KeepWatching[position].Fulltitle);
            Holder.Title.Text = string.Format("T{0}:E{1}", List.KeepWatchingList.KeepWatching[position].Season, List.KeepWatchingList.KeepWatching[position].Ep);
            if (List.KeepWatchingList.KeepWatching[position].IsSubtitled)
                Holder.Title.Text += " LEG";
            else
                Holder.Title.Text += " DUB";

            Holder.Watched.Max = (int)List.KeepWatchingList.KeepWatching[position].Duration;
            Holder.Watched.Progress = (int)List.KeepWatchingList.KeepWatching[position].TimeWatched;

            if (List.KeepWatchingList.KeepWatching[position].IsOnline)
                Holder.Image.Post(() => Picasso.With(context).Load(List.KeepWatchingList.KeepWatching[position].Thumb).Into(Holder.Image));
            else
                Holder.Image.Post(() => Picasso.With(context).Load(new Java.IO.File(List.KeepWatchingList.KeepWatching[position].Thumb)).Into(Holder.Image));
        }

        private void Info_Click(object sender, EventArgs e)
        {
            var row = (View)(((View)sender).Parent.Parent);
            var pos = recycler.GetChildAdapterPosition(row);

            try
            {
                Context wrapper = new ContextThemeWrapper(context, Resource.Style.PopupMenuTheme);
                PopupMenu popup = new PopupMenu(wrapper, (View)((View)sender).Parent);


                popup.MenuItemClick += (s, ex) =>
                {
                    switch (ex.Item.ItemId)
                    {
                        case Resource.Id.keepwatching_synopsis:
                            MainFragm.Loading.Visibility = ViewStates.Visible;
                            MainFragm.Loading.BringToFront();
                            var fragmentMan = ((Android.Support.V7.App.AppCompatActivity)context).SupportFragmentManager.BeginTransaction();

                            BackgroundWorker worker = new BackgroundWorker();
                            worker.DoWork += (s, ex) =>
                            {
                                if (string.IsNullOrWhiteSpace(List.KeepWatchingList.KeepWatching[pos].Synopsis))
                                    List.KeepWatchingList.KeepWatching[pos].Synopsis = Utils.Utils.GetSynopsis(Utils.Utils.VideoPageUrl(List.KeepWatchingList.KeepWatching[pos].Fulltitle));
                            };
                            worker.RunWorkerAsync();
                            worker.RunWorkerCompleted += (s, ex) =>
                            {
                                Synopsis synopsisDialog = new Synopsis();
                                synopsisDialog.SynopsisTitleString = List.KeepWatchingList.KeepWatching[pos].Fulltitle;
                                synopsisDialog.SynopsisContentString = List.KeepWatchingList.KeepWatching[pos].Synopsis;
                                synopsisDialog.Show(fragmentMan, "SDF");
                                MainFragm.Loading.Visibility = ViewStates.Gone;
                            };
                            break;
                        case Resource.Id.delete_from_keepwatching:
                            Utils.Bookmark.DeleteItem(List.KeepWatchingList.KeepWatching[pos].IsSubtitled, List.KeepWatchingList.KeepWatching[pos].Show, List.KeepWatchingList.KeepWatching[pos].Ep, List.KeepWatchingList.KeepWatching[pos].Season);
                            NotifyItemRemoved(pos);
                            if (List.KeepWatchingList.KeepWatching.Count == 0)
                                MainFragm.KeepWatchingLayout.Visibility = ViewStates.Gone;

                            break;
                    }
                };
                popup.Inflate(Resource.Menu.keepwatching_menu);
                popup.Show();
            }
            catch { }
        }

        private void Row_Click(object sender, EventArgs e)
        {
            int ListPos;

            var row = ((View)sender);
            var pos = recycler.GetChildAdapterPosition(row);
            var isOnline = List.KeepWatchingList.KeepWatching[pos].IsOnline;

            Intent intent = new Intent(row.Context, typeof(Activities.Player));
            intent.PutExtra("IsOnline", List.KeepWatchingList.KeepWatching[pos].IsOnline);
            intent.PutExtra("IsFromKeepWatching", true);

            if (!isOnline)
            {
                Utils.Database.ReadDB();
                ListPos = List.GetDownloads.Series.FindIndex(x => x.Show == List.KeepWatchingList.KeepWatching[pos].Show && x.IsSubtitled == List.KeepWatchingList.KeepWatching[pos].IsSubtitled);
                pos = List.GetDownloads.Series[ListPos].Episodes.FindIndex(x => x.ShowSeason == List.KeepWatchingList.KeepWatching[pos].Season && x.EP == List.KeepWatchingList.KeepWatching[pos].Ep);
                
                intent.PutExtra("ListPos", ListPos);
                intent.PutExtra("VideoPath", Utils.Database.GetVideoPath(List.GetDownloads.Series[ListPos].IsSubtitled, List.GetDownloads.Series[ListPos].Show, List.GetDownloads.Series[ListPos].Episodes[pos].EP, List.GetDownloads.Series[ListPos].Episodes[pos].ShowSeason));
            }

            intent.PutExtra("Pos", pos);
            context.StartActivity(intent);
        }
    }
}