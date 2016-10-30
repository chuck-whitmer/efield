using System;
using System.Collections.Generic;
using Efield;

namespace Orbit
{
    class EField
    {
        Vector[] posPoints;
        Vector[] negPoints;
        double kQ = 1.0; // Coulomb constant times real point charge.
        double phiInfinity = 0.0; // Additive constant for phi.

        //  The scale is what must be multiplied into the given coordinates to
        // get meters (SI units).  For example is x,y,z are in cm, then scale=.01
        //  The voltage is the desired real voltage difference between grids.
        // The internal kQ variable will be set to make it so.
        public EField(PointCharge[] charges, double scale, double vPlus, double vMinus)
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
            double phiPlus = posSum / n;
            double phiMinus = negSum / n;
            Console.WriteLine("Delta phi = {0:0.000e+00}", phiPlus - phiMinus);
            kQ = (vPlus-vMinus) / (phiPlus - phiMinus);
            phiInfinity = vPlus - kQ * phiPlus;
        }

        public Vector E(Vector x)
        {
            double ex = 0.0;
            double ey = 0.0;
            double ez = 0.0;
            foreach (Vector pt in posPoints)
            {
                Vector rVec = pt.VectorTo(x); // Repelled from pt.
                double r = rVec.Length();
                double f = kQ / (r * r * r);
                ex += rVec.x * f;
                ey += rVec.y * f;
                ez += rVec.z * f;
            }
            foreach (Vector pt in negPoints)
            {
                Vector rVec = x.VectorTo(pt); // Attracted to pt.
                double r = rVec.Length();
                double f = kQ / (r * r * r);
                ex += rVec.x * f;
                ey += rVec.y * f;
                ez += rVec.z * f;
            }
            return new Vector(ex, ey, ez);
        }

        public double Potential(Vector r)
        {
            return phiInfinity + 
                kQ * (PartialPotential(posPoints, r, -1) - PartialPotential(negPoints, r, -1));
        }

        double PartialPotential(Vector[] points, Vector r, int iExclude)
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
