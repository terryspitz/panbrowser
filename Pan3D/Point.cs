//#define CLASSTYPE
#define STRUCTTYPE

using System.Collections.Generic;
using System;
namespace Terry
{
#if CLASSTYPE
    public class Point
#else
    public struct Point
#endif
    {
        public Point(int a, int b, int c) { i = a; j = b; k = c; }
        public int i, j, k;

#if CLASSTYPE
        public Point Clone() { return (Point)this.MemberwiseClone(); }
#endif
        public static Point operator +(Point a, Point b)
        {
            return new Point(a.i + b.i, a.j + b.j, a.k + b.k);
        }
        public static Point operator -(Point p1, Point p2)
        {
            return new Point(p1.i - p2.i, p1.j - p2.j, p1.k - p2.k);
        }
        public static int operator *(Point p1, Point p2)
        {
            return p1.i * p2.i + p1.j * p2.j + p1.k * p2.k;
        }
        public static Point operator *(Point p1, float m)
        {
            return new Point((int)(p1.i * m), (int)(p1.j * m), (int)(p1.k * m));
        }
        public static Point Cross(Point a, Point b)
        {
            return new Point(a.j * b.k - a.k * b.j, a.k * b.i - a.i * b.k, a.i * b.j - a.j * b.i);
        }
        public Point bumpCorner(int v)
        {
#if CLASSTYPE
            Point corner = this.Clone();
#else
            Point corner = this;//.Clone();
#endif
            corner.i += (v & 1 << 0);
            corner.j += (v & 1 << 1) >> 1;
            corner.k += (v & 1 << 2) >> 2;
            return corner;
        }
        public override string ToString()
        {
            return String.Format("{0}, {1}, {2}", i, j, k);
        }
    }

    class PointComparer : IEqualityComparer<Point>, IComparer<Point>
    {
        public bool Equals(Point lhs, Point rhs)
        { return lhs.i == rhs.i && lhs.j == rhs.j && lhs.k == rhs.k; }
        public int GetHashCode(Point p)
        { return (p.i.GetHashCode() << 4) + (p.j.GetHashCode() << 2) + p.k.GetHashCode(); }
        public int Compare(Point lhs, Point rhs)
        {
            if (lhs.i != rhs.i)
                return lhs.i - rhs.i;
            else if (lhs.j != rhs.j)
                return lhs.j - rhs.j;
            else if (lhs.k != rhs.k)
                return lhs.k - rhs.k;
            return 0;
        }
        public static Point PointToPoint(Point lhs)
        {
            return new Point(lhs.i, lhs.j, lhs.k);
        }
    }
    /*bool operator<(point rhs)
    {
        if(i!=rhs.i) return i<rhs.i;
        if(j!=rhs.j) return j<rhs.j;
        if(k!=rhs.k) return k<rhs.k;
        return false;
    }*/

}