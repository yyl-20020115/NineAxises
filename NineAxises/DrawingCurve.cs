using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NineAxises
{
    public class DrawingCurve
    {
        private Graphics graphics; //Graphics 类提供将对象绘制到显示设备的方法
        private Bitmap bitmap; //位图对象
        private int timeLine = 60;//60s
        private int canvasWidth = 600;//画布长度
        private int sliceCount = 0;//刻度分段个数 = timeLine
        private int xSlice = 10;//X轴刻度分端宽度
        private int xSliceHeight = 10;//X轴刻度高度
        private float tension = 0.5f; //张力系数
        private bool showX = true;
        private bool showY = true;
        private bool showZ = true;

        //Queue<PointF> que = new Queue<PointF>();//曲线fifo
        /// <summary>
        /// 构造函数
        /// </summary>
        public DrawingCurve()
        {
            this.xSlice = this.canvasWidth / timeLine;
        }

        /// <summary>
        /// 绘制画布
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public Bitmap DrawCanvas(int width, int height, List<float> points)
        {
            if (bitmap != null)
            {
                bitmap.Dispose();
                bitmap = null;
            }

            bitmap = new Bitmap(width, height);
            graphics = Graphics.FromImage(bitmap);
            graphics.FillRectangle(Brushes.Black, new Rectangle(0, 0, width, height));
            graphics.Transform = new Matrix(1, 0, 0, -1, 0, 0);//Y轴向上为正，X向右为
            graphics.TranslateTransform(0, height / 2, MatrixOrder.Append);

            Pen pen = new Pen(Color.Red, 1);
            pen.DashStyle = DashStyle.Custom;
            pen.DashPattern = new float[] { 2, 2 };
            graphics.DrawLine(pen, new Point(0, height / 4), new Point(width, height / 4));
            graphics.DrawLine(pen, new Point(0, height / -4), new Point(width, height / -4));
            graphics.DrawLine(new Pen(Color.GreenYellow, 1), new Point(0, 0), new Point(width, 0));
            graphics.DrawString("0", new Font("Vendara", 10), Brushes.White, new Point(0, -15));
            graphics.DrawString("+", new Font("Vendara", 10), Brushes.White, new Point(0, height / 4));
            graphics.DrawString("-", new Font("Vendara", 10), Brushes.White, new Point(0, height / -4 - 15));
            graphics.Transform = new Matrix(1, 0, 0, 1, 0, 0);//Y轴向上为正，X向右为
            graphics.TranslateTransform(0, height / 2, MatrixOrder.Append);
            graphics.DrawString("-59s", new Font("Vendara", 8), Brushes.White, new Point(0, height / 2 - 15));
            graphics.DrawString("0s", new Font("Vendara", 8), Brushes.White, new Point(width - 20, height / 2 - 15));
            for (int i = 0; i < timeLine; i++)
            {
                int scale = i * xSlice;
                graphics.DrawLine(new Pen(new SolidBrush(Color.Blue)), 0 + scale, 0 + xSliceHeight * 0.1f, 0 + scale, 0 - xSliceHeight * 0.1f);
            }

            graphics.Transform = new Matrix(-1, 0, 0, -1, 0, 0);//Y轴向上为正，X向右为
            graphics.TranslateTransform(width, height / 2, MatrixOrder.Append);

            if (showX) DrawX(graphics, points);
            if (showY) DrawY(graphics, points);
            if (showZ) DrawZ(graphics, points);
            graphics.Dispose();
            return bitmap;
        }

        #region 绘制曲线
        private void DrawX(Graphics graphics, List<float> points)
        {
            Pen CurvePen = new Pen(Color.Cyan, 2);
            PointF[] CurvePointF = new PointF[points.Count];
            float keys = 0;
            float values = 0;
            for (int i = 0; i < points.Count; i++)
            {
                keys = xSlice * i;
                values = 10 * (points[i] / 10);
                CurvePointF[i] = new PointF(keys, values);
            }
            graphics.DrawCurve(CurvePen, CurvePointF, this.tension);
        }

        private void DrawY(Graphics graphics, List<float> points)
        {
            Pen CurvePen = new Pen(Color.Purple, 2);
            PointF[] CurvePointF = new PointF[points.Count];
            float keys = 0;
            float values = 0;
            for (int i = 0; i < points.Count; i++)
            {
                keys = xSlice * i;
                values = 10 * (points[i] / 10);
                CurvePointF[i] = new PointF(keys, values);
            }
            graphics.DrawCurve(CurvePen, CurvePointF, this.tension);
        }

        private void DrawZ(Graphics graphics, List<float> points)
        {
            Pen CurvePen = new Pen(Color.OrangeRed, 2);
            PointF[] CurvePointF = new PointF[points.Count];
            float keys = 0;
            float values = 0;
            for (int i = 0; i < points.Count; i++)
            {
                keys = xSlice * i;
                values = 10 * (points[i] / 10);
                CurvePointF[i] = new PointF(keys, values);
            }
            graphics.DrawCurve(CurvePen, CurvePointF, this.tension);
        }

        /// <summary>
        /// 曲线开关
        /// </summary>
        /// <param name="_xyz"></param>
        /// <param name="show"></param>
        public void HideCurve(string _xyz, bool show)
        {
            switch (_xyz)
            {
                case "x":
                    showX = show;
                    break;
                case "y":
                    showY = show;
                    break;
                case "z":
                    showZ = show;
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
