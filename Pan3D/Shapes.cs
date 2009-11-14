using System;
using System.Collections.Generic;
using System.Text;

namespace Terry
{
    interface IShape
    {
        bool Test();
    }

    public class Shapes
    {
        public enum ShapeType { Ball, Balls, Caustic, StaticMandlebrot, ResolvingMandlebrot, ZoomingMandlebrot, Dots, Squares };

        protected Caustic caustic = new Caustic();

        static List<ShapeType> TimeDependentShapes =
            new List<ShapeType>() 
            { ShapeType.Caustic
                ,ShapeType.Balls
                ,ShapeType.ResolvingMandlebrot
                ,ShapeType.ZoomingMandlebrot
                ,ShapeType.Squares
            };

        public static bool IsTimeDependent(ShapeType s) { return TimeDependentShapes.Contains(s); }

        public float Test(ShapeType shape, Point p, int resolution, float time)
        {
            float x = (float)p.i / resolution, y = (float)p.j / resolution, z = (float)p.k / resolution;

            switch (shape)
            {
                case ShapeType.Ball:
                    //sphere
                    return (float)(resolution * resolution) / (p * p);
                case ShapeType.Balls:
                    {
                        const float size = 3;
                        Point p1 = p + new Point(0, -resolution, 0);
                        Point p2 = p + new Point(0, resolution, 0);
                        float x2 = x + 2 * (float)Math.Sin(time);

                        return 1f / size / (float)System.Math.Sqrt(1e-4 + x * x + (y - 1) * (y - 1) + z * z)
                            + 1f / size / (float)System.Math.Sqrt(1e-4 + x * x + (y + 1) * (y + 1) + z * z)
                            + 1f / size / (float)System.Math.Sqrt(1e-4 + (x2 * x2 + y * y + z * z));
                    }
                case ShapeType.Caustic:
                    return 1/caustic.Calc(x, y, z);

                case ShapeType.StaticMandlebrot:
                case ShapeType.ResolvingMandlebrot:
                case ShapeType.ZoomingMandlebrot:
                    //crop to sphere
                    if (x * x + y * y + z * z > 4) return 0f;
                    //mandlebrot, starting from k,0  using offsets i and j

                    if (shape == ShapeType.ZoomingMandlebrot)
                    {
                        float zoom = (float)time / 30f;
                        x = (((float)p.i / resolution) + .87591f * zoom) / (1f + zoom * 5f);
                        y = (((float)p.j / resolution) - .3f * zoom) / (1f + zoom * 5f);
                        z = ((float)p.k / resolution) / (1f + zoom * 2f);
                    }
                    else
                    {
                        x = (float)p.i / resolution;
                        y = (float)p.j / resolution;
                        z = (float)p.k / resolution;
                    }

                    double i = z, i2 = z * z, j = 0, j2 = 0;
                    int iter = 1;

                    int maxiter = 100;
                    if (shape == ShapeType.ResolvingMandlebrot)
                    {
                        int iterlimit = 20;
                        maxiter = ((int)(time*3)) % (iterlimit*2);
                        if(maxiter > iterlimit) maxiter = (2*iterlimit)+1-maxiter;
                        maxiter = maxiter + 4; // *maxiter;
                    }

                    while (iter < maxiter && i2 + j2 < 4)
                    {
                        double temp = i2 - j2 + x;
                        j = 2 * i * j + y;
                        i = temp;
                        i2 = i * i;
                        j2 = j * j;
                        iter++;
                    }
                    return (float)iter / (maxiter-2);

                case ShapeType.Dots:
                    {
                        int size = 10;
                        if ((float)(resolution * resolution) < (p * p))
                            return 0;
                        else
                            return ((Math.Abs(x * size) % 2) < 1 ^ (Math.Abs(y * size) % 2) < 1 ^ (Math.Abs(z * size) % 2) < 1) ? 2 : 0;
                    }
                case ShapeType.Squares:
                    {
                        int size = 2;
                        if (x * x + y * y + z * z > Math.Abs(3 * Math.Sin(time/6))) return 0;
                        return ((Math.Abs(x * size) % 2) < 1 ^ (Math.Abs(y * size) % 2) < 1 ^ (Math.Abs(z * size) % 2) < 1) ? 2 : 0;
                    }
                default:
                    return 0f;

            }
        }

        public void Tick(double t)
        {
            caustic.Tick(t);
        }
    }
}
