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

        public void RemoveDuplicatedFaces()
        {
            Dictionary<string, List<int>> dirFaces = new Dictionary<string, List<int>>();
            List<int> del = new List<int>();
            for (int i = 0; i < Faces.Length; i++)
            {
                string meshKey = SortKey(Faces[i]);
                if (!dirFaces.ContainsKey(meshKey))
                {
                    var ids = new List<int>();
                    ids.Add(i);
                    dirFaces.Add(meshKey, ids);
                }
                else
                {
                    dirFaces[meshKey].Add(i);
                    del.Add(dirFaces[meshKey][1]);
                    del.Add(dirFaces[meshKey][0]);
                }
            }

            var upd_faces = Faces.ToList();
            int num = 0;
            for (int i = 0; i < del.Count; i++)
            {
                upd_faces.RemoveAt(del[i] - num);
                num++;
            }
            Faces = upd_faces.ToArray();
        }
        private static string SortKey(Face face)
        {
            List<int> list = new List<int>();
            if (face.Vert_ID.Length==3)
            {
                list = new List<int> { face.Vert_ID[0], face.Vert_ID[1], face.Vert_ID[2] };
                list.Sort();
            }
            if (face.Vert_ID.Length == 4)
            {
                list = new List<int> { face.Vert_ID[0], face.Vert_ID[1], face.Vert_ID[2], face.Vert_ID[3] };
                list.Sort();
            }
            return string.Join(",", list);
        }
        public static Mesh CombineMeshes(Mesh[] meshes)
        {
            List<Vector> verts = new List<Vector>();
            List<Face> faces = new List<Face>();

            int num = 0;
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] != null)
                {
                    var id = new int[meshes[i].Vertices.Length];
                    for (int j = 0; j < meshes[i].Vertices.Length; j++)
                    {
                        id[j] = num;
                        verts.Add(meshes[i].Vertices[j]);
                        num++;
                    }
                    for (int j = 0; j < meshes[i].Faces.Length; j++)
                    {
                        if (meshes[i].Faces[j].Vert_ID.Length == 3)
                        {
                            faces.Add(new Face(
                            id[meshes[i].Faces[j].Vert_ID[0]],
                            id[meshes[i].Faces[j].Vert_ID[1]],
                            id[meshes[i].Faces[j].Vert_ID[2]]
                            ));
                        }
                        else if (meshes[i].Faces[j].Vert_ID.Length == 4)
                        {
                            faces.Add(new Face(
                            id[meshes[i].Faces[j].Vert_ID[0]],
                            id[meshes[i].Faces[j].Vert_ID[1]],
                            id[meshes[i].Faces[j].Vert_ID[2]],
                            id[meshes[i].Faces[j].Vert_ID[3]]
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
