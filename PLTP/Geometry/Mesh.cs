using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class Mesh
    {
        public List<Vector> vertices = new List<Vector>();
        public List<Face> faces = new List<Face>();

        #region Constructors
        public Mesh() { }
        public Mesh(Mesh mesh)
        {
            vertices = mesh.vertices;
            faces = mesh.faces;
        }
        public Mesh(List<Vector> vertices, List<Face> faces)
        {
            this.vertices = vertices;
            this.faces = faces;
        }
        #endregion
        public void Copy(Mesh mesh)
        {
            vertices = mesh.vertices.ToList();
            faces = mesh.faces.ToList();
        }

    }
}
