using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using static Android.Graphics.Paint;

namespace TFlix.Utils
{
    public class PercentStyle
    {
        public Paint.Align Align { get; set; }
        public float TextSize { get; set; }
        public bool IsPercentSign { get; set; }
        public string CustomText { get; set; } = "%";
        public int TextColor { get; set; } = Color.Black;

        public PercentStyle()
        {
            // do nothing
        }

        public PercentStyle(Align align, float textSize, bool percentSign)
        {
            Align = align;
            TextSize = textSize;
            IsPercentSign = percentSign;
        }
    }
}