using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Orbit
{
    class orbit
    {
        static TextWriter tw;
        static string outFile;
        static bool writeToConsole = true;
        static string jsonFileName;
        static Vector[] positiveCharges;
        static Vector[] negativeCharges;
        static double voltage;

        static void Main(string[] args)
        {
            if (!ReadArgs(args)) return;

            if (outFile != null) tw = new StreamWriter(outFile);
            WriteLine("Command line: orbit {0}", String.Join(" ", args));
            DateTime startTime = DateTime.Now;
            WriteLine(startTime.ToString());
            if (jsonFileName == null)
            {
                WriteLine("No JSON input was provided.");
                return;
            }

            List<Vector> gcPosList = new List<Vector>();
            List<Vector> gcNegList = new List<Vector>();
            JArray jj = JArray.Parse(File.ReadAllText(jsonFileName));
            foreach (JObject j in jj)
            {
                int charge = (int)j["charge"];
                if (charge==1)
                    gcPosList.Add(new Vector((double)j["x"], (double)j["y"], (double)j["z"]));
                else
                    gcNegList.Add(new Vector((double)j["x"], (double)j["y"], (double)j["z"]));
            }
            positiveCharges = gcPosList.ToArray();
            negativeCharges = gcNegList.ToArray();

            WriteLine("Have {0} positive and {1} negative charges.", positiveCharges.Length, negativeCharges.Length);

            // Get the delta phi for the given points.
            double phiPlus = 0.0;
            for (int i = 0; i < positiveCharges.Length; i++)
            {
                phiPlus += Potential(positiveCharges[i], positiveCharges, i)
                    - Potential(positiveCharges[i], negativeCharges);
            }



            tw.Close();
        }

        static public double Potential(Vector x, Vector[] pts, int omit=-1)
        {
            double sum = 0.0;
            for (int i=0; i<pts.Length; i++)
            {
                if (i != omit)
                    sum += 1 / Vector.Distance(x, pts[i]);
            }
            return sum;
        }

        static void WriteLine(string format, params object[] args)
        {
            string line = String.Format(format, args);
            if (writeToConsole)
                Console.WriteLine(line);
            if (tw != null)
                tw.WriteLine(line);
        }

        static bool ReadArgs(string[] args)
        {
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-f":
                            if (++i == args.Length)
                                throw new Exception("No argument for -f");
                            jsonFileName = args[i];
                            break;
                        case "-tee":
                            if (++i == args.Length)
                                throw new Exception("No argument for -tee");
                            outFile = args[i];
                            break;
                        case "-v":
                            if (++i == args.Length)
                                throw new Exception("No argument for -v");
                            if (!double.TryParse(args[i],out voltage))
                                throw new Exception("Invalid argument for -v");
                            break;
                        case "-?":
                            PrintUsage();
                            return false;
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

        static void PrintUsage()
        {
            string[] usage = {
@"orbit [switches] - Calculate the orbit of a charged particle in an electric field produced by an arrangement of charged conductors",
@" -f <jsonfile>         - The filename for the configuration.",
@" -tee <file>          - Sends the output to a file in addition to the console.",
@" -?  - Prints this usage message."
            };

            foreach (string s in usage)
                Console.WriteLine(s);
        }
    }

    class Vector
    {
        public double x, y, z;

        public Vector(double _x, double _y, double _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public void Rotate(double theta, double phi)
        {
            // Rotate a point by theta around the y axis, then phi around the z axis.
            double c = Math.Cos(theta);
            double s = Math.Sin(theta);
            double z1 = z * c - x * s;
            double x1 = z * s + x * c;
            double y1 = y;
            c = Math.Cos(phi);
            s = Math.Sin(phi);
            x = x1 * c - y1 * s;
            y = x1 * s + y1 * c;
            z = z1;
        }

        public void Offset(double dx, double dy, double dz)
        {
            x += dx;
            y += dy;
            z += dz;
        }

        // Distance
        //  Return the distance between two points represented as vectors.
        public static double Distance(Vector v1, Vector v2)
        {
            double dx = v1.x - v2.x;
            double dy = v1.y - v2.y;
            double dz = v1.z - v2.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // Normalize
        //  Return a vector which is parallel to the given vector, but with length 1.
        public Vector Normalize()
        {
            double len = Math.Sqrt(x * x + y * y + z * z);
            return new Vector(x / len, y / len, z / len);
        }

        // Cross
        //  Return the cross product of two vectors.
        public static Vector Cross(Vector v1, Vector v2)
        {
            return new Vector(v1.y * v2.z - v1.z * v2.y,
                              v1.z * v2.x - v1.x * v2.z,
                              v1.x * v2.y - v1.y * v2.x);
        }

        // Dot
        //  Return the dot product of two vectors.
        public static double Dot(Vector v1, Vector v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        public static Vector Difference(Vector v1, Vector v2)
        {
            return new Vector(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        // GetPerpendiculars
        //  Return two mutually orthogonal vectors of length 1 that are orthogonal to the given vector.
        public Vector[] GetPerpendiculars()
        {
            Vector[] vecs = new Vector[2];
            double r = Math.Sqrt(x * x + y * y);
            double len = Math.Sqrt(x * x + y * y + z * z);
            double zNorm = z / len;
            if (r < 1e-14)
            {
                vecs[0] = new Vector(zNorm, 0.0, 0.0);
                vecs[1] = new Vector(0.0, zNorm, 0.0);
            }
            else
            {
                vecs[0] = new Vector(x * zNorm / r, y * zNorm / r, -r / len);
                vecs[1] = new Vector(-y / r, x / r, 0.0);
            }
            return vecs;
        }

        public override string ToString()
        {
            return String.Format("({0,13:0.0000000000},{1,13:0.0000000000},{2,13:0.0000000000})", x, y, z);
        }
    }

}
