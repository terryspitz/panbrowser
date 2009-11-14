using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace Terry
{
    public class CubeRenderer
    {
        static Color FaceColour = Colors.Pink;
        static Color FadeFaceColour = Color.FromArgb(100, FaceColour.R, FaceColour.G, FaceColour.B);

        public CubeRenderer()
        {
        }

        #region ShapeRenderer Members

        /*
        public RenderData OnDeviceChange(Device device)
        {
            return OnNextShape(device);
        }*/

        /*public RenderData OnNextShape(Device device)
        {
        }*/

        public RenderData Render(Sampler sampler, Flags flags)
        {
            int faces = 0, normallines = 0, edgelines = 0;
            foreach (Sampler.Cell cell in sampler.Cells.Values)
                faces += cell.Faces.Length;
            if (flags.ShowNormals)
            {
                normallines = faces * 3;
            }
            if (flags.ShowEdges)
            {
                edgelines = faces * 3;
            }

            int length = (flags.ShowFaces ? faces*3 : 0) + normallines*2 + edgelines*2;
            RenderData r = new RenderData();
            r.counts = "";
            r.triangleCount = faces;

            int start = 0;
            if (flags.ShowFaces)
            {
                FillFaces(sampler, r.Mesh, ref start);
                r.counts += String.Format("{0} Faces, ", faces);
            }
            /*
            if (flags.ShowNormals)
            {
                FillNormals(sampler, r.Mesh, ref start);
                Primitive p = new Primitive();
                p.startIndex = start;
                p.type = PrimitiveType.LineList;
                p.primitiveCount = normallines;
                r.primitives.Add(p);
                start += normallines*2;
                r.counts += String.Format("{0} Normals, ", normallines);
            }
            if (flags.ShowEdges)
            {
                FillEdges(sampler, stream);
                Primitive p = new Primitive();
                p.startIndex = start;
                p.type = PrimitiveType.LineList;
                p.primitiveCount = edgelines;
                r.primitives.Add(p);
                start += edgelines*2;
                r.counts += String.Format("{0} Edges", edgelines);
            }*/

            r.bound = 1;
            return r;
        }

        static System.Windows.Point textureOrigin = new System.Windows.Point();
        public void FillFaces(Sampler sampler, MeshGeometry3D mesh, ref int start)
        {
            foreach (Sampler.Cell cell in sampler.Cells.Values)
            {
                for (int i = 0; i < cell.Cornerset.Triangles.Length; i += 3)
                {
                    for (int j = 0; j < 3; j ++)
                    {
                        mesh.Positions.Add(cell.Faces[i / 3].Vertexes[j]);
                        mesh.Normals.Add(cell.Faces[i / 3].VertexNormals[j].vec);
                        mesh.TextureCoordinates.Add(textureOrigin);
                        mesh.TriangleIndices.Add(start++);
                    }
                }
            }
            Debug.Assert(mesh.Positions.Count == start); 
        }

        /*
        public void FillNormals(Sampler sampler, GraphicsStream stream)
        {
            List<CustomVertex.PositionNormalColored> vertexarray = new List<CustomVertex.PositionNormalColored>();
            foreach (Sampler.Cell cell in sampler.Cells.Values)
            {
                Cube.CornerSet c = cell.Cornerset;
                for (int i = 0; i < c.Triangles.Length; i += 3)
                {
                    //Point normal = c.sampler.Normals[i / 6];
                    for (int j = 0; j < 3; j ++)
                    {

                        Vector3 vn = cell.Faces[i / 3].Vertexes[j];
                        //Debug.Assert(vn.Length() > 0.1f);
                        Debug.Assert(!float.IsNaN(vn.X));
                        Vector3 normal = cell.Faces[i / 3].VertexNormals[j].vec;
                        stream.Write(new CustomVertex.PositionNormalColored(vn, normal, Color.White.ToArgb()));
                        stream.Write(new CustomVertex.PositionNormalColored(vn + normal * .25f, normal, Color.White.ToArgb()));
                    }
                }
            }
        }

        public void FillEdges(Sampler sampler, GraphicsStream stream)
        {
            foreach (Sampler.Cell cell in sampler.Cells.Values)
            {
                for (int i = 0; i < cell.Cornerset.Triangles.Length; i += 3)
                {
                    for (int j = 0; j < 3; j ++)
                    {
                        CustomVertex.PositionNormalColored vn = new CustomVertex.PositionNormalColored(cell.Faces[i / 3].Vertexes[j],
                            cell.Faces[i / 3].VertexNormals[j].vec, Color.LightGoldenrodYellow.ToArgb());
                        stream.Write(vn);
                        if(j>0)
                            stream.Write(vn);   //2nd copy of each point, from and to
                    }
                    stream.Write(
                        new CustomVertex.PositionNormalColored(cell.Faces[i / 3].Vertexes[0],
                        cell.Faces[i / 3].VertexNormals[0].vec, Color.Pink.ToArgb()));
                }
            }
        }
        */

        #endregion
    }
}
