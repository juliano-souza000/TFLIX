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

namespace TFlix.Decoration
{
    public class MarginItemDecoration : RecyclerView.ItemDecoration
    {
        private int Margin { get; set; }

        public MarginItemDecoration(int margin)
        {
            Margin = margin;
        }

        public override void GetItemOffsets(Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
        {
            if (parent.GetLayoutManager() == null)
                return;

            outRect.Left = Margin;

            //base.GetItemOffsets(outRect, view, parent, state);
        }
    }
}