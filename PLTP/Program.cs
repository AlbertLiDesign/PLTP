using System;

namespace PLTP // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = "D://Research//Smoothing topology optimization results " +
                "using pre-built lookup tables//FEA_Results//" +
                "ShortCantilever//Job-1_BESO.inp";

            INP_Reader reader = new INP_Reader();
            var meshes = reader.ReadHex(path);

            Console.WriteLine(meshes.Count);
        }
    }
}