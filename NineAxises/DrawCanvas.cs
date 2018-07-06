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
        private VisualCollection graphics;
        private DrawingVisual visual;
        public float Tension = 0.5f;
        public float PenWidth = 1.0f;

        private System.Drawing.Color xColor = System.Drawing.Color.FromArgb(255, 0, 0);
        private System.Drawing.Color yColor = System.Drawing.Color.FromArgb(0, 255, 0);
        private System.Drawing.Color zColor = System.Drawing.Color.FromArgb(0, 0, 255);
        public System.Drawing.Color XColor { get => xColor; set => xColor = value; }
        public System.Drawing.Color YColor { get => yColor; set => yColor = value; }
        public System.Drawing.Color ZColor { get => zColor; set => zColor = value; }

        private List<Vector3D> data = new List<Vector3D>();

        public List<Vector3D> Data => this.data;


        public DrawCanvas()
        {
            graphics = new VisualCollection(this);
            visual = new DrawingVisual();
            graphics.Add(visual);
        }
        public void ClearData()
        {
            this.Data.Clear();
        }
        public void Draw(Vector3D V)
        {
            this.Data.Add(V);

            if (this.data.Count >= 2 * this.ActualWidth)
            {
                this.data = this.data.Skip((int)this.ActualWidth).ToList();
            }

            if (!double.IsNaN(this.ActualWidth) && !double.IsNaN(this.ActualHeight) && this.ActualWidth>=1.0 && this.ActualHeight>=1.0) 
            {
                int sc = this.data.Count > this.ActualWidth ? (int)this.ActualWidth : this.data.Count;

                (double[], System.Drawing.Color)[] lines = new(double[], System.Drawing.Color)[3];
                lines[0] = (new double[sc], this.XColor);
                lines[1] = (new double[sc], this.YColor);
                lines[2] = (new double[sc], this.ZColor);

                int i = 0;
                foreach (var d in this.data.Skip(this.Data.Count - sc))
                {
                    lines[0].Item1[i] = d.X;
                    lines[1].Item1[i] = d.Y;
                    lines[2].Item1[i] = d.Z;
                    i++;
                }
                this.DrawCurves(lines);

            }
        }

        public void DrawCurves((double[], System.Drawing.Color)[] Lines)
        {
            if (!double.IsNaN(this.ActualWidth) && !double.IsNaN(this.ActualHeight) && this.ActualWidth >= 1.0 && this.ActualHeight >= 1.0)
            {
                var bitmap = new Bitmap((int)this.ActualWidth, (int)this.ActualHeight);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.FillRectangle(System.Drawing.Brushes.White, new Rectangle(0, 0, (int)ActualWidth, (int)ActualHeight));
                    graphics.Transform = new System.Drawing.Drawing2D.Matrix(1, 0, 0, -1, 0, 0);//Y轴向上为正，X向右为
                    graphics.TranslateTransform(0, (int)(ActualHeight / 2), MatrixOrder.Append);

                    using (var context = this.visual.RenderOpen())
                    {
                        foreach (var line in Lines)
                        {
                            this.DrawCurve(graphics, line.Item1, line.Item2, this.PenWidth, this.Tension);
                        }
                        context.DrawImage(this.ToBitmapImage(bitmap), new Rect(0.0, 0.0, this.ActualWidth, this.ActualHeight));
                    }
                }
            }
        }

        private void DrawCurve(Graphics graphics, double[] points, System.Drawing.Color color, float penWidth, float tension)
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
        /// Get number of children: VisualCollection count.
        /// If in-place editing textbox is active, add 1.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                int n = graphics.Count;
                return n;
            }
        }


        /// <summary>
        /// Get visual child - one of GraphicsBase objects
        /// or in-place editing textbox, if it is active.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= graphics.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return graphics[index];
        }


    }

}
