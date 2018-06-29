using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NineAxises
{

    public class DrawCanvas : Canvas
    {
        private VisualCollection graphics;
        private DrawingVisual visual;
        private Point start;

        public DrawCanvas()
        {
            graphics = new VisualCollection(this);
            visual = new DrawingVisual();
            graphics.Add(visual);
        }

        public int Y0 = 0;
        public int X0 = 0;

        public void DrawCurves(List<(List<double>,Brush,Pen)> Lines)
        {
            using (var context = this.visual.RenderOpen())
            {
                foreach(var line in Lines)
                {
                    StreamGeometry geometry = new StreamGeometry();
                    using (StreamGeometryContext ctx = geometry.Open())
                    {

                        ctx.BeginFigure(new Point(0, Y0), true, true);
                        for (int x = 0; x < this.Width; x++)
                        {
                            ctx.LineTo(new Point(x, line.Item1[x]), true, false);
                        }
                    }
                    context.DrawGeometry(line.Item2, line.Item3,geometry);
                }


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
