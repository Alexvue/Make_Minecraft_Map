using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Class
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool is_pressed_W = false;
        bool is_pressed_A = false;
        bool is_pressed_S = false;
        bool is_pressed_D = false;
        bool is_pressed_LeftShift = false;
        bool is_pressed_Space = false;
        double latitude, longitude;
        Point prev_mouse_pos;
        Vector[,] grid;
        double cube_size = 0.1;
        double field_size = 3;

        public MainWindow()
        {
            InitializeComponent();
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 25)
            };
            timer.Tick += new EventHandler(TimerTick);
            timer.Start();
            SetupCam();
        }

        private double Fade(double t)
        {
            return 6 * t * t * t * t * t - 15 * t * t * t * t + 10 * t * t * t;
        }

        private double GetWeightedAverage(double a, double b, double weight)
        {
            return a + Fade(weight) * (b - a);
        }

        void setup_vectors(int width, int height)
        {
            grid = new Vector[width, height];
            Random random = new Random();

            Vector[] availableGradients = new Vector[] { new Vector(1, 0), new Vector(-1, 0),
                                                    new Vector(0, 1), new Vector(0, -1)  };

            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    grid[i, j] = availableGradients[random.Next(3)];
                }
            }
        }

        double get_perlin_noise(double x, double y)
        {
            int x0 = (int)x;
            double dx = x - x0;
            int y0 = (int)y;
            double dy = y - y0;

            var vx0y0 = grid[x0, y0];
            var vx0y1 = grid[x0, y0 + 1];
            var vx1y0 = grid[x0 + 1, y0];
            var vx1y1 = grid[x0 + 1, y0 + 1];
            var s = vx0y0 * (new Vector(x - x0, y - y0));
            var t = vx1y0 * (new Vector(x - x0 - 1, y - y0));
            var u = vx0y1 * (new Vector(x - x0, y - y0 - 1));
            var v = vx1y1 * (new Vector(x - x0 - 1, y - y0 - 1));

            var a = GetWeightedAverage(s, t, dx);
            var b = GetWeightedAverage(u, v, dx);
            var c = GetWeightedAverage(a, b, dy);

            return c;
        }

        private void SetupCam()
        {
            double x = cam.LookDirection.X;
            double y = cam.LookDirection.Y;
            double z = cam.LookDirection.Z;
                
            double r = Math.Sqrt(x * x + y * y + z * z);
            latitude = Math.Acos(y / r) * 180 / Math.PI;
            longitude = Math.Atan(x / z) * 180 / Math.PI + 180;
        }

        private void TimerTick(object sender, EventArgs e)
        {
            double rot = 0;
            double up_down = 0;

            if (is_pressed_A)
            {
                rot = 90;
            }
            if (is_pressed_D)
            {
                rot = -90;
            }
            if (is_pressed_W)
            {
                rot = rot / 2;
            }
            if (is_pressed_S)
            {
                rot = 180 - rot / 2;
            }
            if (is_pressed_LeftShift)
            {
                up_down = 1;
            }
            if (is_pressed_Space)
            {
                up_down = -1;
            }

            rot = (longitude - rot) % 360;

            if (is_pressed_W || is_pressed_S || is_pressed_A || is_pressed_D)
            {
                double r_x = Math.Cos(rot * (Math.PI / 180));
                double r_z = Math.Sin(rot * (Math.PI / 180));
                cam.Position = new Point3D(cam.Position.X + r_x / 10,
                cam.Position.Y + up_down / 10,
                cam.Position.Z + r_z / 10);
            }else if (is_pressed_LeftShift || is_pressed_Space)
            {
               cam.Position = new Point3D(cam.Position.X,
               cam.Position.Y + up_down / 10,
               cam.Position.Z);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W) is_pressed_W = true;
            if (e.Key == Key.S) is_pressed_S = true;
            if (e.Key == Key.A) is_pressed_A = true;
            if (e.Key == Key.D) is_pressed_D = true;
            if (e.Key == Key.LeftShift) is_pressed_LeftShift = true;
            if (e.Key == Key.Space) is_pressed_Space = true;
            if (e.Key == Key.R)
            {
                mainViewport.Children.Clear();
                Model3DGroup myModel3DGroup = new Model3DGroup(); 
                ModelVisual3D myModelVisual3D = new ModelVisual3D(); 
                PointLight light = new PointLight(Colors.White, new Point3D(0, 5, 0)); 
                myModel3DGroup.Children.Add(light); 
                myModelVisual3D.Content = myModel3DGroup; 
                mainViewport.Children.Add(myModelVisual3D);
                setup_vectors((int)field_size, (int)field_size);
                window.Title = "Регенерация...";
                for (double x = 0; x < field_size - 1; x += cube_size)
                {
                    for (double y = 0; y < field_size - 1; y += cube_size)
                    {// Вызываем функцию для генерации значения
                        double height = get_perlin_noise(x, y);
                        if (height < -0.2)
                        {
                            drawCubeByOnePoint(new Point3D(x, height, y), cube_size, Colors.Blue, mainViewport);
                        }else if ((height >= -0.2) && (height <= -0.1))
                        {
                            drawCubeByOnePoint(new Point3D(x, height, y), cube_size, Colors.Yellow, mainViewport);
                        }else if ((height >= -0.1) && (height <= 0))
                        {
                            drawCubeByOnePoint(new Point3D(x, height, y), cube_size, Colors.GreenYellow, mainViewport);
                        }else if ((height >= 0) && (height <= 0.3))
                        {
                            drawCubeByOnePoint(new Point3D(x, height, y), cube_size, Colors.LimeGreen, mainViewport);
                        }
                        else
                        {
                            drawCubeByOnePoint(new Point3D(x, height, y), cube_size, Colors.Green, mainViewport);
                        }
                    }
                }
                window.Title = "Регенерация завершена!";
                Thread.Sleep(2000);
                window.Title = "Введение в 3D";
            }
        }

        private void drawCubeByOnePoint(Point3D center, double dist, Color color, Viewport3D viewport)
        {
            Point3D p1 = new Point3D(center.X - dist / 2, center.Y - dist / 2, center.Z - dist / 2);
            Point3D p2 = new Point3D(center.X - dist / 2, center.Y - dist / 2, center.Z + dist / 2);
            Point3D p3 = new Point3D(center.X + dist / 2, center.Y - dist / 2, center.Z - dist / 2);
            Point3D p4 = new Point3D(center.X + dist / 2, center.Y - dist / 2, center.Z + dist / 2);
            Point3D p5 = new Point3D(center.X - dist / 2, center.Y + dist / 2, center.Z - dist / 2);
            Point3D p6 = new Point3D(center.X - dist / 2, center.Y + dist / 2, center.Z + dist / 2);
            Point3D p7 = new Point3D(center.X + dist / 2, center.Y + dist / 2, center.Z - dist / 2);
            Point3D p8 = new Point3D(center.X + dist / 2, center.Y + dist / 2, center.Z + dist / 2);
            drawCube(p1, p2, p3, p4, p5, p6, p7, p8, color, viewport);
        }

        private void drawCube(Point3D p1, Point3D p2, Point3D p3, Point3D p4, Point3D p5, Point3D p6, Point3D p7, Point3D p8, Color color, Viewport3D viewport)
        {
            drawRectangle(p6, p2, p4, p8, color, viewport);
            drawRectangle(p8, p4, p3, p7, color, viewport);
            drawRectangle(p7, p3, p1, p5, color, viewport);
            drawRectangle(p5, p1, p2, p6, color, viewport);
            drawRectangle(p5, p6, p8, p7, color, viewport);
            drawRectangle(p1, p2, p4, p3, color, viewport);
        }

        private void drawRectangle(Point3D p1, Point3D p2, Point3D p3, Point3D p4, Color color, Viewport3D viewport)
        {
            drawTriangle(p1, p2, p4, color, viewport);
            drawTriangle(p2, p3, p4, color, viewport);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            prev_mouse_pos = e.GetPosition(this);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point curr_mouse_pos = e.GetPosition(this);

                double dx = curr_mouse_pos.X - prev_mouse_pos.X;
                double dy = curr_mouse_pos.Y - prev_mouse_pos.Y;

                longitude = (longitude + dx) % 360;
                latitude = latitude + dy;
                if (latitude < 0)
                {
                    latitude = 0;
                }
                if (latitude > 180)
                {
                    latitude = 180;
                }

                double p_x = Math.Sin(latitude * (Math.PI / 180)) * Math.Cos(longitude * (Math.PI / 180));
                double p_y = Math.Sin(latitude * (Math.PI / 180)) * Math.Sin(longitude * (Math.PI / 180));
                double p_z = Math.Cos(latitude * (Math.PI / 180));
                cam.LookDirection = new Vector3D(p_x, p_z, p_y);

                prev_mouse_pos = curr_mouse_pos;
            }
                
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W) is_pressed_W = false;
            if (e.Key == Key.S) is_pressed_S = false;
            if (e.Key == Key.A) is_pressed_A = false;
            if (e.Key == Key.D) is_pressed_D = false;
            if (e.Key == Key.LeftShift) is_pressed_LeftShift = false;
            if (e.Key == Key.Space) is_pressed_Space = false;
        }

        public static void drawTriangle(Point3D p1, Point3D p2, Point3D p3,
                                Color color, Viewport3D viewport)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            SolidColorBrush brush = new SolidColorBrush(color);
            Material material = new DiffuseMaterial(brush);

            GeometryModel3D geometry = new GeometryModel3D(mesh, material);
            ModelUIElement3D model = new ModelUIElement3D();
            model.Model = geometry;

            viewport.Children.Add(model);
        }
    }
}
