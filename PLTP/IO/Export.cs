using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    /// <summary>
    /// IO class for writting .obj and .stl files
    /// </summary>
    public class Export
    {
        /// <summary>
        /// Write a binary STL. Quadrangles are split into two triangles on the fly,
        /// and the facet normal is taken from the winding, which is what slicers and
        /// mesh repair tools expect. Binary rather than ASCII because an extracted
        /// model easily reaches millions of facets.
        /// </summary>
        public static void WriteStl(Mesh mesh, string path, string header = "Created by PLTP")
        {
            if (mesh == null || mesh.Vertices.Length == 0 || mesh.Faces.Length == 0)
                return;

            // Count first: a quadrangle contributes two facets.
            int facetCount = 0;
            for (int i = 0; i < mesh.Faces.Length; i++)
                facetCount += mesh.Faces[i].Vert_ID.Length == 4 ? 2 : 1;

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                var head = new byte[80];
                var text = System.Text.Encoding.ASCII.GetBytes(header);
                Array.Copy(text, head, System.Math.Min(text.Length, 79));
                writer.Write(head);
                writer.Write((uint)facetCount);

                for (int i = 0; i < mesh.Faces.Length; i++)
                {
                    var f = mesh.Faces[i].Vert_ID;
                    if (f.Length == 4)
                    {
                        WriteFacet(writer, mesh.Vertices[f[0]], mesh.Vertices[f[1]], mesh.Vertices[f[3]]);
                        WriteFacet(writer, mesh.Vertices[f[3]], mesh.Vertices[f[1]], mesh.Vertices[f[2]]);
                    }
                    else
                    {
                        WriteFacet(writer, mesh.Vertices[f[0]], mesh.Vertices[f[1]], mesh.Vertices[f[2]]);
                    }
                }
            }
        }

        private static void WriteFacet(BinaryWriter writer, Vector a, Vector b, Vector c)
        {
            double ux = b.X - a.X, uy = b.Y - a.Y, uz = b.Z - a.Z;
            double vx = c.X - a.X, vy = c.Y - a.Y, vz = c.Z - a.Z;

            double nx = uy * vz - uz * vy;
            double ny = uz * vx - ux * vz;
            double nz = ux * vy - uy * vx;

            double length = System.Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (length > 0.0) { nx /= length; ny /= length; nz /= length; }

            writer.Write((float)nx); writer.Write((float)ny); writer.Write((float)nz);
            writer.Write((float)a.X); writer.Write((float)a.Y); writer.Write((float)a.Z);
            writer.Write((float)b.X); writer.Write((float)b.Y); writer.Write((float)b.Z);
            writer.Write((float)c.X); writer.Write((float)c.Y); writer.Write((float)c.Z);
            writer.Write((ushort)0);   // attribute byte count
        }

        public static void WriteObj(Mesh mesh, string path)
        {
            if (mesh != null && mesh.Vertices.Length !=0 && mesh.Faces.Length !=0)
            {
                StreamWriter sw = new StreamWriter(path);
                sw.WriteLine("# The table written by Albert Li");
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                for (int i = 0; i < mesh.Vertices.Length; i++)
                {
                    var v = mesh.Vertices[i];
                    sw.Write("v ");
                    sw.Write(v.X.ToString("r", ci));
                    sw.Write(" ");
                    sw.Write(v.Y.ToString("r", ci));
                    sw.Write(" ");
                    sw.WriteLine(v.Z.ToString("r", ci));
                }
                for (int i = 0; i < mesh.Faces.Length; i++)
                {
                    var f = mesh.Faces[i].Vert_ID;
                    sw.Write("f");
                    if (f.Length == 4)
                    {
                        sw.Write(' ');
                        sw.Write((f[0] + 1).ToString(ci) + ' ' + (f[1] + 1).ToString(ci) + ' ' + (f[2] + 1).ToString(ci) + ' ' + (f[3] + 1).ToString(ci));
                    }
                    else
                    {
                        sw.Write(' ');
                        sw.Write((f[0] + 1).ToString(ci) + ' ' + (f[1] + 1).ToString(ci) + ' ' + (f[2] + 1).ToString(ci));
                    }
                    sw.WriteLine();
                }
                sw.WriteLine("# end of OBJ file");

                sw.Flush();
                sw.Close();
                sw.Dispose();
            }
            else
            {
                //throw new Exception("The mesh is an invalid mesh.");
            }
        }
    }
}
