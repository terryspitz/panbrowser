// sample 3D points matching a bool function and build a metasurface
// terry spitz (c) 2006

using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Text;
using System.Windows.Media.Media3D;

namespace Terry
{
    /// <summary>
    /// Class to sample the values of the function at grid points (cells) and construct a surface through them
    /// </summary>
    public class Sampler
    {
        /// <summary>
        /// wraps a Vector into a reference object that is shared by all face vertexes at the same point
        /// </summary>
        public class SharedNormal
        {
            public Vector3D vec = new Vector3D();
            public int count = 0;
        }

        /// <summary>
        /// represents a face of the surface
        /// </summary>
        public class Face
        {
            public Point3D[] Vertexes = new Point3D[3];
            public Vector3D FaceNormal;
            public SharedNormal[] VertexNormals = new SharedNormal[3];
            public double Area;
        }

        /// <summary>
        /// represents a unit cube with the faces that pass through it
        /// </summary>
        public class Cell
        {
            public Point p;
            public Cube.CornerSet Cornerset;
            public Face[] Faces;
            public Cell(Point _p, int c) { p = _p; Cornerset = Cube.Singleton.cornersetData[c]; }
            public override string ToString()
            {
                return String.Format("{0} ({1})", p, Cornerset.Id);
            }
        }

        //public Shapes shapes = new Shapes();
        protected Converter<Vector3D, double> _shape;

        public IDictionary<Point, float> Points { get; set; }
        public IDictionary<Point, Cell> Cells { get; set; }
        protected Stack<Point> cubelist = new Stack<Point>();
        protected int[] BitCount = new int[256];
        protected float time = 0;
        public int Resolution { get; set; }
        public readonly float isosurface = 1f;
        protected bool interpolate;
        public string Description { get; set; }

        public Sampler(Converter<Vector3D, double> fn)
        {
            Points = new Dictionary<Point, float>(new PointComparer());
            Cells = new Dictionary<Point, Cell>(new PointComparer());
            Resolution = 10;

            _shape = fn;
            for(int i=0; i<256; i++)
            {
                int result = 0;
                int x = i;
                //from http://en.wikipedia.org/wiki/Lookup_table#Counting_bits
                while (x != 0)
                {
                    result++;
                    x = x & (x - 1);
                }
                BitCount[i] = result;
            }

        }

        public void Calculate(Flags flags)
        {
            Stopwatch st = new Stopwatch();
            TimeSpan time = new TimeSpan();
            Cells.Clear();
            cubelist.Clear();
            interpolate = flags.InterpolateEdges;

            //probe for starting points
            for(int i=-5; i<5; i++)
                for (int j = -5; j < 5; j++)
                {
                    Point p = new Point(2 * Resolution + 5, 2 * Resolution * i/5, 2 * Resolution *j/5);
                    while (TestPoint(p)<isosurface && p.i > -2*Resolution)
                        p.i -= 1;
                    if(p.i > -2*Resolution)
                        cubelist.Push(p);
                }
            StringBuilder desc = new StringBuilder();
            st.Start();
            IterateCubes();
            desc.AppendFormat("Points: {0}, Cells: {1}", Points.Count, Cells.Count);

            desc.AppendLine("");

                
            desc.Append("Cell Samples: ");
            desc.Append(st.ElapsedMilliseconds.ToString());
            time += st.Elapsed;

            st.Reset(); st.Start();
            CalculateFaces();
            desc.Append("ms, Faces: ");
            desc.Append (st.ElapsedMilliseconds.ToString());
            time += st.Elapsed;

            st.Reset(); st.Start();
            CalculateFaceNormals();
            desc.Append("ms, Face normals: ");
            desc.Append(st.ElapsedMilliseconds.ToString());
            time += st.Elapsed;

            st.Reset(); st.Start();
            CalculateVertexNormals();
            desc.Append("ms, Vertex normals: ");
            desc.Append(st.ElapsedMilliseconds.ToString());
            desc.Append("ms");
            time += st.Elapsed;
            
            Description = desc.ToString();
            Debug.WriteLine(string.Format("Calculated frame: Cubes: {0} in {1}ms", Cells.Count, time.Milliseconds));
        }

        protected bool Differ(int a, int b, int c, int d)
        {
            int count = BitCount[a|b|c|d];
            return count > 0 && count < 4;
        }

