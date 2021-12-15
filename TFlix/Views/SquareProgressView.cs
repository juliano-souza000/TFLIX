using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Util;
using Android.Views;
using TFlix.Utils;
using static Android.Graphics.Paint;

namespace TFlix.Views
{
    [Register("com.toddy.tflix.Views.SquareProgressView")]
    public class SquareProgressView : View
    {

        public Paint ProgressBarPaint;
        public Paint OutlinePaint;
        public Paint textPaint;

        public float Strokewidth = 0;
        public Canvas Canvas;

        private double _Max = 100;
        private double _Progress;
        private bool _Outline;
        private bool _IsStartLine;
        private bool _ShowProgress;
        private bool _IsCenterline;
        private PercentStyle _PercentSettings = new PercentStyle(Paint.Align.Center, 150, true);
        private bool _ClearOnHundred;
        private bool _IsIndeterminate;

        public double Max
        {
            get { return _Max; }
            set
            {
                _Max = value;
                this.Invalidate();
            }
        }

        public double Progress
        {
            get { return _Progress; }
            set
            {
                _Progress = (value/Max)*100;
                this.Invalidate();
            }
        }

        public Color Color
        {
            get { return ProgressBarPaint.Color; }
            set
            {
                ProgressBarPaint.Color = value;
                this.Invalidate();
            }
        }

        public float WidthInDp
        {
            get { return ProgressBarPaint.StrokeWidth; }
            set
            {
                ProgressBarPaint.StrokeWidth = Utils.Utils.DPToPX(Context, value);
                this.Invalidate();
            }
        }

        public bool Outline
        {
            get { return _Outline; }
            set
            {
                _Outline = value;
                this.Invalidate();
            }
        }

        public bool IsStartLine
        {
            get { return _IsStartLine; }
            set
            {
                _IsStartLine = value;
                this.Invalidate();
            }
        }

        public bool ShowProgress
        {
            get { return _ShowProgress; }
            set
            {
                _ShowProgress = value;
                this.Invalidate();
            }
        }

        public bool IsCenterline
        {
            get { return _IsCenterline; }
            set
            {
                _IsCenterline = value;
                this.Invalidate();
            }
        }

        public bool IsRoundedCorners = false;
        public float RoundedCornersRadius = 10;

        public PercentStyle PercentSettings
        {
            get { return _PercentSettings; }
            set
            {
                _PercentSettings = value;
                this.Invalidate();
            }
        }

        public bool ClearOnHundred
        {
            get { return _ClearOnHundred; }
            set
            {
                _ClearOnHundred = value;
                this.Invalidate();
            }
        }
        public bool IsIndeterminate
        {
            get { return _IsIndeterminate; }
            set
            {
                _IsIndeterminate = value;
                this.Invalidate();
            }
        }

        public int Indeterminate_count = 1;

        public float Indeterminate_width = 0.0f;

        public void SetRoundedCorners(bool roundedCorners, float radius)
        {
            this.IsRoundedCorners = roundedCorners;
            this.RoundedCornersRadius = radius;
            if (roundedCorners)
            {
                ProgressBarPaint.SetPathEffect(new CornerPathEffect(RoundedCornersRadius));
            }
            else
            {
                ProgressBarPaint.SetPathEffect(null);
            }
            this.Invalidate();
        }

        public SquareProgressView(Context context) : base(context)
        {
            InitializePaints(context);
        }

        public SquareProgressView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            InitializePaints(context);
        }

        public SquareProgressView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {   
            InitializePaints(context);
        }

        private void InitializePaints(Context context)
        {
            ProgressBarPaint = new Paint();
            WidthInDp = 1;
            ProgressBarPaint.Color = new Color(ContextCompat.GetColor(context, Resource.Color.colorRed));
            ProgressBarPaint.StrokeWidth = Utils.Utils.DPToPX(Context, WidthInDp);
            ProgressBarPaint.AntiAlias = true;
            ProgressBarPaint.SetStyle(Style.Stroke);

            OutlinePaint = new Paint();
            OutlinePaint.Color = Color.White;
            OutlinePaint.StrokeWidth = 1;
            OutlinePaint.AntiAlias = true;
            OutlinePaint.SetStyle(Style.Stroke);

            textPaint = new Paint();
            textPaint.Color = Color.White;
            textPaint.AntiAlias = true;
            textPaint.SetStyle(Style.Stroke);
        }

