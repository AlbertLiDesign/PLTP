using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KDTree;

namespace PLTP
{
    public class HexModel
    {
        public List<Vector> NodeList = new List<Vector>();
        public List<Hexahedron> Elements = new List<Hexahedron>();
        public List<double> ElemSenNum = new List<double>();
        public double[] NdlSenNum;

        private double InitialVolume = 0.0;
        private double TargetVolume = 0.0;
        public double Isovalue = 0.0;
        public double Tolerance = 0.01;

        public bool Interpolation = true;
        public bool KeepVolume = false;

        #region Parameters for keeping volume
        private double Step = 0.01;
        private int MaximumIteration = 20;
        #endregion

        /// <summary>
        /// The cases for each hexahedron
        /// </summary>
        private int[] Cases;

        #region Hexahedral order
        /// <summary>
        /// The order of the vertices
        /// </summary>
        private double[,] Vertices = new double[8, 3]
          {
            {0.0, 0.0, 0.0},{1.0, 0.0, 0.0},{1.0, 1.0, 0.0},{0.0, 1.0, 0.0},
            {0.0, 0.0, 1.0},{1.0, 0.0, 1.0},{1.0, 1.0, 1.0},{0.0, 1.0, 1.0}
          };

        /// <summary>
        /// The conncetion relationship of the edges
        /// </summary>
        private int[,] EdgeConnection = new int[12, 2]
        {
            {0,1}, {1,2}, {2,3}, {3,0},
            {4,5}, {5,6}, {6,7}, {7,4},
            {0,4}, {1,5}, {2,6}, {3,7}
        };

        /// <summary>
        /// The direction of each edge
        /// </summary>
        private double[,] EdgeDirection = new double[12, 3]
          {
            {1.0, 0.0, 0.0},{0.0, 1.0, 0.0},{-1.0, 0.0, 0.0},{0.0, -1.0, 0.0},
            {1.0, 0.0, 0.0},{0.0, 1.0, 0.0},{-1.0, 0.0, 0.0},{0.0, -1.0, 0.0},
            {0.0, 0.0, 1.0},{0.0, 0.0, 1.0},{ 0.0, 0.0, 1.0},{0.0, 0.0, 1.0}
          };
        #endregion

        #region Constructors
        public HexModel() { }
        /// <summary>
        /// Post-processing method for hexahedron model without keeping volume
        /// </summary>
        public HexModel(List<Vector> nodeList, List<Hexahedron> elements, List<double> elemSenNum)
        {
            NodeList = nodeList;
            Elements = elements;
            ElemSenNum = elemSenNum;

            Cases = new int[elements.Count];
        }
        #endregion

