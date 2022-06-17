using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class Hexahedron
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

        public Hexahedron() 
        {
            vertices = new Vector[8];
            faces = new Face[6];
        }
        public Hexahedron(Vector[] vertices, Face[] faces)
        {
            if (vertices.Length != 8) throw new ArgumentException("The number of vertices must be 8!");
            if (faces.Length != 6) throw new ArgumentException("The number of faces must be 6!");
            this.vertices = vertices;
            this.faces = faces;
        }
        public Hexahedron(Vector[] vertices, Face[] faces, double[] nodalSensitivityNumbers, bool isNonDesign)
        {
            if (vertices.Length != 8) throw new ArgumentException("The number of vertices must be 8!");
            if (faces.Length != 6) throw new ArgumentException("The number of faces must be 6!");
            if (nodalSensitivityNumbers.Length != 8) throw new ArgumentException("The number of nodal sensitivity numbers must be 8!");
            this.vertices = vertices;
            this.faces = faces;
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
            return new Mesh(vertices, faces);
        }
    }
}
