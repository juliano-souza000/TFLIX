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
                TextView title = row.FindViewById<TextView>(Resource.Id.title_dpg_ep);
                TextView epmb = row.FindViewById<TextView>(Resource.Id.episodes_dpg_ep);
                CheckBox selector = row.FindViewById<CheckBox>(Resource.Id.episodes_dpg_selector);
                ProgressBar downloadProgress = row.FindViewById<ProgressBar>(Resource.Id.download_progressbar); 
                ProgressBar timeWatched = row.FindViewById<ProgressBar>(Resource.Id.timewatched_progressbar);
                RelativeLayout imageHolder = row.FindViewById<RelativeLayout>(Resource.Id.imageframe_dpg_ep);
                selector.Clickable = false;

                row.Click += Row_Click;
                row.LongClick += Row_LongClick;
                //pauseResumeDownload.Click += PauseResumeDownload_Click;
                //downloadProgress.Click += PauseResumeDownload_Click;

                image.SetScaleType(ImageView.ScaleType.CenterCrop);
                EpisodesAdapterHolder view = new EpisodesAdapterHolder(row) { Image = image, Title = title, EPMB = epmb, Selector = selector, DownloadProgress = downloadProgress, Done = done, TimeWatched = timeWatched, Play = play, ImageHolder = imageHolder, PauseResumeDownload = pauseResumeDownload};
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
                Console.WriteLine("EP: {0} Season: {1} IsDownloading: {2}", List.GetDownloads.Series[ListPos].Episodes[position].EP, List.GetDownloads.Series[ListPos].Episodes[position].ShowSeason, List.GetDownloads.Series[ListPos].Episodes[position].downloadInfo.IsDownloading);

                if (List.GetDownloads.Series[ListPos].Episodes[position].downloadInfo.IsDownloading && !IsUserSelecting && !List.GetDownloads.Series[ListPos].Episodes[position].downloadInfo.IsPaused)
                {
                    Holder.DownloadProgress.Visibility = ViewStates.Visible;
                    Holder.DownloadProgress.Progress = List.GetDownloads.Series[ListPos].Episodes[position].Progress;
                    Holder.PauseResumeDownload.Visibility = ViewStates.Gone;
                }
                else
                {
                    Holder.DownloadProgress.Visibility = ViewStates.Gone;
                }

                if (List.GetDownloads.Series[ListPos].Episodes[position].downloadInfo.IsPaused)
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

                if(List.GetDownloads.Series[ListPos].Episodes[position].Progress == 100)
                {
                    Holder.Play.Visibility = ViewStates.Visible;
                    Holder.DownloadProgress.Visibility = ViewStates.Gone;
                }
                else
                {
                    Holder.Play.Visibility = ViewStates.Gone;
                }

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

                Intent intentPauseResume = new Intent(context, typeof(DownloadFilesService));

                if (List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.IsPaused)
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

                /*Console.WriteLine("URL: {0}\n" + 
                                  "DownloadPos: {1}\n" +
                                  "ListPos: {2}\n" + 
                                  "NotificationID: {5}" +
                                  "IsFromSearch: {3}\n" +
                                  "FullTitle: {4}\n", List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.URL
                                                    , List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.DownloadPos
                                                    , List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.ListPos
                                                    , List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.IsFromSearch
                                                    , List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.FullTitle
                                                    , List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.NotificationID);*/

                intentPauseResume.PutExtra("IsFromNotification", false);
                intentPauseResume.PutExtra("IsRequestingPauseResume", true);
                intentPauseResume.PutExtra("NotificationID", List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.NotificationID);
                intentPauseResume.PutExtra("PauseResume", pause);
                intentPauseResume.PutExtra("DownloadPos", List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.DownloadPos);
                intentPauseResume.PutExtra("ListPos", List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.ListPos);
                intentPauseResume.PutExtra("IsFromSearch", List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.IsFromSearch);

                intentPauseResume.PutExtra("URL", List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.URL);
                intentPauseResume.PutExtra("FullTitle", List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.FullTitle);
                intentPauseResume.PutExtra("Show", List.GetDownloads.Series[ListPos].Show);
                intentPauseResume.PutExtra("Ep", List.GetDownloads.Series[ListPos].Episodes[Pos].EP);
                intentPauseResume.PutExtra("ShowSeason", List.GetDownloads.Series[ListPos].Episodes[Pos].ShowSeason);
                intentPauseResume.PutExtra("IsSubtitled", List.GetDownloads.Series[ListPos].IsSubtitled);
                intentPauseResume.PutExtra("Thumb", List.GetDownloads.Series[ListPos].Episodes[Pos].EpThumb);
                intentPauseResume.PutExtra("ShowThumb", List.GetDownloads.Series[ListPos].ShowThumb);

                context.StartService(intentPauseResume);

                List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.IsPaused = !List.GetDownloads.Series[ListPos].Episodes[Pos].downloadInfo.IsPaused;
            }
            catch { }
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