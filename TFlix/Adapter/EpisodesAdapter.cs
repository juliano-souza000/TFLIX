using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

using TFlix.Activities;
using TFlix.Interface;
using TFlix.Services;
using Square.Picasso;
using PopupMenu = Android.Widget.PopupMenu;
using Android.Views.Animations;

namespace TFlix.Adapter
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
            public ImageView Done { get; set; }
            public ImageView Play { get; set; }
            public ImageView PauseResumeDownload { get; set; }
            public ImageView Error { get; set; }
            public ImageView LoadingProgress { get; set; }
            public TextView Title { get; set; }
            public TextView EPMB { get; set; }
            public CheckBox Selector { get; set; }
            public ProgressBar DownloadProgress { get; set; }
            public ProgressBar TimeWatched { get; set; }
            public RelativeLayout ImageHolder { get; set; }


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
                ImageView done = row.FindViewById<ImageView>(Resource.Id.episodes_dpg_ep_dp);
                ImageView play = row.FindViewById<ImageView>(Resource.Id.play_dpg_ep);
                ImageView pauseResumeDownload = row.FindViewById<ImageView>(Resource.Id.pauseresume_dpg_ep_dp);
                ImageView error = row.FindViewById<ImageView>(Resource.Id.error_dpg_ep_dp);
                ImageView loadingProgress = row.FindViewById<ImageView>(Resource.Id.episodes_dpg_ep_loading_progressbar);
                TextView title = row.FindViewById<TextView>(Resource.Id.title_dpg_ep);
                TextView epmb = row.FindViewById<TextView>(Resource.Id.episodes_dpg_ep);
                CheckBox selector = row.FindViewById<CheckBox>(Resource.Id.episodes_dpg_selector);
                ProgressBar downloadProgress = row.FindViewById<ProgressBar>(Resource.Id.download_progressbar); 
                ProgressBar timeWatched = row.FindViewById<ProgressBar>(Resource.Id.timewatched_progressbar);
                RelativeLayout imageHolder = row.FindViewById<RelativeLayout>(Resource.Id.imageframe_dpg_ep); 
                selector.Clickable = false;

                row.Click += Row_Click;
                row.LongClick += Row_LongClick;
                pauseResumeDownload.Click += PauseResumeDownload_Click;
                downloadProgress.Click += PauseResumeDownload_Click;
                error.Click += Error_Click;

                image.SetScaleType(ImageView.ScaleType.CenterCrop);
                EpisodesAdapterHolder view = new EpisodesAdapterHolder(row) { Image = image, Title = title, EPMB = epmb, Selector = selector, DownloadProgress = downloadProgress, Done = done, TimeWatched = timeWatched, Play = play, ImageHolder = imageHolder, PauseResumeDownload = pauseResumeDownload, Error = error, LoadingProgress = loadingProgress};
                return view;
            }
        }

        public override long GetItemId(int position) { return base.GetItemId(position); }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
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
                {
                    if (Holder.Selector.Visibility == ViewStates.Visible)
                        Holder.Selector.Checked = List.GetDownloads.Series[ListPos].Episodes[position].IsSelected;
                    Holder.Selector.Visibility = ViewStates.Visible;
                }
                else
                {
                    Holder.Selector.Visibility = ViewStates.Gone;
                    Holder.Selector.Checked = false;
                }

                if (List.GetDownloads.Series[ListPos].Episodes[position].IsSelected)
                {
                    Holder.Image.LayoutParameters.Width = (int)Utils.Utils.DPToPX(context, 120);
                    Holder.Image.LayoutParameters.Height = (int)Utils.Utils.DPToPX(context, 70);
                }
                else
                {
                    Holder.Image.LayoutParameters.Width = (int)Utils.Utils.DPToPX(context, 150);
                    Holder.Image.LayoutParameters.Height = (int)Utils.Utils.DPToPX(context, 100);
                }
                Holder.Image.RequestLayout();
                Console.WriteLine("EP: {0} Season: {1} IsDownloading: {2}", List.GetDownloads.Series[ListPos].Episodes[position].EP, List.GetDownloads.Series[ListPos].Episodes[position].ShowSeason, List.GetDownloads.Series[ListPos].Episodes[position].IsDownloading);

                if (List.GetDownloads.Series[ListPos].Episodes[position].IsDownloading && !IsUserSelecting && !List.GetDownloads.Series[ListPos].Episodes[position].IsPaused)
                {
                    Holder.DownloadProgress.Visibility = ViewStates.Visible;
                    Holder.DownloadProgress.Progress = List.GetDownloads.Series[ListPos].Episodes[position].Progress;
                    Holder.PauseResumeDownload.Visibility = ViewStates.Gone;
                }
                else
                {
                    Holder.DownloadProgress.Visibility = ViewStates.Gone;
                }

                if (List.GetDownloads.Series[ListPos].Episodes[position].IsPaused)
                {
                    Holder.DownloadProgress.Visibility = ViewStates.Gone;
                    Holder.PauseResumeDownload.Visibility = ViewStates.Visible;
                }
                

                if (List.GetDownloads.Series[ListPos].Episodes[position].Progress == 100 && !IsUserSelecting)
                {
                    var res = context.GetDrawable(Resource.Drawable.baseline_mobile_friendly_24);
                    Holder.Done.SetImageDrawable(res);
                    Holder.Done.Visibility = ViewStates.Visible;
                }
                else
                {
                    Holder.Done.Visibility = ViewStates.Gone;
                }

                if (List.GetDownloads.Series[ListPos].Episodes[position].Progress == 100)
                {
                    Holder.Play.Visibility = ViewStates.Visible;
                    Holder.DownloadProgress.Visibility = ViewStates.Gone;
                }
                else
                {
                    Holder.Play.Visibility = ViewStates.Gone;

                    if (!List.GetDownloads.Series[ListPos].Episodes[position].IsDownloading && !IsUserSelecting)
                        Holder.Error.Visibility = ViewStates.Visible;
                    else
                        Holder.Error.Visibility = ViewStates.Gone;
                }

                try
                {
                    var queueIndex = List.Queue.DownloadQueue.FindIndex(x => x.FullTitle == List.GetDownloads.Series[ListPos].Episodes[position].FullTitle);
                    if (!List.GetDownloads.Series[ListPos].Episodes[position].IsDownloading && !IsUserSelecting && queueIndex >= 0)
                    {
                        Holder.LoadingProgress.SetColorFilter(Android.Graphics.Color.ParseColor("#0171F0"));
                        Holder.LoadingProgress.SetImageDrawable(Android.Support.V4.Content.Res.ResourcesCompat.GetDrawable(context.Resources, Resource.Drawable.baseline_loading_waiting, null));
                        Holder.Error.Visibility = ViewStates.Gone;
                        Holder.LoadingProgress.Visibility = ViewStates.Visible;
                    }
                }
                catch { }

                Holder.TimeWatched.Max = (int)(List.GetDownloads.Series[ListPos].Episodes[position].Duration);
                Holder.TimeWatched.Progress = (int)List.GetDownloads.Series[ListPos].Episodes[position].TimeWatched;

                if (!string.IsNullOrWhiteSpace(List.GetDownloads.Series[ListPos].Episodes[position].EpThumb))
                {
                    try
                    {
                        //Holder.Image.SetImageBitmap(await GetBitmapFromStorageAsync(List.GetDownloads.Series[ListPos].Episodes[position].EpThumb));
                        Picasso.With(context).Load(new Java.IO.File(List.GetDownloads.Series[ListPos].Episodes[position].EpThumb)).Into(Holder.Image);
                        //Console.WriteLine("POS: {0} EP{1}: Path:{2}", position, List.GetDownloads.Series[ListPos].Episodes[position].EP, List.GetDownloads.Series[ListPos].Episodes[position].EpThumb);
                    }
                    catch { }
                }

                try
                {
                    Holder.Title.Text = string.Format("{0}. Episódio {0}", List.GetDownloads.Series[ListPos].Episodes[position].EP);
                    Holder.EPMB.Text = string.Format("{0} | {1}", GetDuration(List.GetDownloads.Series[ListPos].Episodes[position].Duration), Utils.Utils.Size(List.GetDownloads.Series[ListPos].Episodes[position].Bytes));
                }
                catch { }
            }
        }

        public override void OnViewRecycled(Java.Lang.Object holder)
        {
            try
            {
                EpisodesAdapterHolder Holder = holder as EpisodesAdapterHolder;
                Holder.Image.SetImageDrawable(null);
                Picasso.With(context).CancelRequest(Holder.Image);
            }
            catch { }
            base.OnViewRecycled(holder);
        }


        private void Error_Click(object sender, EventArgs e)
        {
            View v = (View)sender;
            Context wrapper = new ContextThemeWrapper(context, Resource.Style.PopupMenuTheme);
            PopupMenu popup = new PopupMenu(wrapper, v);
            int pos = rec.GetChildAdapterPosition((View)v.Parent);

            RecyclerView.ViewHolder holder = rec.FindViewHolderForAdapterPosition(pos);
            ImageView loadingProgress = holder.ItemView.FindViewById<ImageView>(Resource.Id.episodes_dpg_ep_loading_progressbar);
            ImageView error = holder.ItemView.FindViewById<ImageView>(Resource.Id.error_dpg_ep_dp);

            popup.MenuItemClick += (s, ex) =>
            {
                switch (ex.Item.ItemId)
                {
                    case Resource.Id.download:
                        Task.Run(() => Utils.Utils.DownloadVideo(List.GetDownloads.Series[ListPos].Episodes[pos].FullTitle, List.GetDownloads.Series[ListPos].Show, List.GetDownloads.Series[ListPos].ShowThumb, List.GetDownloads.Series[ListPos].Episodes[pos].EpThumb, List.GetDownloads.Series[ListPos].Episodes[pos].EP, List.GetDownloads.Series[ListPos].Episodes[pos].ShowSeason, context));

                        RotateAnimation rotate = new RotateAnimation(0, 360, Dimension.RelativeToSelf, 0.48f, Dimension.RelativeToSelf, 0.48f);
                        rotate.Duration = 1000;
                        rotate.SetInterpolator(context, Resource.Animation.linear_interpolator);

                        rotate.AnimationEnd += (se, eex) =>
                        {
                            loadingProgress.SetColorFilter(Android.Graphics.Color.ParseColor("#0171F0"));
                            loadingProgress.SetImageDrawable(Android.Support.V4.Content.Res.ResourcesCompat.GetDrawable(context.Resources, Resource.Drawable.baseline_loading_waiting, null));
                        };

                        loadingProgress.SetColorFilter(Android.Graphics.Color.Argb(255, 255, 255, 255));
                        error.Visibility = ViewStates.Gone;
                        loadingProgress.Visibility = ViewStates.Visible;
                        loadingProgress.StartAnimation(rotate);

                        break;
                    case Resource.Id.delete:
                        Utils.Database.DeleteItem(List.GetDownloads.Series[ListPos].IsSubtitled, List.GetDownloads.Series[ListPos].Show, List.GetDownloads.Series[ListPos].Episodes[pos].EP, List.GetDownloads.Series[ListPos].Episodes[pos].ShowSeason);
                        DetailedDownloads.ReloadDataset();
                        break;
                }
                
            };
            popup.Inflate(Resource.Menu.error_menu);
            popup.Show();
        }

        private void PauseResumeDownload_Click(object sender, EventArgs e)
        {
            View v = (View)sender;
            try
            {
                int Pos = rec.GetChildAdapterPosition((View)v.Parent);
                bool pause;
                RecyclerView.ViewHolder holder = rec.FindViewHolderForAdapterPosition(Pos);
                ImageView pauseResumeDownload = holder.ItemView.FindViewById<ImageView>(Resource.Id.pauseresume_dpg_ep_dp);
                ProgressBar downloadProgress = holder.ItemView.FindViewById<ProgressBar>(Resource.Id.download_progressbar);

                if (List.GetDownloads.Series[ListPos].Episodes[Pos].IsPaused)
                {
                    pauseResumeDownload.Visibility = ViewStates.Gone;
                    downloadProgress.Visibility = ViewStates.Visible;
                    downloadProgress.Progress = List.GetDownloads.Series[ListPos].Episodes[Pos].Progress;
                    pause = false;
                }
                else
                {
                    downloadProgress.Visibility = ViewStates.Gone;
                    pauseResumeDownload.Visibility = ViewStates.Visible;
                    pause = true;
                }

                Task.Run(() => DownloadFilesService.OnPauseDownload.IOnPauseDownload(false));

                List.GetDownloads.Series[ListPos].Episodes[Pos].IsPaused = pause;
            }
            catch (Exception ex)
            { 
                Console.WriteLine("Line 328 Episodes Adapter: " + ex.Message);
            }
        }

        private void Row_LongClick(object sender, View.LongClickEventArgs e)
        {

            int Pos = rec.GetChildLayoutPosition((View)sender);
            RecyclerView.ViewHolder holder = rec.FindViewHolderForAdapterPosition(Pos);
            CheckBox selector = holder.ItemView.FindViewById<CheckBox>(Resource.Id.episodes_dpg_selector);

            _IOnUserSelectItems.IsUserSelecting(true);
            if (selector.Visibility != ViewStates.Visible)
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, ex) =>
                {
                    do
                    {
                        holder = rec.FindViewHolderForLayoutPosition(Pos);
                        selector = holder.ItemView.FindViewById<CheckBox>(Resource.Id.episodes_dpg_selector);
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

        private void Row_Click(object sender, EventArgs e)
        {
            int Pos = rec.GetChildLayoutPosition((View)sender);
            //Console.WriteLine("Pos: " + Pos);
            if (!IsUserSelecting)
            {
                _IOnUserSelectItems.IsUserSelecting(false);
                if (List.GetDownloads.Series[ListPos].Episodes[Pos].Progress == 100)
                {
                    Intent intent = new Intent(context, typeof(Player));
                    intent.PutExtra("ListPos", ListPos);
                    intent.PutExtra("Pos", Pos);
                    intent.PutExtra("VideoPath", Utils.Database.GetVideoPath(List.GetDownloads.Series[ListPos].IsSubtitled, List.GetDownloads.Series[ListPos].Show, List.GetDownloads.Series[ListPos].Episodes[Pos].EP, List.GetDownloads.Series[ListPos].Episodes[Pos].ShowSeason));
                    intent.PutExtra("IsOnline", false);
                    context.StartActivity(intent);
                }
            }
            else
            {
                RecyclerView.ViewHolder holder = rec.FindViewHolderForLayoutPosition(Pos);
                CheckBox selector = holder.ItemView.FindViewById<CheckBox>(Resource.Id.episodes_dpg_selector);
                ImageView thumb = holder.ItemView.FindViewById<ImageView>(Resource.Id.thumbnail_dpg_ep);
                List.GetDownloads.Series[ListPos].Episodes[Pos].IsSelected = !List.GetDownloads.Series[ListPos].Episodes[Pos].IsSelected;

                _IOnUserSelectItems.IsUserSelecting(true);
                selector.PerformClick();

                if (List.GetDownloads.Series[ListPos].Episodes[Pos].IsSelected)
                {
                    thumb.LayoutParameters.Width = (int)Utils.Utils.DPToPX(context, 120);
                    thumb.LayoutParameters.Height = (int)Utils.Utils.DPToPX(context, 70);
                }
                else
                {
                    thumb.LayoutParameters.Width = (int)Utils.Utils.DPToPX(context, 150);
                    thumb.LayoutParameters.Height = (int)Utils.Utils.DPToPX(context, 100);
                }
                thumb.RequestLayout();

                var countOfItemsWithPosSeason = List.GetDownloads.Series[ListPos].Episodes.FindAll(delegate (List.Downloads dl) { return dl.ShowSeason == List.GetDownloads.Series[ListPos].Episodes[Pos].ShowSeason && dl.EP != 0; }).Count;
                var countOfItemsWithPosSeasonSelected = List.GetDownloads.Series[ListPos].Episodes.FindAll(delegate (List.Downloads dl) { return dl.ShowSeason == List.GetDownloads.Series[ListPos].Episodes[Pos].ShowSeason && dl.IsSelected && dl.EP != 0; }).Count;

                var headerIndex = List.GetDownloads.Series[ListPos].Episodes.FindIndex(x => x.EP == 0 && x.ShowSeason == List.GetDownloads.Series[ListPos].Episodes[Pos].ShowSeason && x.ShowID == List.GetDownloads.Series[ListPos].Episodes[Pos].ShowID);

                if (countOfItemsWithPosSeason == countOfItemsWithPosSeasonSelected)
                    List.GetDownloads.Series[ListPos].Episodes[headerIndex].IsSelected = true;
                else
                    List.GetDownloads.Series[ListPos].Episodes[headerIndex].IsSelected = false;
            }
        }

        private string GetDuration(long timeInMillisec)
        {
           
            long sec = timeInMillisec / 1000;
            long min = sec / 60;

            if (min >= 60)
            {
                return string.Format("{0} h", min/60);
            }
            else
                return string.Format("{0} min", min);
        }
    }

}