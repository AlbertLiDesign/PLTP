using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class Tetrahedron
    {
        public Vector[] vertices;
        public Face[] faces;
        public Vector Center;
        /// <summary>
        /// Nodal sensitivity number
        /// </summary>
        public double[] ndlSen;

        public int ID;
        public int[] NdlID;

        /// <summary>
        /// If the element is in a non-design domain
        /// </summary>
        public bool isNonDesign = false;

        /// <summary>
        /// If the element was pinned empty for the whole optimization. The
        /// counterpart of <see cref="isNonDesign"/>: that one is emitted whole,
        /// this one contributes nothing. Without it the iso-surface can put
        /// material back into a hole, because a node on the boundary of a void
        /// region averages the sensitivities of its solid neighbours and can sit
        /// above the isovalue.
        /// </summary>
        public bool isVoid = false;

        public Tetrahedron()
        {
            vertices = new Vector[4];
            faces = new Face[4];
            Center = Vector.Origin(3);
        }
        public Tetrahedron(Vector[] vertices, Face[] faces)
        {
            if (vertices.Length != 4) throw new ArgumentException("The number of vertices must be 4!");
            if (faces.Length != 4) throw new ArgumentException("The number of faces must be 4!");
            this.vertices = vertices;
            Center = Vector.Origin(3);
            for (int i = 0; i < 4; i++)
            {
                Center += vertices[i];
            }
            Center *= 0.25;
            this.faces = faces;
        }
        public Tetrahedron(Vector[] vertices, Face[] faces, double[] nodalSensitivityNumbers, bool isNonDesign)
        {
            if (vertices.Length != 4) throw new ArgumentException("The number of vertices must be 4!");
            if (faces.Length != 4) throw new ArgumentException("The number of faces must be 4!");
            if (nodalSensitivityNumbers.Length != 4) throw new ArgumentException("The number of nodal sensitivity numbers must be 4!");
            this.vertices = vertices;
            this.faces = faces;
            ndlSen = nodalSensitivityNumbers;
            this.isNonDesign = isNonDesign;

            Center = Vector.Origin(3);
            for (int i = 0; i < 4; i++)
            {
                Center += vertices[i];
            }
            Center *= 0.25;
        }
        public void SetID(int id)
        {
            ID = id;
        }
        public void SetNdlID(int[] ndlID)
        {
            if (ndlID.Length != 4) throw new ArgumentException("The number of nodal sensitivity numbers must be 4!");
            NdlID = ndlID;
        }
        public void SetNdlSenNum(double[] nodalSensitivityNumbers)
        {
            if (nodalSensitivityNumbers.Length != 4) throw new ArgumentException("The number of nodal sensitivity numbers must be 4!");
            ndlSen = nodalSensitivityNumbers;
        }
        public void SetNonDesign(bool isNonDesign)
        {
            this.isNonDesign = isNonDesign;
        }
        public void SetVoid(bool isVoid)
        {
            this.isVoid = isVoid;
        }

        public Mesh ToMesh()
        {
            return new Mesh(vertices, faces);
        }
    }
}
