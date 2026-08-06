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
        /// The filter to use, when one has already been built for this mesh and
        /// radius. Left null, <see cref="CalNdlSenNums"/> builds its own and
        /// throws it away - fine for a single extraction, but the search costs
        /// seconds on a large mesh and repeats it for every step of an
        /// optimization. See <see cref="NodalFilter"/>.
        /// </summary>
        public NodalFilter Filter;

        /// <summary>
        /// Calculate the nodal sensitivity numbers
        /// </summary>
        public void CalNdlSenNums()
        {
            NdlSenNum = new double[NodeList.Count];

            var filter = Filter;
            if (filter == null || filter.NodeCount != NodeList.Count || filter.Radius != FilterRadius)
                filter = new NodalFilter(NodeList, Elements, FilterRadius);
            filter.Apply(ElemSenNum, NdlSenNum);

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
                if (Elements[i].isVoid)
                {
                    // Pinned empty for the whole optimization, so it contributes
                    // nothing whatever the interpolated field says here. Emitting
                    // nothing cannot leave a seam, so this stays a special case.
                    meshes[i] = new Mesh(new Vector[0], new Face[0]);
                    continue;
                }

                if (Elements[i].isNonDesign)
                {
                    // The whole tetrahedron, from its corners.
                    //
                    // This leaves a seam. A neighbouring design element builds its
                    // patch from points interpolated along the edges, which never
                    // coincide with the corners, so the two do not weld and the
                    // fixed regions come out as surface sitting against the body
                    // rather than joined to it - 337 pieces on the 100k model
                    // against one with the fixed domains switched off.
                    //
                    // Closing the seam by raising these elements' nodes above the
                    // isovalue, so they come through the same interpolation, does
                    // not work: the nodes are shared, so the raise reaches into
                    // every neighbouring element as well, and since the bisection
                    // moves the isovalue while the pinning follows it, no isovalue
                    // removes that material. Measured: the volume floors at 27,286
                    // against a target of 24,000, with the entire genuine design
                    // region emptied to try to reach it. Whichever value is pinned,
                    // 1 or a hair above the isovalue, gives the same 27,286.
                    //
                    // So the seam stays for now, and the volume constraint - which
                    // counts these elements - is the thing being honoured.
                    meshes[i] = Elements[i].ToMesh();
                    continue;
                }

                int flag = ComputeFlag(i, Elements[i].ndlSen, isovalue);
                meshes[i] = IsoSenMdl_Tetra(Elements[i], flag, isovalue);
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
                    faces.Add(new Face(0, 3, 5, 1));
                    faces.Add(new Face(4, 5, 3, 2));
                    faces.Add(new Face(1, 5, 4));
                    faces.Add(new Face(0, 2, 3));
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
                    faces.Add(new Face(1, 2, 3));
                    faces.Add(new Face(3, 0, 1));
                    faces.Add(new Face(2, 0, 3));
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
                    faces.Add(new Face(0, 3, 4, 2));
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
                    vertices.Add(pts[1]);
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

                // Stop when the bracket stops meaning anything, as well as on the
                // volume tolerance. Volume is monotone in the isovalue, so once
                // the bracket is narrower than this the target is simply not
                // attainable - the curve jumps over it, or saturates short of it -
                // and every further halving is one more full extraction buying a
                // millionth of an element's worth of movement.
                //
                // It happens whenever the design is still much denser than the
                // target, which is every early step: measured on the
                // million-tetrahedron chair at step 2, the loop ran all 50
                // iterations for 51 s where 20 would have said the same thing.
                const double bracketFloor = 1e-6;

                while (Math.Abs(cur_vol - tar_vol) > Tolerance
                       && highest - lowest > bracketFloor
                       && iter < MaximumIteration)
                {
                    isovalue = (highest + lowest) * 0.5;
                    meshes = Extract(isovalue);
                    var v = Mesh.GetVolumeFromMeshes(meshes);
                    cur_vol = v / ini_vol;

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
        /// <summary>
        /// Mark elements the optimization was not allowed to remove. The four-
        /// argument constructor does the same thing, but only for a model built
        /// from element sensitivities; this works alongside SetNdlSenNums too.
        /// </summary>
        public void SetNonDesign(IEnumerable<int> elementIDs)
        {
            foreach (var id in elementIDs) Elements[id].SetNonDesign(true);
        }

        /// <summary>
        /// Write the fixed domains into the nodal field, so the iso-surface can
        /// treat every element alike.
        ///
        /// A node of a non-design element goes to 1, which clears any isovalue the
        /// volume bisection can arrive at - it searches strictly inside (0, 1) and
        /// ComputeFlag tests with a strict greater-than - so all four of that
        /// element's corners are above the threshold and the element comes out
        /// whole. Void goes to 0 for the mirror reason. Where the two meet,
        /// material wins.
        ///
        /// The value has to sit just past the isovalue, not at 1. Nodes are shared,
        /// so a design element touching a non-design one sees those corners too,
        /// and where its cut lands depends on how far past the threshold they are.
        /// Put them at 1 and the interpolated cut falls a long way outside the
        /// fixed region - the whole boundary inflates by an element, which on this
        /// model added a layer under the full width of the deck and pushed the
        /// volume 14% past the target with the bisection already saturated. Put
        /// them a hair above and the cut lands on the boundary itself.
        ///
        /// The isovalue moves during the volume search, so this is applied per
        /// trial, always from the field as originally computed.
        /// </summary>
        public void ApplyFixedDomains(double isovalue)
        {
            if (NdlSenNum == null || NdlSenNum.Length == 0) return;

            if (ndlBase == null) ndlBase = (double[])NdlSenNum.Clone();
            Array.Copy(ndlBase, NdlSenNum, NdlSenNum.Length);

            // Far enough off the threshold that the interpolation is not dividing
            // by a near-zero difference, close enough to be geometrically nothing.
            const double margin = 1e-6;
            double above = isovalue + margin;
            double below = isovalue - margin;

            foreach (var e in Elements)
                if (e.isVoid)
                    for (int k = 0; k < e.NdlID.Length; k++)
                        NdlSenNum[e.NdlID[k]] = System.Math.Min(NdlSenNum[e.NdlID[k]], below);

            foreach (var e in Elements)
                if (e.isNonDesign)
                    for (int k = 0; k < e.NdlID.Length; k++)
                        NdlSenNum[e.NdlID[k]] = System.Math.Max(NdlSenNum[e.NdlID[k]], above);

            Parallel.For(0, Elements.Count, i =>
            {
                var v = new double[4];
                for (int j = 0; j < 4; j++) v[j] = NdlSenNum[Elements[i].NdlID[j]];
                Elements[i].SetNdlSenNum(v);
            });
        }

        // The nodal field as computed, kept because ApplyFixedDomains overwrites
        // parts of it and runs again for every isovalue the bisection tries.
        private double[] ndlBase;

        /// <summary>
        /// Mark elements the optimization was not allowed to fill. They are
        /// emitted empty.
        /// </summary>
        public void SetVoid(IEnumerable<int> elementIDs)
        {
            foreach (var id in elementIDs) Elements[id].SetVoid(true);
        }

        /// <summary>
        /// Supply the element field that CalNdlSenNums will project onto the
        /// nodes. Use this rather than SetNdlSenNums when the caller has an
        /// element field: the projection here is a gather over everything within
        /// FilterRadius weighted by (rmin - distance), which is the filter the
        /// optimization itself ran with. Handing over a nodal field instead
        /// bypasses it, and the surface then carries whatever smoothing the caller
        /// happened to apply - typically a one-ring average, which is the element
        /// size rather than the filter radius.
        /// </summary>
        public void SetElemSenNums(List<double> elem_sen)
        {
            if (elem_sen.Count != Elements.Count)
                throw new ArgumentException(
                    $"Expected one value per element ({Elements.Count}), got {elem_sen.Count}.");
            ElemSenNum = new List<double>(elem_sen);
        }

        public void SetNdlSenNums(double[] ndl_sen)
        {
            NdlSenNum = (double[])ndl_sen.Clone();
            Parallel.For(0, Elements.Count, i =>
            {
                Elements[i].ndlSen = new double[4];
                for (int j = 0; j < 4; j++)
                {
                    Elements[i].ndlSen[j] = NdlSenNum[Elements[i].NdlID[j]];
                }
            });
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
