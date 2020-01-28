using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using TFlix.Adapter;
using TFlix.Interface;
using Toolbar = Android.Widget.Toolbar;

namespace TFlix.Fragments
{
    class DownloadsFragment : Android.Support.V4.App.Fragment, IOnUserSelectItems
    {
        private RecyclerView Series;
        private DownloadsAdapter adapter;
        private Toolbar toolbar;

        private bool HasDownloads;
        private bool IsSelecting;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            Utils.Database.ReadDB();

            if (List.GetDownloads.Series == null || List.GetDownloads.Series.Count == 0)
            {
                HasDownloads = false;
                return inflater.Inflate(Resource.Layout.nodownloads_layout, null);
            }
            else
            {
                HasDownloads = true;
                HasOptionsMenu = true;
                return inflater.Inflate(Resource.Layout.downloads_fragment, null);
            }
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            Utils.Database.ReadDB();

            if (HasDownloads)
            {
                Series = (RecyclerView)view.FindViewById(Resource.Id.downloaded_series);
                toolbar = (Toolbar)view.FindViewById(Resource.Id.downloaded_series_toolbar);

                Activity.SetActionBar(toolbar);

                adapter = new DownloadsAdapter(Application.Context, Series, this);

                Activity.ActionBar.Title = "Séries";

                Activity.ActionBar.SetDisplayShowHomeEnabled(false);
                Activity.ActionBar.SetDisplayHomeAsUpEnabled(false);
                toolbar.NavigationOnClick += Toolbar_NavigationOnClick;
                toolbar.MenuItemClick += Toolbar_MenuItemClick;

                Series.SetLayoutManager(new LinearLayoutManager(Application.Context));
                Series.SetAdapter(adapter);
            }
        }

        public override void OnResume()
        {
            Utils.Database.ReadDB();
            try
            {
                if (List.GetDownloads.Series == null || List.GetDownloads.Series.Count == 0)
                {
                    if (HasDownloads)
                    {
                        HasDownloads = false;
                        if (FragmentManager != null)
                            FragmentManager.BeginTransaction().Replace(Resource.Id.main_frame, new DownloadsFragment()).Commit();
                    }
                }
                else
                {
                    HasDownloads = true;
                    adapter.NotifyDataSetChanged();
                }
            }
            catch { }
            base.OnResume();
        }

        private void Toolbar_NavigationOnClick(object sender, EventArgs e)
        {
            Activity.ActionBar.Subtitle = "";
            Activity.ActionBar.Title = "Séries";
            Activity.ActionBar.SetDisplayShowHomeEnabled(false);
            Activity.ActionBar.SetDisplayHomeAsUpEnabled(false);
            toolbar.SetBackgroundColor(Color.ParseColor("#" + Android.Support.V4.Content.Res.ResourcesCompat.GetColor(Resources, Resource.Color.colorPrimary, null).ToString("X")));
            toolbar.Menu.Clear();

            adapter.IsUserSelecting = false;
            adapter.NotifyDataSetChanged();
            IsSelecting = false;
        }

        private void Toolbar_MenuItemClick(object sender, Toolbar.MenuItemClickEventArgs e)
        {
            switch (e.Item.ItemId)
            {
                case Resource.Id.delete_appbar:
                    Utils.Database.DeleteItems();
                    try
                    {
                        if (List.GetDownloads.Series == null || List.GetDownloads.Series.Count == 0)
                        {
                            adapter.IsUserSelecting = false;
                            IsSelecting = false;

                            Activity.ActionBar.Subtitle = "";
                            Activity.ActionBar.Title = "Séries";
                            Activity.ActionBar.SetDisplayShowHomeEnabled(false);
                            Activity.ActionBar.SetDisplayHomeAsUpEnabled(false);
                            toolbar.SetBackgroundColor(Color.ParseColor("#" + Android.Support.V4.Content.Res.ResourcesCompat.GetColor(Resources, Resource.Color.colorPrimary, null).ToString("X")));
                            toolbar.Menu.Clear();
                            if(FragmentManager != null)
                                FragmentManager.BeginTransaction().Replace(Resource.Id.main_frame, new DownloadsFragment()).Commit();
                        }
                        else
                        {
                            Activity.ActionBar.Subtitle = "";
                            Activity.ActionBar.Title = "Séries";
                            Activity.ActionBar.SetDisplayShowHomeEnabled(false);
                            Activity.ActionBar.SetDisplayHomeAsUpEnabled(false);
                            toolbar.SetBackgroundColor(Color.ParseColor("#" + Android.Support.V4.Content.Res.ResourcesCompat.GetColor(Resources, Resource.Color.colorPrimary, null).ToString("X")));
                            toolbar.Menu.Clear();
                        }
                    }
                    catch { }
                    break;
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

                if(toolbar.Menu.FindItem(Resource.Id.delete_appbar) == null)
                    toolbar.InflateMenu(Resource.Menu.delete_appbar);

                Activity.ActionBar.SetDisplayShowHomeEnabled(true);
                Activity.ActionBar.SetDisplayHomeAsUpEnabled(true);

                toolbar.SetBackgroundColor(Color.ParseColor("#0362FC"));
                if (List.GetDownloads.Series.Where(row => row.IsSelected).Count() > 0)
                {
                    Activity.ActionBar.Subtitle = "";

                    Activity.ActionBar.Title = string.Format("{0} ({1})", List.GetDownloads.Series.Where(row => row.IsSelected).Count(), Utils.Utils.Size(List.GetDownloads.Series.Where(row => row.IsSelected).Select(row => row.TotalBytes).Sum()));
                }
                else
                {
                    Activity.ActionBar.Title = "";
                    Activity.ActionBar.Subtitle = "Selecione Itens Para Remover";
                }
                Activity.ActionBar.SetHomeAsUpIndicator(Resource.Drawable.baseline_close_24);
                Activity.InvalidateOptionsMenu();
            }
            else
            {
                toolbar.Menu.Clear();
                adapter.IsUserSelecting = false;
                adapter.NotifyDataSetChanged();
                Activity.ActionBar.Subtitle = "";
                Activity.ActionBar.Title = "Séries";
                Activity.ActionBar.SetDisplayShowHomeEnabled(false);
                Activity.ActionBar.SetDisplayHomeAsUpEnabled(false);
                toolbar.SetBackgroundColor(Color.ParseColor("#" + Android.Support.V4.Content.Res.ResourcesCompat.GetColor(Resources, Resource.Color.colorPrimary, null).ToString("X")));
            }
            IsSelecting = userSelecting;
        }

    }
}