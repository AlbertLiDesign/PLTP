using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KDTree;

namespace PLTP
{
    public class TetraModel
    {
        public List<Vector> NodeList = new List<Vector>();
        public List<Tetrahedron> Elements = new List<Tetrahedron>();
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

        #region Constructors
        public TetraModel() { }

        /// <summary>
        /// Post-processing method for Tetrahedron model without keeping volume
        /// </summary>
        public TetraModel(List<Vector> nodeList, List<Tetrahedron> elements)
        {
            NodeList = nodeList;
            Elements = elements;
            Cases = new int[elements.Count];
        }
        public TetraModel(List<Vector> nodeList, List<Tetrahedron> elements, List<double> senNum, bool elemSen = true)
        {
            NodeList = nodeList;
            Elements = elements;
            if (elemSen) ElemSenNum = senNum;
            else NdlSenNum = senNum.ToArray();

            Cases = new int[elements.Count];
        }

        public TetraModel(List<Vector> nodeList, List<Tetrahedron> elements, List<double> elemSenNum, List<int> NonDesign)
        {
            NodeList = nodeList;
            Elements = elements;
            ElemSenNum = elemSenNum;

            for (int i = 0; i < NonDesign.Count; i++)
            {
                elements[NonDesign[i]].SetNonDesign(true);
            }

            Cases = new int[elements.Count];
        }
        #endregion

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

            for (int i = 0; i < Elements.Count; i++)
            {
                var upd_ndlSen = new double[4];
                upd_ndlSen[0] = NdlSenNum[Elements[i].NdlID[0]];
                upd_ndlSen[1] = NdlSenNum[Elements[i].NdlID[1]];
                upd_ndlSen[2] = NdlSenNum[Elements[i].NdlID[2]];
                upd_ndlSen[3] = NdlSenNum[Elements[i].NdlID[3]];
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
                if (Elements[i].isNonDesign)
                {
                    // output the original hexahedron
                    meshes[i] = Elements[i].ToMesh();
                }
                else
                {
                    meshes[i] = IsoSenMdl_Tetra(Elements[i], flag, isovalue);
                }
            }
            return meshes;
        }

