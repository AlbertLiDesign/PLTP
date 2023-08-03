using PLTP;
using System;

namespace PLTP // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            YuLiExample_4();
        }

        public static void YuLiExample_4()
        {
            string mdl_path = "../../../../../data/YuLi_4/Job-2_BESO_96.inp";
            string sen_path = "../../../../../data/YuLi_4/Sensitivities.txt";
            string output_path = "../../../../../data/YuLi_4/Smoothing.obj";
            Test.TestTetra(mdl_path, sen_path, 0.2, 3.0, 0.01, 50, true, false, output_path);
        }
        public static void YuLiExample()
        {
            string mdl_path = "../../../../../data/YuLi/Job-1_BESO_111.inp";
            string sen_path = "../../../../../data/YuLi/Sensitivities.txt";
            string output_path = "../../../../../data/YuLi/Smoothing.obj";
            Test.TestHex(mdl_path, sen_path, 0.2, 3.0, 0.01, 50, true, true, output_path);
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
        public static void GetCantileverSenMdl()
        {
            string mdl_path = "../../../../../data/Cantilever/Job-1_BESO.inp";
            string sen_path = "../../../../../data/Cantilever/Sensitivities.txt";
            string output_path = "../../../../../data/Cantilever/Smoothing.obj";
            Test.ObtainSensitivityMdl(mdl_path, sen_path, 0.15, 3.0, 0.01, 50, true, output_path);
        }

        public static void testMCC()
        {
            string mdl_path = "C:\\Users\\alber\\OneDrive - RMIT University\\Work\\AResearch\\BuildingBlocksForTopOptMdl\\Mdl";
            MCCTest.MCC(mdl_path, true);
        }
        
    }
}