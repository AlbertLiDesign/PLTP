using System;

namespace PLTP // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TableExample();
        }

        public static void CanteileverExample()
        {
            string mdl_path = "../../../../../data/Cantilever/Job-1_BESO.inp";
            string sen_path = "../../../../../data/Cantilever/Sensitivities.txt";
            string output_path = "../../../../../data/Cantilever/Smoothing.obj";
            Test.TestHex(mdl_path, sen_path, 0.15, 3.0, 0.01, 50, true, true, output_path);
        }
        public static void TableExample()
        {
            string mdl_path = "../../../../../data/Table/Job-1_BESO.inp";
            string sen_path = "../../../../../data/Table/Sensitivities.txt";
            string output_path = "../../../../../data/Table/Smoothing.obj";
            Test.TestHex(mdl_path, sen_path, 0.2, 3.0, 0.01, 50, true, true, output_path);
        }
    }
}