        public Mesh[] ExtractAllCases()
        {
            Mesh[] meshes = new Mesh[256];

            for (int i = 0; i < 16; i++)
            {
                int flag = i;
                meshes[i] = IsoSenMdl_Tetra(Elements[0], flag, 0.5);
            }
            return meshes;
        }
        public Mesh IsoSenMdl_Tetra(Tetrahedron elem, int flag, double isovalue)
        {
            var vertices = new List<Vector>();
            var faces = new List<Face>();
            var values = elem.ndlSen;
            var pts = elem.vertices;
            Vector pt0, pt1, pt2, pt3, pt4, pt5;
            switch (flag)
            {
                case 0:
                    break;
                case 1:
                    pt0 = VertexInterp(isovalue, pts[0], pts[1], values[0], values[1]);
                    pt1 = VertexInterp(isovalue, pts[0], pts[2], values[0], values[2]);
                    pt2 = VertexInterp(isovalue, pts[0], pts[3], values[0], values[3]);
                    vertices.Add(pts[0]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    faces.Add(new Face(0, 2, 1));
                    faces.Add(new Face(0, 1, 3));
                    faces.Add(new Face(0, 3, 2));
                    faces.Add(new Face(1, 2, 3));
                    break;
                case 2:
                    pt0 = VertexInterp(isovalue, pts[1], pts[0], values[1], values[0]);
                    pt1 = VertexInterp(isovalue, pts[1], pts[3], values[1], values[3]);
                    pt2 = VertexInterp(isovalue, pts[1], pts[2], values[1], values[2]);
                    vertices.Add(pts[1]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    faces.Add(new Face(0, 3, 2));
                    faces.Add(new Face(2, 3, 1));
                    faces.Add(new Face(0, 2, 1));
                    faces.Add(new Face(0, 1, 3));
                    break;
                case 3:
                    pt0 = VertexInterp(isovalue, pts[0], pts[3], values[0], values[3]);
                    pt1 = VertexInterp(isovalue, pts[0], pts[2], values[0], values[2]);
                    pt2 = VertexInterp(isovalue, pts[1], pts[3], values[1], values[3]);
                    pt3 = VertexInterp(isovalue, pts[1], pts[2], values[1], values[2]);
                    vertices.Add(pts[0]);
                    vertices.Add(pts[1]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    vertices.Add(pt3);
                    faces.Add(new Face(0, 1, 4, 2));
                    faces.Add(new Face(0, 1, 5, 3));
                    faces.Add(new Face(3, 5, 4, 2));
                    faces.Add(new Face(1, 4, 5));
                    faces.Add(new Face(0, 3, 2));
                    break;
                case 4:
                    pt0 = VertexInterp(isovalue, pts[2], pts[0], values[2], values[0]);
                    pt1 = VertexInterp(isovalue, pts[2], pts[1], values[2], values[1]);
                    pt2 = VertexInterp(isovalue, pts[2], pts[3], values[2], values[3]);
                    vertices.Add(pts[2]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    faces.Add(new Face(0, 2, 1));
                    faces.Add(new Face(2, 3, 1));
                    faces.Add(new Face(3, 0, 1));
                    faces.Add(new Face(2, 3, 0));
                    break;
                case 5:
                    pt0 = VertexInterp(isovalue, pts[0], pts[1], values[0], values[1]);
                    pt1 = VertexInterp(isovalue, pts[2], pts[3], values[2], values[3]);
                    pt2 = VertexInterp(isovalue, pts[0], pts[3], values[0], values[3]);
                    pt3 = VertexInterp(isovalue, pts[1], pts[2], values[1], values[2]);
                    vertices.Add(pts[0]);
                    vertices.Add(pts[2]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    vertices.Add(pt3);
                    faces.Add(new Face(0, 1, 5, 2));
                    faces.Add(new Face(1, 0, 4, 3));
                    faces.Add(new Face(0, 2, 4));
                    faces.Add(new Face(1, 3, 5));
                    faces.Add(new Face(2, 5, 3, 4));
                    break;
                case 6:
                    pt0 = VertexInterp(isovalue, pts[0], pts[1], values[0], values[1]);
                    pt1 = VertexInterp(isovalue, pts[1], pts[3], values[1], values[3]);
                    pt2 = VertexInterp(isovalue, pts[2], pts[3], values[2], values[3]);
                    pt3 = VertexInterp(isovalue, pts[0], pts[2], values[0], values[2]);
                    vertices.Add(pts[1]);
                    vertices.Add(pts[2]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    vertices.Add(pt3);
                    faces.Add(new Face(0, 1, 4, 3));
                    faces.Add(new Face(2, 5, 1, 0));
                    faces.Add(new Face(2, 3, 4, 5));
                    faces.Add(new Face(2, 0, 3));
                    faces.Add(new Face(4, 1, 5));
                    break;
                case 7:
                    pt0 = VertexInterp(isovalue, pts[3], pts[0], values[3], values[0]);
                    pt1 = VertexInterp(isovalue, pts[3], pts[2], values[3], values[2]);
                    pt2 = VertexInterp(isovalue, pts[3], pts[1], values[3], values[1]);
                    vertices.Add(pts[0]);
                    vertices.Add(pts[1]);
                    vertices.Add(pts[2]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    faces.Add(new Face(0, 2, 1));
                    faces.Add(new Face(5, 4, 3));
                    faces.Add(new Face(0, 1, 5, 3));
                    faces.Add(new Face(0, 2, 4, 3));
                    faces.Add(new Face(1, 2, 4, 5));
                    break;
                case 8:
                    pt0 = VertexInterp(isovalue, pts[3], pts[0], values[3], values[0]);
                    pt1 = VertexInterp(isovalue, pts[3], pts[2], values[3], values[2]);
                    pt2 = VertexInterp(isovalue, pts[3], pts[1], values[3], values[1]);
                    vertices.Add(pts[3]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    faces.Add(new Face(1, 3, 0));
                    faces.Add(new Face(1, 0, 2));
                    faces.Add(new Face(3, 2, 0));
                    faces.Add(new Face(1, 2, 3));
                    break;
                case 9:
                    pt0 = VertexInterp(isovalue, pts[0], pts[1], values[0], values[1]);
                    pt1 = VertexInterp(isovalue, pts[1], pts[3], values[1], values[3]);
                    pt2 = VertexInterp(isovalue, pts[2], pts[3], values[2], values[3]);
                    pt3 = VertexInterp(isovalue, pts[0], pts[2], values[0], values[2]);
                    vertices.Add(pts[0]);
                    vertices.Add(pts[3]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    vertices.Add(pt3);
                    faces.Add(new Face(0, 2, 3, 1));
                    faces.Add(new Face(0, 1, 4, 5));
                    faces.Add(new Face(2, 5, 4, 3));
                    faces.Add(new Face(0, 5, 2));
                    faces.Add(new Face(1, 3, 4));
                    break;
                case 10:
                    pt0 = VertexInterp(isovalue, pts[0], pts[1], values[0], values[1]);
                    pt1 = VertexInterp(isovalue, pts[2], pts[3], values[2], values[3]);
                    pt2 = VertexInterp(isovalue, pts[0], pts[3], values[0], values[3]);
                    pt3 = VertexInterp(isovalue, pts[1], pts[2], values[1], values[2]);
                    vertices.Add(pts[0]);
                    vertices.Add(pts[3]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    vertices.Add(pt3);
                    faces.Add(new Face(0, 1, 4, 2));
                    faces.Add(new Face(0, 5, 3, 1));
                    faces.Add(new Face(1, 3, 4));
                    faces.Add(new Face(2, 5, 0));
                    faces.Add(new Face(2, 4, 3, 5));
                    break;
                case 11:
                    pt0 = VertexInterp(isovalue, pts[2], pts[0], values[2], values[0]);
                    pt1 = VertexInterp(isovalue, pts[2], pts[1], values[2], values[1]);
                    pt2 = VertexInterp(isovalue, pts[2], pts[3], values[2], values[3]);
                    vertices.Add(pts[0]);
                    vertices.Add(pts[1]);
                    vertices.Add(pts[3]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    faces.Add(new Face(0, 1, 2));
                    faces.Add(new Face(3, 5, 4));
                    faces.Add(new Face(1, 4, 5, 2));
                    faces.Add(new Face(2, 5, 3, 0));
                    faces.Add(new Face(0, 3, 4, 1));
                    break;
                case 12:
                    pt0 = VertexInterp(isovalue, pts[0], pts[3], values[0], values[3]);
                    pt1 = VertexInterp(isovalue, pts[0], pts[2], values[0], values[2]);
                    pt2 = VertexInterp(isovalue, pts[1], pts[3], values[1], values[3]);
                    pt3 = VertexInterp(isovalue, pts[1], pts[2], values[1], values[2]);
                    vertices.Add(pts[2]);
                    vertices.Add(pts[3]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    vertices.Add(pt3);
                    faces.Add(new Face(3, 5, 4, 2));
                    faces.Add(new Face(4, 5, 0, 1));
                    faces.Add(new Face(2, 1, 0, 3));
                    faces.Add(new Face(2, 4, 1));
                    faces.Add(new Face(3, 0, 5));
                    break;
                case 13:
                    pt0 = VertexInterp(isovalue, pts[1], pts[0], values[1], values[0]);
                    pt1 = VertexInterp(isovalue, pts[1], pts[3], values[1], values[3]);
                    pt2 = VertexInterp(isovalue, pts[1], pts[2], values[1], values[2]);
                    vertices.Add(pts[0]);
                    vertices.Add(pts[2]);
                    vertices.Add(pts[3]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    faces.Add(new Face(0, 2, 1));
                    faces.Add(new Face(3, 5, 4));
                    faces.Add(new Face(0, 3, 4, 2));
                    faces.Add(new Face(4, 5, 1, 2));
                    faces.Add(new Face(0, 1, 5, 3));
                    break;
                case 14:
                    pt0 = VertexInterp(isovalue, pts[0], pts[1], values[0], values[1]);
                    pt1 = VertexInterp(isovalue, pts[0], pts[2], values[0], values[2]);
                    pt2 = VertexInterp(isovalue, pts[0], pts[3], values[0], values[3]);
                    vertices.Add(pts[1]);
                    vertices.Add(pts[2]);
                    vertices.Add(pts[3]);
                    vertices.Add(pt0);
                    vertices.Add(pt1);
                    vertices.Add(pt2);
                    faces.Add(new Face(0, 1, 2));
                    faces.Add(new Face(5, 4, 3));
                    faces.Add(new Face(3, 0, 2, 5));
                    faces.Add(new Face(5, 2, 1, 4));
                    faces.Add(new Face(3, 4, 1, 0));
                    break;
                case 15:
                    vertices.Add(pts[0]);
                    vertices.Add(pts[1]);
                    vertices.Add(pts[2]);
                    vertices.Add(pts[3]);
                    faces.Add(new Face(0, 2, 1));
                    faces.Add(new Face(0, 1, 3));
                    faces.Add(new Face(0, 3, 2));
                    faces.Add(new Face(1, 2, 3));
                    break;
                default:
                    break;
            }

            return new Mesh(vertices.ToArray(),faces.ToArray());
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
                var tar_vol = VolumeFraction;
                var ini_vol = GetVolume();

                while (Math.Abs(cur_vol - tar_vol) > Tolerance && iter < MaximumIteration)
                {
                    isovalue = (highest + lowest) * 0.5;
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
                meshes = Extract(isovalue);
                var vol = Mesh.GetVolumeFromMeshes(meshes);
                Console.WriteLine("Volume is " + vol.ToString());
            }

            return meshes;
        }

        #region Private Methods
        private int ComputeFlag(int id, double[] values, double isovalue)
        {
            int flag = 0;
            for (int i = 0; i < 4; i++)
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

        private static Vector VertexInterp(double isovalue, Vector p1, Vector p2, double valp1, double valp2)
        {
            double mu;
            Vector p = Vector.Origin(3);

            mu = (isovalue - valp1) / (valp2 - valp1);
            p[0] = p1.X + mu * (p2.X - p1.X);
            p[1] = p1.Y + mu * (p2.Y - p1.Y);
            p[2] = p1.Z + mu * (p2.Z - p1.Z);
            return (p);
        }
        #endregion
    }
}
