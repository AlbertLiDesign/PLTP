using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class Hexahedron
    {
        public Vector[] Vertices;
        public Face[] Faces;

        /// <summary>
        /// Nodal sensitivity number
        /// </summary>
        public double[] ndlSen;

        /// <summary>
        /// If the element is in a non-design domain
        /// </summary>
        public bool isNonDesign = false;

        public Hexahedron() 
        {
            Vertices = new Vector[8];
            Faces = new Face[6];
        }
        public Hexahedron(Vector[] vertices, Face[] faces)
        {
            if (vertices.Length != 8) throw new ArgumentException("The number of vertices must be 8!");
            if (faces.Length != 6) throw new ArgumentException("The number of faces must be 6!");
            Vertices = vertices;
            Faces = faces;
        }
        public Hexahedron(Vector[] vertices, Face[] faces, double[] nodalSensitivityNumbers, bool isNonDesign)
        {
            if (vertices.Length != 8) throw new ArgumentException("The number of vertices must be 8!");
            if (faces.Length != 6) throw new ArgumentException("The number of faces must be 6!");
            if (nodalSensitivityNumbers.Length != 8) throw new ArgumentException("The number of nodal sensitivity numbers must be 8!");
            Vertices = vertices;
            Faces = faces;
            ndlSen = nodalSensitivityNumbers;
            this.isNonDesign = isNonDesign;
        }
        public void SetNdlSenNum(double[] ndlSen)
        {
            if (ndlSen.Length != 8) throw new ArgumentException("The number of nodal sensitivity numbers must be 8!");
            this.ndlSen = ndlSen;
        }
        public void SetNonDesign(bool isNonDesign)
        {
            this.isNonDesign = isNonDesign;
        }

        public Mesh ToMesh()
        {
            return new Mesh(Vertices, Faces);
        }

        public void SortingVertices()
        {
            double[] x_list = new double[8];
            double[] y_list = new double[8];
            double[] z_list = new double[8];
            for (int i = 0; i < 8; i++)
            {
                x_list[i] = Vertices[i].X;
                y_list[i] = Vertices[i].Y;
                z_list[i] = Vertices[i].Z;
            }

            var x_min = x_list.Min();
            var y_min = y_list.Min();
            var z_min = z_list.Min();
            var x_max = x_list.Max();
            var y_max = y_list.Max();
            var z_max = z_list.Max();

            Vertices = new Vector[8]
            {
                new Vector(x_min, y_min, z_min),
                new Vector(x_max, y_min, z_min),
                new Vector(x_max, y_max, z_min),
                new Vector(x_min, y_max, z_min),
                
                new Vector(x_min, y_min, z_max),
                new Vector(x_max, y_min, z_max),
                new Vector(x_max, y_max, z_max),
                new Vector(x_min, y_max, z_max)
            };
        }

        #region Static methods
        public static void SortHexahedrons_Verts(Hexahedron[] elems)
        {
            Parallel.For(0, elems.Length, i =>
            {
                elems[i].SortingVertices();
            });
        }
        public static Mesh CombineHexahedrons(Hexahedron[] elems)
        {
            Vector[] vertices = new Vector[elems.Length * 8];
            Face[] faces = OffsetFaceID(elems);
            Parallel.For(0, elems.Length, i=>
            {
                vertices[i * 8] = elems[i].Vertices[0];
                vertices[i * 8 + 1] = elems[i].Vertices[1];
                vertices[i * 8 + 2] = elems[i].Vertices[2];
                vertices[i * 8 + 3] = elems[i].Vertices[3];
                vertices[i * 8 + 4] = elems[i].Vertices[4];
                vertices[i * 8 + 5] = elems[i].Vertices[5];
                vertices[i * 8 + 6] = elems[i].Vertices[6];
                vertices[i * 8 + 7] = elems[i].Vertices[7];
            });

            return new Mesh(vertices,faces);
        }
       

        private static Face[] OffsetFaceID(Hexahedron[] elems)
        {
            Face[] faces = new Face[elems.Length * 6];
            Parallel.For(0, elems.Length, i =>
            {
                int offset = i * 8;
                for (int j = 0; j < 6; j++)
                {
                    int a = elems[i].Faces[j].Vert_ID[0] + offset;
                    int b = elems[i].Faces[j].Vert_ID[1] + offset;
                    int c = elems[i].Faces[j].Vert_ID[2] + offset;
                    int d = elems[i].Faces[j].Vert_ID[3] + offset;
                    faces[i * 6 + j] = new Face(a, b, c, d);
                }
            });
            return faces;
        }
        #endregion

    }
}
