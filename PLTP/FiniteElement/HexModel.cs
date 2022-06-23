using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class HexModel
    {
        public Hexahedron[] Elements;
        private Vector voxelSize;

        private double initialVolume = 0.0;
        private double targetVolume = 0.0;
        public double isoValue = 0.0;
        public double tolerance = 0.01;

        public bool interpolation = true;
        public bool keepVolume = false;

        #region Parameters for keeping volume
        private double step = 0.01;
        private int maximumIteration = 20;
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
        public HexModel(Hexahedron[] elements, double[] nodalSensitivityNumbers, Vector voxelSize, double isoValue, double tolerance, bool interpolation = true)
        {
            Elements = elements;
            this.voxelSize = voxelSize;

            this.isoValue = isoValue;
            this.tolerance = tolerance;

            this.interpolation = interpolation;
            keepVolume = false;

            Cases = new int[elements.Length];
        }

        /// <summary>
        /// Post-processing method for hexahedron model while keeping volume
        /// </summary>
        public HexModel(Hexahedron[] elements, Vector voxelSize, double initialVolume, double targetVolume, double isoValue, double tolerance, bool interpolation = true)
        {
            Elements = elements;
            this.voxelSize = voxelSize;

            this.initialVolume = initialVolume;
            this.targetVolume = targetVolume;
            this.isoValue = isoValue;
            this.tolerance = tolerance;

            this.interpolation = interpolation;
            keepVolume = true;

            Cases = new int[elements.Length];
        }
        #endregion

        /// <summary>
        /// Set the parameters for the keeping volume method
        /// </summary>
        /// <param name="step"></param>
        /// <param name="maximumIteration"></param>
        public HexModel(double step, int maximumIteration)
        {
            this.step = step;
            this.maximumIteration = maximumIteration;
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

        #endregion
    }
}
