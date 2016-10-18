using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Efield
{
    struct Particle
    {
        public Vector Position;
        public int ElementNumber;

        public Particle(Vector pos, int iElement)
        {
            Position = pos;
            ElementNumber = iElement;
        }
    }
}
