
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace Terry
{
    public class RenderData
    {
        public MeshGeometry3D Mesh { get; set; }

        public RenderData()
        {
            Mesh = new MeshGeometry3D();

            // Create a collection of normal vectors for the Mesh.
            Vector3DCollection normalCollection = new Vector3DCollection();
            Mesh.Normals = normalCollection;

            // Create a collection of vertex positions for the Mesh. 
            Point3DCollection positionCollection = new Point3DCollection();
            Mesh.Positions = positionCollection;

            // Create a collection of texture coordinates for the Mesh.
            PointCollection textureCoordinatesCollection = new PointCollection();
            Mesh.TextureCoordinates = textureCoordinatesCollection;

            // Create a collection of triangle indices for the Mesh.
            Int32Collection indicesCollection = new Int32Collection();
            Mesh.TriangleIndices = indicesCollection;
        }

        public string counts;
        public float bound;
        public int triangleCount;
        public long calcMilliseconds;
        public long renderMilliseconds;
        public string description;

        public string Description
        {
            get
            {
                return string.Format("{6}\n{0}, {1} triangle in {2}+{3}ms={4}tri/ms\n{5} fps",
                    counts, triangleCount, calcMilliseconds, renderMilliseconds, triangleCount / (calcMilliseconds + renderMilliseconds), 1000 / (int)((calcMilliseconds + renderMilliseconds)),
                    description);
            }
        }
    }


}