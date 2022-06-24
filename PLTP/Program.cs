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

            Console.WriteLine("Start");
            Console.Write("|....................| 0%");
            Console.WriteLine(" (Import the model...)");
            // Import a FE model
            var elems = Import.ReadHex(mdl_path, ref nodeList, ref solidID, ref nonDesignID);
            // Import elemental sensitivity numbers
            var elemSen = Import.ReadElemSenNum(sen_path);

            // Construct a model
            Console.Write("|*...................| 5%");
            Console.WriteLine(" (Construct a FE model...)");
            Vector voxelSize= new Vector(1.0, 1.0, 1.0);
            HexModel model = new HexModel(nodeList, elems, elemSen, voxelSize);

            Console.Write("|**..................| 10%");
            Console.WriteLine(" (Calculate nodal sensitivity field...)");
            double[] ndlSenNum = model.CalNdlSenNums(3.0);

            // Adjust the vertex order
            Console.Write("|****................| 20%");
            Console.WriteLine(" (Sorting vertices...)");
            model.SortVerts(ndlSenNum);

            Console.Write("|******************..| 90%");
            Console.WriteLine("Export the result...");
            // Combine all hexahedrons into a mesh
            Mesh mesh = Hexahedron.CombineHexahedrons(elems);
            // Write the mesh
            Export.WriteObj(mesh, "C:/test/model.obj");
            Console.WriteLine("Done");
        }
    }
}