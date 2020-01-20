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

namespace TFlix.Dialog
{
    class Synopsis : DialogFragment
    {
        public string SynopsisTitleString;
        public string SynopsisContentString;

        private TextView SynopsisTitle;
        private TextView SynopsisContent;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            Dialog.Window.SetBackgroundDrawable(Android.Support.V4.Content.ContextCompat.GetDrawable(Context, Resource.Drawable.rounded_dialog));

            var view = inflater.Inflate(Resource.Layout.synopsis, container, false);

            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            SynopsisTitle = (TextView)view.FindViewById(Resource.Id.synopsis_title);
            SynopsisContent = (TextView)view.FindViewById(Resource.Id.synopsis_content);

            if (!SynopsisContentString.StartsWith(" "))
                SynopsisContentString = SynopsisContentString.Insert(0, " ");

            var (Show, Season, Ep) = Utils.Utils.BreakFullTitleInParts(SynopsisTitleString);

            SynopsisTitle.Text =  Show;
            SynopsisContent.Text = SynopsisContentString;
        }
    }
}