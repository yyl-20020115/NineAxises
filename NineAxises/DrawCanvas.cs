using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace NineAxises
{
    public class DrawCanvas : Canvas
    {
        private VisualCollection graphics = null;
        private DrawingVisual visual = null;

        private const double MinValue = 1.0 / double.MaxValue;

        private Vector3D RangeVector = new Vector3D(MinValue, MinValue, MinValue);

        private float tension = 0.5f;
        private float penWidth = 1.0f;
        private List<Vector3D> data = new List<Vector3D>();
        private string _xText = string.Empty;
        private string _yText = string.Empty;
        private string _zText = string.Empty;

        private System.Drawing.Color xColor = System.Drawing.Color.FromArgb(255, 0, 0);
        private System.Drawing.Color yColor = System.Drawing.Color.FromArgb(0, 255, 0);
        private System.Drawing.Color zColor = System.Drawing.Color.FromArgb(0, 0, 255);
        private System.Drawing.Color aColor = System.Drawing.Color.FromArgb(0, 0, 0);
        private System.Drawing.Brush _backgroundBrush = System.Drawing.Brushes.White;
        private Font _textFont = System.Drawing.SystemFonts.DefaultFont;
        private bool _drawText = true;

        public System.Drawing.Color XColor { get => xColor; set { xColor = value; } }
        public System.Drawing.Color YColor { get => yColor; set { yColor = value; } }
        public System.Drawing.Color ZColor { get => zColor; set { zColor = value; } }
        public System.Drawing.Color AColor { get => aColor; set => aColor = value; }

        public List<Vector3D> Data => this.data;

        public string XText { get => _xText; set { _xText = value; } }
        public string YText { get => _yText; set { _yText = value; } }
        public string ZText { get => _zText; set { _zText = value; } }

        public float Tension { get => tension; set { tension = value; } }
        public float PenWidth { get => penWidth; set { penWidth = value; } }

        public System.Drawing.Brush BackgroundBrush { get => _backgroundBrush; set => _backgroundBrush = value; }

        public System.Drawing.Font TextFont { get => _textFont; set { _textFont = value; } }

        public bool DrawText { get => _drawText; set { _drawText = value; } }

        protected override int VisualChildrenCount => this.graphics.Count;


        public DrawCanvas()
        {
            this.graphics = new VisualCollection(this);
            this.visual = new DrawingVisual();
            this.graphics.Add(visual);
        }
        public void ClearData()
        {
            this.data.Clear();
        }
        public void AddData(Vector3D V, bool Draw = true)
        {
            this.data.Add(V);
            if (Draw)
            {
                this.Draw();
            }
        }
        public void Draw()
        {
            if (this.data.Count >= 2 * this.ActualWidth)
            {
                this.data = this.data.Skip((int)this.ActualWidth).ToList();
            }
            this.Draw(this.data);
        }
        public void Draw(List<Vector3D> data)
        {
            if (data != null && !double.IsNaN(this.ActualWidth) && !double.IsNaN(this.ActualHeight) && this.ActualWidth >= 1.0 && this.ActualHeight >= 1.0)
            {
                int sc = data.Count > this.ActualWidth ? (int)this.ActualWidth : data.Count;

                (double[], System.Drawing.Color)[] lines = new(double[], System.Drawing.Color)[3];
                lines[0] = (new double[sc], this.XColor);
                lines[1] = (new double[sc], this.YColor);
                lines[2] = (new double[sc], this.ZColor);

                var ds = data.Skip(data.Count - sc).ToList();

                this.RangeVector.X = ds.Max(d => Math.Abs(d.X));
                this.RangeVector.Y = ds.Max(d => Math.Abs(d.Y));
                this.RangeVector.Z = ds.Max(d => Math.Abs(d.Z));

                if(this.RangeVector.X == 0.0)
                {
                    this.RangeVector.X = 1.0;
                }
                if (this.RangeVector.Y == 0.0)
                {
                    this.RangeVector.Y = 1.0;
                }
                if (this.RangeVector.Z == 0.0)
                {
                    this.RangeVector.Z = 1.0;
                }
                int i = 0;
                foreach (var d in ds)
                {
                    lines[0].Item1[i] = d.X/this.RangeVector.X;
                    lines[1].Item1[i] = d.Y/this.RangeVector.Y;
                    lines[2].Item1[i] = d.Z/this.RangeVector.Z;
                    i++;
                }
                this.DrawCurves(lines);
            }
        }
        protected SizeF MeasureSize(Graphics graphics)
        {
            return graphics.MeasureString("H", this.TextFont);
        }
        public void DrawCurves((double[], System.Drawing.Color)[] Lines)
        {
            var bitmap = new Bitmap((int)this.ActualWidth, (int)this.ActualHeight);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.FillRectangle(this._backgroundBrush, new Rectangle(0, 0, (int)ActualWidth, (int)ActualHeight));

                graphics.Transform = new System.Drawing.Drawing2D.Matrix(1, 0, 0, -1, 0, 0);//Y轴向上为正，X向右为
                graphics.TranslateTransform(0, (int)(ActualHeight / 2), MatrixOrder.Append);

                using (var pen = new System.Drawing.Pen(this.aColor, this.penWidth))
                {
                    graphics.DrawLine(pen, PointF.Empty, new PointF((float)this.ActualWidth, 0.0f));
                }
                if (!double.IsNaN(this.ActualWidth) && !double.IsNaN(this.ActualHeight) && this.ActualWidth >= 1.0 && this.ActualHeight >= 1.0)
                {
                    foreach (var line in Lines)
                    {
                        this.DrawCurve(graphics, line.Item1, line.Item2, this.PenWidth, this.Tension);
                    }
                }
                if (this.DrawText)
                {
                    graphics.Transform = new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, 0, 0);//Y轴向上为正，X向右为
                    graphics.TranslateTransform(0, (int)(ActualHeight / 2), MatrixOrder.Append);

                    float h = this.MeasureSize(graphics).Height;
                    using (var XBrush = new SolidBrush(this.XColor))
                    {
                        graphics.DrawString(this.XText ?? string.Empty, _textFont, XBrush, 0.0f, -h - h / 2);
                    }
                    using (var YBrush = new SolidBrush(this.YColor))
                    {
                        graphics.DrawString(this.YText ?? string.Empty, _textFont, YBrush, 0.0f, 0 - h / 2);
                    }
                    using (var ZBrush = new SolidBrush(this.ZColor))
                    {
                        graphics.DrawString(this.ZText ?? string.Empty, _textFont, ZBrush, 0.0f, h - h / 2);
                    }

                }
                using (var context = this.visual.RenderOpen())
                {
                    context.DrawImage(this.ToBitmapImage(bitmap), new Rect(0.0, 0.0, this.ActualWidth, this.ActualHeight));
                }
            }
        }

        private void DrawCurve(Graphics graphics, double[] points, System.Drawing.Color color, float penWidth, float tension)
        {
            if (points != null && points.Length > 1)
            {
                using (System.Drawing.Pen CurvePen = new System.Drawing.Pen(color, penWidth))
                {
                    PointF[] CurvePointF = new PointF[points.Length];
                    float xSlice = (float)this.ActualWidth / points.Length;
                    float yHeight = (float)this.ActualHeight / 2.0f;
                    for (int i = 0; i < points.Length; i++)
                    {
                        float x = xSlice * i;
                        float y = (float)(yHeight * points[i]);
                        CurvePointF[i] = new PointF(x, y);
                    }
                    graphics.DrawCurve(CurvePen, CurvePointF, tension);
                }
            }
        }

        private BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Seek(0L, SeekOrigin.Begin);
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        /// <summary>
        /// Get visual child - one of GraphicsBase objects
        /// or in-place editing textbox, if it is active.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        => (index < 0 || index >= graphics.Count)
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : graphics[index];

    }

}
