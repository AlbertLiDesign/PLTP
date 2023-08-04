using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class Box

    {
        public Vector Min;
        public Vector Max;
        public Vector Center;
        private Mesh mesh;
        public Box()
        {
            Min = new Vector(double.MaxValue, double.MaxValue, double.MaxValue);
            Max = new Vector(double.MinValue, double.MinValue, double.MinValue);
            Center = (Min + Max) * 0.5;
        }
        public Box(Mesh mesh, Vector min, Vector max)
        {
            Min = min;
            Max = max;
            this.mesh = mesh;
        }
    }
}
