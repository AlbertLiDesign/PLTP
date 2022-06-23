using System;

namespace PLTP // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = "D:/Research/Smoothing topology optimization results " +
                "using pre-built lookup tables/FEA_Results/" +
                "ShortCantilever/Job-1_BESO.inp";

            // 读取Abaqus计算好的六面体结果
            INP_Reader reader = new INP_Reader();
            var elems = reader.ReadHex(path);

            // 调整六面体的点序
            Hexahedron.SortHexahedrons_Verts(elems.ToArray());
            // 合并六面体集合成一个mesh
            Mesh mesh = Hexahedron.CombineHexahedrons(elems.ToArray());
            // 导出mesh
            OBJ_Writer.WriteObj(mesh, "C:/test/model.obj");

            Console.WriteLine(elems.Count);
        }
    }
}