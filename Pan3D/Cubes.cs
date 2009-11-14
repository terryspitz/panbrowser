//Basic marching cube algorithm with points, cubes and surfaces.
// terry spitz (c) 2006

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System;

namespace Terry
{
    /// <summary>
    /// Class to calculate the 'marching cubes' algorithm
    /// 
    /// Axes
    /// 0--x    z--.
    /// |  |    |  |
    /// y--.    .--.
    /// 
    /// A Corner is an int representing a single corner of a unit cube:
    /// front   back
    /// face    face
    /// 0--1    4--5
    /// |  |    |  |
    /// 2--3    6--7
    /// 
    /// a CornerSet is a int bitmask of which of these corners are 'turned on', i.e. sum of:
    /// 1--2    16--32
    /// |  |    |    |
    /// 4--8    64--128
    /// 
    /// </summary>
    public class Cube
    {
        int triangleCount = 0;

        /// <summary>
        /// all rotations and mirror Transformations of the unit cube
        /// </summary>
        enum Tf { RX, RY, RZ, MX, MY, MZ, TransCount };

        /// <summary>
        /// corner to corner correspondance for the tranforms above
        /// </summary>
        int[,] transformation = new int[(int)Tf.TransCount, 8]
        {
        {2,3,6,7,0,1,4,5},	//Tf.RX
        {1,5,3,7,0,4,2,6},	//Tf.RY
        {1,3,0,2,5,7,4,6},	//Tf.RZ
        {1,0,3,2,5,4,7,6},	//Tf.MX
        {2,3,0,1,6,7,4,5},	//Tf.MY
        {4,5,6,7,0,1,2,3}	//Tf.MZ
        };

        enum Faces { X0, X1, Y0, Y1, Z0, Z1 };

        public struct CubePoint 
        {
            public int fromCorner, toCorner;
            public CubePoint(int f, int t) { fromCorner=f; toCorner=t; }
            public override string ToString()
            {
                return String.Format("{0}-{1}", fromCorner, toCorner);
            }
        }

        public class CornerSet
        {
            public int Id;
            public CubePoint[] Triangles;
            public List<Point> Normals;

            public CornerSet(int id, int[] t, List<Point> n) 
            {
                Id = id;
                Triangles = new CubePoint[t.Length/2];
                for(int i=0; i<t.Length; i+=2)
                    Triangles[i/2] = new CubePoint(t[i], t[i+1] );
                Normals = n; 
            }
        }

        public CornerSet[] cornersetData = new CornerSet[256];

        //successively transforms each cornerset into all reflections and rotations
        Tf[] all = new Tf[] 
                {   Tf.RY,Tf.RY,Tf.RY,Tf.RY, 
                    Tf.RX,Tf.RZ,Tf.RZ,Tf.RZ,Tf.RZ, 
                    Tf.RY,Tf.RX,Tf.RX,Tf.RX,Tf.RX,
                    Tf.RZ,Tf.RY,Tf.RY,Tf.RY,Tf.RY, 
                    Tf.RX,Tf.RZ,Tf.RZ,Tf.RZ,Tf.RZ, 
                    Tf.RY,Tf.RX,Tf.RX,Tf.RX,Tf.RX};

        public Cube()
        {
            init();
            CalculateAdjacentPoints();
        }

        static Cube singleton = new Cube();
        public static Cube Singleton 
        { 
            get { return singleton; }
        }

