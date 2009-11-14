using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Animation;

namespace Terry
{
    class BrillRenderer
    {
        Geometry geo;
        IEnumerable enumerable;
        IEnumerator en = null;
        int generation = 0;

        //Ripples ripples;
        Random random;

        static float period = 5f;
        double startTime;
        bool shapeChangeReady = false;
        bool timeChangeReady = false;
        Color thisColor, nextColor;
        int thisGeneration, nextGeneration;
        private Storyboard _storyboard;
        public Storyboard Storyboard { set { _storyboard = value; } }

        public MeshGeometry3D Init()
        {
            //geo = new Test().BuildTestPoly1();
            while(geo==null)
            {
                try {
                    geo = BrillManager.BuildEverything();
                }
                catch
                {
                }
            }
            geo.Shapes.Insert(0, null);  //means 'show all'
            enumerable = geo.Shapes;
            //ripples = new Ripples();
            random = new Random();
            return OnNextShape();
        }

        public MeshGeometry3D OnNextShape()
        {
            generation += 1;
            if (generation >= geo.maxGeneration)
                generation = 1;

            thisGeneration = nextGeneration;
            nextGeneration = random.Next(geo.maxGeneration - 2) + 1;
            thisColor = Color.FromArgb(255, nextColor.R, nextColor.G, nextColor.B);
            nextColor = Color.FromArgb(50, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
            //thisColor = Color.FromArgb(255, random.Next(255), random.Next(255), random.Next(255));
            //thisColor = Color.White;
            //nextColor = Color.FromArgb(50, Color.Blue);
            MeshGeometry3D r = null;
            OnTimerEvent(0, ref r);
            return r;
        }

        public void OnTimerEvent(double time, ref MeshGeometry3D renderer)
        {
            double modulo = (time / period * 2) % 2;
            if (modulo > 1)
                shapeChangeReady = true;
            else if (modulo < 1 && shapeChangeReady)
            {
                shapeChangeReady = false;
                renderer = OnNextShape();
            }

            modulo = (time * 10 * 2) % 2;
            if (modulo > 1)
                timeChangeReady = true;
            else if (modulo < 1 && timeChangeReady)
            {
                timeChangeReady = false;
                double max;
                renderer = Fill(time, out max);
            }
        }

        public MeshGeometry3D Fill(double time, out double max)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            MeshGeometry3D meshGeometry3D = new MeshGeometry3D();

            // Create a collection of normal vectors for the MeshGeometry3D.
            Vector3DCollection normalCollection = new Vector3DCollection();
            meshGeometry3D.Normals = normalCollection;

            // Create a collection of vertex positions for the MeshGeometry3D. 
            Point3DCollection positionCollection = new Point3DCollection();
            meshGeometry3D.Positions = positionCollection;

            // Create a collection of texture coordinates for the MeshGeometry3D.
            PointCollection textureCoordinatesCollection = new PointCollection();
            meshGeometry3D.TextureCoordinates = textureCoordinatesCollection;

            // Create a collection of triangle indices for the MeshGeometry3D.
            Int32Collection indicesCollection = new Int32Collection();
            meshGeometry3D.TriangleIndices = indicesCollection;

            // Apply the mesh to the geometry model.
            //myGeometryModel.Geometry = myMeshGeometry3D;

            int startIndex = 0;
            int shapes = 0;

            //draw faces
            do
            {
                foreach (Shape shape in (List<Shape>)enumerable)
                {
                    if (shape != null && shape.generation == generation)
                    {
                        //Color c = ColorOperator.Scale(Color.White, 1 / ripples.GetDisplacement(shape.centre, (float)time).Length());
                        //Color c = Color.FromArgb(random.Next(255), random.Next(255), random.Next(255), random.Next(255));
                        foreach (Face face in shape.faces)
                            startIndex = AddFace(shape.colour, startIndex, positionCollection, normalCollection, indicesCollection, textureCoordinatesCollection, face, shape, _storyboard);
                        shapes += 1;
                    }
                }
                if ((positionCollection.Count== 0))
                    generation = 0;
            } while (positionCollection.Count == 0);

            //get bounds
            max=0.01;
            foreach (Point3D vertex in positionCollection)
            {
                max = Math.Max(max, new Vector3D(vertex.X, vertex.Y, vertex.Z).Length);
            }


            //r.counts = String.Format("Gen: {0}, Shapes: {1}, Faces: {2}, Trianges {3}",
            //    generation, shapes, vertexarray.Count, startIndex);
            //r.bound = max;

            Debug.WriteLine("FillVertexBuffer took ms " + watch.ElapsedMilliseconds);

            return meshGeometry3D;
        }

        private int AddFace(Color color, int startIndex, 
            Point3DCollection points, Vector3DCollection normals, Int32Collection indices, PointCollection texture,
            Face face, Shape shape, Storyboard storyboard)
        {
            //build a trianglefan representing the face
            foreach(Edge e in face.edges)
            {
                points.Add(new Point3D(e.origin.X, e.origin.Y, e.origin.Z));
                normals.Add(face.plane.Normal);
                texture.Add(new Point(face.colour.R/255.0,0));
                Point3DAnimation an = new Point3DAnimation(
                    new Point3D(e.origin.X + face.normal.X, e.origin.Y + face.normal.Y, e.origin.Z + face.normal.Z), Duration.Automatic);
                an.IsAdditive = true;
                //Storyboard.SetTargetProperty(points, points);
                //storyboard.Children.Add(an);
            }

            for (int i = 1; i < face.edges.Count-1; i++)
            {
                indices.Add(startIndex);
                indices.Add(startIndex + i+1);
                indices.Add(startIndex + i);
            }
            return startIndex + face.edges.Count;
        }


    }

}