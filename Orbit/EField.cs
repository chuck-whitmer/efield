using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Efield;

namespace Orbit
{
    class EField
    {
        Vector[] posPoints;
        Vector[] negPoints;
        double kQ = 1.0; // Coulomb constant times real point charge.

        //  The scale is what must be multiplied into the given coordinates to
        // get meters (SI units).  For example is x,y,z are in cm, then scale=.01
        //  The voltage is the desired real voltage difference between grids.
        // The internal kQ variable will be set to make it so.
        public EField(PointCharge[] charges, double scale, double voltage)
        {
            List<Vector> posList = new List<Vector>();
            List<Vector> negList = new List<Vector>();
            foreach (PointCharge pt in charges)
            {
                Vector v = new Vector(pt.x * scale, pt.y * scale, pt.z * scale);
                if (pt.charge == 1)
                    posList.Add(v);
                else if (pt.charge == -1)
                    negList.Add(v);
                else
                    throw new Exception("Unexpected charge in PointCharges");
            }
            posPoints = posList.ToArray();
            negPoints = negList.ToArray();
            if (posPoints.Length != negPoints.Length)
                throw new Exception("Unequal number of positive and negative charges");

            // Figure out the kQ normalization.
            double posSum = 0.0;
            double negSum = 0.0;
            int n = posPoints.Length;
            for (int i = 0; i < n; i++)
            {
                Vector r = posPoints[i];
                posSum += PartialPotential(posPoints, r, i)
                    - PartialPotential(negPoints, r, -1);
                r = negPoints[i];
                negSum += PartialPotential(posPoints, r, -1)
                    - PartialPotential(negPoints, r, i);
            }
            double deltaPhi = (posSum - negSum) / n;
            Console.WriteLine("Delta phi = {0:0.000e+00}", deltaPhi);
            kQ = voltage / deltaPhi;
        }

        public double PartialPotential(Vector[] points, Vector r, int iExclude)
        {
            double phiSum = 0.0;
            for (int i=0; i<points.Length; i++)
            {
                if (i != iExclude)
                    phiSum += 1.0 / Vector.Distance(r, points[i]);
            }
            return phiSum;
        }
    }
}
