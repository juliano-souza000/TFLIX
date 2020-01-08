using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using SeuSeriado.List;

namespace SeuSeriado.Fragments
{
    class MainPageFragment : Fragment
    {
        private RecyclerView Series;
        private ProgressBar Loading;
        private Adapter.MainPage_SeriesAdapter adapter;

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
                return inflater.Inflate(Resource.Layout.mainpage, null);
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
                Series = (RecyclerView)view.FindViewById(Resource.Id.popular_series);
                Loading = (ProgressBar)view.FindViewById(Resource.Id.popular_series_loading);

                adapter = new Adapter.MainPage_SeriesAdapter(Application.Context, Series);

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, ex) =>
                {
                    if (GetMainPageSeries.Series == null)
                        GetMainPageSeries.Series = JsonConvert.DeserializeObject<List<MainPageSeries>>(Utils.Utils.Download(page));
                };
                worker.RunWorkerAsync();

                Series.SetLayoutManager(new GridLayoutManager(Application.Context, 3));
                Series.SetItemViewCacheSize(21);
                Series.DrawingCacheEnabled = true;
                Series.DrawingCacheQuality = DrawingCacheQuality.High;

                worker.RunWorkerCompleted += (s, ex) =>
                {
                    Console.WriteLine("Done!");
                    if (List.GetMainPageSeries.Series != null)
                        Series.SetAdapter(adapter);
                };

                Series.ScrollChange += Series_ScrollChange;
            }

        }

        private void Series_ScrollChange(object sender, View.ScrollChangeEventArgs e)
        {
            if (!Series.CanScrollVertically(1) && !IsDownloading && GetMainPageSeries.Series != null && Internet)
            {
                Loading.BringToFront();
                Loading.Visibility = ViewStates.Visible;
                try
                {
                    int posStarted = GetMainPageSeries.Series.Count;
                    List <MainPageSeries> newItems = null;

                    page++;
                    IsDownloading = true;

                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += (s, ex) =>
                    {
                        newItems = JsonConvert.DeserializeObject<List<MainPageSeries>>(Utils.Utils.Download(page));
                        GetMainPageSeries.Series.AddRange(newItems);
                    };
                    worker.RunWorkerAsync();
                    worker.RunWorkerCompleted += (s, ex) =>
                    {
                        Loading.Visibility = ViewStates.Gone;
                        IsDownloading = false;
                        adapter.NotifyItemRangeInserted(posStarted, newItems.Count);
                    };
                }
                catch { }
            }
        }
    }
}