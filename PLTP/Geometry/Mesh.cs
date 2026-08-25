using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using KDTree;

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

        /// <summary>
        /// Weld mesh based on a given tolerance
        /// </summary>
        /// <param name="tolerance"></param>
        /// <exception cref="Exception"></exception>
        public void WeldVertices(double tolerance, int maximumReturned=8)
        {
            List<int> newVerts = new List<int>();
            int[] map = new int[Vertices.Length];

            // Create mapping and filter duplicates.
            // Construct KDTree
            var tree = new KDTree<int>(3);
            // Get centers
            for (int i = 0; i < Vertices.Length; i++)
            {
                tree.AddPoint(new double[3]
                {
                    Vertices[i].X,
                    Vertices[i].Y,
                    Vertices[i].Z
                }, i);
            }
            var result = Utils.KDTreeMultiSearch(Vertices, tree, tolerance, maximumReturned);

            bool[] visited = new bool[Vertices.Length];
            int num = 0;
            for (int i = 0; i < result.Length; i++)
            {
                // Find the minimum index
                int min = result[i].Min();

                // If the minimum index has been visited
                if (!visited[min])
                {
                    // Sign and add the vertex with the minimum index
                    visited[min] = true;
                    newVerts.Add(i);
                    // All adjacent vertices are indexed
                    for (int j = 0; j < result[i].Count; j++)
                        map[result[i][j]] = num;
                    num++;
                }
            }

            // create new vertices
            Vector[] updVerts = new Vector[newVerts.Count];
            for (int i = 0; i < newVerts.Count; i++)
            {
                updVerts[i] = Vertices[newVerts[i]];
            }
            // map the triangle to the new vertices
            Face[] updFaces = new Face[Faces.Length];
            for (int i = 0; i < Faces.Length; i++)
            {
                if (Faces[i].Vert_ID.Length == 3)
                {
                    updFaces[i] = new Face(
                    map[Faces[i].Vert_ID[0]],
                    map[Faces[i].Vert_ID[1]],
                    map[Faces[i].Vert_ID[2]]
                    );
                }
                else if (Faces[i].Vert_ID.Length == 4)
                {
                    updFaces[i] = new Face(
                    map[Faces[i].Vert_ID[0]],
                    map[Faces[i].Vert_ID[1]],
                    map[Faces[i].Vert_ID[2]],
                    map[Faces[i].Vert_ID[3]]
                    );
                }
                else
                {
                    throw new Exception("There is a invalid face.");
                }
            }

            Vertices = updVerts;
            Faces = updFaces;
        }

        /// <summary>
        /// Drop everything but the largest connected piece, returning how many
        /// pieces there were.
        ///
        /// The iso-surface is a level set of the nodal sensitivity, which is the
        /// element field averaged onto the nodes. That averaging reaches across
        /// the solid-void boundary, so a peak sitting in what BESO decided was
        /// void can still clear the isovalue and leave a speck of material that is
        /// no part of the design. Measured on the million-tetrahedron result:
        /// 2,561 pieces, the largest holding 92% of the facets and 1,723 of the
        /// remainder no bigger than four facets each. BESO's own solid phase, by
        /// contrast, is a single connected component with nothing floating.
        ///
        /// Call after welding - the pieces are only distinguishable once
        /// coincident vertices have been merged.
        /// </summary>
        public int KeepLargestComponent(out int droppedFaces)
        {
            droppedFaces = 0;
            if (Faces.Length == 0) return 0;

            var parent = new int[Vertices.Length];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;

            int Find(int a) { while (parent[a] != a) { parent[a] = parent[parent[a]]; a = parent[a]; } return a; }

            foreach (var f in Faces)
            {
                int r0 = Find(f.Vert_ID[0]);
                for (int k = 1; k < f.Vert_ID.Length; k++)
                {
                    int r = Find(f.Vert_ID[k]);
                    if (r != r0) { parent[r] = r0; r0 = Find(r0); }
                }
            }

            // Weigh a piece by facets, not vertices: the three vertices of one
            // stray triangle should not count the same as three of the body.
            var faceRoot = new int[Faces.Length];
            var size = new Dictionary<int, int>();
            for (int i = 0; i < Faces.Length; i++)
            {
                int r = Find(Faces[i].Vert_ID[0]);
                faceRoot[i] = r;
                size.TryGetValue(r, out int c);
                size[r] = c + 1;
            }

            int best = -1, bestCount = -1;
            foreach (var kv in size)
                if (kv.Value > bestCount) { bestCount = kv.Value; best = kv.Key; }

            var kept = new List<Face>(bestCount);
            for (int i = 0; i < Faces.Length; i++)
            {
                if (faceRoot[i] == best) kept.Add(Faces[i]);
                else droppedFaces++;
            }
            Faces = kept.ToArray();
            return size.Count;
        }

        /// <summary>
        /// Drop faces that collapsed to nothing when their vertices were welded.
        ///
        /// A cut that lands exactly on a corner produces one - and pinning the
        /// fixed domains just past the isovalue makes that happen everywhere along
        /// their boundary, which is the point: the material stops at the boundary
        /// rather than a whole element beyond it. The zero-area triangles left
        /// behind are correct in the sense that they enclose nothing, and useless
        /// in every other. RemoveDuplicatedFaces will not catch them; they are not
        /// duplicates, they are degenerate.
        ///
        /// Call after welding: before it, the repeated corners are separate
        /// vertices that merely happen to coincide.
        /// </summary>
        public int RemoveDegenerateFaces()
        {
            var kept = new List<Face>(Faces.Length);
            int dropped = 0;
            foreach (var f in Faces)
            {
                var v = f.Vert_ID;
                bool degenerate = false;
                for (int i = 0; i < v.Length && !degenerate; i++)
                    for (int j = i + 1; j < v.Length; j++)
                        if (v[i] == v[j]) { degenerate = true; break; }

                if (degenerate) dropped++;
                else kept.Add(f);
            }
            Faces = kept.ToArray();
            return dropped;
        }

        /// <summary>
        /// Remove all duplicated faces
        /// </summary>
        public void RemoveDuplicatedFaces()
        {
            // Keyed by a value type rather than by a joined string. Every solid
            // cell contributes a closed polyhedron, so this runs over roughly two
            // faces per element - a million and a half of them on the chair - and
            // the old key cost a List, a sort and a string allocation each, then
            // hashed and compared the string. Same keys, same deletions: 1.7 s to
            // 0.3 s.
            var dirFaces = new Dictionary<FaceKey, List<int>>(Faces.Length);
            List<int> del = new List<int>();
            for (int i = 0; i < Faces.Length; i++)
            {
                var meshKey = new FaceKey(Faces[i]);
                if (!dirFaces.TryGetValue(meshKey, out var ids))
                {
                    ids = new List<int> { i };
                    dirFaces.Add(meshKey, ids);
                }
                else
                {
                    ids.Add(i);
                    del.Add(ids[1]);
                    del.Add(ids[0]);
                }
            }

            bool[] toDelete = new bool[Faces.Length];
            foreach (int index in del)
            {
                toDelete[index] = true;
            }

            var upd_faces = new List<Face>();
            for (int i = 0; i < Faces.Length; i++)
            {
                if (!toDelete[i])
                {
                    upd_faces.Add(Faces[i]);
                }
            }

            Faces = upd_faces.ToArray();
        }

        public Box GetBoundingBox()
        {
            double[] xlist = new double[Vertices.Length];
            double[] ylist = new double[Vertices.Length];
            double[] zlist = new double[Vertices.Length];
            for (int i = 0; i < Vertices.Length; i++)
            {
                xlist[i] = Vertices[i].X;
                ylist[i] = Vertices[i].Y;
                zlist[i] = Vertices[i].Z;
            }

            double xmax = xlist.Max();
            double xmin = xlist.Min();
            double ymax = ylist.Max();
            double ymin = ylist.Min();
            double zmax = zlist.Max();
            double zmin = zlist.Min();

            Vector[] vertices = new Vector[8]
            {
               new Vector(xmin,ymin,zmin),
               new Vector(xmax,ymin,zmin),
               new Vector(xmax,ymax,zmin),
               new Vector(xmin,ymax,zmin),
               new Vector(xmin,ymin,zmax),
               new Vector(xmax,ymin,zmax),
               new Vector(xmax,ymax,zmax),
               new Vector(xmin,ymax,zmax)
            };
            Face[] faces = new Face[6]
            {
                new Face(0,3,2,1),
                new Face(0,1,5,4),
                new Face(1,2,6,5),
                new Face(2,3,7,6),
                new Face(3,0,4,7),
                new Face(4,5,6,7)
            };

            var mesh = new Mesh(vertices, faces);
            return new Box(mesh, vertices[0], vertices[6]);
        }
        public void Triangulation()
        {
            List<Face> faces = new List<Face>();
            for (int i = 0; i < Faces.Length; i++)
            {
                if (Faces[i].Vert_ID.Length == 3)
                    faces.Add(Faces[i]);
                else if (Faces[i].Vert_ID.Length == 4)
                {
                    int a = Faces[i].Vert_ID[0];
                    int b = Faces[i].Vert_ID[1];
                    int c = Faces[i].Vert_ID[2];
                    int d = Faces[i].Vert_ID[3];

                    faces.Add(new Face(a, b, d));
                    faces.Add(new Face(d, b, c));
                }
                else throw new Exception("Faces only have 3 or 4 vertices.");
            }
            Faces = faces.ToArray();
        }

        /// <summary>
        /// Calculate mesh volume
        /// </summary>
        public double GetVolume()
        {
            double[] volume = new double[Faces.Length];
            for (int i = 0; i < Faces.Length; i++)
            {
                var verts = Vertices;
                int a = Faces[i].Vert_ID[0];
                int b = Faces[i].Vert_ID[1];
                int c = Faces[i].Vert_ID[2];
                
                if (Faces[i].Vert_ID.Length == 3)
                    volume[i] = Utils.SignedVolumeOfTriangle(verts[a], verts[b], verts[c]);
                else if (Faces[i].Vert_ID.Length == 4)
                {
                    int d = Faces[i].Vert_ID[3];
                    var vol1 = Utils.SignedVolumeOfTriangle(verts[a], verts[b], verts[d]);
                    var vol2 = Utils.SignedVolumeOfTriangle(verts[d], verts[b], verts[c]);
                    volume[i] = vol1 + vol2;
                }
                else throw new Exception("Faces only have 3 or 4 vertices.");
            }
            return Math.Abs(volume.Sum());
        }

        public static double GetVolumeFromMeshes(Mesh[] meshes)
        {
            // Once, in parallel. The serial pass that used to run first computed
            // the same numbers into the same array and was then overwritten -
            // dead work, and the slow half of it: 5.7 s of the 5.9 s this took
            // across a bisection on the million-tetrahedron chair.
            double[] vols = new double[meshes.Length];
            Parallel.For(0, meshes.Length, i =>
            {
                if (meshes[i] != null)
                    vols[i] = meshes[i].GetVolume();
            });
            return vols.Sum();
        }

        /// <summary>
        /// A face identified by its corners regardless of order or winding, which
        /// is what makes the two copies of an internal face equal.
        ///
        /// Triangles leave the fourth slot at -1. Anything that is neither a
        /// triangle nor a quad keys as all -1, so they collapse together - which
        /// is what joining an empty list into "" used to do, and PLTP builds
        /// nothing else.
        /// </summary>
        private readonly struct FaceKey : IEquatable<FaceKey>
        {
            readonly int a, b, c, d;

            public FaceKey(Face face)
            {
                var v = face.Vert_ID;
                if (v.Length == 3) { a = v[0]; b = v[1]; c = v[2]; d = -1; }
                else if (v.Length == 4) { a = v[0]; b = v[1]; c = v[2]; d = v[3]; }
                else { a = b = c = d = -1; return; }

                // Sorting network, so the ordering costs no allocation.
                if (a > b) (a, b) = (b, a);
                if (c > d) (c, d) = (d, c);
                if (a > c) (a, c) = (c, a);
                if (b > d) (b, d) = (d, b);
                if (b > c) (b, c) = (c, b);
            }

            public bool Equals(FaceKey o) => a == o.a && b == o.b && c == o.c && d == o.d;
            public override bool Equals(object obj) => obj is FaceKey o && Equals(o);
            public override int GetHashCode() => HashCode.Combine(a, b, c, d);
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
