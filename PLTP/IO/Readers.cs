using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    /// <summary>
    /// IO class for reading files
    /// </summary>
    public class Readers
    {
        public static List<double> ReadElemSenNum(string path)
        {
            List<double> ndlSens = new List<double>();
            if (File.Exists(path))
            {
                StreamReader SR = new StreamReader(path);
                while (!SR.EndOfStream)
                {
                    string line = SR.ReadLine();
                    ndlSens.Add(double.Parse(line));
                }
            }
            return ndlSens;
        }
        public static List<Tetrahedron> ReadTet(string path, ref List<int> solidID, ref List<int> nonDesignID)
        {
            List<Vector> nds = new List<Vector>();
            List<Tetrahedron> elems = new List<Tetrahedron>();
            if (File.Exists(path))
            {
                StreamReader SR = new StreamReader(path);
                while (!SR.EndOfStream)
                {
                    string line = SR.ReadLine();

                    #region Read Nodes
                    if (line == "*Node")
                    {
                        line = SR.ReadLine();
                        while (!line.StartsWith("*"))
                        {
                            line = line.Replace(" ", "");
                            var tokens = line.Split(',');
                            double a = double.Parse(tokens[1]);
                            double b = double.Parse(tokens[2]);
                            double c = double.Parse(tokens[3]);

                            nds.Add(new Vector(a, b, c));
                            line = SR.ReadLine();
                        }
                    }
                    #endregion

                    #region Read Elements
                    // Read Tetrahedra
                    if (line == "*Element, type=C3D4")
                    {
                        line = SR.ReadLine();
                        while (!line.StartsWith("*"))
                        {
                            line = line.Replace(" ", "");
                            var tokens = line.Split(',');

                            List<Vector> verts = new List<Vector>();
                            List<Face> faces = new List<Face>();
                            int n0 = int.Parse(tokens[1]) - 1;
                            int n1 = int.Parse(tokens[2]) - 1;
                            int n2 = int.Parse(tokens[3]) - 1;
                            int n3 = int.Parse(tokens[4]) - 1;

                            verts.Add(nds[n0]);
                            verts.Add(nds[n1]);
                            verts.Add(nds[n2]);
                            verts.Add(nds[n3]);

                            faces.Add(new Face(0, 1, 2));
                            faces.Add(new Face(0, 1, 3));
                            faces.Add(new Face(0, 2, 3));
                            faces.Add(new Face(1, 2, 3));

                            elems.Add(new Tetrahedron(verts.ToArray(), faces.ToArray()));
                            line = SR.ReadLine();
                        }
                    }
                    #endregion

                    #region Read Solid Elements
                    if (line == "*Elset, elset=Solid")
                    {
                        line = SR.ReadLine();
                        while (!line.StartsWith("*"))
                        {
                            var tokens = line.Split(',');
                            solidID.Add(int.Parse(tokens[0]) - 1);
                            line = SR.ReadLine();
                        }
                    }
                    #endregion

                    #region Read Non-design elements
                    if (line == "*Elset, elset=Non_Design")
                    {
                        line = SR.ReadLine();
                        while (!line.StartsWith("*"))
                        {
                            line = line.Replace(" ", "");
                            var tokens = line.Split(',');
                            foreach (var item in tokens)
                            {
                                nonDesignID.Add(int.Parse(item) - 1);
                            }
                            line = SR.ReadLine();
                        }
                    }
                    #endregion
                }
                SR.Close();
                SR.Dispose();
            }
            return elems;
        }
        public static List<Hexahedron> ReadHex(string path, ref List<Vector> nodeList, ref List<int> solidID, ref List<int> nonDesignID)
        {
            List<Hexahedron> elems = new List<Hexahedron>();

            if (File.Exists(path))
            {
                StreamReader SR = new StreamReader(path);
                while (!SR.EndOfStream)
                {
                    string line = SR.ReadLine();

                    #region Read Nodes
                    if (line == "*Node")
                    {
                        line = SR.ReadLine();
                        while (!line.StartsWith("*"))
                        {
                            line = line.Replace(" ", "");
                            var tokens = line.Split(',');
                            double a = double.Parse(tokens[1]);
                            double b = double.Parse(tokens[2]);
                            double c = double.Parse(tokens[3]);

                            nodeList.Add(new Vector(a, b, c));
                            line = SR.ReadLine();
                        }
                    }
                    #endregion

                    #region Read Elements
                    // Read Hexahedrons
                    if (line == "*Element, type=C3D10" || line == "*Element, type=C3D8")
                    {
                        int id = 0;
                        line = SR.ReadLine();
                        while (!line.StartsWith("*"))
                        {
                            line = line.Replace(" ", "");
                            var tokens = line.Split(',');

                            List<Vector> verts = new List<Vector>();
                            List<Face> faces = new List<Face>();

                            int n0 = int.Parse(tokens[1]) - 1;
                            int n1 = int.Parse(tokens[2]) - 1;
                            int n2 = int.Parse(tokens[3]) - 1;
                            int n3 = int.Parse(tokens[4]) - 1;
                            int n4 = int.Parse(tokens[5]) - 1;
                            int n5 = int.Parse(tokens[6]) - 1;
                            int n6 = int.Parse(tokens[7]) - 1;
                            int n7 = int.Parse(tokens[8]) - 1;

                            verts.Add(nodeList[n0]);
                            verts.Add(nodeList[n1]);
                            verts.Add(nodeList[n2]);
                            verts.Add(nodeList[n3]);
                            verts.Add(nodeList[n4]);
                            verts.Add(nodeList[n5]);
                            verts.Add(nodeList[n6]);
                            verts.Add(nodeList[n7]);

                            faces.Add(new Face(1,0,3,2));
                            faces.Add(new Face(0,1,5,4));
                            faces.Add(new Face(1,2,6,5));
                            faces.Add(new Face(6,2,3,7));
                            faces.Add(new Face(3,0,4,7));
                            faces.Add(new Face(6,7,4,5));

                            var elem = new Hexahedron(verts.ToArray(), faces.ToArray());
                            elem.SetID(id);
                            elem.SetNdlID(new int[8] {n0,n1,n2,n3,n4,n5,n6,n7});
                            elems.Add(elem);
                            id++;
                            line = SR.ReadLine();
                        }
                    }
                    #endregion

                    #region Read Solid Elements
                    if (line == "*Elset, elset=Solid")
                    {
                        line = SR.ReadLine();
                        while (!line.StartsWith("*"))
                        {
                            var tokens = line.Split(',');
                            solidID.Add(int.Parse(tokens[0]) - 1);
                            line = SR.ReadLine();
                        }
                    }
                    #endregion

                    #region Read Non-design elements
                    if (line == "*Elset, elset=Non_Design")
                    {
                        line = SR.ReadLine();
                        while (!line.StartsWith("*"))
                        {
                            line = line.Replace(" ", "");
                            var tokens = line.Split(',');
                            foreach (var item in tokens)
                            {
                                nonDesignID.Add(int.Parse(item) - 1);
                            }
                            line = SR.ReadLine();
                        }
                    }
                    #endregion
                }
                SR.Close();
                SR.Dispose();
            }
            return elems;
        }
    }
}
