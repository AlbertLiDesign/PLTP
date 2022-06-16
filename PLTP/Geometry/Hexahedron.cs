using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class Hexahedron
    {
        public Vector[] vertices = new Vector[8];
        public Face[] faces = new Face[6];

        /// <summary>
        /// Nodal sensitivity number
        /// </summary>
        public double[] ndlSen = new double[8];

        /// <summary>
        /// If the element is in a non-design domain
        /// </summary>
        public bool isNonDesign = false;

        public Hexahedron() { }
        public Hexahedron(Vector[] vertices, Face[] faces, double[] nodalSensitivityNumbers, bool isNonDesign)
        {
            this.vertices = vertices;
            this.faces = faces;
            ndlSen = nodalSensitivityNumbers;
            this.isNonDesign = isNonDesign;
        }

        public Mesh ToMesh()
        {
            Mesh mesh = new Mesh();

            return mesh;
        }
    }
}
