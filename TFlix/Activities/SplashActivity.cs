using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using TFlix.List;

namespace TFlix.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.Splash", MainLauncher = true, NoHistory = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, LaunchMode = Android.Content.PM.LaunchMode.SingleInstance, WindowSoftInputMode = SoftInput.StateHidden | SoftInput.AdjustPan, ConfigurationChanges = Android.Content.PM.ConfigChanges.KeyboardHidden | Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class SplashActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {
                if (GetMainPageSeries.Series == null)
                    GetMainPageSeries.Series = JsonConvert.DeserializeObject<List<MainPageSeries>>(Utils.Utils.Download(0));
            };
            worker.RunWorkerAsync();
            worker.RunWorkerCompleted += (s, e) =>
            {
                StartActivity(new Intent(Application.Context, typeof(MainActivity)));
            };
        }
    }
}