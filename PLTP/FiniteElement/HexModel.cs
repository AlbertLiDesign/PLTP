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
        public Vector Size;

        private double VolumeFraction = 0.0;
        private double Tolerance = 0.01;
        private double FilterRadius = 0.0;

        public bool Interpolation = true;
        public bool KeepVolume = false;
        public bool UnitiseSensitivityNumber = true;
        public bool ReverseValues = false;

        #region Parameters for keeping volume
        private int MaximumIteration = 50;
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
        public HexModel(List<Vector> nodeList, List<Hexahedron> elements)
        {
            NodeList = nodeList;
            Elements = elements;
            Cases = new int[elements.Count];
        }
        public HexModel(List<Vector> nodeList, List<Hexahedron> elements, List<double> senNum, bool elemSen = true)
        {
            NodeList = nodeList;
            Elements = elements;
            if (elemSen) ElemSenNum = senNum;
            else NdlSenNum = senNum.ToArray();


            Cases = new int[elements.Count];
        }
        public HexModel(List<Vector> nodeList, List<Hexahedron> elements, List<int> solidID, List<int> voidID)
        {
            NodeList = nodeList;
            Elements = elements;

            for (int i = 0; i < solidID.Count; i++)
            {
                elements[solidID[i]].SetSolid(true);
            }
            for (int i = 0; i < voidID.Count; i++)
            {
                elements[voidID[i]].SetVoid(true);
            }

            Cases = new int[elements.Count];
        }
        public HexModel(List<Vector> nodeList, List<Hexahedron> elements, List<double> elemSenNum, List<int> solidID, List<int> voidID)
        {
            NodeList = nodeList;
            Elements = elements;
            ElemSenNum = elemSenNum;

            for (int i = 0; i < solidID.Count; i++)
            {
                elements[solidID[i]].SetSolid(true); 
            }
            for (int i = 0; i < voidID.Count; i++)
            {
                elements[voidID[i]].SetVoid(true);
            }


            Cases = new int[elements.Count];
        }
        #endregion

        public void SetNdlSenNums(double[] ndl_sen)
        {
            NdlSenNum = (double[])ndl_sen.Clone();
        }
        /// <summary>
        /// Calculate the nodal sensitivity numbers
        /// </summary>
        public void CalNdlSenNums()
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
            var result = Utils.KDTreeMultiSearch(nds, tree, FilterRadius, 1024);

            Parallel.For(0, nds.Length, i =>
            {
                var sum = 0.0;
                foreach (var item in result[i])
                {
                    var weight = FilterRadius - nds[i].DistanceTo(Elements[item].Center);
                    NdlSenNum[i] += weight * ElemSenNum[item];
                    sum += weight;
                }

                NdlSenNum[i] /= sum;
            });
        }
        public void SortVerts()
        {
            // Sort the vertices of each element
            // according to the order of the first element
            if (UnitiseSensitivityNumber)
            {
                double min = NdlSenNum.Min();
                double max = NdlSenNum.Max();
                if (ReverseValues)
                {
                    Parallel.For(0, NdlSenNum.Length, i =>
                    {
                        NdlSenNum[i] = Math.Abs(NdlSenNum[i] - max) / (max - min);
                    });
                }
                else
                {
                    Parallel.For(0, NdlSenNum.Length, i =>
                    {
                        NdlSenNum[i] = (NdlSenNum[i] - min) / (max - min);
                    });
                }
            }

            // Get the correct vertex order
            var idx = Elements[0].SortingVertices();

            var vert_0 = NodeList[Elements[0].NdlID[idx[0]]];
            var vert_6 = NodeList[Elements[0].NdlID[idx[6]]];

            Size = new Vector(Math.Abs(vert_6.X-vert_0.X), Math.Abs(vert_6.Y - vert_0.Y), Math.Abs(vert_6.Z-vert_0.Z));

            // Unify all the vertex order.
            //
            // The node IDs are permuted alongside the corners, so that afterwards
            // NdlID[j] is the node sitting at Vertices[j]. Everything downstream -
            // AdjustSenNum above all - reads the field as NdlSenNum[NdlID[j]] and
            // is then correct by construction.
            //
            // This used to be done the other way round: NdlID was left alone and
            // the global nodal field was permuted instead, one element at a time,
            // as NdlSenNum[NdlID[j]] = copy[NdlID[idx[j]]]. That cannot work,
            // because nodes are shared. A node is corner j of one element and
            // corner j' of the next, so each of the (up to eight) elements
            // touching it wrote a different neighbouring corner's value into the
            // same slot and the last one won. Measured on the 24,000-element
            // cantilever: 99.8% of nodes were written from more than one source,
            // 73.3% of element corners ended up holding the wrong node's value,
            // and the error reached 0.0038 on a field whose entire range was
            // 0.0081. The interpolated crossings then landed almost anywhere
            // along their edges, which is what put spikes all over the extracted
            // boundary.
            for (int i = 0; i < Elements.Count; i++)
            {
                Vector[] verts = new Vector[8];
                int[] nds = new int[8];
                // Update the order according to the correct order
                for (int j = 0; j < 8; j++)
                {
                    nds[j] = Elements[i].NdlID[idx[j]];
                    verts[j] = NodeList[nds[j]];
                }

                Elements[i].Vertices = verts;
                Elements[i].MinVert = verts[0];
                Elements[i].SetNdlID(nds);
            }

            AdjustSenNum(1.0);
        }

        private void AdjustSenNum(double isovalue)
        {
            for (int i = 0; i < Elements.Count; i++)
            {
                if (Elements[i].isSolid)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        NdlSenNum[Elements[i].NdlID[j]] = isovalue * 1.1;
                    }
                }
                if (Elements[i].isVoid)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        NdlSenNum[Elements[i].NdlID[j]] = 0;
                    }
                }
            }
            for (int i = 0; i < Elements.Count; i++)
            {
                var upd_ndlSen = new double[8];
                for (int j = 0; j < 8; j++)
                {
                    upd_ndlSen[j] = NdlSenNum[Elements[i].NdlID[j]];
                }

                Elements[i].SetNdlSenNum(upd_ndlSen);
            }
        }

        /// <summary>
        /// Set the parameters for the keeping volume method
        /// </summary>
        public void SetParameters(double volumeFraction, 
            double tolerance, double rmin,
            int maximumIteration, bool interpolation = true, 
            bool keepVolume = false, bool unitiseSensitivityNumber = true)
        {
            VolumeFraction = volumeFraction;
            Tolerance = tolerance;
            FilterRadius = rmin;
            MaximumIteration = maximumIteration;
            Interpolation = interpolation;
            KeepVolume = keepVolume;
            UnitiseSensitivityNumber = unitiseSensitivityNumber;
        }

        public double GetVolume()
        {
            double[] vols = new double[Elements.Count];
            Parallel.For(0, Elements.Count, i =>
            {
                vols[i] = Elements[i].ToMesh().GetVolume();
            });
            return vols.Sum();
        }

        public Mesh[] Extract(double isovalue)
        {
            Mesh[] meshes = new Mesh[Elements.Count];

            for (int i = 0; i < Elements.Count; i++)
            {
                int flag = ComputeFlag(i, Elements[i].ndlSen, isovalue);
;
                if (flag == 255)
                {
                    meshes[i] = Elements[i].ToMesh();
                }
                else if (flag != 0)
                    meshes[i] = IsoSenMdl_Hex(Elements[i], flag, isovalue);
                else
                    meshes[i] = null;
            }
            return meshes;
        }

        public Mesh[] ExtractAllCases()
        {
            Mesh[] meshes = new Mesh[256];

            for (int i = 0; i < 256; i++)
            {
                int flag = i;
                if (flag == 255)
                {
                    meshes[i] = Elements[0].ToMesh();
                }
                else if (flag != 0)
                    meshes[i] = IsoSenMdl_Hex(Elements[0], flag, 0.5);
                else
                    meshes[i] = null;

            }
            return meshes;
        }
        public Mesh IsoSenMdl_Hex(Hexahedron elem, int flag, double isovalue)
        {
            var values = elem.ndlSen;
            // applying the proposed lookup tables
            var mesh = ApplyLookUpTable(elem, flag);

            // To find edges which intersect with the boundary
            int EdgeFlag = Table.EdgeFlags_Hex[flag];
            // This voxel is in the boundary.
            if (EdgeFlag == 0) return null;

            if (!Interpolation)
                return mesh;

            List<Vector> pts = new List<Vector>();
            Vector[] EdgeVertex = new Vector[12];
            for (int i = 0; i < 12; i++)
            {
                if ((EdgeFlag & (1 << i)) != 0)
                {
                    var Offset = GetOffset(values[EdgeConnection[i, 0]], values[EdgeConnection[i, 1]], isovalue, Interpolation);
                    EdgeVertex[i] = new Vector(
                        (Vertices[EdgeConnection[i, 0], 0] + Offset * EdgeDirection[i, 0]) * Size.X + elem.MinVert.X,
                        (Vertices[EdgeConnection[i, 0], 1] + Offset * EdgeDirection[i, 1]) * Size.Y + elem.MinVert.Y,
                        (Vertices[EdgeConnection[i, 0], 2] + Offset * EdgeDirection[i, 2]) * Size.Z + elem.MinVert.Z);
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
        public Mesh[] ExtractIsoSensitivityModel(double isovalue)
        {
            Mesh[] meshes = new Mesh[Elements.Count];
            if (KeepVolume)
            {
                int iter = 0;
                var lowest = 0.0;
                var highest = 1.0;

                var cur_vol = 0.0;
                var ini_vol = GetVolume();
                var tar_vol = VolumeFraction;

                while (Math.Abs(cur_vol - tar_vol) > Tolerance && iter < MaximumIteration)
                {
                    isovalue = (highest + lowest) * 0.5;
                    AdjustSenNum(isovalue);
                    meshes = Extract(isovalue);
                    cur_vol = Mesh.GetVolumeFromMeshes(meshes) / ini_vol;

                    if (cur_vol - tar_vol > 0.0) lowest = isovalue;
                    else highest = isovalue;
                    iter++;
                }
                Console.WriteLine("Volume is " + (cur_vol * ini_vol).ToString());
            }
            else
            {
                AdjustSenNum(isovalue);
                meshes = Extract(isovalue);
                var vol = Mesh.GetVolumeFromMeshes(meshes);
                Console.WriteLine("Volume is " + vol.ToString());
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
                        Table.VertTable_Hex[flag][3 * v] * Size.X + elem.MinVert.X, 
                        Table.VertTable_Hex[flag][3 * v + 1] * Size.Y + elem.MinVert.Y, 
                        Table.VertTable_Hex[flag][3 * v + 2] * Size.Z + elem.MinVert.Z));
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
