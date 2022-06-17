using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class Tetrahedra
    {
        public Vector[] vertices;
        public Face[] faces;

        /// <summary>
        /// Nodal sensitivity number
        /// </summary>
        public double[] ndlSen;

        /// <summary>
        /// If the element is in a non-design domain
        /// </summary>
        public bool isNonDesign = false;

        public Tetrahedra()
        {
            vertices = new Vector[4];
            faces = new Face[4];
        }
        public Tetrahedra(Vector[] vertices, Face[] faces)
        {
            if (vertices.Length != 4) throw new ArgumentException("The number of vertices must be 4!");
            if (faces.Length != 4) throw new ArgumentException("The number of faces must be 4!");
            this.vertices = vertices;
            this.faces = faces;
        }
        public Tetrahedra(Vector[] vertices, Face[] faces, double[] nodalSensitivityNumbers, bool isNonDesign)
        {
            if (vertices.Length != 4) throw new ArgumentException("The number of vertices must be 4!");
            if (faces.Length != 4) throw new ArgumentException("The number of faces must be 4!");
            if (nodalSensitivityNumbers.Length != 4) throw new ArgumentException("The number of nodal sensitivity numbers must be 4!");
            this.vertices = vertices;
            this.faces = faces;
            ndlSen = nodalSensitivityNumbers;
            this.isNonDesign = isNonDesign;
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

        public Mesh ToMesh()
        {
            return new Mesh(vertices, faces);
        }
    }
}
