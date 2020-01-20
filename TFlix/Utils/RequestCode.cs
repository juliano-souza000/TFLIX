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
using Java.Util.Concurrent.Atomic;

namespace TFlix.Utils
{
    public class RequestCode
    {
        private static AtomicInteger c = new AtomicInteger(0);
        public static int ID()
        {
                return c.IncrementAndGet();
        }
    }
}