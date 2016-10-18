using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whitmer;

namespace Efield
{
    class efieldtest
    {
        static uint seed = 0;
        static bool haveSeed = false;
        static int nVectors = 100;

        static void Main(string[] args)
        {
            bool allPassed = true;
            if (!ReadArgs(args)) return;

            if (!haveSeed) seed = (uint)DateTime.UtcNow.Ticks;
            PseudoDES pdes = new PseudoDES(0, seed);
            Console.WriteLine("Random seed = {0}", seed);
            if (nVectors < 11) nVectors = 11;
            Console.WriteLine("Vectors = {0}", nVectors);

            // Run the PseudoDES test
            if (pdes.Test())
            {
                Console.WriteLine("PseudoDES test passed");
            }
            else
            {
                Console.WriteLine("PseudoDES test FAILED");
                allPassed = false;
            }

            Vector[] vecs = new Vector[nVectors];
            vecs[0] = new Vector(1.0, 0.0, 0.0);
            vecs[1] = new Vector(0.0, 1.0, 0.0);
            vecs[2] = new Vector(0.0, 0.0, 1.0);
            vecs[3] = new Vector(1.0, 0.0, 0.0);
            vecs[4] = new Vector(0.0, 1.0, 0.0);
            vecs[5] = new Vector(0.0, 0.0, 1.0);
            vecs[6] = new Vector(1e-15, 1e-11, 1.0);
            vecs[7] = new Vector(1e-11, 1e-15, -1.0);
            vecs[8] = new Vector(1e-15, 1e-11, 1.01);
            vecs[9] = new Vector(1e-11, 1e-15, -1.01);
            // Fill the rest with random points.
            for (int i = 10; i < vecs.Length; i++)
                vecs[i] = new Vector(pdes.RandomDouble(), pdes.RandomDouble(), pdes.RandomDouble());

            // Test the vector operations.
            bool vectorsPassed = true;
            for (int i = 0; i < vecs.Length; i++)
            {
                Vector v = vecs[i];
                bool passed = true;
                Vector[] perps = v.GetPerpendiculars();
                // All three vectors are perpendicular.
                passed = IsSmall(Vector.Dot(perps[0], perps[1]), 1e-13, "p0.p1");
                passed = passed && IsSmall(Vector.Dot(perps[0], v), 1e-13, "p0.v");
                passed = passed && IsSmall(Vector.Dot(v, perps[1]), 1e-13, "v.p1");
                // The computed vectors have length 1.
                passed = passed && IsSmall(Vector.Dot(perps[0], perps[0]) - 1.0, 1e-13, "p0.p0 - 1");
                passed = passed && IsSmall(Vector.Dot(perps[1], perps[1]) - 1.0, 1e-13, "p1.p1 - 1");
                // The vectors form a right-handed system.
                double len = Math.Sqrt(Vector.Dot(v, v));
                Vector cross = Vector.Cross(perps[0], perps[1]);
                passed = passed && IsSmall(Vector.Dot(cross, v) / len - 1.0, 1e-13, "(p0xp1.v)/sqrt(v.v) - 1");

                if (!passed)
                {
                    Console.WriteLine("Vector test FAILED for i={0}", i);
                    vectorsPassed = false;
                    Console.WriteLine("  v  = {0}", v);
                    Console.WriteLine("  p0 = {0}", perps[0]);
                    Console.WriteLine("  p1 = {0}", perps[1]);
                }
            }
            if (vectorsPassed)
            {
                Console.WriteLine("Vector test passed");
            }
            else
            {
                Console.WriteLine("Vector test FAILED");
                allPassed = false;
            }

            if (allPassed)
            {
                Console.WriteLine("All unit tests passed");
            }
            else
            {
                Console.WriteLine("Some unit tests FAILED");
            }
        }

        static bool IsSmall(double x, double err, string msg)
        {
            bool pass = Math.Abs(x) < err;
            if (!pass)
                Console.WriteLine("  {0} is {1:0.000e+00} and not small", msg, x);
            return pass;
        }

        static bool ReadArgs(string[] args)
        {
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-seed":
                            if (++i == args.Length)
                                throw new Exception("No argument for -seed");
                            if (!uint.TryParse(args[i], out seed))
                                throw new Exception("Invalid random number seed");
                            haveSeed = true;
                            break;
                        case "-vectors":
                            if (++i == args.Length)
                                throw new Exception("No argument for -vectors");
                            if (!int.TryParse(args[i], out nVectors) || nVectors <= 0)
                                throw new Exception("Invalid number of vectors");
                            break;
                        default:
                            throw new Exception("Invalid command line switch");
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on command line: {0}", e.Message);
            }
            return false;
        }

    }
}
