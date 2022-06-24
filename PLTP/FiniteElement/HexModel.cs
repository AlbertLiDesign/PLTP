using System;
using System.Collections.Concurrent;
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
        private Vector VoxelSize;

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
        public HexModel(List<Vector> nodeList, List<Hexahedron> elements, List<double> elemSenNum, Vector voxelSize)
        {
            NodeList = nodeList;
            Elements = elements;
            ElemSenNum = elemSenNum;
            VoxelSize = voxelSize;

            Cases = new int[elements.Count];
        }
        #endregion

        /// <summary>
        /// Calculate the nodal sensitivity numbers
        /// </summary>
        public double[] CalNdlSenNums(double rmin)
        {
            double[] ndlSenNums = new double[NodeList.Count];

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
            var result = KDTreeMultiSearch(nds, tree, rmin, 1024);

            Parallel.For(0, nds.Length, i =>
            {
                var sum = 0.0;
                foreach (var item in result[i])
                {
                    var weight = rmin - nds[i].DistanceTo(Elements[item].Center);
                    ndlSenNums[i] += weight * ElemSenNum[item];
                    sum += weight;
                }

                ndlSenNums[i] /= sum;
            });

            return ndlSenNums;
        }
        public void SortVerts(double[] ndlSenNum)
        {
            // Sort the vertices of each element
            // according to the order of the first element

            // Get the correct vertex order
            var idx = Elements[0].SortingVertices();
            Parallel.For(0, Elements.Count, i =>
            {
                // Update the order according to the correct order
                double[] upd_ndlSen = new double[8];

                upd_ndlSen[0] = ndlSenNum[Elements[i].NdlID[idx[0]]];
                upd_ndlSen[1] = ndlSenNum[Elements[i].NdlID[idx[1]]];
                upd_ndlSen[2] = ndlSenNum[Elements[i].NdlID[idx[2]]];
                upd_ndlSen[3] = ndlSenNum[Elements[i].NdlID[idx[3]]];
                upd_ndlSen[4] = ndlSenNum[Elements[i].NdlID[idx[4]]];
                upd_ndlSen[5] = ndlSenNum[Elements[i].NdlID[idx[5]]];
                upd_ndlSen[6] = ndlSenNum[Elements[i].NdlID[idx[6]]];
                upd_ndlSen[7] = ndlSenNum[Elements[i].NdlID[idx[7]]];

                Elements[i].SetNdlSenNum(upd_ndlSen);
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

        //public Mesh[] Extract()
        //{
        //    Mesh[] meshes = new Mesh[Elements.Length];

        //    //// Each hexahedron has 8 vertices
        //    //Vector[] all_vertices = new Vector[elements.Count * 8];

        //    //// Get all vertices
        //    //for (int i = 0; i < elements.Count; i++)
        //    //{
        //    //    all_vertices[i * 8] = elements[i].vertices[0];
        //    //    all_vertices[i * 8 + 1] = elements[i].vertices[1];
        //    //    all_vertices[i * 8 + 2] = elements[i].vertices[2];
        //    //    all_vertices[i * 8 + 3] = elements[i].vertices[3];
        //    //    all_vertices[i * 8 + 4] = elements[i].vertices[4];
        //    //    all_vertices[i * 8 + 5] = elements[i].vertices[5];
        //    //    all_vertices[i * 8 + 6] = elements[i].vertices[6];
        //    //    all_vertices[i * 8 + 7] = elements[i].vertices[7];
        //    //}

        //    for (int i = 0; i < Elements.Length; i++)
        //    {
        //        int flag = ComputeFlag(i, Elements[i].ndlSen, isoValue);
                
        //        if (Elements[i].isNonDesign)
        //        {
        //            // output the original hexahedron
        //            meshes[i] = Elements[i].ToMesh();
        //        }
        //        else
        //        {
        //            if (flag == 255)
        //                meshes[i] = Elements[i].ToMesh();
        //            if (flag != 0)
        //            {
        //                // applying the proposed lookup tables
        //                var mesh = ApplyLookUpTable(flag);

        //                // To find edges which intersect with the boundary
        //                int EdgeFlag = Table.EdgeFlags_Hex[flag];
        //                // This hexahedron is in the boundary.
        //                if (EdgeFlag == 0) return null;

        //                List<Vector> pts = new List<Vector>();
        //                Vector[] EdgeVertex = new Vector[12];
        //                for (int j = 0; j < 12; i++)
        //                {
        //                    if ((EdgeFlag & (1 << j)) != 0)
        //                    {
        //                        var Offset = GetOffset(values[EdgeConnection[j, 0]], values[EdgeConnection[j, 1]], isovalue, interpolation);
        //                        var vert0 = new Vector(Vertices[EdgeConnection[j, 0], 0], Vertices[EdgeConnection[j, 0], 1], Vertices[EdgeConnection[j, 0], 2]);
        //                        var vert1 = new Vector(Vertices[EdgeConnection[j, 0], 0] + EdgeDirection[j, 0],
        //                            Vertices[EdgeConnection[j, 0], 1] + EdgeDirection[j, 1], Vertices[EdgeConnection[j, 0], 2] + EdgeDirection[j, 2]);

        //                        EdgeVertex[i] = new Vector(
        //                            (Vertices[EdgeConnection[j, 0], 0] + Offset * EdgeDirection[j, 0]),
        //                            (Vertices[EdgeConnection[j, 0], 1] + Offset * EdgeDirection[j, 1]),
        //                            (Vertices[EdgeConnection[j, 0], 2] + Offset * EdgeDirection[j, 2]));
        //                    }
        //                }

        //                // Generate triangles
        //                for (int Triangle = 0; Triangle < 5; Triangle++)
        //                {
        //                    if (Table.ConnectionTable_Hex[flag, 3 * Triangle] < 0)
        //                        break;


        //                    for (int Corner = 0; Corner < 3; Corner++)
        //                    {
        //                        int Vertex = Table.ConnectionTable_Hex[flag, 3 * Triangle + Corner];
        //                        pts.Add(EdgeVertex[Vertex]);
        //                    }
        //                }

        //                var ids = Table.ActiveTable_Hex[flag];
        //                for (int j = 0; j < ids.Count; j++)
        //                    mesh.Vertices[ids[j]] = pts[j];
        //                meshes[i] = mesh;
        //            }
        //        }
        //    }
        //    return meshes;
        //}

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


        private Mesh ApplyLookUpTable(int flag)
        {
            List<Vector> vertices = new List<Vector>();
            List<Face> faces = new List<Face>();
            // Add vertices
            for (int v = 0; v < Table.VertTable_Hex[flag].Count / 3; v++)
            {
                vertices.Add(new Vector(
                        Table.VertTable_Hex[flag][3 * v], 
                        Table.VertTable_Hex[flag][3 * v + 1], 
                        Table.VertTable_Hex[flag][3 * v + 2]));
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

        private static List<int>[] KDTreeMultiSearch(Vector[] pts, KDTree<int> tree, double radius, int maxReturned)
        {
            List<int>[] indices = new List<int>[pts.Length];
            Parallel.ForEach(Partitioner.Create(0, pts.Length, (int)Math.Ceiling(pts.Length / (double)Environment.ProcessorCount * 2.0)), delegate (Tuple<int, int> rng, ParallelLoopState loopState)
            {
                for (int i = rng.Item1; i < rng.Item2; i++)
                {
                    Vector point3d = pts[i];
                    double num = radius;
                    List<int> list = tree.NearestNeighbors(new double[]
                    {
                        point3d.X,
                        point3d.Y,
                        point3d.Z
                    }, maxReturned, num * num).ToList();
                    indices[i] = list;
                }
            });
            return indices;
        }
        #endregion
    }
}
