using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class Mesh
    {
        public Vector[] vertices;
        public Face[] faces;

        #region Constructors
        public Mesh() 
        {
            vertices = new Vector[3];
            faces = new Face[1];
        }
        public Mesh(Mesh mesh)
        {
            vertices = mesh.vertices;
            faces = mesh.faces;
        }
        public Mesh(Vector[] vertices, Face[] faces)
        {
            this.vertices = vertices.ToArray();
            this.faces = faces.ToArray();
        }
        #endregion

    }
}
