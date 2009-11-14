using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;

namespace Terry
{
    class Test
    {
        public void BuildTestCube1x1x1()
        {
            DateTime start = DateTime.Now;
            Geometry g = new Geometry();
            Vector3[] multipliers = new Vector3[]
                    {
                        new Vector3(-1,0,0),
                        new Vector3(1,0,0),
                        new Vector3(0,-1,0),
                        new Vector3(0,1,0),
                        new Vector3(0,0,-1),
                        new Vector3(0,0,1),
                    };
            g._planes = BrillManager.CreatePlanesFromMultipliers(multipliers, g._basis);
            g._lines = BrillManager.MakeEdges(g._planes);
            BrillManager.MakeAllFaces(g._faces, g._planes, g._lines);
            Debug.Assert(g._faces.Count == 12);
            BrillManager.MakeShapes(g._faces, g._shapes);
            Debug.Assert(g._shapes.Count == 2); //one is boundary, matches the other
            foreach (Shape s in g._shapes)
                Debug.Assert(s.faces.Count == 6);

            Debug.WriteLine("\n\nWork() took ms " + (DateTime.Now - start).TotalMilliseconds);
        }

        [Test]
        public void BuildTestCube2x1x1()
        {
            DateTime start = DateTime.Now;
            Geometry g = new Geometry();
            Vector3[] multipliers = new Vector3[]
                    {
                        new Vector3(-1,0,0),
                        new Vector3(1,0,0),
                        new Vector3(2,0,0),
                        new Vector3(0,-1,0),
                        new Vector3(0,1,0),
                        new Vector3(0,0,-1),
                        new Vector3(0,0,1),
                    };
            g._planes = BrillManager.CreatePlanesFromMultipliers(multipliers, g._basis);
            g._lines = BrillManager.MakeEdges(g._planes);
            BrillManager.MakeAllFaces(g._faces, g._planes, g._lines);
            Debug.Assert(g._faces.Count == 22); //2x6x2 - 2
            BrillManager.MakeShapes(g._faces, g._shapes);
            Debug.Assert(g._shapes.Count == 3); //one is boundary, matches the other
            foreach (Shape s in g._shapes)
                Debug.Assert(s.faces.Count == 6 || s.faces.Count == 10);

            Debug.WriteLine("\n\nWork() took ms " + (DateTime.Now - start).TotalMilliseconds);
        }

        [Test]
        public void BuildTestCube2x2x1()
        {
            DateTime start = DateTime.Now;
            Geometry g = new Geometry();
            Vector3[] multipliers = new Vector3[]
                    {
                        new Vector3(-1,0,0),
                        new Vector3(1,0,0),
                        new Vector3(2,0,0),
                        new Vector3(0,-1,0),
                        new Vector3(0,1,0),
                        new Vector3(0,2,0),
                        new Vector3(0,0,-1),
                        new Vector3(0,0,1),
                    };
            g._planes = BrillManager.CreatePlanesFromMultipliers(multipliers, g._basis);
            g._lines = BrillManager.MakeEdges(g._planes);
            BrillManager.MakeAllFaces(g._faces, g._planes, g._lines);
            Assert.AreEqual(40, g._faces.Count); //2x6x2x2 - 8
            BrillManager.MakeShapes(g._faces, g._shapes);
            Assert.AreEqual(5, g._shapes.Count); //one is boundary, matches the other
            foreach (Shape s in g._shapes)
                Assert.IsTrue(s.faces.Count == 6 || s.faces.Count == 16);

            Debug.WriteLine("\n\nWork() took ms " + (DateTime.Now - start).TotalMilliseconds);
        }

