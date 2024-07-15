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
    public class Import
    {
        public static List<double> ReadSenNum(string path)
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
        public static List<Tetrahedron> ReadTet_Abaqus(string path, ref List<Vector> nds, ref List<int> solidID, ref List<int> voidID)
        {
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
                    if (line == "*Element, type=C3D4" ||
                        line == "*Element, type=C3D4R")
                    {
                        line = SR.ReadLine();
                        int id = 0;
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

                            faces.Add(new Face(2, 1, 0));
                            faces.Add(new Face(1, 2, 3));
                            faces.Add(new Face(3, 2, 0));
                            faces.Add(new Face(1, 3, 0));

                            var elem = new Tetrahedron(verts.ToArray(), faces.ToArray());
                            elem.SetID(id);
                            elem.SetNdlID(new int[4] { n0, n1, n2, n3});
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
                                voidID.Add(int.Parse(item) - 1);
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
        public static List<Hexahedron> ReadHex_Abaqus(string path, ref List<Vector> nds, ref List<int> solidID, ref List<int> voidID)
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

                            nds.Add(new Vector(a, b, c));
                            line = SR.ReadLine();
                        }
                    }
                    #endregion

                    #region Read Elements
                    // Read Hexahedrons
                    if (line == "*Element, type=C3D10" ||
                        line == "*Element, type=C3D10R" ||
                        line == "*Element, type=C3D8R" || 
                        line == "*Element, type=C3D8")
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

                            verts.Add(nds[n0]);
                            verts.Add(nds[n1]);
                            verts.Add(nds[n2]);
                            verts.Add(nds[n3]);
                            verts.Add(nds[n4]);
                            verts.Add(nds[n5]);
                            verts.Add(nds[n6]);
                            verts.Add(nds[n7]);

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
                                voidID.Add(int.Parse(item) - 1);
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
        public static List<Tetrahedron> ReadTet_ALFE(string path, ref List<Vector> nds, ref List<int> solidID, ref List<int> voidID)
        {
            List<Tetrahedron> elems = new List<Tetrahedron>();

            if (File.Exists(path))
            {
                StreamReader SR = new StreamReader(path);
                int id = 0;
                while (!SR.EndOfStream)
                {
                    string line = SR.ReadLine();

                    #region Read Nodes
                    var tokens = line.Split(',');
                    if (tokens[0] == "N")
                    {
                        double a = double.Parse(tokens[1]);
                        double b = double.Parse(tokens[2]);
                        double c = double.Parse(tokens[3]);

                        nds.Add(new Vector(a, b, c));
                    }
                    #endregion

                    #region Read Elements
                    // Read Hexahedrons
                    if (tokens[0] == "E")
                    {
                        List<Vector> verts = new List<Vector>();
                        List<Face> faces = new List<Face>();

                        int n0 = int.Parse(tokens[1]);
                        int n1 = int.Parse(tokens[2]);
                        int n2 = int.Parse(tokens[3]);
                        int n3 = int.Parse(tokens[4]);

                        verts.Add(nds[n0]);
                        verts.Add(nds[n1]);
                        verts.Add(nds[n2]);
                        verts.Add(nds[n3]);

                        faces.Add(new Face(2, 1, 0));
                        faces.Add(new Face(1, 2, 3));
                        faces.Add(new Face(3, 2, 0));
                        faces.Add(new Face(1, 3, 0));

                        var elem = new Tetrahedron(verts.ToArray(), faces.ToArray());
                        elem.SetID(id);
                        elem.SetNdlID(new int[4] { n0, n1, n2, n3 });
                        elems.Add(elem);
                        id++;
                    }
                    #endregion

                    #region Read solid elements
                    if (tokens[0] == "SD")
                    {
                        solidID.Add(int.Parse(tokens[1]) - 1);
                    }
                    #endregion

                    #region Read void elements
                    if (tokens[0] == "VD")
                    {
                        voidID.Add(int.Parse(tokens[1]) - 1);
                    }
                    #endregion
                }
                SR.Close();
                SR.Dispose();
            }

            return elems;
        }
        public static List<Hexahedron> ReadHex_ALFE(string path, ref List<Vector> nds, ref List<int> solidID, ref List<int> voidID)
        {
            List<Hexahedron> elems = new List<Hexahedron>();

            if (File.Exists(path))
            {
                StreamReader SR = new StreamReader(path);
                int id = 0;
                while (!SR.EndOfStream)
                {
                    string line = SR.ReadLine();

                    #region Read Nodes
                    var tokens = line.Split(',');
                    if (tokens[0] == "N")
                    {
                        double a = double.Parse(tokens[1]);
                        double b = double.Parse(tokens[2]);
                        double c = double.Parse(tokens[3]);

                        nds.Add(new Vector(a, b, c));
                    }
                    #endregion

                    #region Read Elements
                    // Read Hexahedrons
                    if (tokens[0] == "E")
                    {
                        List<Vector> verts = new List<Vector>();
                        List<Face> faces = new List<Face>();

                        int n0 = int.Parse(tokens[1]);
                        int n1 = int.Parse(tokens[2]);
                        int n2 = int.Parse(tokens[3]);
                        int n3 = int.Parse(tokens[4]);
                        int n4 = int.Parse(tokens[5]);
                        int n5 = int.Parse(tokens[6]);
                        int n6 = int.Parse(tokens[7]);
                        int n7 = int.Parse(tokens[8]);

                        verts.Add(nds[n0]);
                        verts.Add(nds[n1]);
                        verts.Add(nds[n2]);
                        verts.Add(nds[n3]);
                        verts.Add(nds[n4]);
                        verts.Add(nds[n5]);
                        verts.Add(nds[n6]);
                        verts.Add(nds[n7]);

                        faces.Add(new Face(1, 0, 3, 2));
                        faces.Add(new Face(0, 1, 5, 4));
                        faces.Add(new Face(1, 2, 6, 5));
                        faces.Add(new Face(6, 2, 3, 7));
                        faces.Add(new Face(3, 0, 4, 7));
                        faces.Add(new Face(6, 7, 4, 5));

                        var elem = new Hexahedron(verts.ToArray(), faces.ToArray());
                        elem.SetID(id);
                        elem.SetNdlID(new int[8] { n0, n1, n2, n3, n4, n5, n6, n7 });
                        elems.Add(elem);
                        id++;
                    }
                    #endregion

                    #region Read solid elements
                    if (tokens[0] == "SD")
                    {
                        solidID.Add(int.Parse(tokens[1]) - 1);
                    }
                    #endregion

                    #region Read void elements
                    if (tokens[0] == "VD")
                    {
                        voidID.Add(int.Parse(tokens[1]) - 1);
                    }
                    #endregion
                }
                SR.Close();
                SR.Dispose();
            }
            return elems;
        }

        public static List<Hexahedron> ReadHex_MCC(string path, ref List<Vector> nodeList, ref List<double> values)
        {
            List<Hexahedron> elems = new List<Hexahedron>();

            if (File.Exists(path))
            {
                StreamReader SR = new StreamReader(path);
                while (!SR.EndOfStream)
                {
                    string line = SR.ReadLine();

                    #region Read Nodes
                    if (line.StartsWith("Nds"))
                    {
                        var nds_num = int.Parse(line.Split(",")[1]);
                        for (int i = 0; i < nds_num; i++)
                        {
                            line = SR.ReadLine();
                            var tokens = line.Split(",");
                            double x = double.Parse(tokens[0]);
                            double y = double.Parse(tokens[1]);
                            double z = double.Parse(tokens[2]);

                            nodeList.Add(new Vector(x, y, z));
                        }
                    }
                    #endregion

                    #region Read Elements
                    // Read Hexahedrons
                    if (line.StartsWith("Elems"))
                    {
                        
                        var elems_num = int.Parse(line.Split(",")[1]);
                        int id = 0;
                        line = SR.ReadLine();
                        for (int i = 0; i < elems_num; i++)
                        {
                            var tokens = line.Split(",");

                            List<Vector> verts = new List<Vector>();
                            List<Face> faces = new List<Face>();

                            int n0 = int.Parse(tokens[0]);
                            int n1 = int.Parse(tokens[1]);
                            int n2 = int.Parse(tokens[2]);
                            int n3 = int.Parse(tokens[3]);
                            int n4 = int.Parse(tokens[4]);
                            int n5 = int.Parse(tokens[5]);
                            int n6 = int.Parse(tokens[6]);
                            int n7 = int.Parse(tokens[7]);

                            verts.Add(nodeList[n0]);
                            verts.Add(nodeList[n1]);
                            verts.Add(nodeList[n2]);
                            verts.Add(nodeList[n3]);
                            verts.Add(nodeList[n4]);
                            verts.Add(nodeList[n5]);
                            verts.Add(nodeList[n6]);
                            verts.Add(nodeList[n7]);

                            faces.Add(new Face(1, 0, 3, 2));
                            faces.Add(new Face(0, 1, 5, 4));
                            faces.Add(new Face(1, 2, 6, 5));
                            faces.Add(new Face(6, 2, 3, 7));
                            faces.Add(new Face(3, 0, 4, 7));
                            faces.Add(new Face(6, 7, 4, 5));

                            var elem = new Hexahedron(verts.ToArray(), faces.ToArray());
                            elem.SetID(id);
                            elem.SetNdlID(new int[8] { n0, n1, n2, n3, n4, n5, n6, n7 });
                            elems.Add(elem);
                            id++;
                            line = SR.ReadLine();
                        }
                    }
                    #endregion


                    #region Read Values
                    if (line.StartsWith("Val"))
                    {
                        var val_num = int.Parse(line.Split(",")[1]);
                        for (int i = 0; i < val_num; i++)
                        {
                            line = SR.ReadLine();
                            values.Add(double.Parse(line));
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