        /// <summary>
        /// Calculate the nodal sensitivity numbers
        /// </summary>
        public void CalNdlSenNums(double rmin)
        {
            NdlSenNum = new double[NodeList.Count];

            // Construct KDTree
            var tree = new KDTree<int>(3);

            // Get centers
            for (int i = 0; i < Elements.Count; i++)
            {
                tree.AddPoint(new double[3]
                {
                    Elements[i].Center.X,
                    Elements[i].Center.Y,
                    Elements[i].Center.Z
                }, i);
            }

            // Searching
            var nds = NodeList.ToArray();
            var result = Utils.KDTreeMultiSearch(nds, tree, rmin, 1024);

            Parallel.For(0, nds.Length, i =>
            {
                var sum = 0.0;
                foreach (var item in result[i])
                {
                    var weight = rmin - nds[i].DistanceTo(Elements[item].Center);
                    NdlSenNum[i] += weight * ElemSenNum[item];
                    sum += weight;
                }

                NdlSenNum[i] /= sum;
            });
        }
        public void SortVerts(bool unitisation = true)
        {
            // Sort the vertices of each element
            // according to the order of the first element

            if (unitisation)
            {
                double min = NdlSenNum.Min();
                double max = NdlSenNum.Max();
                Parallel.For(0, NdlSenNum.Length, i =>
                {
                    NdlSenNum[i] = (NdlSenNum[i] - min) / (max- min);
                });
            }

            // Get the correct vertex order
            var idx = Elements[0].SortingVertices();
            Parallel.For(0, Elements.Count, i =>
            {
                // Update the order according to the correct order
                double[] upd_ndlSen = new double[8];
                Vector[] verts= new Vector[8];

                upd_ndlSen[0] = NdlSenNum[Elements[i].NdlID[idx[0]]];
                upd_ndlSen[1] = NdlSenNum[Elements[i].NdlID[idx[1]]];
                upd_ndlSen[2] = NdlSenNum[Elements[i].NdlID[idx[2]]];
                upd_ndlSen[3] = NdlSenNum[Elements[i].NdlID[idx[3]]];
                upd_ndlSen[4] = NdlSenNum[Elements[i].NdlID[idx[4]]];
                upd_ndlSen[5] = NdlSenNum[Elements[i].NdlID[idx[5]]];
                upd_ndlSen[6] = NdlSenNum[Elements[i].NdlID[idx[6]]];
                upd_ndlSen[7] = NdlSenNum[Elements[i].NdlID[idx[7]]];

                verts[0] = NodeList[Elements[i].NdlID[idx[0]]];
                verts[1] = NodeList[Elements[i].NdlID[idx[1]]];
                verts[2] = NodeList[Elements[i].NdlID[idx[2]]];
                verts[3] = NodeList[Elements[i].NdlID[idx[3]]];
                verts[4] = NodeList[Elements[i].NdlID[idx[4]]];
                verts[5] = NodeList[Elements[i].NdlID[idx[5]]];
                verts[6] = NodeList[Elements[i].NdlID[idx[6]]];
                verts[7] = NodeList[Elements[i].NdlID[idx[7]]];

                Elements[i].SetNdlSenNum(upd_ndlSen);
                Elements[i].Vertices = verts;
                Elements[i].MinVert = verts[0];
            });
        }

        /// <summary>
        /// Set the parameters for the keeping volume method
        /// </summary>
        public void SetParameters(double initialVolume, double targetVolume, 
            double isoValue, double tolerance, double step, 
            int maximumIteration, bool interpolation = true, bool keepVolume = false)
        {
            InitialVolume = initialVolume;
            TargetVolume = targetVolume;
            Isovalue = isoValue;
            Tolerance = tolerance;
            Step = step;
            MaximumIteration = maximumIteration;
            Interpolation = interpolation;
            KeepVolume = keepVolume;
        }

        public Mesh[] Extract()
        {
            Mesh[] meshes = new Mesh[Elements.Count];

            for (int i = 0; i < Elements.Count; i++)
            {
                int flag = ComputeFlag(i, Elements[i].ndlSen, Isovalue);

                if (Elements[i].isNonDesign)
                {
                    // output the original hexahedron
                    meshes[i] = Elements[i].ToMesh();
                }
                else
                {
                    if (flag == 255)
                        meshes[i] = Elements[i].ToMesh();
                    else if (flag != 0)
                        meshes[i] = IsoSenMdl_Hex(Elements[i], flag);
                    else
                        meshes[i] = null;
                }
            }
            return meshes;
        }