        public void IterateCubes()
        {
            int empty = 0;
            while (cubelist.Count > 0 && Cells.Count < 100000)
            {
                Point p = cubelist.Pop();
                int cornerset = testCube(p);
                if (cornerset > 0 && cornerset < 255)
                    if (!Cells.ContainsKey(p))
                        Cells.Add(p, new Cell(p, cornerset));

                //push more interesting Cubes to test
                if (cornerset > 0 && cornerset < 255)
                {
                    if( Differ(cornerset & 2, cornerset&8, cornerset&32, cornerset&128))
                        addCube(new Point(p.i + 1, p.j, p.k));
                    if (Differ(cornerset & 1, cornerset & 4, cornerset & 16, cornerset & 64))
                        addCube(new Point(p.i - 1, p.j, p.k));
                    if (Differ(cornerset & 4, cornerset & 8, cornerset & 64, cornerset & 128))
                        addCube(new Point(p.i, p.j + 1, p.k));
                    if (Differ(cornerset & 1, cornerset & 2, cornerset & 16, cornerset & 32))
                        addCube(new Point(p.i, p.j - 1, p.k));
                    if (Differ(cornerset & 16, cornerset & 32, cornerset & 64, cornerset & 128))
                        addCube(new Point(p.i, p.j, p.k + 1));
                    if (Differ(cornerset & 1, cornerset & 2, cornerset & 4, cornerset & 8))
                        addCube(new Point(p.i, p.j, p.k - 1));
                }
                else empty++;
            }
            if (Cells.Count == 100000)
                throw new Exception("Too many cubes found - can't draw");
        }

        void addCube(Point p)
        {
            if (!Cells.ContainsKey(p) && p*p<1000)
            {
                cubelist.Push(p);
            }
        }

        float FindOrTestPoint(Point p, out bool found)
        {
            float pointVal;
            if (!Points.TryGetValue(p, out pointVal))
            {
                found = false;
                pointVal = TestPoint(p);
                Points.Add(p, pointVal);
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
                if(FindOrTestPoint(corner, out found)>isosurface)
                    cornerset += 1 << v;
            }
            return cornerset;
        }

        Vector3D PointToVector(Point p, int resolution)
        {
            return new Vector3D(((double)p.i) / resolution, ((double)p.j) / resolution, ((double)p.k) / resolution);
        }

        float TestPoint(Point p)
        {
            if (interpolate)
                return (float)Math.Min(_shape.Invoke(PointToVector(p, Resolution)), 1e5f);
            else
                return _shape.Invoke(PointToVector(p, Resolution)) >= isosurface ? isosurface * 2 : 0f;
        }

        public void CalculateFaces()
        {
            foreach (Cell cell in Cells.Values)
            {
                Cube.CubePoint[] tris = cell.Cornerset.Triangles;
                cell.Faces = new Face[tris.Length / 3];

                for (int i = 0; i < tris.Length; i += 3)
                {
                    Face f = new Face();

                    for (int j = 0; j < 3; j ++)
                    {
                        Point p1 = cell.p.bumpCorner(tris[i + j].fromCorner);
                        Point p2 = cell.p.bumpCorner(tris[i + j].toCorner);
                        float f1 = Points[p1];
                        float f2 = Points[p2];
                        float interp = (isosurface - f1) / (f2 - f1);
                        if(interp <0.1f)
                            interp = 0.1f;
                        if (interp > 0.9f)
                            interp = 0.9f;

                        Vector3D v1 = new Vector3D((float)p1.i / Resolution, (float)p1.j / Resolution, (float)p1.k / Resolution);
                        Vector3D v2 = new Vector3D((float)p2.i / Resolution, (float)p2.j / Resolution, (float)p2.k / Resolution);
                        v2 = v2 - v1;
                        v2*=interp;
                        f.Vertexes[j] = (Point3D)(v1 + v2);
                    }
                    cell.Faces[i / 3] = f;
                }
            }
        }

