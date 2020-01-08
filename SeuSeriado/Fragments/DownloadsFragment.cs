using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace SeuSeriado.Fragments
{
    class DownloadsFragment : Fragment
    {
        RecyclerView Series;
        Adapter.DownloadsAdapter adapter;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.downloads_fragment, null);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            Utils.Database.ReadDB();

            Series = (RecyclerView)view.FindViewById(Resource.Id.downloaded_series);

            Utils.Database.ReadDB();
            adapter = new Adapter.DownloadsAdapter(Application.Context, Series);

            Series.SetLayoutManager(new LinearLayoutManager(Application.Context));
            Series.SetAdapter(adapter);
        }

        public override void OnResume()
        {
            adapter.NotifyDataSetChanged();
            base.OnResume();
        }
    }
}