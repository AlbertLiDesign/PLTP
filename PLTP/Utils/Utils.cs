using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KDTree;
using System.Collections.Concurrent;

namespace PLTP
{
    public static class Utils
    {
        public static double SignedVolumeOfTriangle(Vector p1, Vector p2, Vector p3)
        {
            var v321 = p3.X * p2.Y * p1.Z;
            var v231 = p2.X * p3.Y * p1.Z;
            var v312 = p3.X * p1.Y * p2.Z;
            var v132 = p1.X * p3.Y * p2.Z;
            var v213 = p2.X * p1.Y * p3.Z;
            var v123 = p1.X * p2.Y * p3.Z;
            return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
        }
        public static List<int>[] KDTreeMultiSearch(Vector[] pts, KDTree<int> tree, double radius, int maxReturned)
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
    }
}
