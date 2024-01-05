using PLTP;
using System;

namespace PLTP // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main()
        {
            TestModel();
        }
        //static void Main(string[] args)
        //{
        //    string mdl_path = null;
        //    string sen_path = null;
        //    string output_path = null;
        //    double volumeFraction = 0.2;
        //    double filterRadius = 3.0;
        //    double tolerance = 0.01;
        //    int maximumIteration = 50;
        //    bool interpolation = true;
        //    bool keepVolume = true;
        //    bool isHex = true;

        //    for (int i = 0; i < args.Length; i++)
        //    {
        //        switch (args[i])
        //        {
        //            case "-h":
        //            case "--help":
        //                PrintHelp();
        //                return;
        //            case "-type":
        //                isHex = Convert.ToBoolean(args[++i]);
        //                break;
        //            case "-v":
        //                volumeFraction = Convert.ToDouble(args[++i]);
        //                break;
        //            case "-r":
        //                filterRadius = Convert.ToDouble(args[++i]);
        //                break;
        //            case "-t":
        //                tolerance = Convert.ToDouble(args[++i]);
        //                break;
        //            case "-i":
        //                maximumIteration = Convert.ToInt32(args[++i]);
        //                break;
        //            case "-k":
        //                keepVolume = Convert.ToBoolean(args[++i]);
        //                break;
        //            case "-p":
        //                interpolation = Convert.ToBoolean(args[++i]);
        //                break;
        //            case "-m":
        //                mdl_path = args[++i];
        //                break;
        //            case "-s":
        //                sen_path = args[++i];
        //                break;
        //            case "-o":
        //                output_path = args[++i];
        //                break;
        //        }
        //    }

        //    if (mdl_path != null && sen_path != null && output_path != null)
        //    {
        //        if (isHex)
        //        {
        //            Test.TestHex(mdl_path, sen_path, volumeFraction, filterRadius, tolerance, maximumIteration, interpolation, keepVolume, output_path);
        //        }
        //        else
        //        {
        //            Test.TestTetra(mdl_path, sen_path, volumeFraction, filterRadius, tolerance, maximumIteration, interpolation, keepVolume, output_path);
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Invalid number of arguments provided.");
        //        Console.WriteLine("Expected usage: Program.exe -m <mdl_path> -s <sen_path> -o <output_path> [-v <volumeFraction>] [-r <filterRadius>] [-t <tolerance>] [-i <maximumIteration>] [-p <interpolation>] [-k <keepVolume>]");
        //    }
        //}
        static void PrintHelp()
        {
            Console.WriteLine("Usage: Program.exe -type <elem_type> -m <mdl_path> -s <sen_path> -o <output_path> [-v <volumeFraction>] [-r <filterRadius>] [-t <tolerance>] [-i <maximumIteration>] [-p <interpolation>] [-k <keepVolume>]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help           Show this help message");
            Console.WriteLine("  -type <elem_type>           The element type is hexahedron (True) or tetrahedra (False)（default: True)");
            Console.WriteLine("  -m <mdl_path>        Path to the model file");
            Console.WriteLine("  -s <sen_path>        Path to the sensitivities file");
            Console.WriteLine("  -o <output_path>     Path for the output file");
            Console.WriteLine("  -v <volumeFraction>  Volume fraction (default: 0.2)");
            Console.WriteLine("  -r <filterRadius>    Filter radius (default: 3.0)");
            Console.WriteLine("  -t <tolerance>       Volume tolerance (default: 0.01)");
            Console.WriteLine("  -i <maximumIteration> Maximum iterations (default: 50)");
            Console.WriteLine("  -p <interpolation>  Apply interpolation or not (default: true)");
            Console.WriteLine("  -k <keepVolume>     Keep volume or not (default: true)");
        }
        public static void TestModel()
        {
            //string mdl_path = "../../../../../data/LetterA/beso.txt";
            //string sen_path = "../../../../../data/LetterA/elem_sen_113.txt";
            //string output_path = "../../../../../data/LetterA/Smoothing.obj";
            string mdl_path = "F:\\OneDrive - RMIT University\\Work\\AResearch\\SPBESO_VR\\Numerical examples\\Bridge2\\beso.txt";
            string sen_path = "F:\\OneDrive - RMIT University\\Work\\AResearch\\SPBESO_VR\\Numerical examples\\Bridge2\\solution_0.6\\ndl_sen_98.txt";
            string output_path = "F:\\OneDrive - RMIT University\\Work\\AResearch\\SPBESO_VR\\Numerical examples\\Bridge2\\sol_0.6.obj";
            Test.TestHex(mdl_path, sen_path, 0.2, 30, 0.01, 50, true, true, output_path);
        }

        //public static void testMCC()
        //{
        //    string mdl_path = "C:\\Users\\alber\\OneDrive - RMIT University\\Work\\AResearch\\BuildingBlocksForTopOptMdl\\Mdl";
        //    MCCTest.MCC(mdl_path, true);
        //}

    }
}