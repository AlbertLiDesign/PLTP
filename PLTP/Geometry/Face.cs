using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class Face
    {
        public int[] Vert_ID;
        public bool isTriagnle = true;
        public Face(int a, int b, int c)
        {
            Vert_ID = new int[3];
            Vert_ID[0] = a;
            Vert_ID[1] = b;
            Vert_ID[2] = c;
            isTriagnle = true;
        }
        public Face(int a, int b, int c, int d)
        {
            Vert_ID = new int[4];
            Vert_ID[0] = a;
            Vert_ID[1] = b;
            Vert_ID[2] = c;
            Vert_ID[3] = d;
            isTriagnle = false;
        }
    }
}
