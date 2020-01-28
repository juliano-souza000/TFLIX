using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using TFlix.Event;
using SearchView = Android.Support.V7.Widget.SearchView;

namespace TFlix.Fragments
{
    public class SearchFragment : Android.Support.V4.App.Fragment
    {
        SearchView Search;
        RecyclerView SearchRecycler;
        Adapter.SearchAdapter adapter;
        ProgressBar Loading;
        FrameLayout Frame;

        private int page = 1;
        private bool IsDownloading;
        private bool Internet;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (MainActivity.current == Xamarin.Essentials.NetworkAccess.Internet)
            {
                Internet = true;
                return inflater.Inflate(Resource.Layout.search_page_fragment, null);
            }
            else
            {
                Internet = false;
                return inflater.Inflate(Resource.Layout.nointernet_layout, null);
            }
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            if (Internet)
            {
                Search = (SearchView)view.FindViewById(Resource.Id.search);
                SearchRecycler = (RecyclerView)view.FindViewById(Resource.Id.search_series);
                Loading = (ProgressBar)view.FindViewById(Resource.Id.result_series_loading);
                Frame = (FrameLayout)view.FindViewById(Resource.Id.notfound_frame);

                adapter = new Adapter.SearchAdapter(view.Context, SearchRecycler);

                SearchRecycler.SetLayoutManager(new LinearLayoutManager(Application.Context));

                if (Search != null && !string.IsNullOrWhiteSpace(List.GetSearch.LastSearch))
                {
                    Search.SetQuery(List.GetSearch.LastSearch, false);
                    if (List.GetSearch.Search != null)
                        if (List.GetSearch.Search[0].Title.ToLower().Contains(List.GetSearch.LastSearch.ToLower()))
                            SearchRecycler.SetAdapter(adapter);
                }


                Search.QueryTextSubmit += Search_QueryTextSubmit;
                Search.QueryTextChange += Search_QueryTextChange;
                SearchRecycler.ScrollChange += SearchRecycler_ScrollChange;
            }
        }

        public override void OnResume()
        {
            base.OnResume();
            if(Internet)
                Search.ClearFocus();
        }

        private void Search_QueryTextChange(object sender, SearchView.QueryTextChangeEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Search.Query))
                List.GetSearch.LastSearch = Search.Query;
        }

        private void SearchRecycler_ScrollChange(object sender, View.ScrollChangeEventArgs e)
        {
            if (!SearchRecycler.CanScrollVertically(1) && !IsDownloading && List.GetSearch.Search != null && Internet)
            {
                Loading.BringToFront();
                Loading.Visibility = ViewStates.Visible;

                try
                {
                    int posStarted = List.GetSearch.Search.Count;
                    List<List.Search> newItems = null;

                    page++;
                    IsDownloading = true;

                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += (s, ex) =>
                    {
                        try
                        {
                            newItems = JsonConvert.DeserializeObject<List<List.Search>>(Utils.Utils.Download(page, Search.Query));
                            newItems.RemoveAll(x => x.Title.Contains("AO VIVO"));
                            newItems.RemoveAll(x => !x.Title.Contains("Temporada"));
                            List.GetSearch.Search.AddRange(newItems);
                        }
                        catch
                        {
                            Toast.MakeText(Application.Context, "FIM", ToastLength.Long).Show();
                        }
                    };
                    worker.RunWorkerAsync();
                    worker.RunWorkerCompleted += (s, ex) =>
                    {
                        try
                        {
                            Loading.Visibility = ViewStates.Gone;
                            IsDownloading = false;
                            adapter.NotifyItemRangeInserted(posStarted, newItems.Count);
                        }
                        catch { }
                    };
                }
                catch { }
            }
        }

        private void Search_QueryTextSubmit(object sender, SearchView.QueryTextSubmitEventArgs e)
        {
            bool error = false;
            Search.ClearFocus();
            Frame.RemoveAllViews();

            Loading.Visibility = ViewStates.Visible;

            try
            {
                List.GetSearch.Search.Clear();
                SearchRecycler.SetAdapter(null);
            }
            catch { }

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, ex) =>
            {
                try
                {
                    List.GetSearch.Search = JsonConvert.DeserializeObject<List<List.Search>>(Utils.Utils.Download(page, e.NewText));
                    List.GetSearch.Search.RemoveAll(x => x.Title.Contains("AO VIVO"));
                    List.GetSearch.Search.RemoveAll(x => !x.Title.Contains("Temporada"));
                    if (List.GetSearch.Search.Count == 0 || List.GetSearch.Search == null)
                        throw new Exception();
                    Activity.RunOnUiThread(() => SearchRecycler.Visibility = ViewStates.Visible);
                }
                catch
                {
                    error = true;
                    Activity.RunOnUiThread(()=>
                    {
                        SearchRecycler.Visibility = ViewStates.Gone;
                        LayoutInflater li = LayoutInflater.From(Context);
                        View notfound = li.Inflate(Resource.Layout.show_notfound, Frame, false);
                        Frame.AddView(notfound);
                    });
                }
            };
            worker.RunWorkerAsync();
            worker.RunWorkerCompleted += (s, ex) =>
            {
                try
                {
                    if(!error)
                        SearchRecycler.SetAdapter(adapter);
                }
                catch { }
                Loading.Visibility = ViewStates.Gone;
            };
        }

    }
}