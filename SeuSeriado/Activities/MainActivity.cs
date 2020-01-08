using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.IO;
using Android.Support.Design.Widget;
using Android.Views;
using System.ComponentModel;
using Android.Views.Animations;
using Xamarin.Essentials;

namespace SeuSeriado
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, LaunchMode = Android.Content.PM.LaunchMode.SingleInstance, WindowSoftInputMode = SoftInput.AdjustNothing, ConfigurationChanges = Android.Content.PM.ConfigChanges.KeyboardHidden | Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class MainActivity : AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener, BottomNavigationView.IOnNavigationItemReselectedListener
    {

        private const int NAV_MAIN = 0;
        private const int NAV_SEARCH = 1;
        private const int NAV_DOWNLOADS = 2;

        public static NetworkAccess current;

        private FrameLayout Frame;
        private BottomNavigationView _Toolbar;

        private int FramePos = 0;
        private int PrevFramePos = -1;

        public void OnNavigationItemReselected(IMenuItem item) { }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.nav_main:
                    FramePos = NAV_MAIN;
                    break;
                case Resource.Id.nav_search:
                    FramePos = NAV_SEARCH;
                    break;
                case Resource.Id.nav_downloads:
                    FramePos = NAV_DOWNLOADS;
                    break;
            }
            ChangeFrame();
            
            return true;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            this.RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait;

            if (List.GetDownloads.Series == null || List.GetDownloads.Series.Count == 0)
                Utils.Database.CreateDB();

            Frame = (FrameLayout)FindViewById(Resource.Id.main_frame);
            _Toolbar = (BottomNavigationView)FindViewById(Resource.Id.bottom_navigation);

            try { Frame.RemoveAllViews(); } catch { }

            _Toolbar.SetOnNavigationItemSelectedListener(this);
            _Toolbar.SetOnNavigationItemReselectedListener(this);

            if (savedInstanceState == null)
                _Toolbar.SelectedItemId = Resource.Id.nav_main;

            //Handle Current network state
            current = Connectivity.NetworkAccess;
            if (current != NetworkAccess.Internet)
                ConnHandle();

            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutInt("FRAGMENT", FramePos);
            outState.PutInt("PREVFRAGMENT", PrevFramePos);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);
            FramePos = savedInstanceState.GetInt("FRAGMENT");
            PrevFramePos = savedInstanceState.GetInt("PREVFRAGMENT");
            ChangeFrame();
        }

        private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e) { ConnHandle(); }

        private void ConnHandle()
        {
            current = Connectivity.NetworkAccess;

            if (current == NetworkAccess.Internet)
                ChangeFrame();
            else
                ChangeFrame();
        }

        private void ChangeFrame()
        {
            if (RequestedOrientation == Android.Content.PM.ScreenOrientation.Portrait && (PrevFramePos != FramePos))
            {
                FragmentManager fragmentManager = FragmentManager;
                FragmentTransaction fragmentTransaction = fragmentManager.BeginTransaction();
                Frame.RemoveAllViews();
                switch (FramePos)
                {
                    case NAV_MAIN:
                        Fragments.MainPageFragment MPF = new Fragments.MainPageFragment();
                        fragmentTransaction.Add(Resource.Id.main_frame, MPF, "MPF");
                        break;
                    case NAV_SEARCH:
                        Fragments.SearchFragment SF = new Fragments.SearchFragment();
                        fragmentTransaction.Add(Resource.Id.main_frame, SF, "SF");
                        break;
                    case NAV_DOWNLOADS:
                        Fragments.DownloadsFragment DF = new Fragments.DownloadsFragment();
                        fragmentTransaction.Add(Resource.Id.main_frame, DF, "DF");
                        break;
                }
                fragmentTransaction.Commit();
                PrevFramePos = FramePos;
            }
        }

    }
}