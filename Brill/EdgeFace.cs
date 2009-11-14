using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace Terry
{
    [DebuggerDisplay("id = {id}, normal = {Normal}")]
    class MyPlane// : Plane
    {
        public int id;
        public Vector3D Normal;
        public double Magnitude;
    }
    //class Point is a Vector3D

    class Line
    {
        public Vector3D origin;
        public Vector3D bearing;
        public LinkedList<Edge> edges1 = new LinkedList<Edge>();    //edges facing bearing
        public LinkedList<MyPlane> planes = new LinkedList<MyPlane>();

        public bool FindPlane(MyPlane p1, MyPlane p2)
        {
            return planes.Contains(p1) && planes.Contains(p2);
        }
    }

    /// <summary>
    /// represents a half-edge.
    /// (see ... for definition)
    /// a half edge is a directed edge with a start point 
    /// and a twin representing the same vector with opposite direction.
    /// </summary>
    [DebuggerDisplay("id = {id}, origin = {origin} {twin.origin}")]
    class Edge
    {
        public int id;
        public Vector3D origin;
        public Edge twin;
        public LinkedList<Face> faces = new LinkedList<Face>();
        public Edge next;
        public bool used;
    }

    /// <summary>
    /// A face consists of a collection of directed edges in order
    /// representing a 2D shape with n sides
    /// </summary>
    [DebuggerDisplay("id = {id}, edges={edges.Count}, normal = {normal}")]
    class Face
    {
        public Face(int i, MyPlane p, Vector3D n, Color c) { id = i; plane = p; normal = n; colour = c; }
        public int id;
        public MyPlane plane;
        public Vector3D normal;
        public Color colour;
        public List<Edge> edges = new List<Edge>();
        public Face twin;
        public bool used = false;
        public bool outer = false;  //is this face on the insider or outside?
    }

    [DebuggerDisplay("id = {id}, faces = {faces.Count}")]
    class Shape
    {
        public Shape(int i, int g, Color c) { id = i; generation = g; colour = c; }
        public int id;
        public int generation;
        public Color colour;
        public List<Face> faces = new List<Face>();
        public Vector3D centre;
    }

    ///helper
    [DebuggerDisplay("point = {point}, bearing = {bearing}")]
    class LineVertex
    {
        public LineVertex(Vector3D pt, double a, MyPlane p, Vector3D b) { point = pt; along = a; plane = p; bearing = b; }
        public Vector3D point;
        public double along;
        MyPlane plane;
        public Vector3D bearing;
    }
}