        /// <summary>
        /// set up triangles 
        /// </summary>
        public void init()
        {
            //256 entries/2 inverted=128
            //vX are the list of included corners
            //tX are the corresponding list of triplets of vertex pairs the
            // midpoints of which are triangle points

            /// A Corner is an int representing a single corner of a unit cube:
            /// front   back
            /// face    face
            /// 0--1    4--5
            /// |  |    |  |
            /// 2--3    6--7
            /// 
            /// a CornerSet is a int bitmask of which of these corners are 'turned on', i.e. sum of:
            /// 1--2    16--32
            /// |  |    |    |
            /// 4--8    64--128

            int[][] v1 = new int[][] { new int[] { 0 }, new int[] { 0, 1, 0, 2, 0, 4 } };
            int[] v2 = new int[] { 0, 1 }; int[] t2 = new int[] { 0, 2, 0, 4, 1, 3,  0, 4, 1, 5, 1, 3 };
            int[] v3 = new int[] { 0, 3 }; int[] t3 = new int[] { 0, 1, 0, 2, 0, 4,  3, 2, 3, 1, 3, 7 };
            int[] v4 = new int[] { 0, 7 }; int[] t4 = new int[] { 0, 1, 0, 2, 0, 4,  7, 5, 7, 6, 7, 3 };
            int[] v5 = new int[] { 0, 1, 2 }; int[] t5 = new int[] { 0, 4, 1, 5, 2, 6, 2, 6, 1, 5, 2, 3, 2, 3, 1, 5, 1, 3 };
            int[] v6 = new int[] { 0, 1, 6 }; int[] t6 = new int[] { 0, 2, 0, 4, 1, 3, 0, 4, 1, 5, 1, 3, 6, 7, 6, 4, 6, 2 };
            int[] v7 = new int[] { 0, 3, 5 }; int[] t7 = new int[] { 0, 1, 0, 2, 0, 4,  3, 2, 3, 1, 3, 7,  5, 1, 5, 4, 5, 7 };
            int[] v8 = new int[] { 0, 1, 2, 3 }; int[] t8 = new int[] { 0, 4, 1, 5, 2, 6,  1, 5, 3, 7, 2, 6 };
            int[] v9 = new int[] { 0, 1, 3, 5 }; int[] t9 = new int[] { 0, 2, 3, 7, 2, 3,  3, 7, 0, 2, 5, 7,  0, 2, 0, 4, 5, 7,  0, 4, 4, 5, 5, 7};   
            int[] v10 = new int[] { 0, 1, 6, 7 }; int[] t10 = new int[] { 0, 2, 0, 4, 1, 3,  0, 4, 1, 5, 1, 3,  7, 3, 7, 5, 6, 2,  6, 2, 7, 5, 6, 4 };
            int[] v14 = new int[] { 0, 1, 2, 5 }; int[] t14 = new int[] { 2, 6, 0, 4, 2, 3, 2, 3, 0, 4, 1, 3, 1, 3, 0, 4, 4, 5, 1, 3, 4, 5, 5, 7 };
            int[] v11 = new int[] { 0, 1, 3, 4 }; int[] t11 = new int[] { 0, 2, 4, 6, 2, 3, 2, 3, 4, 6, 3, 7, 3, 7, 4, 6, 4, 5, 4, 5, 1, 5, 3, 7 };
            int[] v12 = new int[] { 0, 1, 2, 7 }; int[] t12 = new int[] { 0, 4, 1, 5, 2, 6,  1, 5, 2, 6, 2, 3,  1, 5, 2, 3, 1, 3,  7, 6, 7, 5, 3, 7 };
            int[] v13 = new int[] { 0, 3, 5, 6 }; int[] t13 = new int[] { 0, 1, 0, 2, 0, 4,  3, 2, 3, 1, 3, 7,  5, 1, 5, 4, 5, 7,  6, 7, 6, 4, 6, 2 };
            int[] v15 = new int[] {}; int[] t15 = new int[] {};

            populateTriangles(v1[0], v1[1]);
            populateTriangles(v2, t2);
            populateTriangles(v3, t3);
            populateTriangles(v4, t4);
            populateTriangles(v5, t5);
            populateTriangles(v6, t6);
            populateTriangles(v7, t7);
            populateTriangles(v8, t8);
            populateTriangles(v9, t9);
            populateTriangles(v10, t10);
            populateTriangles(v11, t11);
            populateTriangles(v12, t12);
            populateTriangles(v13, t13); 
            populateTriangles(v14, t14);
            populateTriangles(v15, t15);

            //FindCorrespondants();
        }

        private void FindCorrespondants()
        {
            for(int c=0; c<256; c++)
            {
                CornerSet cornerset = cornersetData[c];
                if ((c & 1) > 0 ^ (c & 2) > 0)
                {
                    for (int c2 = 0; c < 256; c++)
                    {
                        CornerSet matchCornerset = cornersetData[c2];
                        for (int i = 0; i < matchCornerset.Triangles.Length; i++)
                        {
                            if ((cornerset.Triangles[i].fromCorner == matchCornerset.Triangles[i].fromCorner && cornerset.Triangles[i].toCorner == matchCornerset.Triangles[i].toCorner) ||
                            (cornerset.Triangles[i].fromCorner == matchCornerset.Triangles[i].toCorner && cornerset.Triangles[i].toCorner == matchCornerset.Triangles[i].fromCorner))
                            {
                                /*
                                 * Debug.Assert(Faces[p][i / 6].VertexNormals[i % 6 / 2] == null);
                                Faces[p][i / 6].VertexNormals[i % 6 / 2] = normal;  //share a common reference to the calculated normal
                                normal.vec += Faces[p][i / 6].FaceNormal * (float)Math.Sqrt(Faces[p][i / 6].Area);
                                normal.count++;
                                */
                            }
                        }
                    }
                }
            }
        }

