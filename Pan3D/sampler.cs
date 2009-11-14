// sample 3D points matching a bool function and build a metasurface
// terry spitz (c) 2006

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Collections.Generic;

namespace Terry
{

    enum Axis { X, Y, Z }

    class Edge
    {
        public Edge() { start = new Point();  }
        public Edge Clone() 
        { 
            Edge newEdge = (Edge)this.MemberwiseClone();
            newEdge.start = this.start;//.Clone();
            return newEdge;
        }
        //public Edge(Point p) { i=p.i; j=p.j; k=p.k; }

        public Point start;
        public Axis direction;
        public bool forward;

        public Point next()
        {
            Point p = start;//.Clone();
            switch (direction)
            {
                case Axis.X: p.i += forward ? 1 : -1; break;
                case Axis.Y: p.j += forward ? 1 : -1; break;
                case Axis.Z: p.k += forward ? 1 : -1; break;
            }
            return p;
        }
    }
    class EdgeComparer : IEqualityComparer<Edge>
    {
        public bool Equals(Edge lhs, Edge rhs)
        {
            return new PointComparer().Equals(lhs.start, rhs.start) && lhs.direction == rhs.direction;
        }
        public int GetHashCode(Edge e)
        {
            return e.start.GetHashCode() + (int)e.direction << 5;
        }
    }

    class EdgeList : LinkedList<Edge>
    {
        /// <summary>
        /// create a set of edges to use to continue testing the border
        /// </summary>
        /// <param name="edge"></param>
        public void AddNext(Edge edge)
        {
            Edge oldEdge = edge;
            Edge newEdge = new Edge();
            newEdge.start = edge.next();
            for (Axis a = Axis.X; a <= Axis.Z; a++)
            {
                if (a != edge.direction)
                {
                    oldEdge.direction = a;
                    oldEdge.forward = true; AddLast(oldEdge.Clone());
                    oldEdge.forward = false; AddLast(oldEdge.Clone());
                    newEdge.direction = a;
                    newEdge.forward = true; AddLast(newEdge.Clone());
                    newEdge.forward = false; AddLast(newEdge.Clone());
                }
            }
            newEdge = edge;
            for (int a = 0; a < 9; a++)
            {
                if (a == 4) continue;
                switch (edge.direction)
                {
                    case Axis.X:
                        newEdge.start.j = edge.start.j + a % 3 - 1;
                        newEdge.start.k = edge.start.k + a / 3 - 1;
                        break;
                    case Axis.Y:
                        newEdge.start.i = edge.start.i + a % 3 - 1;
                        newEdge.start.k = edge.start.k + a / 3 - 1;
                        break;
                    case Axis.Z:
                        newEdge.start.i = edge.start.i + a % 3 - 1;
                        newEdge.start.j = edge.start.j + a / 3 - 1;
                        break;
                }
                AddLast(newEdge.Clone());
            }
        }
    }

    class Sampler
    {
        enum DrawTypes { Points = 1 << 0, Lines = 1 << 1, Faces = 1 << 2 };
        public enum Shapes { Ball, Balls, Caustic, Mandlebrot };

        public Shapes shape = Shapes.Caustic;

        //struct VectorNormal { float v0, v1, v2, n0, n1, n2; };

        protected EdgeList work = new EdgeList();
        protected Dictionary<Point, bool> points = new Dictionary<Point, bool>(new PointComparer());
        protected Dictionary<Edge, bool> lines = new Dictionary<Edge,bool>(new EdgeComparer());
        
        protected Dictionary<Point, int> cubes = new Dictionary<Point, int>(new PointComparer());
        protected Stack<Point> cubelist = new Stack<Point>();

        protected int resolution = 10;
        
        public int Resolution
        {
            get { return resolution; }
            set
            {
                resolution = value;
                if (shape == Shapes.Caustic)
                    caustic.Init(resolution);

            }
        }
        protected Caustic caustic = new Caustic();

        public void calculate()
        {
            work.Clear();
            points.Clear();
            lines.Clear();
            cubes.Clear();
            cubelist.Clear();

            //probe for starting points
            for(int i=-5; i<5; i++)
                for (int j = -5; j < 5; j++)
                {
                    Point p = new Point(2 * resolution + 5, 2 * resolution * i/5, 2 * resolution *j/5);
                    while (!doTest(p) && p.i > -2*resolution)
                        p.i -= 1;
                    if(p.i > -2*resolution)
                        cubelist.Push(p);
                }
            iterateCubes();
        }

        public void iterateCubes()
        {
            while (cubelist.Count > 0)
            {
                Point p = cubelist.Pop();
                int cornerset = testCube(p);
                if(!cubes.ContainsKey(p))
                    cubes.Add(p, cornerset);

                //push more interesting cubes to test
                if (cornerset > 0 && cornerset < 255)
                {
                    addCube(new Point(p.i + 1, p.j, p.k));
                    addCube(new Point(p.i - 1, p.j, p.k));
                    addCube(new Point(p.i, p.j+1, p.k));
                    addCube(new Point(p.i, p.j-1, p.k));
                    addCube(new Point(p.i, p.j, p.k+1));
                    addCube(new Point(p.i, p.j, p.k-1));
                }
            }
        }