        protected override void OnDraw(Canvas canvas)
        {
            Canvas = canvas;
            base.OnDraw(canvas);
            Strokewidth = Utils.Utils.DPToPX(Context, WidthInDp);
            int cW = canvas.Width;
            int cH = canvas.Height;
            float scope = (2 * cW) + (2 * cH) - (4 * Strokewidth);
            float hSw = Strokewidth / 2;

            if (Outline)
            {
                DrawOutline();
            }

            if (IsStartLine)
            {
                DrawStartline();
            }

            if (ShowProgress)
            {
                DrawPercent(PercentSettings);
            }

            if (IsCenterline)
            {
                DrawCenterline(Strokewidth);
            }

            if ((ClearOnHundred && Progress == 100.0) || (Progress <= 0.0))
            {
                return;
            }

            if (IsIndeterminate)
            {
                Path path = new Path();
                DrawStop drawEnd = DrawEnd((scope / 100) * System.Convert.ToSingle(Indeterminate_count), canvas);

                if (drawEnd.Place == Place.Top)
                {
                    path.MoveTo(drawEnd.Location - Indeterminate_width - Strokewidth, hSw);
                    path.LineTo(drawEnd.Location, hSw);
                    Canvas.DrawPath(path, ProgressBarPaint);
                }

                if (drawEnd.Place == Place.Right)
                {
                    path.MoveTo(cW - hSw, drawEnd.Location - Indeterminate_width);
                    path.LineTo(cW - hSw, Strokewidth
                            + drawEnd.Location);
                    Canvas.DrawPath(path, ProgressBarPaint);
                }

                if (drawEnd.Place == Place.Bottom)
                {
                    path.MoveTo(drawEnd.Location - Indeterminate_width - Strokewidth,
                            cH - hSw);
                    path.LineTo(drawEnd.Location, cH
                            - hSw);
                    Canvas.DrawPath(path, ProgressBarPaint);
                }

                if (drawEnd.Place == Place.Left)
                {
                    path.MoveTo(hSw, drawEnd.Location - Indeterminate_width
                            - Strokewidth);
                    path.LineTo(hSw, drawEnd.Location);
                    Canvas.DrawPath(path, ProgressBarPaint);
                }

                Indeterminate_count++;
                if (Indeterminate_count > 100)
                {
                    Indeterminate_count = 0;
                }
                Invalidate();
            }
            else
            {
                Path path = new Path();
                DrawStop drawEnd = DrawEnd((scope / 100) * System.Convert.ToSingle(Progress), Canvas);

                if (drawEnd.Place == Place.Top)
                {
                    if (drawEnd.Location > (cW / 2) && Progress < 100.0)
                    {
                        path.MoveTo(cW / 2, hSw);
                        path.LineTo(drawEnd.Location, hSw);
                    }
                    else
                    {
                        path.MoveTo(cW / 2, hSw);
                        path.LineTo(cW - hSw, hSw);
                        path.LineTo(cW - hSw, cH - hSw);
                        path.LineTo(hSw, cH - hSw);
                        path.LineTo(hSw, hSw);
                        path.LineTo(Strokewidth, hSw);
                        path.LineTo(drawEnd.Location, hSw);
                    }
                    Canvas.DrawPath(path, ProgressBarPaint);
                }

                if (drawEnd.Place == Place.Right)
                {
                    path.MoveTo(cW / 2, hSw);
                    path.LineTo(cW - hSw, hSw);
                    path.LineTo(cW - hSw, 0 + drawEnd.Location);
                    Canvas.DrawPath(path, ProgressBarPaint);
                }

                if (drawEnd.Place == Place.Bottom)
                {
                    path.MoveTo(cW / 2, hSw);
                    path.LineTo(cW - hSw, hSw);
                    path.LineTo(cW - hSw, cH - hSw);
                    path.LineTo(cW - Strokewidth, cH - hSw);
                    path.LineTo(drawEnd.Location, cH - hSw);
                    Canvas.DrawPath(path, ProgressBarPaint);
                }

                if (drawEnd.Place == Place.Left)
                {
                    path.MoveTo(cW / 2, hSw);
                    path.LineTo(cW - hSw, hSw);
                    path.LineTo(cW - hSw, cH - hSw);
                    path.LineTo(hSw, cH - hSw);
                    path.LineTo(hSw, cH - Strokewidth);
                    path.LineTo(hSw, drawEnd.Location);
                    Canvas.DrawPath(path, ProgressBarPaint);
                }
            }
        }

