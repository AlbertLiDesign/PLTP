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
        public Vector Center;
        public Vector Size;
        public Vector MinVert;

        public int ID;
        public int[] NdlID;

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

            Center = new Vector(0.0, 0.0, 0.0);
            for (int i = 0; i < 8; i++)
            {
                Center += vertices[i];
            }
            Center /= 8;
            Size = new Vector(1.0, 1.0, 1.0);
        }
        public void SetSize(Vector size)
        {
            Size = size;
        }
        public void SetMinimumVertex(Vector miniVert)
        {
            MinVert = miniVert;
        }
        public void SetID(int id)
        {
            ID = id;
        }
        public void SetNdlID(int[] ndlID)
        {
            if (ndlID.Length != 8) throw new ArgumentException("The number of nodal sensitivity numbers must be 8!");
            NdlID = ndlID;
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

        public int[] SortingVertices()
        {
            var sorted = Vertices.Select((p, i) => new KeyValuePair<Vector, int>(p, i))
                .OrderBy(p => p.Key.X).ThenBy(p => p.Key.Y).ThenBy(p => p.Key.Z).ToList();

            // the correct vertex order
            int[] idx = sorted.Select(p => p.Value).ToArray();

            // adjust the vertex order
            var verts = sorted.Select(p => p.Key).ToArray();
            Vertices = new Vector[8] {verts[0], verts[4], verts[6], verts[2], verts[1], verts[5], verts[7],verts[3]};
            MinVert = Vertices[0];

            return new int[8] { idx[0], idx[4], idx[6], idx[2], idx[1], idx[5], idx[7], idx[3] };
        }

        #region Static methods
        public static Mesh CombineHexahedrons(List<Hexahedron> elems)
        {
            Vector[] vertices = new Vector[elems.Count * 8];
            Face[] faces = OffsetFaceID(elems);
            Parallel.For(0, elems.Count, i=>
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
       
        private static Face[] OffsetFaceID(List<Hexahedron> elems)
        {
            Face[] faces = new Face[elems.Count * 6];
            Parallel.For(0, elems.Count, i =>
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
