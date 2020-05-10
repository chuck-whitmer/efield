using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whitmer;

namespace Efield
{
    class Balancer
    {
        public Vector[] posParticles;
        public Vector[] negParticles;
        Surface anode;
        Surface cathode;
        
        public Balancer(int nParticles, Surface anode, Surface cathode)
        {
            this.anode = anode;
            this.cathode = cathode;
            posParticles = new Vector[nParticles];
            negParticles = new Vector[nParticles];
            // Fill the particle arrays randomly.
            for (int i=0; i<nParticles; i++)
            {
                posParticles[i] = anode.RandomPoint();
                negParticles[i] = cathode.RandomPoint();
            }
        }

        public void DoSteps(int nSteps)
        {
            int n = posParticles.Length;
            double charge = 1.0 / n;  // Total one Coulomb on each surface.
            for (int iStep=0; iStep<nSteps; iStep++)
            {
                // All the positive particles.
                for (int i = 0; i < n; i++)
                {
                    Vector x0 = posParticles[i];
                    double phi0 = EField.GetPotential(x0, posParticles, negParticles, charge);
                    Vector x1 = anode.RandomPoint();
                    posParticles[i] = x1;
                    double phi1 = EField.GetPotential(x1, posParticles, negParticles, charge);
                    // Put back the old position if the energy is not lowered.
                    if (phi1 > phi0)
                        posParticles[i] = x0;
                }
                // All the negative particles.
                for (int i = 0; i < n; i++)
                {
                    Vector x0 = negParticles[i];
                    double phi0 = EField.GetPotential(x0, posParticles, negParticles, charge);
                    Vector x1 = cathode.RandomPoint();
                    negParticles[i] = x1;
                    double phi1 = EField.GetPotential(x1, posParticles, negParticles, charge);
                    // Put back the old position if the energy is not lowered. (I.e. moved toward zero.)
                    if (phi1 < phi0)
                        negParticles[i] = x0;
                }
            }
        }

        public double[] GetAveragePotentials()
        {
            int n = posParticles.Length;
            double pSum = 0.0;
            double pSum2 = 0.0;
            double charge = 1.0 / n;  // Total one Coulomb on each surface.
            foreach (Vector x in posParticles)
            {
                double phi = EField.GetPotential(x, posParticles, negParticles, charge);
                pSum += phi;
                pSum2 += phi * phi;
            }
            double nSum = 0.0;
            double nSum2 = 0.0;
            foreach (Vector x in negParticles)
            {
                double phi = EField.GetPotential(x, posParticles, negParticles, charge);
                nSum += phi;
                nSum2 += phi * phi;
            }
            double pAvg = pSum / n;
            double pVar = (pSum2 - n * pAvg * pAvg) / (n - 1);
            double pSdev = Math.Sqrt(pVar);
            double nAvg = nSum / n;
            double nVar = (nSum2 - n * nAvg * nAvg) / (n - 1);
            double nSdev = Math.Sqrt(nVar);
            return new double[] { pAvg, pSdev, nAvg, nSdev };
        }
    }
}
