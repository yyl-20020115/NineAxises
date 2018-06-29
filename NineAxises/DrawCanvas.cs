using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NineAxises
{

    public class DrawCanvas : Canvas
    {
        private VisualCollection graphics;
        private DrawingVisual visual;
        public float Tension = 0.5f;
        public float PenWidth = 1.0f;

        public DrawCanvas()
        {
            graphics = new VisualCollection(this);
            visual = new DrawingVisual();
            graphics.Add(visual);
        }

        public void DrawCurves(List<(List<float>, System.Drawing.Color)> Lines)
        {
            var bitmap = new Bitmap((int)this.Width, (int)this.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.FillRectangle(System.Drawing.Brushes.Black, new Rectangle(0, 0, (int)Width, (int)Height));
                graphics.Transform = new System.Drawing.Drawing2D.Matrix(1, 0, 0, -1, 0, 0);//Y轴向上为正，X向右为
                graphics.TranslateTransform(0, (int)(Height / 2), MatrixOrder.Append);

                using (var context = this.visual.RenderOpen())
                {
                    foreach (var line in Lines)
                    {
                        this.DrawCurve(graphics,line.Item1,line.Item2,this.PenWidth, this.Tension);
                    }
                    context.DrawImage(this.ToBitmapImage(bitmap), new Rect(0.0,0.0,this.Width,this.Height)); 
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


        private void DrawCurve(Graphics graphics, List<float> points,System.Drawing.Color color,float penWidth,float tension)
        {
            using (System.Drawing.Pen CurvePen = new System.Drawing.Pen(color, penWidth))
            {
                PointF[] CurvePointF = new PointF[points.Count];
                float xSlice =(float) this.Width / points.Count;
                float yHeight = (float)this.Height / 2.0f;
                for (int i = 0; i < points.Count; i++)
                {
                    float x = xSlice * i;
                    float y = yHeight * points[i];
                    CurvePointF[i] = new PointF(x, y);
                }
                graphics.DrawCurve(CurvePen, CurvePointF, tension);
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
