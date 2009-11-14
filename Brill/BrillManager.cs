using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace Terry
{
    class Geometry
    {
        public List<MyPlane> _planes = new List<MyPlane>();
        public LinkedList<Line> _lines = new LinkedList<Line>();
        public List<Face> _faces = new List<Face>();
        public List<Shape> _shapes = new List<Shape>(); public List<Shape> Shapes { get { return _shapes; } }
        public Matrix3D _basis = Matrix3D.Identity;
        public int maxGeneration;
    }

    class BrillManager
    {
        protected static readonly double small = 1e-4, clip=4;
        protected static int edgeId = 0, faceId =0, shapeId = 0;
        protected static Random random = new Random();

        public static Geometry BuildEverything()
        {
            DateTime start = DateTime.Now;
            Geometry g = new Geometry();
            //g._basis.Scale(0.5f, 0.5f, 0.5f);
            g._basis.M12 = random.NextDouble() - 0.5f;
            g._basis = RandomBasis();
            g._planes = MakeAllPlanesFromBasis(g._basis);
            g._lines = MakeEdges(g._planes);
            DateTime end = DateTime.Now;
            Debug.WriteLine("MakeEdges took ms " + (end - start).TotalMilliseconds);
            start = end;
            MakeAllFaces(g._faces, g._planes, g._lines);
            end = DateTime.Now;
            Debug.WriteLine("MakeFaces took ms " + (end - start).TotalMilliseconds);
            start = end;
            g.maxGeneration = MakeShapes(g._faces, g._shapes);
            end = DateTime.Now;
            Debug.WriteLine("MakeShapes took ms " + (end - start).TotalMilliseconds);
            return g;
        }

        public static Matrix3D RandomBasis()
        {
            Matrix3D m;
            do
            {
                m = new Matrix3D(
                   RandomVal(), RandomVal(), RandomVal(), RandomVal(),
                   RandomVal(), RandomVal(), RandomVal(), RandomVal(),
                   RandomVal(), RandomVal(), RandomVal(), RandomVal(),
                   RandomVal(), RandomVal(), RandomVal(), RandomVal()
                   );
            } while (m.Determinant < 0.5);
            return m;
        }

        public static double RandomVal()
        {
            return (random.NextDouble() - 0.5)*2;
        }

        /// <summary>
        /// Makes planes using each 
        /// </summary>
        /// <param name="basis"></param>
        /// <returns></returns>
        public static List<MyPlane> MakeAllPlanesFromBasis(Matrix3D basis)
        {
            List<MyPlane> planes = new List<MyPlane>();
            const int xsize = 1;
            const int ysize = 1;
            const int zsize = 1;
            int id = 0;

            // set up planes----------------------------------
            for (int i = -xsize; i <= xsize; i++)
                for (int j = -ysize; j <= ysize; j++)
                    for (int k = -zsize; k <= zsize; k++)
                        if (i!=0 || j!=0 || k!=0)
                        {
                            Vector3D multiplier = new Vector3D(i, j, k);
                            Vector3D normal = Vector3D.Multiply(multiplier , basis);
                            //Plane plane = Plane.FromPointNormal(normal.Length(), normal);
                            MyPlane plane = new MyPlane();
                            plane.id = id++;

                            plane.Magnitude = normal.Length;
                            normal.Normalize();
                            plane.Normal = normal;

                            //if(i==1 && j==0 && k==0) m_closestplane = m_planes;
                            planes.Add(plane);
                        }

            Debug.WriteLine(string.Format("Planes: {0}", planes.Count));
            return planes;
        }

        public static List<MyPlane> CreatePlanesFromMultipliers(Vector3D[] multipliers, Matrix3D basis)
        {
            List<MyPlane> planes = new List<MyPlane>();
            int id = 0;
            foreach (Vector3D multiplier in multipliers)
            {
                Vector3D normal = Vector3D.Multiply(multiplier, basis);
                //Plane plane = Plane.FromPointNormal(normal.Length(), normal);
                MyPlane plane = new MyPlane();
                plane.id = id++;
                plane.Magnitude = normal.Length;
                normal.Normalize();
                plane.Normal = normal;

                //if(i==1 && j==0 && k==0) m_closestplane = m_planes;
                planes.Add(plane);
            }
            Debug.WriteLine(string.Format("Planes: {0}", planes.Count));
            return planes;
        }

        /// <summary>
        /// Iterate through each pair of planes to calculate their line of intersection
        /// Iterate through each triplet of planes to calculate the points of intersection along that line
        /// </summary>
        public static LinkedList<Line> MakeEdges(List<MyPlane> planes)
        {
            LinkedList<Line> lines = new LinkedList<Line>();
            foreach (MyPlane p1 in planes)
            {
                foreach (MyPlane p2 in planes)      //for each line
	            {
                    if (p1.Equals(p2))
                        continue;
                    //check if we've got line already
                    if (FindPlane(lines, p1, p2))
                        continue;

                    List<LineVertex> tmpPoints = new List<LineVertex>();
                    Matrix3D trans = new Matrix3D();
                    Vector3D lineorigin;
                    Vector3D linebearing = Vector3D.CrossProduct(p1.Normal, p2.Normal);//calculate edges in line
                    if (IsSmall(linebearing.LengthSquared))
                        continue;	// parallel planes


                    //calculate line origin point
	                trans.M11 = p1.Normal.X; trans.M21 = p1.Normal.Y; trans.M31 = p1.Normal.Z; 
	                trans.M12 = p2.Normal.X; trans.M22 = p2.Normal.Y; trans.M32 = p2.Normal.Z; 
	                trans.M13 = linebearing.X; trans.M23 = linebearing.Y; trans.M33 = linebearing.Z;
                    trans.M44 = 1;
	                Debug.Assert( !IsSmall(trans.Determinant) );
                    trans.Invert();

                    lineorigin = Vector3D.Multiply(new Vector3D(p1.Magnitude, p2.Magnitude, 0f), trans);
                    //lineorigin.X = o.X; lineorigin.Y = o.Y; lineorigin.Z = o.Z;
                    /* check point is in the right planes */
                    Debug.Assert(IsPointOnPlane(lineorigin, p1));
                    Debug.Assert(IsPointOnPlane(lineorigin, p2));
                    linebearing.Normalize();

	                //check line (i.e. two points) is not colinear with another line in plane
	                Line oldline = FindColinear( lines, p1, linebearing, lineorigin );
	                if( oldline != null )
	                {
    		            oldline.planes.AddLast(p2);
	    	            continue;		//stop this line
	                }
            	    
	                Line newline = new Line();
	                newline.planes.AddLast(p1);
	                newline.planes.AddLast(p2);
	                newline.origin= lineorigin;
	                newline.bearing = linebearing;

                    GetPointsOnLine(planes, p1, p2, tmpPoints, linebearing, lineorigin);
            	    
	                if( tmpPoints.Count==0 ) /* no segments on this line */
	                {
		                //Debug.WriteLine("\nNo points on line");
		                continue;
	                }
                    else
	                    lines.AddLast(newline);	    
            	    
	                Debug.Assert(tmpPoints.Count!=1);
            	    
                    FillLineWithEdges(tmpPoints, newline);

	                tmpPoints.Clear();	//delete leftover lines
	            }// foreach line 
            }// next p1
            return lines;
        }

        protected static bool FindPlane(LinkedList<Line> lines, MyPlane p1, MyPlane p2)
        {
            foreach (Line l in lines)
                if (l.FindPlane(p1, p2))
                    return true;
            return false;
        }

        protected static void GetPointsOnLine(List<MyPlane> planes, MyPlane p1, MyPlane p2, List<LineVertex> tmpPoints, Vector3D linebearing, Vector3D lineorigin)
        {
            foreach (MyPlane p3 in planes)     // find each point 
            {
                if (p1.Equals(p3) || p2.Equals(p3)) continue;

                //calculate position on line
                if (IsSmall(Vector3D.DotProduct(p3.Normal, linebearing)))
                    continue;
                double along = (p3.Magnitude - Vector3D.DotProduct(p3.Normal, lineorigin)) / Vector3D.DotProduct(p3.Normal, linebearing);
                LineVertex vertex = new LineVertex(along * linebearing + lineorigin, along, p3, linebearing);
                if (vertex.point.Length > clip)
                {
                    vertex.point.Normalize();
                    vertex.point *= clip;
                }
                else
                {
                    /* check point is in the right planes */
                    Debug.Assert(IsPointOnPlane(vertex.point, p1));
                    Debug.Assert(IsPointOnPlane(vertex.point, p2));
                    Debug.Assert(IsPointOnPlane(vertex.point, p3));
                }
                tmpPoints.Add(vertex);
            }
        }


        /// make sections of line into edges
        protected static void FillLineWithEdges(List<LineVertex> tmpPoints, Line newline)
        {
            //sort points according to distance along line, then join successive pairs into edges
            tmpPoints.Sort(delegate(LineVertex v1, LineVertex v2) { return Comparer<double>.Default.Compare(v1.along, v2.along); });

            //tmpPoints.Dump(afxDump);
            int lastp = 0, thisp=0;
            LineVertex lastpoint = tmpPoints[lastp];
            while (thisp < tmpPoints.Count && tmpPoints[thisp].along < tmpPoints[lastp].along + small )
                thisp++;
            Debug.Assert(thisp != tmpPoints.Count);

            while (thisp<tmpPoints.Count)
            {
                LineVertex thispoint = tmpPoints[thisp];
                Edge newedge1 = new Edge();
                Edge newedge2 = new Edge();

                newedge1.id = edgeId++;
                newedge1.origin = lastpoint.point;
                newedge1.twin = newedge2;
                newedge2.id = edgeId++;
                newedge2.origin = thispoint.point;
                newedge2.twin = newedge1;

                newline.edges1.AddLast(newedge1);
                lastpoint = thispoint;
                lastp = thisp;
                //m_points.push_back( lastpoint );		//add point
                do
                    thisp++;
                while (thisp < tmpPoints.Count && tmpPoints[thisp].along < tmpPoints[lastp].along + small);
            }
        }

        protected class PlaneEdge
        {
            public Edge edge;
            public double angle, angle2;
            public Line line;
            public PlaneEdge(Line l, Edge e, double a, double a2)
            { edge = e; angle = a; angle2 = a2; line = l; }
        }

        public static void MakeAllFaces(List<Face> faces, List<MyPlane> planes, LinkedList<Line> lines)
        {
            foreach (MyPlane plane in planes)
                MakeFacesFromPlaneEdges(faces, plane, lines);
        }

        public static void MakeFacesFromPlaneEdges(List<Face> faces, MyPlane plane, LinkedList<Line> lines)
        {
            List<PlaneEdge> planeEdges = GetPlaneEdges(plane, lines);
            SetNextEdgesRound(planeEdges);
            JoinEdges(faces, plane, planeEdges);
        }

        protected static void JoinEdges(List<Face> faces, MyPlane plane, List<PlaneEdge> planeEdges)
        {
            Face boundary = null;
            int boundarylength = 0;
            //LinkedList<PlaneEdge> planeEdges2 = new LinkedList<PlaneEdge>(planeEdges);
            //Debug.WriteLine("\nEdges in faces: ");
            int startn = 0;

            //build faces
            do
            {
                Face face = new Face(faceId++, plane, plane.Normal, MakeColour(random));
                faces.Add(face);
                Edge curr = planeEdges[startn].edge;

                do
                {
                    Debug.Assert(!curr.used);
                    face.edges.Add(curr);
                    curr.used = true;
                    curr.faces.AddLast(face);
                    curr = curr.twin.next;
                }
                while (face.edges[0].id != curr.id);
                // while not back to start

                Debug.Assert(face.edges.Count > 2);
                if (face.edges.Count <= 2)
                    throw new Exception("JoinEdges failed");

                //find 'outside' face
                if (face.edges.Count > boundarylength)
                {
                    boundary = face;
                    boundarylength = face.edges.Count;
                }

                //add the 'twin face', facing the opposite normal with twin edges
                Face face2 = new Face(faceId++, plane, -plane.Normal, MakeColour(random));
                faces.Add(face2);
                face.twin = face2;
                face2.twin = face;
                foreach(Edge edge in face.edges)
                {
                    face2.edges.Add(edge.twin);
                    edge.twin.faces.AddLast(face2);
                }

                //find next unused edge
                while (startn < planeEdges.Count && planeEdges[startn].edge.used)
                    startn++;
            }
            while (startn < planeEdges.Count);

            //remove boundary (largest face)
            if (boundarylength > 0)
            {
                RemoveFace(faces, boundary);
                RemoveFace(faces, boundary.twin);
            }
        }

        protected static Color MakeColour(Random r)
        {
            return Color.FromArgb(50, (byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255));
        }

        protected static void RemoveFace(List<Face> faces, Face boundary)
        {
            foreach (Edge e in boundary.edges)
            {
                e.faces.Remove(boundary);
                //e.twin.faces.Remove(boundary);
                Debug.Assert(!e.twin.faces.Contains(boundary));
                if (e.twin.faces.Contains(boundary))
                    throw new Exception();
            }
            faces.Remove(boundary);
        }

        protected static void SortPlaneEdges(List<PlaneEdge> planeEdges)
        {
            //sort edges by their origin, then by their orientation
            planeEdges.Sort(
                delegate(PlaneEdge e1, PlaneEdge e2)
                {
                    if (CloseTo(e1.edge.origin, e2.edge.origin))
                        return Comparer<double>.Default.Compare(e1.angle, e2.angle);
                    else
                        return Compare(e1.edge.origin, e2.edge.origin);
                }
            );
            //foreach (PlaneEdge p in planeEdges) 
                //Debug.WriteLine(String.Format("id {0,3}\tA:{3}{1}\tto {2}", p.edge.id, ToString(p.edge.origin), ToString(p.edge.twin.origin), p.angle));
        }

        protected static void SetNextEdgesRound(List<PlaneEdge> planeEdges)
        {
            SortPlaneEdges(planeEdges);

            //for each edge, set next edge round.
            List<PlaneEdge>.Enumerator en = planeEdges.GetEnumerator();
            en.MoveNext();
            PlaneEdge start = en.Current;
            start.edge.used = false;
            PlaneEdge previous = en.Current;
            //Debug.WriteLine("\n\nEdges round points");
            int c = 1;
            while (en.MoveNext())
            {
                if (CloseTo(start.edge.origin, en.Current.edge.origin))
                    previous.edge.next = en.Current.edge;
                else
                {
                    previous.edge.next = start.edge;
                    start = en.Current;
                    Debug.Assert(c > 1);
                    if (c <= 1) throw new Exception();
                    //Debug.Write(String.Format(",{0}", c));
                    c = 0;
                }
                en.Current.edge.used = false;
                previous = en.Current;
                c++;
            }
            previous.edge.next = start.edge;
            //Debug.Write(String.Format(",{0}", c));
        }

        protected static List<PlaneEdge> GetPlaneEdges(MyPlane plane, LinkedList<Line> lines)
        {
            //get edges from all the lines that are in this plane (note lines are in more than one plane)
            bool first_line = true;
            double ang, ang2;
            Vector3D refvec = new Vector3D();
            List<PlaneEdge> planeEdges = new List<PlaneEdge>();

            foreach(Line thisline in lines)
            {
	            if(thisline.planes.Contains(plane))
	            {
	                // find bearing of line
	                GetAngleBetween2( thisline.bearing, plane.Normal, ref refvec, ref first_line, out ang, out ang2 );
	                foreach(Edge edge in thisline.edges1)
	                {
		                planeEdges.Add(new PlaneEdge(thisline, edge, ang, ang2));
                        planeEdges.Add(new PlaneEdge(thisline, edge.twin, ang2, ang));
	                }
	            }
            }
            return planeEdges;
        }

        // find bearing of line bearing/vector by comparing with a reference vector 
        protected static void GetAngleBetween2( Vector3D vector, Vector3D normal, ref Vector3D refvec,
	            ref bool first_line, out double ang, out double ang2 )
        {
            // reference vector in plane is first edge
            if( first_line )
            {
	            first_line = false;
	            refvec = vector;
	            ang = 0.0f;
	            ang2 = 4;
            }
            else
                GetAngleBetween(vector, normal, refvec, out ang, out ang2);

            //ang = (double)Math.Atan2( sin, cos );
            //ang2 = (double)Math.Atan2( -sin, -cos );
        }

        /// <summary>
        /// gets the angle between vector and refvec, using normal as a helper. 
        /// to avoid using atan for speed, angle is between 0 and 8
        /// method: map each 45degree to a unit range and use sign to sort as follows:
        /// unit sin cos sin>cos degree
        /// 0    +   +   false   0
        /// 1    +   +   true    45
        /// 2    +   -   true    90
        /// 3    +   -   false   135
        /// 4    -   -   false   180
        /// 5    -   -   true    225
        /// 6    -   +   true    270
        /// 7    -   +   false   315
        /// i.e.  (sin<0)?4:0 + ((sin<0)^(cos<0))?2:0 + ((sin<0)^(cos<0)^(sin>cos))?1:0
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="normal"></param>
        /// <param name="refvec"></param>
        /// <param name="ang">angle between vector and refvec</param>
        /// <param name="ang2">angle between -vector and refvec</param>
        protected static void GetAngleBetween(Vector3D vector, Vector3D normal, Vector3D refvec, out double ang, out double ang2)
        {
            double cos = Vector3D.DotProduct(refvec, vector);
            double sin = Vector3D.CrossProduct(refvec, vector).Length;
            if (Vector3D.DotProduct(Vector3D.CrossProduct(refvec, vector), normal) < 0)
                sin = -sin;
            bool sinBigger = Math.Abs(sin) + small > Math.Abs(cos);
            //ang = ((sin < 0) ? 4 : 0) + (((sin < 0) ^ (cos < 0)) ? 2 : 0) + (((sin < 0) ^ (cos < 0) ^ (sinBigger)) ? 1 : 0);
            //ang = ((sin < 0) ? 8 : 0) + Math.Sign(sin) * ((cos < 0) ? 2 : 0) + !sinBigger *  
            //add fractional part
            if (Math.Abs(sin) > Math.Abs(cos))
                ang = Math.Abs(cos) / Math.Abs(sin);
            else
                ang = Math.Abs(sin) / Math.Abs(cos);
            if (sinBigger) ang = 2 - ang;
            if (cos < 0) ang = 4 - ang;
            if (sin < 0) ang = 8 - ang;

            ang2 = ang + 4;
            if (ang2 >= 8) ang2 -= 8;
        }

        /// <summary>
        /// Make shapes from faces
        /// </summary>
        public static int MakeShapes(List<Face> faces, List<Shape> shapes)
        {
            //pick a face
            Face bestface = null;
            double bestlen = 0f;
            foreach (Face face in faces)
            {
                double len = 0;
                int count = 0;
                foreach (Edge e in face.edges)
                {
                    len += e.origin.LengthSquared;
                    count += 1;
                }
                if (bestface == null || len/count < bestlen)
                {
                    bestface = face;
                    bestlen = len/count;
                }
            }
            MakeShapeFromStartFace(bestface, shapes, 0);

            int gen = 1;
            while (MakeSuccessiveShapes(shapes, gen))
                gen += 1;

            BuildCentres(shapes);
            return gen;
        }

        private static void BuildCentres(List<Shape> shapes)
        {
            foreach (Shape s in shapes)
            {
                Vector3D c = new Vector3D();
                int count = 0;
                foreach (Face f in s.faces)
                    foreach (Edge e in f.edges)
                    {
                        c += e.origin;
                        count += 1;
                    }
                s.centre = Vector3D.Multiply(1 / (double)count *10, c);
            }
        }

        protected static bool MakeSuccessiveShapes(List<Shape> shapes, int generation)
        {
            List<Face> startfaces = new List<Face>();
            foreach (Shape s in shapes)
                if(s.generation == generation-1)
                    foreach (Face face in s.faces)
                        if (!face.twin.used)
                            startfaces.Add(face.twin);
            bool newshape = false;
            foreach(Face startface in startfaces)
                if (!startface.used)
                {
                    newshape = true;
                    MakeShapeFromStartFace(startface, shapes, generation);
                }
            return newshape;
        }

        protected static void MakeShapeFromStartFace(Face startface, List<Shape> shapes, int generation)
        {
            Shape shape = new Shape(shapeId++,generation,MakeColour(random));
            AddShapeFaces(startface, shape.faces);
            if (shape.faces.Count < 20)
                shapes.Add(shape);
            else
                Debug.WriteLine(string.Format("Ditch Shape, faces: {0}", shape.faces.Count));
        }

        protected static void AddShapeFaces(Face startface, List<Face> faces)
        {
            Dictionary<Edge, Face> unmatchedEdges = new Dictionary<Edge, Face>();
            faces.Add(startface);
            startface.used = true;
            startface.outer = false;
            foreach (Edge e in startface.edges)
                unmatchedEdges.Add(e, startface);

            while (unmatchedEdges.Count > 0)
            {
                //get the first remaining edge to process
                Dictionary<Edge, Face>.Enumerator e = unmatchedEdges.GetEnumerator();
                e.MoveNext();
                Edge edge = e.Current.Key;
                Face eface = e.Current.Value;
                Vector3D edgeDirection = edge.twin.origin - edge.origin;
                edgeDirection.Normalize();
                //Vector3D faceDirection = GetFaceDirection(eface, edge.origin, edgeDirection);
                //unmatchedEdges.Remove(edge); //done later as part of removing all edges in bestface

                //find best next face
                double ang, ang2, bestang = 0;
                Face bestface = null;
                foreach (Face thisface in edge.twin.faces)
                {
                    //not sure which way face faces, so find angle between lines, but normal to edge direction
                    //Vector3D thisFaceDirection = GetFaceDirection(thisface, edge.origin, edgeDirection);

                    //check if this face is in the right half volume
                    //if (Compare(Vector3D.Dot(thisFaceDirection, normalDirection), 0) < 0) continue;

                    GetAngleBetween(thisface.normal, edgeDirection, eface.normal, out ang, out ang2);
                    if (!thisface.used && ang2 > 0 && (bestface == null || ang2 < bestang))
                    {
                        bestface = thisface;
                        bestang = ang2;
                    }
                }
                if (bestface == null)
                {
                    unmatchedEdges.Remove(edge);
                    continue;
                }

                Debug.Assert(bestface != eface);
                //Debug.Assert(bestang <= 4.0);     //assume this is the outer boundary shape
                //add edges
                foreach (Edge ed in bestface.edges)
                {
                    if (unmatchedEdges.ContainsKey(ed))
                        unmatchedEdges.Remove(ed);
                    else if (unmatchedEdges.ContainsKey(ed.twin))
                        unmatchedEdges.Remove(ed.twin);
                    else
                        unmatchedEdges.Add(ed, bestface);
                }
                faces.Add(bestface);
                bestface.used = true;
                bestface.outer = true;
            }
        }

        #region StaticHelpers
        protected static Vector3D GetFaceDirection(Face face, Vector3D origin, Vector3D edgeDirection)
        {
            Vector3D nextEdgeDirection = new Vector3D();
            List<Edge>.Enumerator en = face.edges.GetEnumerator();
            do
            {
                if(!en.MoveNext()) break;
                nextEdgeDirection = en.Current.origin - origin;
                nextEdgeDirection.Normalize();
                nextEdgeDirection -= edgeDirection * Vector3D.DotProduct(edgeDirection, nextEdgeDirection);
            } while (CloseTo(nextEdgeDirection, new Vector3D()));
            nextEdgeDirection.Normalize();
            return nextEdgeDirection;
        }

        protected static Vector3D Normal(MyPlane p)
        {
            return p.Normal;
        }
        protected static Line FindColinear(LinkedList<Line> lines, MyPlane p1, Vector3D linebearing, Vector3D lineorigin)
        {
            foreach(Line line in lines)
            {
	            if( line.planes.Contains(p1) && CloseTo( lineorigin, line.origin ) &&
	                    (CloseTo( linebearing, line.bearing ) || CloseTo( linebearing, -line.bearing ))  )
	                return line;
            }
            return null;
        }
        protected static bool CloseTo(Vector3D a, Vector3D b)
        {
            return Compare(a, b) == 0;
        }
        protected static int Compare(double l, double r)
        {
            if (l < r - small)
                return -1;
            else if (l > r + small)
                return +1;
            else return 0;
        }
        protected static int Compare(Vector3D l, Vector3D r)
        {
            int comp = Compare(l.X, r.X);
            if (comp!=0) return comp;
            comp = Compare(l.Y, r.Y);
            if (comp != 0) return comp;
            comp = Compare(l.Z, r.Z);
            return comp;
        }
        protected static string ToString(Vector3D v)
        {
            return String.Format("X\t{0:N}\tY\t{1:N}\tZ\t{2:N}", v.X, v.Y, v.Z);
        }
        protected static bool IsSmall(double p)
        {
            return Math.Abs(p) < small;
        }
        protected static bool IsOrdered(double a, double b, double c)
        {
            return a < b && b < c;
        }
        protected static bool IsPointOnPlane(Vector3D vertex, MyPlane plane)
        {
            return IsSmall(Vector3D.DotProduct(vertex, plane.Normal) - plane.Magnitude);
        }
        #endregion
    }
}
