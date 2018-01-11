using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NineAxises
{
    /// <summary>
    /// AxisDisplayerControl.xaml 的交互逻辑
    /// </summary>
    public partial class AxisDisplayerControl : UserControl
    {
        private string unitValue = string.Empty;
        private string unitAngle = string.Empty;

        private double D = 0.0;
        private double A = 0.0;
        private double P = 0.0;

        private double scaleFactor = 1.0;
        private double bodyRadius = 1.0;
        private double bodyThickness = 0.01;
        private double stickRaidus = 0.02;
        private double stickLength = 1.2;
        private int segments = 64;

        private Vector3D lastVector = default(Vector3D);
        private Vector3D zeroVector = default(Vector3D);

        private enum Op
        {
            None,
            Rotate,
            Redirect,
        }
        private Op lastOp = Op.None;

        public string Title
        {
            get { return this.TitleText.Text; }
            set { this.TitleText.Text = value ?? string.Empty;}
        }

        public string UnitValue { get => unitValue; set { unitValue = value; this.UpdateLastOp(); } }
        public string UnitAngle { get => unitAngle; set { unitAngle = value; this.UpdateLastOp(); } }
        public double ScaleFactor { get => scaleFactor; set { scaleFactor = value; this.BuildParts();  } }

        public double BodyRadius { get => bodyRadius; set { bodyRadius = value; this.BuildParts(); } }
        public double BodyThickness { get => bodyThickness; set { bodyThickness = value; this.BuildParts(); } }
        public double StickRaidus { get => stickRaidus; set { stickRaidus = value; this.BuildParts(); } }
        public double StickLength { get => stickLength; set { stickLength = value; this.BuildParts(); } }
        public int Segments { get => segments; set { segments = value; this.BuildParts(); } }

        public Vector3D ZeroVector { get => zeroVector; set => zeroVector = value; }

        public AxisDisplayerControl()
        {
            InitializeComponent();
        }

        public virtual void Look(Point3D SelfLocation)
        {
            this.Look(SelfLocation, new Point3D());
        }

        public virtual void Look(Point3D SelfLocation,Point3D TargetLocation)
        {
            this.Camera.Position = SelfLocation;
            this.Camera.LookDirection = TargetLocation - SelfLocation;
        }


        private void BuildParts()
        {
            this.Head.Geometry = this.BuildCylinder(this.StickRaidus, this.StickLength, this.Segments);
            this.Body.Geometry = this.BuildCylinder(this.BodyRadius, this.BodyThickness, this.Segments);
            this.Tail.Geometry = this.BuildCylinder(this.StickRaidus, -this.StickLength, this.Segments);
            this.Arm.Geometry = this.BuildCylinder(this.StickRaidus, this.StickLength, this.Segments);
        }
        private void UpdateLastOp()
        {
            if (this.lastOp != Op.None)
            {
                switch (this.lastOp)
                {
                    case Op.Redirect:
                        this.UpdateAsRedirect();
                        break;
                    case Op.Rotate:
                        this.UpdateAsRotate();
                        break;
                }
                this.lastOp = Op.None;
            }

        }
        public virtual void RotatePointerTo(Vector3D V)
        {
            this.RotatePointerTo(V.X, V.Y, V.Z);
        }
        /*
            滚转角（x轴）Roll
            俯仰角（y轴）Pitch
            偏航角（z轴）Yaw
          */
        public virtual void RotatePointerTo(double Roll, double Pitch, double Yaw)
        {
            this.lastVector.X = Roll;
            this.lastVector.Y = Pitch;
            this.lastVector.Z = Yaw;
            this.lastVector -= this.zeroVector;
            this.lastOp = Op.Rotate;

            this.YAxisRotation.Angle = this.lastVector.X;
            this.ZAxisRotation.Angle = this.lastVector.Y;
            this.XAxisRotation.Angle = this.lastVector.Z;

            this.UpdateAsRotate();
        }
        protected void UpdateAsRotate()
        {
            this.XValueText.Text = string.Format("Roll:  {0}{1}", this.lastVector.X, this.UnitAngle);
            this.YValueText.Text = string.Format("Pitch: {0}{1}", this.lastVector.Y, this.UnitAngle);
            this.ZValueText.Text = string.Format("Yaw:   {0}{1}", this.lastVector.Z, this.UnitAngle);
            this.DValueText.Text = string.Empty;
        }
        public virtual void RedirectPointerTo(Vector3D V)
        {
            this.lastVector = V - this.zeroVector;

            if ((D= this.lastVector.Length) > 0.0)
            {
                this.lastOp = Op.Redirect;

                A = Math.Asin(this.lastVector.Z / D);
                P = Math.Atan2(this.lastVector.Y, this.lastVector.X);

                //No XAxisRotation needed
                this.YAxisRotation.Angle = A * 180.0 / Math.PI;
                this.ZAxisRotation.Angle = P * 180.0 / Math.PI;

                this.HeadLengthScale.ScaleZ 
                    = this.TailLengthScale.ScaleZ 
                    = D * this.ScaleFactor;

            }
            this.UpdateAsRedirect();
        }
        private void UpdateAsRedirect()
        {
            this.XValueText.Text = string.Format("X: {0}{1}", this.lastVector.X, this.UnitValue);
            this.YValueText.Text = string.Format("Y: {0}{1}", this.lastVector.Y, this.UnitValue);
            this.ZValueText.Text = string.Format("Z: {0}{1}", this.lastVector.Z, this.UnitValue);
            this.DValueText.Text = string.Format("D: {0}{1}", D, this.UnitValue);
            this.AValueText.Text = string.Format("A: {0}{1}", A, this.UnitAngle);
            this.PValueText.Text = string.Format("P: {0}{1}", P, this.UnitAngle);

        }
        private void UserControl_Initialized(object sender, EventArgs e)
        {
            this.BuildParts();
        }
        protected virtual MeshGeometry3D BuildDisk(double R, double Delta)
        {
            var pc = new Point3DCollection();
            var tc = new Int32Collection();

            double x1 = R * Math.Cos(0.0);
            double y1 = R * Math.Sin(0.0);

            int idx = 0;
            for (double d = Delta; d <= 2.0 * Math.PI; d += Delta)
            {
                double x2 = R * Math.Cos(d);
                double y2 = R * Math.Sin(d);
                pc.Add(new Point3D(0.0, 0.0, 0.0));
                pc.Add(new Point3D(x1, y1, 0.0));
                pc.Add(new Point3D(x2, y2, 0.0));
                tc.Add(idx++);
                tc.Add(idx++);
                tc.Add(idx++);
                x1 = x2;
                y1 = y2;
            }

            return new MeshGeometry3D
            {
                Positions = pc,
                TriangleIndices = tc
            };
        }
        protected virtual MeshGeometry3D BuildCube(double length, double width, double height, double x = 0, double y = 0, double z = 0)
        {
            List<Point3D> Points = new List<Point3D>();
            List<int> Indices = new List<int>();

            Points.Add(new Point3D(length / 2 + x, width / 2 + y, height / 2 + z));
            Points.Add(new Point3D(length / 2 + x, width / 2 + y, -height / 2 + z));
            Points.Add(new Point3D(-length / 2 + x, width / 2 + y, -height / 2 + z));
            Points.Add(new Point3D(-length / 2 + x, width / 2 + y, height / 2 + z));
            Points.Add(new Point3D(length / 2 + x, -width / 2 + y, height / 2 + z));
            Points.Add(new Point3D(length / 2 + x, -width / 2 + y, -height / 2 + z));
            Points.Add(new Point3D(-length / 2 + x, -width / 2 + y, -height / 2 + z));
            Points.Add(new Point3D(-length / 2 + x, -width / 2 + y, height / 2 + z));

            //f0
            Indices.Add(0);
            Indices.Add(2);
            Indices.Add(3);
            //
            Indices.Add(0);
            Indices.Add(1);
            Indices.Add(2);

            //f1
            Indices.Add(4);
            Indices.Add(3);
            Indices.Add(7);
            //
            Indices.Add(4);
            Indices.Add(0);
            Indices.Add(3);

            //f2
            Indices.Add(5);
            Indices.Add(0);
            Indices.Add(4);
            //
            Indices.Add(5);
            Indices.Add(1);
            Indices.Add(0);

            //f3
            Indices.Add(6);
            Indices.Add(1);
            Indices.Add(5);
            //
            Indices.Add(6);
            Indices.Add(2);
            Indices.Add(1);

            //f4
            Indices.Add(7);
            Indices.Add(2);
            Indices.Add(6);
            //
            Indices.Add(7);
            Indices.Add(3);
            Indices.Add(2);

            //f5
            Indices.Add(4);
            Indices.Add(6);
            Indices.Add(5);
            //
            Indices.Add(4);
            Indices.Add(7);
            Indices.Add(6);


            return new MeshGeometry3D
            {
                Positions = new Point3DCollection(Points),
                TriangleIndices = new Int32Collection(Indices)
            };
        }
        protected virtual MeshGeometry3D BuildCone(double radius, double height, int segments = 64, double x = 0, double y = 0, double z = 0, bool sideOnly = false)
        {
            List<Point3D> Points = new List<Point3D>();
            List<int> Indices = new List<int>();
            Points.Add(new Point3D(x, y, z));
            Points.Add(new Point3D(x, y, z + height));
            for (int i = 0; i < segments; i++)
            {
                double d = 2 * Math.PI / segments * i;
                Points.Add(new Point3D(Math.Cos(d) * radius + x, Math.Sin(d) * radius + y, z));
            }

            for (int i = 2; i < segments + 2; i++)
            {
                int j = i + 1;

                if (j >= segments + 2)
                {
                    j = 2;
                }

                if (!sideOnly)
                {
                    //bottom
                    Indices.Add(0);
                    Indices.Add(i);
                    Indices.Add(j);
                }
                //side
                Indices.Add(1);
                Indices.Add(j);
                Indices.Add(i);

            }
            return new MeshGeometry3D
            {
                Positions = new Point3DCollection(Points),
                TriangleIndices = new Int32Collection(Indices)
            };
        }
        protected virtual MeshGeometry3D BuildCylinder(double radius, double height, int segments = 64, double x = 0, double y = 0, double z = 0, bool sideOnly = false)
        {
            List<Point3D> Points = new List<Point3D>();
            List<int> Indices = new List<int>();
            Points.Add(new Point3D(x, y, z));
            Points.Add(new Point3D(x, y, z + height));
            for (int i = 0; i < segments; i++)
            {
                double d = 2 * Math.PI / segments * i;

                double lx = Math.Cos(d) * radius;
                double ly = Math.Sin(d) * radius;

                Points.Add(new Point3D(lx + x, ly + y,  z));
                Points.Add(new Point3D(lx + x, ly + y,  z + height));
            }

            for (int i = 0; i < segments; i++)
            {
                int j = i + 1;

                if (j >= segments)
                {
                    j = 0;
                }

                if (!sideOnly)
                {
                    //bottom
                    Indices.Add(0);
                    Indices.Add((i + 1) * 2);
                    Indices.Add((j + 1) * 2);

                    //top
                    Indices.Add(1);
                    Indices.Add((j + 1) * 2 + 1);
                    Indices.Add((i + 1) * 2 + 1);
                }
                //sides
                Indices.Add((j + 1) * 2);
                Indices.Add((i + 1) * 2);
                Indices.Add((i + 1) * 2 + 1);

                Indices.Add((i + 1) * 2 + 1);
                Indices.Add((j + 1) * 2 + 1);
                Indices.Add((j + 1) * 2);
            }
            return new MeshGeometry3D
            {
                Positions = new Point3DCollection(Points),
                TriangleIndices = new Int32Collection(Indices)
            };
        }
        protected virtual MeshGeometry3D BuildHalfCone(double radiusBottom, double radiusTop, double height, int segments = 64, double x = 0, double y = 0, double z = 0, bool sideOnly = false)
        {
            List<Point3D> Points = new List<Point3D>();

            List<int> Indices = new List<int>();
            Points.Add(new Point3D(x, y, z));
            Points.Add(new Point3D(x, y, z + height));
            for (int i = 0; i < segments; i++)
            {
                double d = 2 * Math.PI / segments * i;

                double lxb = Math.Cos(d) * radiusBottom;
                double lyb = Math.Sin(d) * radiusBottom;

                double lxt = Math.Cos(d) * radiusTop;
                double lyt = Math.Sin(d) * radiusTop;

                Points.Add(new Point3D(lxb + x, lyb + y, z));
                Points.Add(new Point3D(lxt + x, lyt + y, z + height));
            }

            for (int i = 0; i < segments; i++)
            {
                int j = i + 1;

                if (j >= segments)
                {
                    j = 0;
                }

                if (!sideOnly)
                {
                    //bottom
                    Indices.Add(0);
                    Indices.Add((i + 1) * 2);
                    Indices.Add((j + 1) * 2);

                    //top
                    Indices.Add(1);
                    Indices.Add((j + 1) * 2 + 1);
                    Indices.Add((i + 1) * 2 + 1);
                }
                //sides
                Indices.Add((j + 1) * 2);
                Indices.Add((i + 1) * 2);
                Indices.Add((i + 1) * 2 + 1);

                Indices.Add((i + 1) * 2 + 1);
                Indices.Add((j + 1) * 2 + 1);
                Indices.Add((j + 1) * 2);
            }
            return new MeshGeometry3D
            {
                Positions = new Point3DCollection(Points),
                TriangleIndices = new Int32Collection(Indices)
            };
        }
        protected virtual MeshGeometry3D BuildSphere(double radius, int segments = 64, double x = 0.0, double y = 0.0, double z = 0.0)
        {
            List<Point3D> Points = new List<Point3D>();

            List<int> Indices = new List<int>();
            double d = 2 * Math.PI / segments;
            double h = 0.0;
            double dh = 0.0;
            double ly = radius;
            double rb = radius;
            double rt = 0.0;
            double h2 = 0.0;
            rt = Math.Cos(d) * radius;
            dh = Math.Sin(d) * radius;

            for (int i = -segments / 4; i < segments / 4; i++)
            {
                if (i == segments / 4 - 1 || i == -segments / 4 + 1)
                {
                    rt = 0.0;
                }

                var t = this.BuildHalfCone(rb, rt, dh, segments, x, h2, z, true);

                rb = rt;

                rt = Math.Cos(d * (i + 1)) * radius;

                h2 += dh;

                double nh = Math.Sin(d * (i + 1)) * radius;

                dh = nh - h;

                h = nh;

                Points.AddRange(t.Positions);

                Indices.AddRange(t.TriangleIndices.Select(idx => idx + Points.Count));
            }

            return new MeshGeometry3D
            {
                Positions = new Point3DCollection(Points),
                TriangleIndices = new Int32Collection(Indices)
            };
        }

        private void ZeroCheckBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.ZeroCheckBox.IsChecked.GetValueOrDefault(false))
            {
                this.ZeroVector = this.lastVector;
            }
            else
            {
                this.ZeroVector = default(Vector3D);
            }
        }
    }
}
