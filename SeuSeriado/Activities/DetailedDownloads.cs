using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using SeuSeriado.Adapter;
using Toolbar = Android.Widget.Toolbar;

namespace SeuSeriado.Activities
{
    [Activity(Label = "DetailedDownloads", ParentActivity = typeof(MainActivity), ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class DetailedDownloads : Activity, IOnUserSelectItems
    {
        private static RecyclerView Episodes;
        private Toolbar toolbar;
        private static EpisodesAdapter adapter;

        private static int Pos;
        private bool IsSelecting;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.downloaded_episodes);

            toolbar = FindViewById<Toolbar>(Resource.Id.downloaded_episodes_toolbar);
            Episodes = (RecyclerView)FindViewById(Resource.Id.downloaded_episodes);

            SetActionBar(toolbar);

            Pos = Intent.Extras.GetInt("ItemPos");

            adapter = new EpisodesAdapter(this, Episodes, Pos, this);

            ActionBar.Title = List.GetDownloads.Series[Pos].Show;
            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayHomeAsUpEnabled(true);

            Episodes.SetLayoutManager(new LinearLayoutManager(this));
            Episodes.HasFixedSize = true;
            Episodes.SetAdapter(adapter);

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
            switch(item.ItemId)
            {
                case Android.Resource.Id.Home:
                    if(IsSelecting)
                    {
                        ActionBar.Subtitle = "";
                        ActionBar.Title = List.GetDownloads.Series[Pos].Show;
                        ActionBar.SetHomeAsUpIndicator(Resource.Drawable.abc_ic_ab_back_material);
                        toolbar.SetBackgroundColor(Color.ParseColor("#" + Android.Support.V4.Content.Res.ResourcesCompat.GetColor(Resources, Resource.Color.colorPrimary, null).ToString("X")));
                        InvalidateOptionsMenu();

                        adapter.IsUserSelecting = false;
                        adapter.NotifyDataSetChanged();
                        IsSelecting = false;
                    }else
                    {
                        return base.OnOptionsItemSelected(item);
                    }
                    return true;

                case Resource.Id.delete_appbar:
                    Utils.Database.DeleteItems();

                    try
                    {
                        if (List.GetDownloads.Series[Pos].Episodes == null || List.GetDownloads.Series[Pos].Episodes.Count == 0)
                        {
                            adapter.IsUserSelecting = false;
                            IsSelecting = false;

                            Finish();
                        }
                        else
                        {
                            ActionBar.Subtitle = "";
                            ActionBar.Title = List.GetDownloads.Series[Pos].Show;
                            ActionBar.SetHomeAsUpIndicator(Resource.Drawable.abc_ic_ab_back_material);
                            toolbar.SetBackgroundColor(Color.ParseColor("#" + Android.Support.V4.Content.Res.ResourcesCompat.GetColor(Resources, Resource.Color.colorPrimary, null).ToString("X")));
                            InvalidateOptionsMenu();
                        }
                    }
                    catch { Finish(); }
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        public static void ReloadDataset()
        {
            try
            {
                adapter.NotifyDataSetChanged();
            }
            catch { }
        }

        public static void ProgressChanged(int ShowSeason, int EP)
        {
            var epIndex = List.GetDownloads.Series[Pos].Episodes.FindIndex(x => x.EP == EP && x.ShowSeason == ShowSeason);

            try
            {
                var holder = Episodes.FindViewHolderForAdapterPosition(epIndex);
                var downloadProgress = holder.ItemView.FindViewById<ProgressBar>(Resource.Id.download_progressbar);
                var downloaded = holder.ItemView.FindViewById<ImageView>(Resource.Id.episodes_dpg_ep_dp);

                if (List.GetDownloads.Series[Pos].Episodes[epIndex].Progress != 100)
                {
                    if (downloadProgress.Visibility != ViewStates.Visible)
                        downloadProgress.Visibility = ViewStates.Visible;
                    downloadProgress.Progress = List.GetDownloads.Series[Pos].Episodes[epIndex].Progress;
                }
                else
                {
                    downloadProgress.Visibility = ViewStates.Gone;
                    downloaded.Visibility = ViewStates.Visible;
                    adapter.NotifyItemChanged(epIndex);
                }
            }
            catch { }
            //Console.WriteLine("Progress has changed");
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

                toolbar.SetBackgroundColor(Color.ParseColor("#0362FC"));
                if (List.GetDownloads.Series[Pos].Episodes.Where(row => row.IsSelected).Count() > 0)
                {
                    ActionBar.Subtitle = "";

                    ActionBar.Title = string.Format("{0} ({1})", List.GetDownloads.Series[Pos].Episodes.Where(row => row.IsSelected).Count(), Utils.Utils.Size(List.GetDownloads.Series[Pos].Episodes.Where(row => row.IsSelected).Select(row => row.TotalBytesEP).Sum()));
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
                toolbar.SetBackgroundColor(Color.ParseColor("#" + Android.Support.V4.Content.Res.ResourcesCompat.GetColor(Resources, Resource.Color.colorPrimary, null).ToString("X")));
            }
            IsSelecting = userSelecting;
        }
    }

    public interface IOnUserSelectItems
    {
        void IsUserSelecting(bool userSelecting);
    }
}