        void populateTriangles(int[] corners, int[] triangles)
        {
            Tf[] trans = all;
            int id;
            for (int i = 0; i <= trans.Length; i++)
            {
                id = toCornerset(corners);
                //Debug.Assert(myTriangles[cornerset] == null);	    //check not populated yet
                if (cornersetData[(int)id] == null)
                {
                    WriteCorners(corners, id);
                    CornerSet c = new CornerSet(id, (int[])triangles.Clone(), SetNormals(triangles, false));
                    cornersetData[id] = c;
                    
                    if (corners.Length < 4)
                    {
                        CornerSet c2 = new CornerSet(id, (int[])triangles.Clone(), SetNormals(triangles, true));
                        Array.Reverse(c2.Triangles);
                        cornersetData[id ^ 255] = c2;
                    }
                    triangleCount++;
                }
#if DEBUG
                else
                {
                    //for(int j=0; j<myTriangles[cornerset].Length; j++)
                        //Debug.Assert(myTriangles[cornerset][j]==triangles[j]);
                    List<Point> normals = SetNormals(triangles, false);
                    normals.Sort(new PointComparer());

                    List<Point> n2 = cornersetData[id].Normals.ConvertAll<Point>(PointComparer.PointToPoint);
                    n2.Sort(new PointComparer());

                    for(int j=0; j<normals.Count; j++)
                        Debug.Assert(new PointComparer().Equals(n2[j],normals[j]));
                }
#endif
                if (i < trans.Length)
                    Transform(trans[i], ref corners, ref triangles);
            }
        }

        void WriteCorners(int[] corners, int cornerset)
        {
            StringBuilder s = new StringBuilder();
            foreach(int c in corners)
            {
                s.Append(c);
                s.Append(",");
            }
            Debug.WriteLine(string.Format("{0}\t{1}\t{2}", cornerset, Convert.ToString(cornerset, 2), s));
        }

        /// <summary>
        /// turns an array of corners into an bitmap int
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public int toCornerset(int[] cornerSet)
        {
            int cv = 0;
            foreach (int i in cornerSet)
                cv += 1 << i;	//==2^v[i]
            return cv;
        }

        /// <summary>
        /// adjest corners and triangles arrays by the tranform trans
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="corners"></param>
        /// <param name="triangles"></param>
        void Transform(Tf trans, ref int[] corners, ref int[] triangles)
        {
            for (int i = 0; i < corners.Length; i++)
                corners[i] = transformation[(int)trans, corners[i]];
            for (int j = 0; j < triangles.Length; j++)
                triangles[j] = transformation[(int)trans, triangles[j]];
        }

        List<Point> SetNormals(int[] tris, bool reverse)
        {
            List<Point> normals = new List<Point>();
            Point origin = new Point(0, 0, 0);
            for (int i = 0; i < tris.Length; i += 6)
            {
                Point p1 = origin.bumpCorner(tris[i]);
                Point p2 = origin.bumpCorner(tris[i + 1]);
                Point p3 = origin.bumpCorner(tris[i + 2]);
                Point p4 = origin.bumpCorner(tris[i + 3]);
                Point p5 = origin.bumpCorner(tris[i + 4]);
                Point p6 = origin.bumpCorner(tris[i + 5]);
                //centres
                Point c1 = p1 + p2;
                Point c2 = p3 + p4;
                Point c3 = p5 + p6;
                //2 side vectors
                Point v1 = c2 - c1;
                Point v2 = c3 - c2;
                //cross product
                Point n = new Point(
                    v1.j * v2.k - v1.k * v2.j,
                    v1.k * v2.i - v1.i * v2.k,
                    v1.i * v2.j - v1.j * v2.i);
                if (!reverse)
                    normals.Add(n);
                else
                {
                    Point n2 = new Point(-n.i, -n.j, -n.k);
                    normals.Add(n2);
                }
            }
            if (reverse)
                normals.Reverse();
            return normals;
        }

