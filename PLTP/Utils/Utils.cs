using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KDTree;
using System.Collections.Concurrent;
using System.Drawing;
using System.Reflection;

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
    public static class MeshWeld
    {
        public static Mesh Weld(Mesh mesh_0, double double_0)
        {
            int num;
            Box boundingBox = mesh_0.GetBoundingBox();
            List<Vector> vertices = new List<Vector>();
            List<Face> faces = new List<Face>();
            int[] numArray = new int[mesh_0.Vertices.Length];
            Class18 class2 = new Class18(double_0, boundingBox);
            int num2 = 0;
            for (num = 0; num < mesh_0.Vertices.Length; num++)
            {
                Vector pointf2 = mesh_0.Vertices[num];
                Class19 class4 = class2.Method_1(pointf2);
                if (class4 == null)
                {
                    vertices.Add(pointf2);
                    class2.Method_3(new Class19(num2, (double)pointf2.X, (double)pointf2.Y, (double)pointf2.Z));
                    numArray[num] = num2++;
                }
                else
                {
                    numArray[num] = class4.int_0;
                }
            }
            for (num = 0; num < mesh_0.Faces.Length; num++)
            {
                var face = mesh_0.Faces[num];
                if (face.Vert_ID.Length!=0)
                {
                    int num3 = numArray[face.Vert_ID[0]];
                    int num4 = numArray[face.Vert_ID[1]];
                    int num5 = numArray[face.Vert_ID[2]];
                    if (face.isTriagnle)
                    {
                        faces.Add (new Face(num3, num4, num5));
                    }
                    else
                    {
                        int num6 = numArray[face.Vert_ID[3]];
                        faces.Add(new Face(num3, num4, num5, num6));
                    }
                }
            }
            Mesh mesh = new Mesh(vertices.ToArray(), faces.ToArray());
            return mesh;
        }

        private class Class18
        {
            protected MeshWeld.Class20 class20_0;
            internal MeshWeld.Class20 class20_1;
            internal double double_0;
            protected double double_1;
            internal int int_0;

            public Class18(double double_2)
            {
                this.int_0 = 200;
                this.double_1 = 50.0;
                this.double_0 = double_2;
            }

            public Class18(double double_2, Box boundingBox_0)
            {
                this.int_0 = 200;
                this.double_1 = 50.0;
                this.double_0 = double_2;
                if (boundingBox_0 != null)
                {
                    this.class20_0 = new MeshWeld.Class20(boundingBox_0.Min.X, boundingBox_0.Max.X, boundingBox_0.Min.Y, boundingBox_0.Max.Y, boundingBox_0.Min.Z, boundingBox_0.Max.Z, this);
                }
            }

            internal MeshWeld.Class19 Method_0(Vector point3d_0)
            {
                double x = point3d_0.X;
                double y = point3d_0.Y;
                double z = point3d_0.Z;
                if (this.class20_0 == null)
                {
                    return null;
                }
                if ((this.class20_1 != null) && this.class20_1.Method_0(x, y, z))
                {
                    MeshWeld.Class19 class2 = this.class20_1.Method_2(x, y, z);
                    if (class2 == null)
                    {
                        return class2;
                    }
                }
                return this.class20_0.Method_2(x, y, z);
            }

            internal MeshWeld.Class19 Method_1(Vector point3f_0)
            {
                double x = point3f_0.X;
                double y = point3f_0.Y;
                double z = point3f_0.Z;
                if (this.class20_0 == null)
                {
                    return null;
                }
                if ((this.class20_1 != null) && this.class20_1.Method_0(x, y, z))
                {
                    MeshWeld.Class19 class2 = this.class20_1.Method_2(x, y, z);
                    if (class2 != null)
                    {
                        return class2;
                    }
                }
                return this.class20_0.Method_2(x, y, z);
            }

            internal MeshWeld.Class19 Method_2(Vector point3f_0, Color color_0)
            {
                double x = point3f_0.X;
                double y = point3f_0.Y;
                double z = point3f_0.Z;
                if (this.class20_0 == null)
                {
                    return null;
                }
                if ((this.class20_1 != null) && this.class20_1.Method_0(x, y, z))
                {
                    MeshWeld.Class19 class2 = this.class20_1.Method_2(x, y, z);
                    if (class2 != null)
                    {
                        return class2;
                    }
                }
                return this.class20_0.Method_2(x, y, z);
            }

            internal void Method_3(MeshWeld.Class19 class19_0)
            {
                double num = class19_0.double_0;
                double num2 = class19_0.double_1;
                double num3 = class19_0.double_2;
                if (this.class20_0 == null)
                {
                    double num4 = num - this.double_1;
                    double num5 = num + this.double_1;
                    double num6 = num2 - this.double_1;
                    double num7 = num2 + this.double_1;
                    double num8 = num3 - this.double_1;
                    double num9 = num3 + this.double_1;
                    this.class20_0 = new MeshWeld.Class20(num4, num5, num6, num7, num8, num9, this);
                    this.class20_0.Method_3(class19_0);
                }
                else if ((this.class20_1 != null) && this.class20_1.Method_0(num, num2, num3))
                {
                    this.class20_1.Method_3(class19_0);
                }
                else if (!this.class20_0.Method_0(num, num2, num3))
                {
                    while (!this.class20_0.Method_0(num, num2, num3))
                    {
                        double num10 = this.class20_0.double_1 - this.class20_0.double_0;
                        if ((num3 > this.class20_0.double_4) && ((num3 - this.class20_0.double_4) > this.double_0))
                        {
                            if (num3 > this.class20_0.double_5)
                            {
                                if ((num2 > this.class20_0.double_5) && ((num2 - this.class20_0.double_2) > this.double_0))
                                {
                                    if ((num > this.class20_0.double_0) && ((num - this.class20_0.double_0) > this.double_0))
                                    {
                                        MeshWeld.Class20 class9 = new MeshWeld.Class20(this.class20_0.double_0, this.class20_0.double_1 + num10, this.class20_0.double_2, this.class20_0.double_3 + num10, this.class20_0.double_4, this.class20_0.double_5 + num10, this)
                                        {
                                            class20_0 = this.class20_0
                                        };
                                        this.class20_0 = class9;
                                    }
                                    else
                                    {
                                        MeshWeld.Class20 class8 = new MeshWeld.Class20(this.class20_0.double_0 - num10, this.class20_0.double_1, this.class20_0.double_2, this.class20_0.double_3 + num10, this.class20_0.double_4, this.class20_0.double_5 + num10, this)
                                        {
                                            class20_1 = this.class20_0
                                        };
                                        this.class20_0 = class8;
                                    }
                                }
                                else if ((num > this.class20_0.double_5) && ((num - this.class20_0.double_0) > this.double_0))
                                {
                                    MeshWeld.Class20 class7 = new MeshWeld.Class20(this.class20_0.double_0, this.class20_0.double_1 + num10, this.class20_0.double_2 - num10, this.class20_0.double_3, this.class20_0.double_4, this.class20_0.double_5 + num10, this)
                                    {
                                        class20_2 = this.class20_0
                                    };
                                    this.class20_0 = class7;
                                }
                                else
                                {
                                    MeshWeld.Class20 class6 = new MeshWeld.Class20(this.class20_0.double_0 - num10, this.class20_0.double_1, this.class20_0.double_2 - num10, this.class20_0.double_3, this.class20_0.double_4, this.class20_0.double_5 + num10, this)
                                    {
                                        class20_3 = this.class20_0
                                    };
                                    this.class20_0 = class6;
                                }
                            }
                            else if ((num2 > this.class20_0.double_2) && ((num2 - this.class20_0.double_2) > this.double_0))
                            {
                                if (num2 > this.class20_0.double_3)
                                {
                                    if ((num > this.class20_0.double_0) && ((num - this.class20_0.double_0) > this.double_0))
                                    {
                                        MeshWeld.Class20 class13 = new MeshWeld.Class20(this.class20_0.double_0, this.class20_0.double_1 + num10, this.class20_0.double_2, this.class20_0.double_3 + num10, this.class20_0.double_4, this.class20_0.double_5 + num10, this)
                                        {
                                            class20_0 = this.class20_0
                                        };
                                        this.class20_0 = class13;
                                    }
                                    else
                                    {
                                        MeshWeld.Class20 class12 = new MeshWeld.Class20(this.class20_0.double_0 - num10, this.class20_0.double_1, this.class20_0.double_2, this.class20_0.double_3 + num10, this.class20_0.double_4, this.class20_0.double_5 + num10, this)
                                        {
                                            class20_1 = this.class20_0
                                        };
                                        this.class20_0 = class12;
                                    }
                                }
                                else if ((num > this.class20_0.double_0) && ((num - this.class20_0.double_0) > this.double_0))
                                {
                                    MeshWeld.Class20 class15 = new MeshWeld.Class20(this.class20_0.double_0, this.class20_0.double_1 + num10, this.class20_0.double_2, this.class20_0.double_3 + num10, this.class20_0.double_4, this.class20_0.double_5 + num10, this)
                                    {
                                        class20_0 = this.class20_0
                                    };
                                    this.class20_0 = class15;
                                }
                                else
                                {
                                    MeshWeld.Class20 class14 = new MeshWeld.Class20(this.class20_0.double_0 - num10, this.class20_0.double_1, this.class20_0.double_2, this.class20_0.double_3 + num10, this.class20_0.double_4, this.class20_0.double_5 + num10, this)
                                    {
                                        class20_1 = this.class20_0
                                    };
                                    this.class20_0 = class14;
                                }
                            }
                            else if ((num > this.class20_0.double_0) && ((num - this.class20_0.double_0) > this.double_0))
                            {
                                MeshWeld.Class20 class11 = new MeshWeld.Class20(this.class20_0.double_0, this.class20_0.double_1 + num10, this.class20_0.double_2 - num10, this.class20_0.double_3, this.class20_0.double_4, this.class20_0.double_5 + num10, this)
                                {
                                    class20_2 = this.class20_0
                                };
                                this.class20_0 = class11;
                            }
                            else
                            {
                                MeshWeld.Class20 class10 = new MeshWeld.Class20(this.class20_0.double_0 - num10, this.class20_0.double_1, this.class20_0.double_2 - num10, this.class20_0.double_3, this.class20_0.double_4, this.class20_0.double_5 + num10, this)
                                {
                                    class20_3 = this.class20_0
                                };
                                this.class20_0 = class10;
                            }
                        }
                        else if ((num2 > this.class20_0.double_2) && ((num2 - this.class20_0.double_2) > this.double_0))
                        {
                            if ((num > this.class20_0.double_0) && ((num3 - this.class20_0.double_0) > this.double_0))
                            {
                                MeshWeld.Class20 class5 = new MeshWeld.Class20(this.class20_0.double_0, this.class20_0.double_1 + num10, this.class20_0.double_2, this.class20_0.double_3 + num10, this.class20_0.double_4 - num10, this.class20_0.double_5, this)
                                {
                                    class20_4 = this.class20_0
                                };
                                this.class20_0 = class5;
                            }
                            else
                            {
                                MeshWeld.Class20 class4 = new MeshWeld.Class20(this.class20_0.double_0 - num10, this.class20_0.double_1, this.class20_0.double_2, this.class20_0.double_3 + num10, this.class20_0.double_4 - num10, this.class20_0.double_5, this)
                                {
                                    class20_5 = this.class20_0
                                };
                                this.class20_0 = class4;
                            }
                        }
                        else if ((num > this.class20_0.double_0) && ((num3 - this.class20_0.double_0) > this.double_0))
                        {
                            MeshWeld.Class20 class3 = new MeshWeld.Class20(this.class20_0.double_0, this.class20_0.double_1 + num10, this.class20_0.double_2 - num10, this.class20_0.double_3, this.class20_0.double_4 - num10, this.class20_0.double_5, this)
                            {
                                class20_6 = this.class20_0
                            };
                            this.class20_0 = class3;
                        }
                        else
                        {
                            MeshWeld.Class20 class2 = new MeshWeld.Class20(this.class20_0.double_0 - num10, this.class20_0.double_1, this.class20_0.double_2 - num10, this.class20_0.double_3, this.class20_0.double_4 - num10, this.class20_0.double_5, this)
                            {
                                class20_7 = this.class20_0
                            };
                            this.class20_0 = class2;
                        }
                    }
                    this.class20_0.bool_0 = true;
                    this.class20_0.Method_3(class19_0);
                }
                else
                {
                    this.class20_0.Method_3(class19_0);
                }
            }
        }

        private class Class19
        {
            internal double double_0;
            internal double double_1;
            internal double double_2;
            internal int int_0;

            internal Class19(int int_1, double double_3, double double_4, double double_5)
            {
                this.int_0 = int_1;
                this.double_0 = double_3;
                this.double_1 = double_4;
                this.double_2 = double_5;
            }
        }

        private class Class20
        {
            internal bool bool_0;
            private MeshWeld.Class18 class18_0;
            internal MeshWeld.Class20 class20_0;
            internal MeshWeld.Class20 class20_1;
            internal MeshWeld.Class20 class20_2;
            internal MeshWeld.Class20 class20_3;
            internal MeshWeld.Class20 class20_4;
            internal MeshWeld.Class20 class20_5;
            internal MeshWeld.Class20 class20_6;
            internal MeshWeld.Class20 class20_7;
            internal double double_0;
            internal double double_1;
            internal double double_2;
            internal double double_3;
            internal double double_4;
            internal double double_5;
            internal double double_6;
            internal double double_7;
            internal double double_8;
            protected List<MeshWeld.Class19> list_0 = new List<MeshWeld.Class19>();

            public Class20(double double_9, double double_10, double double_11, double double_12, double double_13, double double_14, MeshWeld.Class18 class18_1)
            {
                this.class18_0 = class18_1;
                this.class20_0 = null;
                this.class20_1 = null;
                this.class20_2 = null;
                this.class20_3 = null;
                this.class20_4 = null;
                this.class20_5 = null;
                this.class20_6 = null;
                this.class20_7 = null;
                this.double_0 = double_9;
                this.double_1 = double_10;
                this.double_2 = double_11;
                this.double_3 = double_12;
                this.double_4 = double_13;
                this.double_5 = double_14;
                this.double_6 = (double_9 + double_10) / 2.0;
                this.double_7 = (double_11 + double_12) / 2.0;
                this.double_8 = (double_13 + double_14) / 2.0;
            }

            public bool Method_0(double double_9, double double_10, double double_11)
            {
                if ((double_9 < this.double_0) && ((this.double_0 - double_9) > this.class18_0.double_0))
                {
                    return false;
                }
                if ((double_9 > this.double_1) && ((double_9 - this.double_1) > this.class18_0.double_0))
                {
                    return false;
                }
                if ((double_10 < this.double_2) && ((this.double_2 - double_10) > this.class18_0.double_0))
                {
                    return false;
                }
                if ((double_10 > this.double_3) && ((double_10 - this.double_3) > this.class18_0.double_0))
                {
                    return false;
                }
                if ((double_11 < this.double_4) && ((this.double_4 - double_11) > this.class18_0.double_0))
                {
                    return false;
                }
                if ((double_11 > this.double_5) && ((double_11 - this.double_5) > this.class18_0.double_0))
                {
                    return false;
                }
                return true;
            }

            private void Method_1()
            {
                this.class20_0 = new MeshWeld.Class20(this.double_0, this.double_6, this.double_2, this.double_7, this.double_4, this.double_8, this.class18_0);
                this.class20_1 = new MeshWeld.Class20(this.double_6, this.double_1, this.double_2, this.double_7, this.double_4, this.double_8, this.class18_0);
                this.class20_2 = new MeshWeld.Class20(this.double_6, this.double_1, this.double_7, this.double_3, this.double_4, this.double_8, this.class18_0);
                this.class20_3 = new MeshWeld.Class20(this.double_0, this.double_6, this.double_7, this.double_3, this.double_4, this.double_8, this.class18_0);
                this.class20_4 = new MeshWeld.Class20(this.double_0, this.double_6, this.double_2, this.double_7, this.double_8, this.double_5, this.class18_0);
                this.class20_5 = new MeshWeld.Class20(this.double_6, this.double_1, this.double_2, this.double_7, this.double_8, this.double_5, this.class18_0);
                this.class20_6 = new MeshWeld.Class20(this.double_6, this.double_1, this.double_7, this.double_3, this.double_8, this.double_5, this.class18_0);
                this.class20_7 = new MeshWeld.Class20(this.double_0, this.double_6, this.double_7, this.double_3, this.double_8, this.double_5, this.class18_0);
            }

            internal MeshWeld.Class19 Method_2(double double_9, double double_10, double double_11)
            {
                if (this.bool_0)
                {
                    MeshWeld.Class19 class2;
                    if ((double_11 <= this.double_8) || ((double_11 - this.double_8) <= this.class18_0.double_0))
                    {
                        if ((double_10 <= this.double_7) || ((double_10 - this.double_7) <= this.class18_0.double_0))
                        {
                            if (((double_9 <= this.double_6) || ((double_9 - this.double_6) <= this.class18_0.double_0)) && (this.class20_0 != null))
                            {
                                class2 = this.class20_0.Method_2(double_9, double_10, double_11);
                                if (class2 != null)
                                {
                                    return class2;
                                }
                            }
                            if (((double_9 > this.double_6) || ((this.double_6 - double_9) <= this.class18_0.double_0)) && (this.class20_1 != null))
                            {
                                class2 = this.class20_1.Method_2(double_9, double_10, double_11);
                                if (class2 != null)
                                {
                                    return class2;
                                }
                            }
                        }
                        if ((double_10 > this.double_7) || ((this.double_7 - double_10) <= this.class18_0.double_0))
                        {
                            if (((double_9 <= this.double_6) || ((double_9 - this.double_6) <= this.class18_0.double_0)) && (this.class20_2 != null))
                            {
                                class2 = this.class20_2.Method_2(double_9, double_10, double_11);
                                if (class2 != null)
                                {
                                    return class2;
                                }
                            }
                            if (((double_9 > this.double_6) || ((this.double_6 - double_9) <= this.class18_0.double_0)) && (this.class20_3 != null))
                            {
                                class2 = this.class20_3.Method_2(double_9, double_10, double_11);
                                if (class2 != null)
                                {
                                    return class2;
                                }
                            }
                        }
                    }
                    if ((double_11 > this.double_8) || ((this.double_8 - double_11) <= this.class18_0.double_0))
                    {
                        if ((double_10 <= this.double_7) || ((double_10 - this.double_7) <= this.class18_0.double_0))
                        {
                            if (((double_9 <= this.double_6) || ((double_9 - this.double_6) <= this.class18_0.double_0)) && (this.class20_4 != null))
                            {
                                class2 = this.class20_4.Method_2(double_9, double_10, double_11);
                                if (class2 != null)
                                {
                                    return class2;
                                }
                            }
                            if (((double_9 > this.double_6) || ((this.double_6 - double_9) <= this.class18_0.double_0)) && (this.class20_5 != null))
                            {
                                class2 = this.class20_5.Method_2(double_9, double_10, double_11);
                                if (class2 != null)
                                {
                                    return class2;
                                }
                            }
                        }
                        if ((double_10 > this.double_7) || ((this.double_7 - double_10) <= this.class18_0.double_0))
                        {
                            if (((double_9 <= this.double_6) || ((double_9 - this.double_6) <= this.class18_0.double_0)) && (this.class20_6 != null))
                            {
                                class2 = this.class20_6.Method_2(double_9, double_10, double_11);
                                if (class2 != null)
                                {
                                    return class2;
                                }
                            }
                            if (((double_9 > this.double_6) || ((this.double_6 - double_9) <= this.class18_0.double_0)) && (this.class20_7 != null))
                            {
                                return this.class20_7.Method_2(double_9, double_10, double_11);
                            }
                        }
                    }
                    return null;
                }
                double num = this.class18_0.double_0 * this.class18_0.double_0;
                for (int i = 0; i < this.list_0.Count; i++)
                {
                    MeshWeld.Class19 class3 = this.list_0[i];
                    double num3 = double_9 - class3.double_0;
                    num3 *= num3;
                    if (num3 < num)
                    {
                        double num4 = double_10 - class3.double_1;
                        double num5 = num3 + (num4 * num4);
                        if (num5 < num)
                        {
                            double num6 = double_11 - class3.double_2;
                            num5 += num6 * num6;
                            if (num5 < num)
                            {
                                return this.list_0[i];
                            }
                        }
                    }
                }
                return null;
            }

            public void Method_3(MeshWeld.Class19 class19_0)
            {
                if (this.bool_0)
                {
                    if (class19_0.double_2 <= this.double_8)
                    {
                        if (class19_0.double_1 <= this.double_7)
                        {
                            if (class19_0.double_0 <= this.double_6)
                            {
                                if (this.class20_0 == null)
                                {
                                    this.class20_0 = new MeshWeld.Class20(this.double_0, this.double_6, this.double_2, this.double_7, this.double_4, this.double_8, this.class18_0);
                                }
                                this.class20_0.Method_3(class19_0);
                            }
                            else
                            {
                                if (this.class20_1 == null)
                                {
                                    this.class20_1 = new MeshWeld.Class20(this.double_6, this.double_0, this.double_2, this.double_7, this.double_4, this.double_8, this.class18_0);
                                }
                                this.class20_1.Method_3(class19_0);
                            }
                        }
                        else if (class19_0.double_0 <= this.double_6)
                        {
                            if (this.class20_2 == null)
                            {
                                this.class20_2 = new MeshWeld.Class20(this.double_0, this.double_6, this.double_7, this.double_3, this.double_4, this.double_8, this.class18_0);
                            }
                            this.class20_2.Method_3(class19_0);
                        }
                        else
                        {
                            if (this.class20_3 == null)
                            {
                                this.class20_3 = new MeshWeld.Class20(this.double_6, this.double_0, this.double_7, this.double_3, this.double_4, this.double_8, this.class18_0);
                            }
                            this.class20_3.Method_3(class19_0);
                        }
                    }
                    else if (class19_0.double_1 <= this.double_7)
                    {
                        if (class19_0.double_0 <= this.double_6)
                        {
                            if (this.class20_4 == null)
                            {
                                this.class20_4 = new MeshWeld.Class20(this.double_0, this.double_6, this.double_2, this.double_7, this.double_8, this.double_5, this.class18_0);
                            }
                            this.class20_4.Method_3(class19_0);
                        }
                        else
                        {
                            if (this.class20_5 == null)
                            {
                                this.class20_5 = new MeshWeld.Class20(this.double_6, this.double_0, this.double_2, this.double_7, this.double_8, this.double_5, this.class18_0);
                            }
                            this.class20_5.Method_3(class19_0);
                        }
                    }
                    else if (class19_0.double_0 <= this.double_6)
                    {
                        if (this.class20_6 == null)
                        {
                            this.class20_6 = new MeshWeld.Class20(this.double_0, this.double_6, this.double_7, this.double_3, this.double_8, this.double_5, this.class18_0);
                        }
                        this.class20_6.Method_3(class19_0);
                    }
                    else
                    {
                        if (this.class20_7 == null)
                        {
                            this.class20_7 = new MeshWeld.Class20(this.double_6, this.double_0, this.double_7, this.double_3, this.double_8, this.double_5, this.class18_0);
                        }
                        this.class20_7.Method_3(class19_0);
                    }
                }
                else
                {
                    bool flag = false;
                    for (int i = 0; i < this.list_0.Count; i++)
                    {
                        if (this.list_0[i].int_0 == class19_0.int_0)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        this.class18_0.class20_1 = this;
                        if (this.list_0.Count < this.class18_0.int_0)
                        {
                            this.list_0.Add(class19_0);
                        }
                        else
                        {
                            this.list_0.Add(class19_0);
                            this.Method_5(this.list_0);
                            this.list_0.Clear();
                        }
                    }
                }
            }

            public bool Method_4(long long_0, double double_9, double double_10, double double_11)
            {
                if (this.bool_0)
                {
                    if ((double_11 <= this.double_8) || ((double_11 - this.double_8) <= this.class18_0.double_0))
                    {
                        if ((double_10 <= this.double_7) || ((double_10 - this.double_7) <= this.class18_0.double_0))
                        {
                            if (((double_9 <= this.double_6) || ((double_9 - this.double_6) <= this.class18_0.double_0)) && ((this.class20_0 != null) && this.class20_0.Method_4(long_0, double_9, double_10, double_11)))
                            {
                                return true;
                            }
                            if (((double_9 > this.double_6) || ((this.double_6 - double_9) <= this.class18_0.double_0)) && ((this.class20_1 != null) && this.class20_1.Method_4(long_0, double_9, double_10, double_11)))
                            {
                                return true;
                            }
                        }
                        if ((double_10 > this.double_7) || ((double_10 - this.double_7) <= this.class18_0.double_0))
                        {
                            if (((double_9 <= this.double_6) || ((double_9 - this.double_6) <= this.class18_0.double_0)) && ((this.class20_2 != null) && this.class20_2.Method_4(long_0, double_9, double_10, double_11)))
                            {
                                return true;
                            }
                            if (((double_9 > this.double_6) || ((this.double_6 - double_9) <= this.class18_0.double_0)) && ((this.class20_3 != null) && this.class20_3.Method_4(long_0, double_9, double_10, double_11)))
                            {
                                return true;
                            }
                        }
                    }
                    if ((double_11 > this.double_8) || ((this.double_8 - double_11) <= this.class18_0.double_0))
                    {
                        if ((double_10 <= this.double_7) || ((double_10 - this.double_7) <= this.class18_0.double_0))
                        {
                            if (((double_9 <= this.double_6) || ((double_9 - this.double_6) <= this.class18_0.double_0)) && ((this.class20_4 != null) && this.class20_4.Method_4(long_0, double_9, double_10, double_11)))
                            {
                                return true;
                            }
                            if (((double_9 > this.double_6) || ((this.double_6 - double_9) <= this.class18_0.double_0)) && ((this.class20_5 != null) && this.class20_5.Method_4(long_0, double_9, double_10, double_11)))
                            {
                                return true;
                            }
                        }
                        if ((double_10 > this.double_7) || ((this.double_7 - double_10) <= this.class18_0.double_0))
                        {
                            if (((double_9 <= this.double_6) || ((double_9 - this.double_6) <= this.class18_0.double_0)) && ((this.class20_6 != null) && this.class20_6.Method_4(long_0, double_9, double_10, double_11)))
                            {
                                return true;
                            }
                            if (((double_9 > this.double_6) || ((this.double_6 - double_9) <= this.class18_0.double_0)) && (this.class20_7 != null))
                            {
                                return this.class20_7.Method_4(long_0, double_9, double_10, double_11);
                            }
                        }
                    }
                    return false;
                }
                for (int i = 0; i < this.list_0.Count; i++)
                {
                    if (this.list_0[i].int_0 == long_0)
                    {
                        this.list_0.Remove(this.list_0[i]);
                        return true;
                    }
                }
                return false;
            }

            public void Method_5(List<MeshWeld.Class19> list_1)
            {
                this.class20_0 = null;
                this.class20_1 = null;
                this.class20_2 = null;
                this.class20_3 = null;
                this.class20_4 = null;
                this.class20_5 = null;
                this.class20_6 = null;
                this.class20_7 = null;
                if (list_1.Count <= this.class18_0.int_0)
                {
                    for (int i = 0; i < list_1.Count; i++)
                    {
                        this.list_0.Add(list_1[i]);
                    }
                }
                else
                {
                    this.bool_0 = true;
                    List<MeshWeld.Class19> list = new List<MeshWeld.Class19>();
                    List<MeshWeld.Class19> list2 = new List<MeshWeld.Class19>();
                    List<MeshWeld.Class19> list3 = new List<MeshWeld.Class19>();
                    List<MeshWeld.Class19> list4 = new List<MeshWeld.Class19>();
                    List<MeshWeld.Class19> list5 = new List<MeshWeld.Class19>();
                    List<MeshWeld.Class19> list6 = new List<MeshWeld.Class19>();
                    List<MeshWeld.Class19> list7 = new List<MeshWeld.Class19>();
                    List<MeshWeld.Class19> list8 = new List<MeshWeld.Class19>();
                    for (int j = 0; j < list_1.Count; j++)
                    {
                        MeshWeld.Class19 class2 = list_1[j];
                        double num3 = class2.double_0;
                        double num4 = class2.double_1;
                        double num5 = class2.double_2;
                        if ((num5 > this.double_8) && ((num5 - this.double_8) > this.class18_0.double_0))
                        {
                            if ((num4 > this.double_7) && ((num4 - this.double_7) > this.class18_0.double_0))
                            {
                                if ((num3 > this.double_6) && ((num3 - this.double_6) > this.class18_0.double_0))
                                {
                                    list8.Add(list_1[j]);
                                }
                                else
                                {
                                    list7.Add(list_1[j]);
                                }
                            }
                            else if ((num3 > this.double_6) && ((num3 - this.double_6) > this.class18_0.double_0))
                            {
                                list6.Add(list_1[j]);
                            }
                            else
                            {
                                list5.Add(list_1[j]);
                            }
                        }
                        else if ((num4 > this.double_7) && ((num4 - this.double_7) > this.class18_0.double_0))
                        {
                            if ((num3 > this.double_6) && ((num3 - this.double_6) > this.class18_0.double_0))
                            {
                                list4.Add(list_1[j]);
                            }
                            else
                            {
                                list3.Add(list_1[j]);
                            }
                        }
                        else if ((num3 > this.double_6) && ((num3 - this.double_6) > this.class18_0.double_0))
                        {
                            list2.Add(list_1[j]);
                        }
                        else
                        {
                            list.Add(list_1[j]);
                        }
                    }
                    if (list.Count > 0)
                    {
                        this.class20_0 = new MeshWeld.Class20(this.double_0, this.double_6, this.double_2, this.double_7, this.double_4, this.double_8, this.class18_0);
                        this.class20_0.Method_5(list);
                    }
                    if (list2.Count > 0)
                    {
                        this.class20_1 = new MeshWeld.Class20(this.double_6, this.double_1, this.double_2, this.double_7, this.double_4, this.double_8, this.class18_0);
                        this.class20_1.Method_5(list2);
                    }
                    if (list3.Count > 0)
                    {
                        this.class20_2 = new MeshWeld.Class20(this.double_0, this.double_6, this.double_7, this.double_3, this.double_4, this.double_8, this.class18_0);
                        this.class20_2.Method_5(list3);
                    }
                    if (list4.Count > 0)
                    {
                        this.class20_3 = new MeshWeld.Class20(this.double_6, this.double_1, this.double_7, this.double_3, this.double_4, this.double_8, this.class18_0);
                        this.class20_3.Method_5(list4);
                    }
                    if (list5.Count > 0)
                    {
                        this.class20_4 = new MeshWeld.Class20(this.double_0, this.double_6, this.double_2, this.double_7, this.double_8, this.double_5, this.class18_0);
                        this.class20_4.Method_5(list5);
                    }
                    if (list6.Count > 0)
                    {
                        this.class20_5 = new MeshWeld.Class20(this.double_6, this.double_1, this.double_2, this.double_7, this.double_8, this.double_5, this.class18_0);
                        this.class20_5.Method_5(list6);
                    }
                    if (list7.Count > 0)
                    {
                        this.class20_6 = new MeshWeld.Class20(this.double_0, this.double_6, this.double_7, this.double_3, this.double_8, this.double_5, this.class18_0);
                        this.class20_6.Method_5(list7);
                    }
                    if (list8.Count > 0)
                    {
                        this.class20_7 = new MeshWeld.Class20(this.double_6, this.double_1, this.double_7, this.double_3, this.double_8, this.double_5, this.class18_0);
                        this.class20_7.Method_5(list8);
                    }
                }
            }

            public double Method_6(double double_9, double double_10, double double_11)
            {
                return Math.Sqrt(this.Method_7(double_9, double_10, double_11));
            }

            public double Method_7(double double_9, double double_10, double double_11)
            {
                double num = this.double_6 - this.double_0;
                double num2 = this.double_7 - this.double_2;
                double num3 = this.double_8 - this.double_4;
                if (double_9 < this.double_6)
                {
                    num = this.double_1 - double_9;
                }
                else if (double_9 > this.double_6)
                {
                    num = double_9 - this.double_0;
                }
                if (double_10 < this.double_7)
                {
                    num2 = this.double_3 - double_10;
                }
                else if (double_10 > this.double_7)
                {
                    num2 = double_10 - this.double_2;
                }
                if (double_11 < this.double_8)
                {
                    num3 = this.double_5 - double_11;
                }
                else if (double_11 > this.double_8)
                {
                    num3 = double_11 - this.double_4;
                }
                return (((num * num) + (num2 * num2)) + (num3 * num3));
            }

            public double Method_8(double double_9, double double_10, double double_11)
            {
                return Math.Sqrt(this.Method_9(double_9, double_10, double_11));
            }

            public double Method_9(double double_9, double double_10, double double_11)
            {
                double num = 0.0;
                double num2 = 0.0;
                double num3 = 0.0;
                if (double_9 < this.double_0)
                {
                    num = this.double_0 - double_9;
                }
                else if (double_9 > this.double_1)
                {
                    num = double_9 - this.double_1;
                }
                if (double_10 < this.double_2)
                {
                    num2 = this.double_2 - double_10;
                }
                else if (double_10 > this.double_3)
                {
                    num2 = double_10 - this.double_3;
                }
                if (double_11 < this.double_4)
                {
                    num3 = this.double_4 - double_11;
                }
                else if (double_11 > this.double_5)
                {
                    num3 = double_11 - this.double_5;
                }
                return (((num * num) + (num2 * num2)) + (num3 * num3));
            }

            public override string ToString()
            {
                object[] objArray1 = new object[] {
          "OcTree ", this.double_0, " , ", this.double_2, " , ", this.double_4, " : ", this.double_1, " , ", this.double_3, " , ", this.double_5, " : ", this.double_6, " , ", this.double_7,
          " , ", this.double_8
          };
                return string.Concat(objArray1);
            }
        }
    }
}
