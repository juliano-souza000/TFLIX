using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TFlix.Interface
{
    public interface IOnPauseDownload
    {
        void IOnPauseDownload(bool isFromNotification);
    }

    public interface IOnCancelDownload
    {
        void IOnCancelDownload();
    }
}