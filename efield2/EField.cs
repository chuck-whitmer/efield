namespace Efield
{
    class EField
    {
        static double ke = 8.9875517923e9;  // Coulomb constant in Volt-meters/Coulomb.

        // Compute the potential in volts at a given position x, given arrays of positive 
        // and negative particles.
        // An exact match of the x vector with a particle position is considered
        // not a coincidence, and self-energy is excluded.
        // Each particle has a charge q, in Coulombs.
        public static double GetPotential(Vector x, Vector[] pos, Vector[] neg, double q)
        {
            double sum = 0.0;
            foreach (Vector y in pos)
            {
                double r = Vector.Distance(x, y);
                if (r != 0.0) sum += 1.0 / r;
            }
            foreach (Vector y in neg)
            {
                double r = Vector.Distance(x, y);
                if (r != 0.0) sum += -1.0 / r;
            }
            return ke * sum * q;
        }
    }
}
