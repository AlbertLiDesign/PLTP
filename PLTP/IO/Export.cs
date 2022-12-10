using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    /// <summary>
    /// IO class for writting .obj file
    /// </summary>
    public class Export
    {
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
