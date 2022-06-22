using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class Mesh
    {
        public Vector[] Vertices;
        public Face[] Faces;

        #region Constructors
        public Mesh() 
        {
            Vertices = new Vector[3];
            Faces = new Face[1];
        }
        public Mesh(Mesh mesh)
        {
            Vertices = mesh.Vertices;
            Faces = mesh.Faces;
        }
        public Mesh(Vector[] vertices, Face[] faces)
        {
            Vertices = vertices.ToArray();
            Faces = faces.ToArray();
        }
        #endregion
        public void CombineMeshes(Mesh anotherMesh)
        {
            Vertices = Vertices.Concat(anotherMesh.Vertices).ToArray();
            Faces = Faces.Concat(anotherMesh.Faces).ToArray();
        }
    }
}
