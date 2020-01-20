using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace TFlix.Services
{
    public class DownloadFileServiceConnection : Java.Lang.Object, IServiceConnection
    {
        static readonly string TAG = typeof(DownloadFileServiceConnection).FullName;

        public DownloadFileServiceConnection()
        {
            IsConnected = false;
            Binder = null;
        }

        public bool IsConnected { get; private set; }
        public DownloadFilesBinder Binder { get; private set; }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            Binder = service as DownloadFilesBinder;
            IsConnected = this.Binder != null;

            string message = "onServiceConnected - ";
            Log.Debug(TAG, $"OnServiceConnected {name.ClassName}");

            if (IsConnected)
            {
                message = message + " bound to service " + name.ClassName;
            }
            else
            {
                message = message + " not bound to service " + name.ClassName;
            }
            Log.Info(TAG, message);
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            Log.Debug(TAG, $"OnServiceDisconnected {name.ClassName}");
            IsConnected = false;
            Binder = null;
        }
    }
}