        public Geometry BuildTestCubes3x3x3()
        {
            DateTime start = DateTime.Now;
            Geometry g = new Geometry();
            Vector3[] multipliers = new Vector3[]
                    {
                        new Vector3(-2,0,0),
                        new Vector3(-1,0,0),
                        new Vector3(1,0,0),
                        new Vector3(2,0,0),
                        new Vector3(0,-2,0),
                        new Vector3(0,-1,0),
                        new Vector3(0,1,0),
                        new Vector3(0,2,0),
                        new Vector3(0,0,-2),
                        new Vector3(0,0,-1),
                        new Vector3(0,0,1),
                        new Vector3(0,0,2)
                    };
            g._planes = BrillManager.CreatePlanesFromMultipliers(multipliers, g._basis);
            g._lines = BrillManager.MakeEdges(g._planes);
            BrillManager.MakeAllFaces(g._faces, g._planes, g._lines);
            Debug.Assert(g._faces.Count == 3 /*dimensions*/ * 2 /*both sides*/ * 9 /*squares*/ * 4 /*sides per square*/);
            BrillManager.MakeShapes(g._faces, g._shapes);
            Debug.Assert(g._shapes.Count == 28);
            foreach (Shape s in g._shapes)
                Debug.Assert(s.faces.Count == 6 || s.faces.Count == 54);

            Debug.WriteLine("\n\nWork() took ms " + (DateTime.Now - start).TotalMilliseconds);
            return g;
        }

        public Geometry BuildTestPyramid1x1x1()
        {
            DateTime start = DateTime.Now;
            Geometry g = new Geometry();
            Vector3[] multipliers = new Vector3[]
                    {
                        new Vector3(1,0,0),
                        new Vector3(0,1,0),
                        new Vector3(0,0,1),
                        new Vector3(2,2,2),
                    };
            g._planes = BrillManager.CreatePlanesFromMultipliers(multipliers, g._basis);
            g._lines = BrillManager.MakeEdges(g._planes);
            BrillManager.MakeAllFaces(g._faces, g._planes, g._lines);
            Debug.Assert(g._faces.Count / 2 == 4);
            BrillManager.MakeShapes(g._faces, g._shapes);
            Debug.Assert(g._shapes.Count == 2); //one is boundary, matches the other
            foreach (Shape s in g._shapes)
                Debug.Assert(s.faces.Count == 4);

            Debug.WriteLine("\n\nWork() took ms " + (DateTime.Now - start).TotalMilliseconds);
            return g;
        }

