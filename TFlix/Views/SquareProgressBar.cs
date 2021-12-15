using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using TFlix.Utils;
using static Android.Widget.ImageView;

namespace TFlix.Views
{
    [Register("com.toddy.tflix.Views.SquareProgressBar")]
    public class SquareProgressBar : RelativeLayout
    {
        public ImageView _ImageView { get; set; }
        private SquareProgressView _Bar;
        public new bool IsOpaque { get; set; }
        public bool HasGreyscale { get; set; }
        public bool IsFadingOnProgress { get; set; }
        public bool HasRoundedCorners { get; set; }

        public bool DrawCenterLine
        {
            get { return _Bar.IsCenterline; }
            set
            {
                _Bar.IsCenterline = value;
            }
        }

        public bool IsIndeterminate
        {
            get { return _Bar.IsIndeterminate; }
            set
            {
                _Bar.IsIndeterminate = value;
            }
        }

        public bool ClearOnHundred
        {
            get { return _Bar.ClearOnHundred; }
            set
            {
                _Bar.ClearOnHundred = value;
            }
        }

        public PercentStyle PercentStyle
        {
            get { return _Bar.PercentSettings; }
            set
            {
                _Bar.PercentSettings = value;
            }
        }

        public bool ShowProgress
        {
            get { return _Bar.ShowProgress; }
            set
            {
                _Bar.ShowProgress = value;
            }
        }

        public bool IsStartLine
        {
            get { return _Bar.IsStartLine; }
            set
            {
                _Bar.IsStartLine = IsStartLine;
            }
        }

        public bool Outline
        {
            get { return _Bar.Outline; }
            set
            {
                _Bar.Outline = Outline;
            }
        }

        public Color Color
        {
            get { return _Bar.Color; }
            set
            {
                _Bar.Color = value;
            }
        }

        public double Max
        {
            get { return _Bar.Max; }
            set
            {
                _Bar.Max = value;
            }
        }

        public double Progress
        {
            get { return _Bar.Progress; }
            set
            {
                _Bar.Progress = value;
                if (IsOpaque)
                {
                    if (IsFadingOnProgress)
                    {
                        SetOpacity(100 - (int)Progress);
                    }
                    else
                    {
                        SetOpacity((int)Progress);
                    }
                }
                else
                {
                    SetOpacity(100);
                }
            }
        }

        public int WidthInDp
        {
            get { return (int)_Bar.WidthInDp; }
            set
            {
                _Bar.WidthInDp = (float)value;
                //int padding = (int)Utils.Utils.DPToPX(Context, (float)value);
                //_ImageView.SetPadding(padding, padding, padding, padding);
            }
        }

        public SquareProgressBar(Context context) : base(context)
        {
            LayoutInflater mInflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            mInflater.Inflate(Resource.Layout.progressbarview, this, true);
            _Bar = (SquareProgressView)FindViewById(Resource.Id.squareProgressBarT);
            _ImageView = (ImageView)FindViewById(Resource.Id.imageViewT);

            _Bar.SetBackgroundColor(Color.Transparent);
            _Bar.BringToFront();
        }

        public SquareProgressBar(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater mInflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            mInflater.Inflate(Resource.Layout.progressbarview, this, true);
            _Bar = (SquareProgressView)FindViewById(Resource.Id.squareProgressBarT);
            _ImageView = (ImageView)FindViewById(Resource.Id.imageViewT);

            _Bar.SetBackgroundColor(Color.Transparent);
            _Bar.BringToFront();

            if (attrs != null)
            {
                var array = context.ObtainStyledAttributes(attrs, Resource.Styleable.SquareProgressBar, 0, 0);
                _ImageView.SetBackgroundColor(array.GetColor(Resource.Styleable.SquareProgressBar_backgroundColor, Color.Transparent));
                Max = array.GetInt(Resource.Styleable.SquareProgressBar_max, 0);
                Progress = array.GetInt(Resource.Styleable.SquareProgressBar_progress, 0);
                Color = array.GetColor(Resource.Styleable.SquareProgressBar_progressColor, Color.Black);
                SetRoundedCorners(array.GetBoolean(Resource.Styleable.SquareProgressBar_useRoundedCorners, false), array.GetFloat(Resource.Styleable.SquareProgressBar_roundedCornersRadius, 1));
                array.Recycle();
            }
        }

        public SquareProgressBar(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater mInflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            mInflater.Inflate(Resource.Layout.progressbarview, this, true);
            _Bar = (SquareProgressView)FindViewById(Resource.Id.squareProgressBarT);
            _ImageView = (ImageView)FindViewById(Resource.Id.imageViewT);

            _Bar.SetBackgroundColor(Color.Transparent);
            _Bar.BringToFront();

            if (attrs != null)
            {
                var array = context.ObtainStyledAttributes(attrs, Resource.Styleable.SquareProgressBar, 0, 0);
                _ImageView.SetBackgroundColor(array.GetColor(Resource.Styleable.SquareProgressBar_backgroundColor, Color.Transparent));
                Max = array.GetInt(Resource.Styleable.SquareProgressBar_max, 0);
                Progress = array.GetInt(Resource.Styleable.SquareProgressBar_progress, 0);
                Color = array.GetColor(Resource.Styleable.SquareProgressBar_progressColor, Color.Black);
                SetRoundedCorners(array.GetBoolean(Resource.Styleable.SquareProgressBar_useRoundedCorners, false), array.GetFloat(Resource.Styleable.SquareProgressBar_roundedCornersRadius, 1));
                array.Recycle();
            }
        }

        public void SetImageDrawable(Drawable imageDrawable)
        {
            _ImageView.SetImageDrawable(imageDrawable);
        }

        public void SetImageBitmap(Bitmap bitmap)
        {
            _ImageView.SetImageBitmap(bitmap);
        }

        public void SetImageScaleType(ScaleType scale)
        {
            _ImageView.SetScaleType(scale);
        }

        private void SetOpacity(int progress)
        {
#pragma warning disable
            _ImageView.SetAlpha((int)(2.55 * progress));
#pragma warning restore
        }

        public void SetOpacity(bool opaque)
        {
            IsOpaque = opaque;
            Progress = _Bar.Progress;
        }

        public void SetOpacity(bool opaque, bool isFadingOnProgress)
        {
            IsOpaque = opaque;
            IsFadingOnProgress = isFadingOnProgress;
            Progress = _Bar.Progress;
        }
        public void SetImageGrayscale(bool hasGreyscale)
        {
            HasGreyscale = hasGreyscale;
            if (HasGreyscale)
            {
                ColorMatrix matrix = new ColorMatrix();
                matrix.SetSaturation(0);
                _ImageView.SetColorFilter(new ColorMatrixColorFilter(matrix));
            }
            else
            {
                _ImageView.SetColorFilter(null);
            }
        }

        public void SetRoundedCorners(bool useRoundedCorners)
        {
            HasRoundedCorners = useRoundedCorners;
            _Bar.SetRoundedCorners(useRoundedCorners, 10);
        }

        public void SetRoundedCorners(bool useRoundedCorners, float radius)
        {
            HasRoundedCorners = useRoundedCorners;
            _Bar.SetRoundedCorners(useRoundedCorners, radius);
        }

    }
}