        private void DrawCenterline(float Strokewidth)
        {
            float centerOfStrokewidth = Strokewidth / 2;
            Path centerlinePath = new Path();
            centerlinePath.MoveTo(centerOfStrokewidth, centerOfStrokewidth);
            centerlinePath.LineTo(Canvas.Width - centerOfStrokewidth, centerOfStrokewidth);
            centerlinePath.LineTo(Canvas.Width - centerOfStrokewidth, Canvas.Height - centerOfStrokewidth);
            centerlinePath.LineTo(centerOfStrokewidth, Canvas.Height - centerOfStrokewidth);
            centerlinePath.LineTo(centerOfStrokewidth, centerOfStrokewidth);
            Canvas.DrawPath(centerlinePath, OutlinePaint);
        }

        private void DrawStartline()
        {
            Path outlinePath = new Path();
            outlinePath.MoveTo(Canvas.Width / 2, 0);
            outlinePath.LineTo(Canvas.Width / 2, Strokewidth);
            Canvas.DrawPath(outlinePath, OutlinePaint);
        }

        private void DrawOutline()
        {
            Path outlinePath = new Path();
            outlinePath.MoveTo(0, 0);
            outlinePath.LineTo(Canvas.Width, 0);
            outlinePath.LineTo(Canvas.Width, Canvas.Height);
            outlinePath.LineTo(0, Canvas.Height);
            outlinePath.LineTo(0, 0);
            Canvas.DrawPath(outlinePath, OutlinePaint);
        }

        private void DrawPercent(PercentStyle setting)
        {
            string percentString = Progress.ToString("000");

            textPaint.TextAlign = setting.Align;
            if (setting.TextSize == 0)
            {
                textPaint.TextSize = ((Canvas.Height / 10) * 4);
            }
            else
            {
                textPaint.TextSize = setting.TextSize;
            }

            if (setting.IsPercentSign)
            {
                percentString = percentString + PercentSettings.CustomText;
            }

            textPaint.Color = new Color(PercentSettings.TextColor);

            Canvas.DrawText(
                    percentString,
                    Canvas.Width / 2,
                    (int)((Canvas.Height / 2) - ((textPaint.Descent() + textPaint.Ascent()) / 2)), textPaint);
        }

        public DrawStop DrawEnd(float percent, Canvas Canvas)
        {
            DrawStop drawStop = new DrawStop();
            Strokewidth = Utils.Utils.DPToPX(Context, WidthInDp);
            float halfOfTheImage = Canvas.Width / 2;

            // top right
            if (percent > halfOfTheImage)
            {
                float second = percent - (halfOfTheImage);

                // right
                if (second > (Canvas.Height - Strokewidth))
                {
                    float third = second - (Canvas.Height - Strokewidth);

                    // bottom
                    if (third > (Canvas.Width - Strokewidth))
                    {
                        float forth = third - (Canvas.Width - Strokewidth);

                        // left
                        if (forth > (Canvas.Height - Strokewidth))
                        {
                            float fifth = forth - (Canvas.Height - Strokewidth);

                            // top left
                            if (fifth == halfOfTheImage)
                            {
                                drawStop.Place = Place.Top;
                                drawStop.Location = halfOfTheImage;
                            }
                            else
                            {
                                drawStop.Place = Place.Top;
                                drawStop.Location = Strokewidth + fifth;
                            }
                        }
                        else
                        {
                            drawStop.Place = Place.Left;
                            drawStop.Location = Canvas.Height - Strokewidth - forth;
                        }

                    }
                    else
                    {
                        drawStop.Place = Place.Bottom;
                        drawStop.Location = Canvas.Width - Strokewidth - third;
                    }
                }
                else
                {
                    drawStop.Place = Place.Right;
                    drawStop.Location = Strokewidth + second;
                }

            }
            else
            {
                drawStop.Place = Place.Top;
                drawStop.Location = halfOfTheImage + percent;
            }

            return drawStop;
        }

        public class DrawStop
        {

            public Place Place;
            public float Location;

            public DrawStop() { }
        }

        public enum Place
        {
            Top, Right, Bottom, Left
        }

    }
}