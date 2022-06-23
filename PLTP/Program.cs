using System;

namespace PLTP // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // File paths
            string mdl_path = "C:/test/Cantilever/Job-1_BESO.inp";
            string sen_path = "C:/test/Cantilever/Sensitivities.txt";

            List<int> solidID = new List<int>();
            List<int> nonDesignID = new List<int>();
            // Read a FE model
            var elems = Readers.ReadHex(mdl_path, ref solidID, ref nonDesignID);
            // Adjust the vertex order
            Hexahedron.SortVerts(elems.ToArray());
            // Read elemental sensitivity numbers
            var elemSen = Readers.ReadElemSenNum(sen_path);



            // Combine all hexahedrons into a mesh
            Mesh mesh = Hexahedron.CombineHexahedrons(elems.ToArray());
            // Write the mesh
            OBJ_Writer.WriteObj(mesh, "C:/test/model.obj");

            Console.WriteLine(elems.Count);
        }
    }
}