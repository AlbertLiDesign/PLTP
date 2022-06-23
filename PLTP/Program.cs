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
            List<Vector> nodeList = new List<Vector>();
            // Read a FE model
            var elems = Readers.ReadHex(mdl_path, ref nodeList, ref solidID, ref nonDesignID);
            // Adjust the vertex order
            Hexahedron.SortVerts(elems);
            // Read elemental sensitivity numbers
            var elemSen = Readers.ReadElemSenNum(sen_path);

            // Construct a model
            Vector voxelSize= new Vector(1.0, 1.0, 1.0);
            HexModel model = new HexModel(nodeList, elems, elemSen, voxelSize);
            model.FilteringElements(3.0);


            // Combine all hexahedrons into a mesh
            Mesh mesh = Hexahedron.CombineHexahedrons(elems);
            // Write the mesh
            OBJ_Writer.WriteObj(mesh, "C:/test/model.obj");

            Console.WriteLine(elems.Count);
        }
    }
}