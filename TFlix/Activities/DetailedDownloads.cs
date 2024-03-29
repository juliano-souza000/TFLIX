﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using TFlix.Adapter;
using TFlix.Event;
using TFlix.Interface;
using Toolbar = Android.Widget.Toolbar;

namespace TFlix.Activities
{
    [Activity(Label = "DetailedDownloads", ParentActivity = typeof(MainActivity), ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class DetailedDownloads : Activity, IOnUserSelectItems
    {
        private static RecyclerView Episodes;
        private static EpisodesAdapter adapter;
        private static Activity activity;
        private Toolbar toolbar;

        private static int Pos;
        private bool IsSelecting;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.downloaded_episodes);

            activity = this;

            toolbar = FindViewById<Toolbar>(Resource.Id.downloaded_episodes_toolbar);
            Episodes = (RecyclerView)FindViewById(Resource.Id.downloaded_episodes);

            SetActionBar(toolbar);

            Pos = Intent.Extras.GetInt("ItemPos");

            adapter = new EpisodesAdapter(this, Episodes, Pos, this);

            ActionBar.Title = List.GetDownloads.Series[Pos].Show;
            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            toolbar.SetBackgroundResource(Resource.Drawable.action_bar_background);

            Episodes.SetLayoutManager(new LinearLayoutManager(this));
            Episodes.HasFixedSize = true;
            Episodes.SetAdapter(adapter);

            Progress.Updated += Progress_Updated;
            Progress.Completed += Progress_Completed;
            Progress.Paused += Progress_Paused;
            Download.ChangedStatus += Download_ChangedStatus;

        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (IsSelecting)
            {
                MenuInflater menuInflater = MenuInflater;
                menuInflater.Inflate(Resource.Menu.delete_appbar, menu);
            }
            else
            {
                menu.Clear();
            }
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    if (IsSelecting)
                    {
                        ActionBar.Subtitle = "";
                        ActionBar.Title = List.GetDownloads.Series[Pos].Show;
                        ActionBar.SetHomeAsUpIndicator(Resource.Drawable.abc_ic_ab_back_material);
                        toolbar.SetBackgroundResource(Resource.Drawable.action_bar_background);
                        //toolbar.SetBackgroundColor(Color.ParseColor("#" + Android.Support.V4.Content.Res.ResourcesCompat.GetColor(Resources, Resource.Color.colorPrimary, null).ToString("X")));
                        InvalidateOptionsMenu();

                        adapter.IsUserSelecting = false;
                        adapter.NotifyDataSetChanged();
                        IsSelecting = false;
                    }
                    else
                    {
                        return base.OnOptionsItemSelected(item);
                    }
                    return true;

                case Resource.Id.delete_appbar:
                    Task.Run(() =>
                    {
                        Utils.Database.DeleteItems();
                        try
                        {
                            IsSelecting = false;
                            if (List.GetDownloads.Series[Pos].Episodes == null || List.GetDownloads.Series[Pos].Episodes.Count == 0)
                            {
                                Finish();
                            }
                            else
                            {
                                RunOnUiThread(() =>
                                {
                                    adapter.IsUserSelecting = false;
                                    ActionBar.Subtitle = "";
                                    ActionBar.Title = List.GetDownloads.Series[Pos].Show;
                                    ActionBar.SetHomeAsUpIndicator(Resource.Drawable.abc_ic_ab_back_material);
                                    toolbar.SetBackgroundResource(Resource.Drawable.action_bar_background);
                                    //toolbar.SetBackgroundColor(Color.ParseColor("#" + Android.Support.V4.Content.Res.ResourcesCompat.GetColor(Resources, Resource.Color.colorPrimary, null).ToString("X")));
                                    InvalidateOptionsMenu();
                                    adapter.NotifyDataSetChanged();
                                });
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Finish();
                        }
                    });
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        public static void ReloadDataset()
        {
            try
            {
                if (List.GetDownloads.Series[Pos].Episodes != null || List.GetDownloads.Series[Pos].Episodes.Count != 0)
                    adapter.NotifyDataSetChanged();
                else
                    activity.Finish();
            }
            catch { }
        }

        private void Download_ChangedStatus(object sender, ProgressEventArgs e)
        {
            if (!IsSelecting)
            {
                try
                {
                    var epIndex = List.GetDownloads.Series[Pos].Episodes.FindIndex(x => x.EP == e.EP && x.ShowSeason == e.ShowSeason && x.ShowID == e.ShowID);

                    var holder = Episodes.FindViewHolderForAdapterPosition(epIndex);
                    Console.WriteLine(holder.AdapterPosition);
                    var downloadProgress = holder.ItemView.FindViewById<ProgressBar>(Resource.Id.download_progressbar);
                    var error = holder.ItemView.FindViewById<ImageView>(Resource.Id.error_dpg_ep_dp);
                    var waiting = holder.ItemView.FindViewById<ImageView>(Resource.Id.episodes_dpg_ep_loading_progressbar);

                    RunOnUiThread(() =>
                    {
                        if (List.GetDownloads.Series[Pos].Episodes[epIndex].IsDownloading)
                        {
                            downloadProgress.Visibility = ViewStates.Visible;
                            error.Visibility = ViewStates.Gone;
                            waiting.Visibility = ViewStates.Gone;
                        }
                        else
                        {
                            downloadProgress.Visibility = ViewStates.Gone;
                            error.Visibility = ViewStates.Visible;
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void Progress_Paused(object sender, ProgressEventArgs e)
        {
            if (!IsSelecting)
            {
                var epIndex = List.GetDownloads.Series[Pos].Episodes.FindIndex(x => x.EP == e.EP && x.ShowSeason == e.ShowSeason && x.ShowID == e.ShowID);

                try
                {
                    var holder = Episodes.FindViewHolderForAdapterPosition(epIndex);
                    var downloadProgress = holder.ItemView.FindViewById<ProgressBar>(Resource.Id.download_progressbar);
                    var pauseResumeDownload = holder.ItemView.FindViewById<ImageView>(Resource.Id.pauseresume_dpg_ep_dp);
                    var error = holder.ItemView.FindViewById<ImageView>(Resource.Id.error_dpg_ep_dp);

                    RunOnUiThread(() =>
                    {
                        error.Visibility = ViewStates.Gone;
                        if (List.GetDownloads.Series[Pos].Episodes[epIndex].IsPaused)
                        {
                            downloadProgress.Visibility = ViewStates.Gone;
                            pauseResumeDownload.Visibility = ViewStates.Visible;
                        }
                        else
                        {
                            downloadProgress.Visibility = ViewStates.Visible;
                            pauseResumeDownload.Visibility = ViewStates.Gone;
                        }
                    });
                    //List.GetDownloads.Series[Pos].Episodes[epIndex].downloadInfo.IsPaused = !List.GetDownloads.Series[Pos].Episodes[epIndex].downloadInfo.IsPaused;
                    //adapter.NotifyItemChanged(epIndex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void Progress_Completed(object sender, ProgressEventArgs e)
        {
            var epIndex = List.GetDownloads.Series[Pos].Episodes.FindIndex(x => x.EP == e.EP && x.ShowSeason == e.ShowSeason && x.ShowID == e.ShowID);
            if (!List.GetDownloads.Series[Pos].Episodes[epIndex].IsPaused && !IsSelecting)
            {
                try
                {
                    var holder = Episodes.FindViewHolderForAdapterPosition(epIndex);
                    var downloadProgress = holder.ItemView.FindViewById<ProgressBar>(Resource.Id.download_progressbar);
                    var downloaded = holder.ItemView.FindViewById<ImageView>(Resource.Id.episodes_dpg_ep_dp);

                    RunOnUiThread(() =>
                    {
                        downloadProgress.Visibility = ViewStates.Gone;
                        downloaded.Visibility = ViewStates.Visible;
                    });
                    adapter.NotifyItemChanged(epIndex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void Progress_Updated(object sender, ProgressEventArgs e)
        {
            var epIndex = List.GetDownloads.Series[Pos].Episodes.FindIndex(x => x.EP == e.EP && x.ShowSeason == e.ShowSeason && x.ShowID == e.ShowID);
            //Console.WriteLine(IsSelecting);
            if (!List.GetDownloads.Series[Pos].Episodes[epIndex].IsPaused && !IsSelecting)
            {
                try
                {
                    var holder = Episodes.FindViewHolderForAdapterPosition(epIndex);

                    holder = Episodes.FindViewHolderForAdapterPosition(epIndex);

                    var downloadProgress = holder.ItemView.FindViewById<ProgressBar>(Resource.Id.download_progressbar);
                    var downloaded = holder.ItemView.FindViewById<ImageView>(Resource.Id.episodes_dpg_ep_dp);
                    var error = holder.ItemView.FindViewById<ImageView>(Resource.Id.error_dpg_ep_dp);

                    RunOnUiThread(() =>
                    {
                        if (downloadProgress.Visibility != ViewStates.Visible)
                            downloadProgress.Visibility = ViewStates.Visible;
                        error.Visibility = ViewStates.Gone;
                    });
                    downloadProgress.Progress = List.GetDownloads.Series[Pos].Episodes[epIndex].Progress;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void IsUserSelecting(bool userSelecting)
        {
            if (userSelecting)
            {
                if (!adapter.IsUserSelecting)
                {
                    adapter.IsUserSelecting = true;
                    adapter.NotifyDataSetChanged();
                }
                toolbar.SetBackgroundResource(0);
                toolbar.SetBackgroundColor(Color.ParseColor("#0362FC"));
                if (List.GetDownloads.Series[Pos].Episodes.Where(row => row.IsSelected).Count() > 0)
                {
                    ActionBar.Subtitle = "";

                    ActionBar.Title = string.Format("{0} ({1})", List.GetDownloads.Series[Pos].Episodes.Where(row => row.IsSelected).Count(), Utils.Utils.Size(List.GetDownloads.Series[Pos].Episodes.Where(row => row.IsSelected).Select(row => row.Bytes).Sum()));
                }
                else
                {
                    ActionBar.Title = "";
                    ActionBar.Subtitle = "Selecione Itens Para Remover";
                }
                ActionBar.SetHomeAsUpIndicator(Resource.Drawable.baseline_close_24);
                InvalidateOptionsMenu();
            }
            else
            {
                adapter.IsUserSelecting = false;
                adapter.NotifyDataSetChanged();
                ActionBar.Subtitle = "";
                ActionBar.Title = List.GetDownloads.Series[Pos].Show;
                ActionBar.SetHomeAsUpIndicator(Resource.Drawable.abc_ic_ab_back_material);
                toolbar.SetBackgroundResource(Resource.Drawable.action_bar_background);
                //toolbar.SetBackgroundColor(Color.ParseColor("#" + Android.Support.V4.Content.Res.ResourcesCompat.GetColor(Resources, Resource.Color.colorPrimary, null).ToString("X")));
            }
            IsSelecting = userSelecting;
        }
    }
}