        void addCube(Point p)
        {
            if (!cubes.ContainsKey(p))
            {
                cubelist.Push(p);
            }
        }

        void iterateEdges()
        {
            while (work.Count > 0)
            {
                bool oldSeen, newSeen;
                Edge edge = work.First.Value;
                bool oldTest = test(edge.start, out oldSeen);

                Point newpoint = edge.next();
                bool newTest = test(newpoint, out newSeen);
                if (!oldSeen || !newSeen || !lines.ContainsKey(edge))
                {
                    lines.Add(edge, newTest != oldTest);
                    if (newTest != oldTest)
                        work.AddNext(edge);
                }

                work.RemoveFirst();
            }
        }


        /// <summary>
        /// lookup or test a point
        /// </summary>
        /// <param name="p"></param>
        /// <param name="found"></param>
        /// <returns></returns>
        bool test(Point p, out bool found)
        {
            bool pointVal;
            if (!points.TryGetValue(p, out pointVal))
            {
                found = false;
                pointVal = doTest(p);
                points.Add(p, pointVal);
                return pointVal;
            }
            else
            {
                found = true;
                return pointVal;
            }
        }

        int testCube(Point p)
        {
            int cornerset = 0;
            for (int v = 0; v < 8; v++)
            {
                Point corner = p.bumpCorner(v);
                bool found;
                if(test(corner, out found))
                    cornerset += 1 << v;
            }
            return cornerset;
        }

        bool doTest(Point p)
        {
            switch (shape)
            {
                case Shapes.Ball:
                    //sphere
                    return (p * p ) < resolution*resolution;
                case Shapes.Balls:
                    {
                        Point p1 = p + new Point(0, -resolution, 0);
                        Point p2 = p + new Point(0, resolution, 0);
                        Point p3 = p + new Point((int)(resolution * 2*0.866),0, 0);

                        return (resolution/2 / System.Math.Sqrt(1e-4 + p1 * p1)
                            + resolution/2 / System.Math.Sqrt(1e-4 + p2 * p2)
                            + resolution/2 / System.Math.Sqrt(1e-4 + p3 * p3)
                            ) > 1.2;
                    }
                case Shapes.Caustic:
                    return caustic.Calc(p);

                case Shapes.Mandlebrot:
                    //mandlebrot, starting from k,0  using offsets i and j
                    double x = (double)p.k / resolution, x2 = x * x, y = 0, y2 = y * y;
                    double cx = (double)p.i / resolution, cy = (double)p.j / resolution;
                    int iter = 0, maxiter = 50;
                    while (iter < maxiter && x2 + y2 < 100)
                    {
                        double temp = x2 - y2 + cx;
                        y = 2 * x * y + cy;
                        x = temp;
                        x2 = x * x;
                        y2 = y * y;
                        iter++;
                    }
                    return (iter == maxiter);
                default:
                    return false;

            }
        }

        public CustomVertex.PositionNormal[] GetSurface(Cube cube)
        {
            List<CustomVertex.PositionNormal> surface = new List<CustomVertex.PositionNormal>();
            foreach (KeyValuePair<Point, int> v in cubes)
            {
                int index = v.Value;
                AddTriangles(ref surface, v.Key, cube.triangles[index], cube.normals[index], true);
            }
            return surface.ToArray();
        }

        public CustomVertex.PositionNormal[] getSurfacePoints(Cube cube)
        {
            List<CustomVertex.PositionNormal> surface = new List<CustomVertex.PositionNormal>();
            foreach(Point p in points.Keys)
            {
                //check corners and construct vertex bitmap
                int index = 0;
                for (int v = 0; v < 8; v++)
                {
                    Point corner = p.bumpCorner(v);
                    bool hit;
                    if (points.TryGetValue(corner, out hit))
                        if (hit)
                            index += 1 << v;
                }
              
                //ASSERT(tris.size()%6==0);
                //ASSERT(myNormals[index].size()==tris.size()/6);
                AddTriangles(ref surface, p, cube.triangles[index], cube.normals[index], true);
            }
            return surface.ToArray();
        }

        private void AddTriangles(ref List<CustomVertex.PositionNormal> surface, Point p, int[] tris, List<Point> normals, bool shaded)
        {
            if (tris == null)   //todo: fix this
                return;

            for (int i = 0; i < tris.Length; i += 6)
            //int i = 0;
            {
                Point normal = normals[i / 6];
                for (int j = 0; j < 6; j += 2)
                {
                    Vector3 norm = new Vector3(normal.i, normal.j, normal.k);
                    CustomVertex.PositionNormal vn = new CustomVertex.PositionNormal(norm, norm);
                    Point v1 = p.bumpCorner(tris[i + j]);
                    Point v2 = p.bumpCorner(tris[i + j + 1]);
                    vn.X = (float)(v1.i + v2.i) / (2 * resolution);
                    vn.Y = (float)(v1.j + v2.j) / (2 * resolution);
                    vn.Z = (float)(v1.k + v2.k) / (2 * resolution);
                    //vn.Nx = vn.X; vn.Ny = vn.Y; vn.Nz = vn.Z;
                    if(shaded)
                        surface.Add(vn);
                }
            }
        }

        public void OnTimerTick(double time)
        {
            if (shape == Shapes.Caustic)
            {
                caustic.Tick(time);
                calculate();
            }
        }

    }
}