        public class Adjacency
        {
            public Point Bump;
            public Dictionary<int /*cornersetid*/, List<int> /*triangleIndex*/> TriangleIndicesByCornerSetId = new Dictionary<int,List<int>>();
            public Adjacency(Point b) { Bump = b; }
        }

        public Dictionary<int, Adjacency[/*axis*/][/*bump*/]> AdjacencyMapByCornerSetId = new Dictionary<int,Adjacency[][]>();
        
        void SetAdjacency(int c1, int c2, int axisId, int bumpId, Point bump, List<int> matchingVertex)
        {
            Debug.Assert(AdjacencyMapByCornerSetId.ContainsKey(c1));
            Adjacency[][] cornersetAdjacency  = AdjacencyMapByCornerSetId[c1];
            if(cornersetAdjacency[axisId]==null)
                cornersetAdjacency[axisId] = new Adjacency[4];
            if (cornersetAdjacency[axisId][bumpId] == null)
                cornersetAdjacency[axisId][bumpId] = new Adjacency(bump);
            Adjacency adj = cornersetAdjacency[axisId][bumpId];
            Debug.Assert(!adj.TriangleIndicesByCornerSetId.ContainsKey(c2));
            adj.TriangleIndicesByCornerSetId[c2] = matchingVertex;

        }

        void CalculateAdjacentPoints()
        {
            for (int c1 = 0; c1 < 256; c1++)
            {
                AdjacencyMapByCornerSetId[c1] = new Adjacency[3][];

                for (int c2 = 0; c2 < 256; c2++)
                {
                    if ((c1 & 1) > 0 ^ (c1 & 2) > 0)
                    {
                        SetAdjacency(c1, c2, 0, 0, new Point(), FindAdjacentTriangle(cornersetData[c2], 0, 1));
                        SetAdjacency(c1, c2, 0, 1, new Point(0, 0, -1), FindAdjacentTriangle(cornersetData[c2], 4, 5));
                        SetAdjacency(c1, c2, 0, 2, new Point(0, -1, 0), FindAdjacentTriangle(cornersetData[c2], 2, 3));
                        SetAdjacency(c1, c2, 0, 3, new Point(0, -1, -1), FindAdjacentTriangle(cornersetData[c2], 6, 7));
                    }
                    if ((c1 & 1) > 0 ^ (c1 & 4) > 0)
                    {
                        SetAdjacency(c1, c2, 1, 0, new Point(), FindAdjacentTriangle(cornersetData[c2], 0, 2));
                        SetAdjacency(c1, c2, 1, 1, new Point(0, 0, -1), FindAdjacentTriangle(cornersetData[c2], 4, 6));
                        SetAdjacency(c1, c2, 1, 2, new Point( -1, 0, 0), FindAdjacentTriangle(cornersetData[c2], 1, 3));
                        SetAdjacency(c1, c2, 1, 3, new Point( -1, 0, -1), FindAdjacentTriangle(cornersetData[c2], 5, 7));
                    }
                    if ((c1 & 1) > 0 ^ (c1 & 16) > 0)
                    {
                        SetAdjacency(c1, c2, 2, 0, new Point(), FindAdjacentTriangle(cornersetData[c2], 0, 4));
                        SetAdjacency(c1, c2, 2, 1, new Point(-1, 0,0), FindAdjacentTriangle(cornersetData[c2], 1, 5));
                        SetAdjacency(c1, c2, 2, 2, new Point(0, -1, 0), FindAdjacentTriangle(cornersetData[c2], 2, 6));
                        SetAdjacency(c1, c2, 2, 3, new Point(-1, -1, 0), FindAdjacentTriangle(cornersetData[c2], 3, 7));
                    }
                }
            }
        }

        List<int> FindAdjacentTriangle(CornerSet c2, int startCorner, int endCorner)
        {
            List<int> matches = new List<int>();
            for(int i = 0; i < c2.Triangles.Length; i++)
            {
                if((c2.Triangles[i].fromCorner==startCorner && c2.Triangles[i].toCorner==endCorner) ||
                    (c2.Triangles[i].fromCorner==endCorner && c2.Triangles[i].toCorner==startCorner))
                {
                    matches.Add(i);
                }
            }
            return matches;
        }

    }


}