        public Mesh IsoSenMdl_Hex(Hexahedron elem, int flag)
        {
            var values = elem.ndlSen;
            // applying the proposed lookup tables
            var mesh = ApplyLookUpTable(elem, flag);

            // To find edges which intersect with the boundary
            int EdgeFlag = Table.EdgeFlags_Hex[flag];
            // This voxel is in the boundary.
            if (EdgeFlag == 0) return null;

            if (!Interpolation) return mesh;

            List<Vector> pts = new List<Vector>();
            Vector[] EdgeVertex = new Vector[12];
            for (int i = 0; i < 12; i++)
            {
                if ((EdgeFlag & (1 << i)) != 0)
                {
                    var Offset = GetOffset(values[EdgeConnection[i, 0]], values[EdgeConnection[i, 1]], Isovalue, Interpolation);
                    //var vert0 = new Vector(Vertices[EdgeConnection[i, 0], 0], Vertices[EdgeConnection[i, 0], 1], Vertices[EdgeConnection[i, 0], 2]);
                    //var vert1 = new Vector(Vertices[EdgeConnection[i, 0], 0] + EdgeDirection[i, 0],
                    //    Vertices[EdgeConnection[i, 0], 1] + EdgeDirection[i, 1], Vertices[EdgeConnection[i, 0], 2] + EdgeDirection[i, 2]);
                    //Line line = new Line(vert0, vert1);

                    EdgeVertex[i] = new Vector(
                        (Vertices[EdgeConnection[i, 0], 0] + Offset * EdgeDirection[i, 0] + elem.MinVert.X),
                        (Vertices[EdgeConnection[i, 0], 1] + Offset * EdgeDirection[i, 1] + elem.MinVert.Y),
                        (Vertices[EdgeConnection[i, 0], 2] + Offset * EdgeDirection[i, 2] + elem.MinVert.Z));


                    //EdgeVertex[i] = line.ClosestPoint(computeV, true);
                }
            }

            // Generate triangles
            for (int Triangle = 0; Triangle < 5; Triangle++)
            {
                if (Table.ConnectionTable_Hex[flag, 3 * Triangle] < 0)
                    break;


                for (int Corner = 0; Corner < 3; Corner++)
                {
                    int Vertex = Table.ConnectionTable_Hex[flag, 3 * Triangle + Corner];
                    pts.Add(EdgeVertex[Vertex]);
                }
            }

            var ids = Table.ActiveTable_Hex[flag];
            for (int j = 0; j < ids.Count; j++)
                mesh.Vertices[ids[j]] = pts[j];

            return mesh;
        }

        public Mesh[] ToMeshes()
        {
            Mesh[] meshes = new Mesh[Elements.Count];
            for (int i = 0; i < Elements.Count; i++)
            {
                meshes[i] = Elements[i].ToMesh();
            }
            return meshes;
        }

        #region Private Methods
        /// <summary>
        /// To compute interpolated points.
        /// </summary>
        /// <param name="value1"> The first value. </param>
        /// <param name="value2"> The second value. </param>
        /// <param name="isovalue"> The isovalue. </param>
        /// <param name="interpolation"> Whether to use linear interpolation. </param>
        /// <returns></returns>
        private double GetOffset(double value1, double value2, double isovalue, bool interpolation)
        {
            if (!interpolation)
                return 0.5;

            if (Math.Abs(isovalue - value1) < 0)
            {
                return 0;
            }
            return (isovalue - value1) / (value2 - value1);
        }

        private Mesh ApplyLookUpTable(Hexahedron elem, int flag)
        {
            List<Vector> vertices = new List<Vector>();
            List<Face> faces = new List<Face>();
            // Add vertices
            for (int v = 0; v < Table.VertTable_Hex[flag].Count / 3; v++)
            {
                vertices.Add(new Vector(
                        Table.VertTable_Hex[flag][3 * v] * elem.Size.X + elem.MinVert.X, 
                        Table.VertTable_Hex[flag][3 * v + 1] * elem.Size.Y + elem.MinVert.Y, 
                        Table.VertTable_Hex[flag][3 * v + 2] * elem.Size.Z + elem.MinVert.Z));
            }

            // Add faces
            for (int f = 0; f < Table.FaceTable_Hex[flag].Count; f++)
            {
                if (Table.FaceTable_Hex[flag][f].Count == 3)
                {
                    faces.Add(new Face(
                        Table.FaceTable_Hex[flag][f][0], 
                        Table.FaceTable_Hex[flag][f][1],
                        Table.FaceTable_Hex[flag][f][2]));
                }
                else
                {
                    faces.Add(new Face(
                        Table.FaceTable_Hex[flag][f][0], 
                        Table.FaceTable_Hex[flag][f][1],
                        Table.FaceTable_Hex[flag][f][2], 
                        Table.FaceTable_Hex[flag][f][3]));
                }
            }
            return new Mesh(vertices.ToArray(), faces.ToArray());
        }
        private int ComputeFlag(int id, double[] values, double isovalue)
        {
            int flag = 0;
            for (int i = 0; i < 8; i++)
            {
                // check the state of each vertice.
                if (values[i] > isovalue)
                {
                    flag |= 1 << i;
                }
            }
            Cases[id] = flag;
            return flag;
        }

        #endregion
    }
}
