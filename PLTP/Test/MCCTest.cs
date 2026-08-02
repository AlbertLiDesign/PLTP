using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public static class MCCTest
    {
        public static void OuputAllCases(string folder_path)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // File paths
            List<Vector> nodeList = new List<Vector>(8)
            {   
                new Vector(0,0,0), new Vector(1,0,0), new Vector(1,1,0), new Vector(0,1, 0),
                new Vector(0,0,1), new Vector(1,0,1), new Vector(1,1,1), new Vector(0,1,1)
            };
              
            Hexahedron hex = new Hexahedron(
                nodeList.ToArray(), new Face[6]
            {
                new Face(1, 0, 3, 2), new Face(0, 1, 5, 4), new Face(1, 2, 6, 5),
                new Face(6, 2, 3, 7), new Face(3, 0, 4, 7), new Face(6, 7, 4, 5)
            });
            hex.MinVert = hex.Vertices[0];

            // Construct a FE model
            HexModel model = new HexModel(nodeList, new List<Hexahedron>(1) { hex});
            model.Size = new Vector(1, 1, 1);
            model.KeepVolume = false;
            model.Interpolation = false;
            model.UnitiseSensitivityNumber = false;
            model.ReverseValues= false;


            #region Extract iso-sensitivity model
            // Apply pre-built lookup tables
            sw.Restart();
            Console.Write("|*****...............| 25%");
            Console.Write("\t(Extract iso-sensitivity model... ");
            var meshes = model.ExtractAllCases();
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds.ToString() + "ms )\n");
            #endregion


            // Write mesh
            for (int i = 0; i < 256; i++)
            {
                var outputPath = Path.Combine(folder_path, "allcases_" + i + ".obj");
                Console.Write("\t(Write mesh... ");
                Export.WriteObj(meshes[i], outputPath);
            }

            Console.WriteLine("Done");
        }
        public static void MCC(string folder_path, bool interpolation)
        {
            var mdl_path = Path.Combine(folder_path, "Model.mdl");
            Console.WriteLine("====================");
            Console.WriteLine("Welcome to use PLTP.");
            Console.WriteLine("====================");
            Stopwatch sw = Stopwatch.StartNew();

            // File paths
            List<Vector> nodeList = new List<Vector>();
            List<double> valueList = new List<double>();
            #region Preparation
            Console.Write("|....................| 0%");
            Console.Write("\t(Import model... ");
            sw.Start();
            // Import a FE model and values
            var elems = Import.ReadHex_MCC(mdl_path, ref nodeList, ref valueList);
            // Construct a FE model
            HexModel model = new HexModel(nodeList, elems, valueList, false);
            model.KeepVolume = false;
            model.Interpolation = false;
            model.UnitiseSensitivityNumber = true;
            model.ReverseValues= true;
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
            var meshes = model.ExtractIsoSensitivityModel(0.6);
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds.ToString() + "ms )\n");
            #endregion

            #region Combine mesh
            // Combine mesh
            sw.Restart();
            Console.Write("|**************......| 70%");
            Console.Write("\t(Combine mesh... ");
            var output = Mesh.CombineMeshes(meshes);
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds.ToString() + "ms )\n");
            #endregion

            // Write mesh
            var outputPath = Path.Combine(folder_path, "mesh.obj");
            sw.Restart();
            Console.Write("|********************| 100%");
            Console.Write("\t(Write mesh... ");
            Export.WriteObj(output, outputPath);
            sw.Stop();
            Console.Write(sw.ElapsedMilliseconds.ToString() + "ms )\n");
            Console.WriteLine("Done");
        }
    }
}