        public Geometry BuildTestHalfCube1()
        {
            DateTime start = DateTime.Now;
            Geometry g = new Geometry();
            Vector3[] multipliers = new Vector3[]
                    {
                        new Vector3(1,0,0),
                        new Vector3(1.5f,0.5f,0),
                        new Vector3(2,0,0),
                        new Vector3(0,1,0),
                        new Vector3(0,2,0),
                        new Vector3(0,0,1),
                        new Vector3(0,0,2),
                    };
            g._planes = BrillManager.CreatePlanesFromMultipliers(multipliers, g._basis);
            g._lines = BrillManager.MakeEdges(g._planes);
            Assert.AreEqual(16, g._lines.Count);
            BrillManager.MakeAllFaces(g._faces, g._planes, g._lines);
            Assert.AreEqual(14, g._faces.Count/2);
            BrillManager.MakeShapes(g._faces, g._shapes);
            Assert.AreEqual(3, g._shapes.Count-1); 

            Debug.WriteLine("\n\nWork() took ms " + (DateTime.Now - start).TotalMilliseconds);
            return g;
        }
        public Geometry BuildTestHalfCube2()
        {
            DateTime start = DateTime.Now;
            Geometry g = new Geometry();
            Vector3[] multipliers = new Vector3[]
                    {
                        new Vector3(1,0,0),
                        new Vector3(1.5f,0.1f,0),
                        new Vector3(2,0,0),
                        new Vector3(0,1,0),
                        new Vector3(0,2,0),
                        new Vector3(0,0,1),
                        new Vector3(0,0,2),
                    };
            g._planes = BrillManager.CreatePlanesFromMultipliers(multipliers, g._basis);
            g._lines = BrillManager.MakeEdges(g._planes);
            Assert.AreEqual(18, g._lines.Count);
            BrillManager.MakeAllFaces(g._faces, g._planes, g._lines);
            Assert.AreEqual(19, g._faces.Count/2);
            BrillManager.MakeShapes(g._faces, g._shapes);
            Assert.AreEqual(4, g._shapes.Count-1); 

            Debug.WriteLine("\n\nWork() took ms " + (DateTime.Now - start).TotalMilliseconds);
            return g;
        }
        public Geometry BuildTestHalfCube3()
        {
            DateTime start = DateTime.Now;
            Geometry g = new Geometry();
            Vector3[] multipliers = new Vector3[]
                    {
                        new Vector3(1,0,0),
                        new Vector3(1.5f,0.5f,0),
                        new Vector3(2,0,0),
                        new Vector3(0,1,0),
                        new Vector3(0,1.5f,0.5f),
                        new Vector3(0,2,0),
                        new Vector3(0,0,1),
                        new Vector3(0,0,2),
                    };
            g._planes = BrillManager.CreatePlanesFromMultipliers(multipliers, g._basis);
            g._lines = BrillManager.MakeEdges(g._planes);
            Assert.AreEqual(21, g._lines.Count);
            BrillManager.MakeAllFaces(g._faces, g._planes, g._lines);
            Assert.AreEqual(30, g._faces.Count / 2);
            BrillManager.MakeShapes(g._faces, g._shapes);
            Assert.AreEqual(8, g._shapes.Count - 1);
            Debug.WriteLine("\n\nWork() took ms " + (DateTime.Now - start).TotalMilliseconds);
            return g;
        }
        public Geometry BuildTestPoly1()
        {
            DateTime start = DateTime.Now;
            Geometry g = new Geometry();
            Vector3[] multipliers = new Vector3[]
                    {
                        new Vector3(-1,0,0),
                        new Vector3(1,0,0),
                        new Vector3(0,-1,0),
                        new Vector3(0,1,0),
                        new Vector3(0,0,-1),
                        new Vector3(0,0,1),
                        new Vector3(1,1,1),
                        new Vector3(-1,1,1),
                        new Vector3(1,-1,1),
                        new Vector3(1,1,-1),
                        new Vector3(-1,-1,1),
                        new Vector3(1,-1,-1),
                        new Vector3(-1,1,-1),
                    };
            g._planes = BrillManager.CreatePlanesFromMultipliers(multipliers, g._basis);
            g._lines = BrillManager.MakeEdges(g._planes);
            //Assert.AreEqual(21, g._lines.Count);
            BrillManager.MakeAllFaces(g._faces, g._planes, g._lines);
            //Assert.AreEqual(30, g._faces.Count / 2);
            BrillManager.MakeShapes(g._faces, g._shapes);
            //Assert.AreEqual(8, g._shapes.Count - 1);
            Debug.WriteLine("\n\nWork() took ms " + (DateTime.Now - start).TotalMilliseconds);
            return g;
        }
    }


    [TestFixture]
    public class NTest
    {
        [Test]
        public void BuildTestCube1x1x1v()
        {
            new Test().BuildTestCube1x1x1();
        }
        [Test]
        public void BuildTestCube2x1x1v()
        {
            new Test().BuildTestCube2x1x1();
        }
        [Test]
        public void BuildTestCube2x2x1v()
        {
            new Test().BuildTestCube2x2x1();
        }
        [Test]
        public void BuildTestCubes3x3x3v()
        {
            new Test().BuildTestCubes3x3x3();
        }
        [Test]
        public void BuildTestPyramid1x1x1v()
        {
            new Test().BuildTestPyramid1x1x1();
        }
        [Test]
        public void BuildTestHalfCube1v()
        {
            new Test().BuildTestHalfCube1();
        }
        [Test]
        public void BuildTestHalfCube2v()
        {
            new Test().BuildTestHalfCube2();
        }
        public void BuildTestHalfCube3v()
        {
            new Test().BuildTestHalfCube3();
        }
        public void BuildTestPoly1v()
        {
            new Test().BuildTestPoly1();
        }

    }//test
}
