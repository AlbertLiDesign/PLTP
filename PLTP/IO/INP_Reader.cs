using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP.IO
{
    /// <summary>
    /// IO class for reading .inp file
    /// </summary>
    public class INP_Reader
    {
        public List<Vector> nds = new List<Vector>();
        public List<int[]> ids = new List<int[]>();
        public List<int> solid = new List<int>();
        public List<int> nonDesignID = new List<int>();

        public INP_Reader() { }
        public List<Tetrahedra> ReadTet(string path)
        {
            List<Tetrahedra> elems = new List<Tetrahedra>();
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

                            ids.Add(new int[4] { n0, n1, n2, n3 });

                            verts.Add(nds[n0]);
                            verts.Add(nds[n1]);
                            verts.Add(nds[n2]);
                            verts.Add(nds[n3]);

                            faces.Add(new Face(0, 1, 2));
                            faces.Add(new Face(0, 1, 3));
                            faces.Add(new Face(0, 2, 3));
                            faces.Add(new Face(1, 2, 3));

                            elems.Add(new Tetrahedra(verts.ToArray(), faces.ToArray()));
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
                            solid.Add(int.Parse(tokens[0]) - 1);
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
        public List<Hexahedron> ReadHex(string path)
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
                    if (line == "*Element, type=C3D10")
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

                            ids.Add(new int[4] { n0, n1, n2, n3 });

                            mesh.Vertices.Add(Nds[n0]);
                            mesh.Vertices.Add(Nds[n1]);
                            mesh.Vertices.Add(Nds[n2]);
                            mesh.Vertices.Add(Nds[n3]);

                            mesh.Faces.AddFace(0, 1, 2);
                            mesh.Faces.AddFace(0, 1, 3);
                            mesh.Faces.AddFace(0, 2, 3);
                            mesh.Faces.AddFace(1, 2, 3);


                            elems.Add(mesh);
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
                            solid.Add(int.Parse(tokens[0]) - 1);
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
