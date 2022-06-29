using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PLTP
{
    public static class Test
    {
        public static void TestHex()
        {
            Console.WriteLine("====================");
            Console.WriteLine("Welcome to use PLTP.");
            Console.WriteLine("====================");
            Stopwatch sw = Stopwatch.StartNew();

            // File paths
            string mdl_path = "C:/test/Cantilever/Job-1_BESO.inp";
            string sen_path = "C:/test/Cantilever/Sensitivities.txt";

            List<int> solidID = new List<int>();
            List<int> nonDesignID = new List<int>();
            List<Vector> nodeList = new List<Vector>();

            #region Preparation
            Console.Write("|....................| 0%");
            Console.Write("\t(Import model... ");
            sw.Start();
            // Import a FE model
            var elems = Import.ReadHex(mdl_path, ref nodeList, ref solidID, ref nonDesignID);
            // Import elemental sensitivity numbers
            var elemSen = Import.ReadElemSenNum(sen_path);
            // Construct a FE model
            HexModel model = new HexModel(nodeList, elems, elemSen);
            model.SetParameters(24000, 0.15, 0.044, 0.01, 2.0, 3.0, 50, true, false, true);
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds.ToString() + "ms )\n");

            // Calculate nodal sensitivity field
            sw.Restart();
            Console.Write("|**..................| 10%");
            Console.Write("\t(Calculate nodal sensitivity field... ");
            model.CalNdlSenNums();
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds.ToString() + "ms )\n");

            // Adjust vertex order
            sw.Restart();
            Console.Write("|****................| 20%");
            Console.Write("\t(Adjust vertex order... ");
            model.SortVerts();
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds.ToString() + "ms )\n");
            #endregion

            #region Extract iso-sensitivity model
            // Apply pre-built lookup tables
            sw.Restart();
            Console.Write("|*****...............| 25%");
            Console.Write("\t(Extract iso-sensitivity model... ");
            var meshes = model.ExtractIsoSensitivityModel();
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds.ToString() + "ms )\n");
            #endregion

            #region Clean mesh
            // Combine mesh
            sw.Restart();
            Console.Write("|**************......| 70%");
            Console.Write("\t(Combine mesh... ");
            var output = Mesh.CombineMeshes(meshes);
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds.ToString() + "ms )\n");

            // Weld mesh
            sw.Restart();
            Console.Write("|***************.....| 75%");
            Console.Write("\t(Weld mesh... ");
            output.WeldVertices(1e-6);
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds.ToString() + "ms )\n");

            // Remove duplicated faces
            sw.Restart();
            Console.Write("|******************..| 90%");
            Console.Write("\t(Remove duplicated faces... ");
            output.RemoveDuplicatedFaces();
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds.ToString() + "ms )\n");
            #endregion

            // Write mesh
            sw.Restart();
            Console.Write("|********************| 100%");
            Console.Write("\t(Write mesh... ");
            Export.WriteObj(output, "C:/test/smooth.obj");
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds.ToString() + "ms )\n");
            Console.WriteLine("Done");
        }
    }
}