        public void CalculateFaceNormals()
        {
            foreach (Cell c in Cells.Values)
            {
                foreach (Face f in c.Faces)
                {
                    f.FaceNormal = Vector3D.CrossProduct(f.Vertexes[2] - f.Vertexes[0], f.Vertexes[0] - f.Vertexes[1]);
                    f.FaceNormal.Normalize();
                    f.Area = Math.Abs(Vector3D.DotProduct(f.Vertexes[2] - f.Vertexes[0], f.Vertexes[0] - f.Vertexes[1]));
                    //Debug.Assert(f.FaceNormal.LengthSq() > 0.9f && f.FaceNormal.LengthSq() < 1.1f);
                }
            }
        }
        
#if OLDVER
        public void CalculateVertexNormals()
        {
            foreach (Cell cell in Cells.Values)
            {
                if (Cube.Singleton.AdjacencyMapByCornerSetId[cell.Cornerset.Id] != null)
                {
                    for (int axis = 0; axis < 3; axis++)
                    {
                        if (Cube.Singleton.AdjacencyMapByCornerSetId[cell.Cornerset.Id][axis] != null)
                        {
                            SharedNormal normal = new SharedNormal();
                            for (int bumpId = 0; bumpId < 4; bumpId++)
                            {
                                Cube.Adjacency adj = Cube.Singleton.AdjacencyMapByCornerSetId[cell.Cornerset.Id][axis][bumpId];
                                Cell cell2 = Cells[cell.p + adj.Bump];
                                foreach (int triVertex in adj.TriangleIndicesByCornerSetId[cell2.Cornerset.Id])
                                {
                                    Debug.Assert(cell2.Faces[triVertex / 3].VertexNormals[triVertex % 3] == null);
                                    cell2.Faces[triVertex / 3].VertexNormals[triVertex % 3] = normal;  //share a common reference to the calculated normal
                                    normal.vec += cell2.Faces[triVertex / 3].FaceNormal * (float)Math.Sqrt(cell2.Faces[triVertex / 3].Area);
                                    normal.count++;
                                }
                            }
                            normal.vec.Normalize();
                        }
                    }
                }
            }
        }
#else
        
        public void CalculateVertexNormals()
        {
            foreach (Cell cell in Cells.Values)
            {
                Point p = cell.p;
                if ((cell.Cornerset.Id & 1) > 0 ^ (cell.Cornerset.Id & 2) > 0)
                {
                    SharedNormal normal = new SharedNormal();
                    FindAndSetNormal(p, 0, 1, ref normal);
                    FindAndSetNormal(new Point(p.i, p.j, p.k - 1), 4, 5, ref normal);
                    FindAndSetNormal(new Point(p.i, p.j - 1, p.k), 2, 3, ref normal);
                    FindAndSetNormal(new Point(p.i, p.j - 1, p.k - 1), 6, 7, ref normal);
                    normal.vec.Normalize();
                    //normal.vec *= 1f/normal.count;
                    //Debug.Assert(normal.vec.Length() > .8f && normal.vec.Length() < 1.2f);
                }
                if ((cell.Cornerset.Id & 1) > 0 ^ (cell.Cornerset.Id & 4) > 0)
                {
                    SharedNormal normal = new SharedNormal();
                    FindAndSetNormal(p, 0, 2, ref normal);
                    FindAndSetNormal(new Point(p.i, p.j, p.k - 1), 4, 6, ref normal);
                    FindAndSetNormal(new Point(p.i - 1, p.j, p.k), 1, 3, ref normal);
                    FindAndSetNormal(new Point(p.i - 1, p.j, p.k - 1), 5, 7, ref normal);
                    normal.vec.Normalize();
                    //normal.vec *= 1f / normal.count;
                    //Debug.Assert(normal.vec.Length() > .8f && normal.vec.Length() < 1.2f);
                }
                if ((cell.Cornerset.Id & 1) > 0 ^ (cell.Cornerset.Id & 16) > 0)
                {
                    SharedNormal normal = new SharedNormal();
                    FindAndSetNormal(p, 0, 4, ref normal);
                    FindAndSetNormal(new Point(p.i - 1, p.j, p.k), 1, 5, ref normal);
                    FindAndSetNormal(new Point(p.i, p.j - 1, p.k), 2, 6, ref normal);
                    FindAndSetNormal(new Point(p.i - 1, p.j - 1, p.k), 3, 7, ref normal);
                    normal.vec.Normalize();
                    //normal.vec *= 1f / normal.count;
                    //Debug.Assert(normal.vec.Length() > .2f);
                }

            }
        }

        void FindAndSetNormal(Point p, int startCorner, int endCorner, ref SharedNormal normal)
        {
            Cell cell = null;
            if(!Cells.TryGetValue(p, out cell))
                return;

            for(int i = 0; i < cell.Cornerset.Triangles.Length; i++)
            {
                if ((cell.Cornerset.Triangles[i].fromCorner == startCorner && cell.Cornerset.Triangles[i].toCorner == endCorner) ||
                    (cell.Cornerset.Triangles[i].fromCorner == endCorner && cell.Cornerset.Triangles[i].toCorner == startCorner))
                {
                    Debug.Assert(cell.Faces[i / 3].VertexNormals[i%3] == null);
                    cell.Faces[i / 3].VertexNormals[i % 3] = normal;  //share a common reference to the calculated normal
                    normal.vec += cell.Faces[i / 3].FaceNormal * (float)Math.Sqrt(cell.Faces[i / 3].Area);
                    normal.count++;
                }
            }
        }
#endif   

        public void OnTimerTick(double t)
        {
            time = (float)t;
            //shapes.Tick(t);
        }

    }
}