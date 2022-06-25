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

        public static Mesh CombineMeshes(Mesh[] meshes)
        {
            List<Vector> verts = new List<Vector>();
            List<Face> faces = new List<Face>();

            int num = 0;
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] != null)
                {
                    for (int j = 0; j < meshes[i].Vertices.Length; j++)
                    {
                        verts.Add(meshes[i].Vertices[j]);
                        num++;
                    }
                    for (int j = 0; j < meshes[i].Faces.Length; j++)
                    {
                        if (meshes[i].Faces[j].Vert_ID.Length == 3)
                        {
                            faces.Add(new Face(
                            meshes[i].Faces[j].Vert_ID[0] + num,
                            meshes[i].Faces[j].Vert_ID[1] + num,
                            meshes[i].Faces[j].Vert_ID[2] + num
                            ));
                        }
                        else if (meshes[i].Faces[j].Vert_ID.Length == 4)
                        {
                            faces.Add(new Face(
                            meshes[i].Faces[j].Vert_ID[0] + num,
                            meshes[i].Faces[j].Vert_ID[1] + num,
                            meshes[i].Faces[j].Vert_ID[2] + num,
                            meshes[i].Faces[j].Vert_ID[3] + num
                            ));
                        }
                        else
                        {
                            throw new Exception("There is a invalid face.");
                        }
                    }
                }
            }


            return new Mesh(verts.ToArray(), faces.ToArray());
        }
    }
}
