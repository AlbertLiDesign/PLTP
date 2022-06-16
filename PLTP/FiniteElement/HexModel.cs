using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class HexModel
    {
        public List<Hexahedron> elements = new List<Hexahedron>();
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
        public HexModel(List<Hexahedron> elements, double[] nodalSensitivityNumbers, Vector voxelSize, double isoValue, double tolerance, bool interpolation = true)
        {
            this.elements = elements;
            this.voxelSize = voxelSize;

            this.isoValue = isoValue;
            this.tolerance = tolerance;

            this.interpolation = interpolation;
            keepVolume = false;

            Cases = new int[elements.Count];
        }

        /// <summary>
        /// Post-processing method for hexahedron model while keeping volume
        /// </summary>
        public HexModel(List<Hexahedron> elements, Vector voxelSize, double initialVolume, double targetVolume, double isoValue, double tolerance, bool interpolation = true)
        {
            this.elements = elements;
            this.voxelSize = voxelSize;

            this.initialVolume = initialVolume;
            this.targetVolume = targetVolume;
            this.isoValue = isoValue;
            this.tolerance = tolerance;

            this.interpolation = interpolation;
            keepVolume = true;

            Cases = new int[elements.Count];
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

        public List<Mesh> Extract()
        {
            List<Mesh> meshes = new List<Mesh>();

            //// Each hexahedron has 8 vertices
            //Vector[] all_vertices = new Vector[elements.Count * 8];

            //// Get all vertices
            //for (int i = 0; i < elements.Count; i++)
            //{
            //    all_vertices[i * 8] = elements[i].vertices[0];
            //    all_vertices[i * 8 + 1] = elements[i].vertices[1];
            //    all_vertices[i * 8 + 2] = elements[i].vertices[2];
            //    all_vertices[i * 8 + 3] = elements[i].vertices[3];
            //    all_vertices[i * 8 + 4] = elements[i].vertices[4];
            //    all_vertices[i * 8 + 5] = elements[i].vertices[5];
            //    all_vertices[i * 8 + 6] = elements[i].vertices[6];
            //    all_vertices[i * 8 + 7] = elements[i].vertices[7];
            //}

            for (int i = 0; i < elements.Count; i++)
            {
                int flag = ComputeFlag(i, elements[i].ndlSen, isoValue);
                if (elements[i].isNonDesign)
                {
                    // output the original hexahedron
                }
                else
                {
                    if (flag == 255)
                    {
                        // output the original hexahedron
                    }



                }
            }



            return meshes;
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
    }
}
