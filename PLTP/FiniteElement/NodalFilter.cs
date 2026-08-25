using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KDTree;

namespace PLTP
{
    /// <summary>
    /// The element-to-node filter, held apart from the field it filters.
    ///
    /// Which elements reach a node, and how strongly, depends on the mesh and the
    /// filter radius and on nothing else - not on the design, so not on the step.
    /// It was being rebuilt for every surface: 16.5 s of the 21 s a surface took
    /// on the million-tetrahedron chair, 13.9 s of it in the radius search, once
    /// per step for as many steps as the optimization runs.
    ///
    /// Built once and applied per step it costs a fraction of a second, because
    /// applying it is a sparse matrix-vector product over the neighbour lists the
    /// search already found.
    ///
    /// Only the neighbour lists are kept. The weights are recomputed on each
    /// application from a flat table of the element centres - the same arithmetic
    /// in the same order, so the same bits - which is 24 MB against 1.5 GB. At
    /// rmin 30 the chair has 916 neighbours per node and 185 million entries in
    /// all, and keeping their weights measured 0.14 s an application against
    /// 0.3 s: not worth 1.5 GB on a run that already peaks near 10.
    ///
    /// Immutable once constructed, so one filter serves however many extractions
    /// are in flight.
    /// </summary>
    public sealed class NodalFilter
    {
        // CSR over the nodes: node i owns entries [start[i], start[i + 1]).
        readonly int[] start;
        readonly int[] cols;    // element ID
        readonly double[] rowSum;

        // Coordinates flat, xyz per entry, so the inner loop reads numbers rather
        // than chasing a Vector reference to its backing array.
        readonly double[] nodeXyz;
        readonly double[] centreXyz;

        /// <summary>The radius this was built for. Reuse is only valid at the same one.</summary>
        public double Radius { get; }
        public int NodeCount => start.Length - 1;
        public long Entries => cols.LongLength;

        public NodalFilter(List<Vector> nodes, List<Tetrahedron> elements, double radius)
        {
            Radius = radius;

            centreXyz = new double[3 * elements.Count];
            var tree = new KDTree<int>(3);
            for (int i = 0; i < elements.Count; i++)
            {
                var c = elements[i].Center;
                centreXyz[3 * i] = c.X;
                centreXyz[3 * i + 1] = c.Y;
                centreXyz[3 * i + 2] = c.Z;
                tree.AddPoint(new double[3] { c.X, c.Y, c.Z }, i);
            }

            var nds = nodes.ToArray();
            nodeXyz = new double[3 * nds.Length];
            for (int i = 0; i < nds.Length; i++)
            {
                nodeXyz[3 * i] = nds[i].X;
                nodeXyz[3 * i + 1] = nds[i].Y;
                nodeXyz[3 * i + 2] = nds[i].Z;
            }

            var found = Utils.KDTreeMultiSearch(nds, tree, radius, 1024);

            start = new int[nds.Length + 1];
            for (int i = 0; i < nds.Length; i++) start[i + 1] = start[i] + found[i].Count;

            cols = new int[start[nds.Length]];
            rowSum = new double[nds.Length];

            Parallel.For(0, nds.Length, i =>
            {
                var list = found[i];
                var at = start[i];
                var sum = 0.0;
                for (int k = 0; k < list.Count; k++)
                {
                    var e = list[k];
                    cols[at + k] = e;
                    sum += radius - Distance(i, e);
                }
                rowSum[i] = sum;

                // Dropped as it is copied. The search hands back its own copy of
                // the same neighbour lists - 185 million entries on the chair, in
                // a List per node with growth slack on top - and holding both
                // halves at once is the high-water mark of the whole run.
                found[i] = null;
            });
        }

        /// <summary>
        /// Node to element centre. Written out rather than called through
        /// <c>Vector.DistanceTo</c>, which builds a difference vector on the heap
        /// per call - 185 million of them per application here. The operations and
        /// their order are the same, so the result is bit for bit the same.
        /// </summary>
        double Distance(int node, int element)
        {
            var dx = nodeXyz[3 * node] - centreXyz[3 * element];
            var dy = nodeXyz[3 * node + 1] - centreXyz[3 * element + 1];
            var dz = nodeXyz[3 * node + 2] - centreXyz[3 * element + 2];
            return System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Filter an element field onto the nodes, into <paramref name="ndlSen"/>.
        ///
        /// A node no element reaches divides by a zero row sum and comes out NaN,
        /// which is what the search-every-time version did; the filter radius has
        /// to cover the mesh either way.
        /// </summary>
        public void Apply(IReadOnlyList<double> elemSen, double[] ndlSen)
        {
            if (ndlSen.Length != NodeCount)
                throw new ArgumentException(
                    $"filter is for {NodeCount} nodes, field has {ndlSen.Length}", nameof(ndlSen));

            var radius = Radius;
            Parallel.For(0, NodeCount, i =>
            {
                var acc = 0.0;
                var end = start[i + 1];
                for (int k = start[i]; k < end; k++)
                {
                    var e = cols[k];
                    acc += (radius - Distance(i, e)) * elemSen[e];
                }
                ndlSen[i] = acc / rowSum[i];
            });
        }
    }
}
