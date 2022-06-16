using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class Face
    {
        public int[] faces;
        public bool isTriagnle = true;
        public Face(int a, int b, int c)
        {
            faces = new int[3];
            faces[0] = a;
            faces[1] = b;
            faces[2] = c;
            isTriagnle = true;
        }
        public Face(int a, int b, int c, int d)
        {
            faces = new int[4];
            faces[0] = a;
            faces[1] = b;
            faces[2] = c;
            faces[3] = d;
            isTriagnle = false;
        }
    }
}
