using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using Whitmer;

namespace Efield    
{
    class efield1
    {
        static int nParticles = 0;
        static int nReps = 0;
        static uint seed = 0;
        static bool haveSeed = false;
        static TextWriter tw;
        static string outFile;
        static bool writeToConsole = true;
        static string xmlFileName;
        
        // Geometric parameters.
        static double a, b, r;
        static double wireDiameter;
        static PseudoDES pdes;
        static double c = 0;  // A cutoff for potential calculation.

        static volatile bool cancelWasReceived = false;

        static IShape[] posElements;
        static IShape[] negElements;

        static Particle[] posParticles;
        static Particle[] negParticles;

        static XmlDocument doc = new XmlDocument();

        static void Main(string[] args)
        {
            if (!ReadArgs(args)) return;
            if (xmlFileName == null)
            {
                Console.WriteLine("No XML configuration was provided.");
                return;
            }
            if (!xmlFileName.EndsWith(".xml", ignoreCase: true, culture: System.Globalization.CultureInfo.CurrentCulture))
                xmlFileName = xmlFileName + ".xml";
            
            if (outFile != null) tw = new StreamWriter(outFile);
            WriteLine("Command line: efield {0}", String.Join(" ", args));

            DateTime startTime = DateTime.Now;
            WriteLine(startTime.ToString());

            if (!haveSeed) seed = (uint)startTime.Ticks;
            pdes = new PseudoDES(0,seed);
            WriteLine("Seed = {0}", seed);

            try
            {
                SetupGeometryFromXml(xmlFileName);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: {0}", e.Message);
                return;
            }

            WriteLine("Cutoff c = {0:0.0000}", c);

            // Fill the geometry elements with particles.
            posParticles = new Particle[nParticles];
            negParticles = new Particle[nParticles];
            for (int i = 0; i < nParticles; i++)
            {
                posParticles[i] = PlaceParticleRandomly(posElements);
                negParticles[i] = PlaceParticleRandomly(negElements);
            }

            // Trap ^C
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            
            // Do the requested number of repetitions.
            for (int iRep = 0; iRep < nReps && !cancelWasReceived; iRep++)
            {
                WriteLine("Rep: {0}", iRep + 1);
                // Loop over all particles
                for (int i = 0; i < nParticles; i++)
                {
                    // A positive particle
                    double phi = Update(i, posParticles, negParticles, posElements, "+");
                    // A negative particle
                    phi = Update(i, negParticles, posParticles, negElements, "-");
                    if (cancelWasReceived) break;
                }
            }
            DateTime endTime = DateTime.Now;
            TimeSpan runTime = endTime - startTime;

            if (cancelWasReceived)
            {
                WriteLine("Received ^C, terminating early!");
            }

            WriteLine("Run time = {0:0.000} minutes", runTime.TotalMinutes);

            // Dump all the particles
            WriteLine("\nPositive particles");
            double sumPos = 0.0; 
            for (int i = 0; i < nParticles; i++)
            {
                Vector pos = posParticles[i].Position;
                double phi = ComputePotential(pos, posParticles, i) - ComputePotential(pos, negParticles, -1);
                sumPos += phi;
                WriteLine("{0,10:0.00000}{1,10:0.00000}{2,10:0.00000}  {3,11:0.000e+00}", pos.x, pos.y, pos.z, phi);
            }

            WriteLine("\nNegative particles");
            double sumNeg = 0.0;
            for (int i = 0; i < nParticles; i++)
            {
                Vector pos = negParticles[i].Position;
                double phi = ComputePotential(pos, posParticles, -1) - ComputePotential(pos, negParticles, i);
                sumNeg += phi;
                WriteLine("{0,10:0.00000}{1,10:0.00000}{2,10:0.00000}  {3,11:0.000e+00}", pos.x, pos.y, pos.z, phi);
            }
            WriteLine("{0,11:0.000e+00} {1,11:0.000e+00}", sumPos / nParticles, sumNeg / nParticles);
            if (tw != null) tw.Close();
        }

        static void SetupGeometryFromXml(string file)
        {
            try
            {
                doc.Load(xmlFileName);
            }
            catch (Exception e)
            {
                throw new Exception("Error reading XML file: " + e.Message);
            }

            XmlElement root = doc.DocumentElement;
            if (root == null)
                throw new Exception("No root node in XML!");
            XmlNodeList list = root.GetElementsByTagName("config");
            if (list.Count == 0)
                throw new Exception("No config section in XML root");
            if (list.Count > 1)
                throw new Exception("More than one config section in XML root");
            XmlElement config = (XmlElement)list[0];
            list = config.GetElementsByTagName("shapes");
            if (list.Count == 0)
                throw new Exception("No shapes section in XML config");
            if (list.Count > 1)
                throw new Exception("More than one shapes section in XML config");
            XmlElement shapes = (XmlElement)list[0];
            List<IShape> negatives = new List<IShape>();
            List<IShape> positives = new List<IShape>();
            foreach (XmlNode node in shapes.GetElementsByTagName("shape"))
            {
                Dictionary<string, string> dict = DictionaryFromXmlNode(node);
                if (!dict.ContainsKey("type") || !dict.ContainsKey("charge"))
                    throw new Exception("A shape must have a type and a charge");
                int charge;
                if (!int.TryParse(dict["charge"], out charge) || (charge != 1 && charge != -1))
                    throw new Exception("Invalid charge value");
                IShape shape = Shape.FromDictionary(dict, pdes);
                if (charge == 1)
                    positives.Add(shape);
                else
                    negatives.Add(shape);
            }
            negElements = negatives.ToArray();
            posElements = positives.ToArray();
            if (negElements.Length == 0 || posElements.Length == 0)
                throw new Exception("There must be both positive and negative shapes.");
            Console.WriteLine("Configuration has {0} positive and {1} negative shapes", posElements.Length, negElements.Length);
            return;
        }

        static Dictionary<string, string> DictionaryFromXmlNode(XmlNode node)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (XmlNode subnode in node.ChildNodes)
            {
                if (subnode.NodeType != XmlNodeType.Element || 
                    (subnode.HasChildNodes && (subnode.ChildNodes.Count != 1 || subnode.FirstChild.NodeType == XmlNodeType.Element)))
                    throw new Exception(String.Format("Shape has non-simple node: {0}", subnode.InnerXml));
                if (dict.ContainsKey(subnode.Name))
                    throw new Exception(String.Format("Value of {0} is multiply defined in a shape", subnode.Name));
                dict.Add(subnode.Name, subnode.InnerText);
            }
            return dict;
        }

        static void WriteLine(string format, params object[] args)
        {
            string line = String.Format(format, args);
            if (writeToConsole)
                Console.WriteLine(line);
            if (tw != null)
                tw.WriteLine(line);
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            cancelWasReceived = true;
            e.Cancel = true; // Resumes calculation.
        }

        static double Update(int i, Particle[] sameSignParticles, Particle[] oppositeSignParticles, IShape[] elems, string sign)
        {
            Vector oldPosition = sameSignParticles[i].Position;
            double startingPotential = ComputePotential(oldPosition, sameSignParticles, i)
                                        - ComputePotential(oldPosition, oppositeSignParticles, -1);
            Particle newParticle = PlaceParticleRandomly(elems);
            Vector newPosition = newParticle.Position;
            double newPotential = ComputePotential(newPosition, sameSignParticles, i)
                                        - ComputePotential(newPosition, oppositeSignParticles, -1);

            bool doMove = newPotential < startingPotential;

            if (doMove)
            {
                sameSignParticles[i] = newParticle;
                WriteLine(" Moved {0} particle {1,7} from {2,12:0.0000e+00} to {3,12:0.0000e+00} a distance of {4,9:0.0000}", 
                    sign, i, startingPotential, newPotential, Vector.Distance(newPosition,oldPosition));
                return newPotential;
            }
            else
            {
                return startingPotential;
            }
        }

        static double ComputePotential(Vector v, Particle[] particles, int omit)
        {
            double sum = 0.0;
            for (int i = 0; i < particles.Length; i++)
            {
                if (i != omit)
                {
                    Vector v2 = particles[i].Position;
                    double dx = v.x - v2.x;
                    double dy = v.y - v2.y;
                    double dz = v.z - v2.z;
                    double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz + c * c); // c provides a minimum distance.
                    sum += 1.0 / dist;
                }
            }
            return sum;
        }

        static Particle PlaceParticleRandomly(IShape[] elements)
        {
            int iElement = 0;
            if (elements.Length > 1)
                iElement = (int)Math.Floor(pdes.RandomDouble()*elements.Length);
            return new Particle(elements[iElement].RandomPoint(), iElement);
        }

        static void SetupGeometry_SpherePost3Ring()
        {
            // 
            double rSphere = 7.0;
            double wireDiameter = 0.15;
            double innerRatio = 0.3;
            Vector origin = new Vector(0.0, 0.0, 0.0);
            Vector xNeg = new Vector(-1.0, 0.0, 0.0);
            Vector xPos = new Vector(1.0, 0.0, 0.0);
            Vector yPos = new Vector(0.0, 1.0, 0.0);
            Vector zPos = new Vector(0.0, 0.0, 1.0);
            posElements = new IShape[1];
            posElements[0] = new Sphere(origin, xNeg, rSphere, 90.0, -50.0, pdes);
            negElements = new IShape[4];
            negElements[0] = new Torus(origin, zPos, innerRatio * rSphere, wireDiameter, pdes);
            negElements[1] = new Torus(origin, yPos, innerRatio * rSphere, wireDiameter, pdes);
            negElements[2] = new Torus(origin, xPos, innerRatio * rSphere, wireDiameter, pdes);
            negElements[3] = new Post(
                start: new Vector(innerRatio * rSphere, 0.0, 0.0),
                axis: new Vector(rSphere, 0.0, 0.0),
                wireDiameter: wireDiameter,
                pdes: pdes);
            WriteLine("Geometry (sphere post and ring): rSphere = {0:0.000}  d = {1:0.000}", rSphere, wireDiameter);
        }

        static void SetupGeometry_SpherePostRing()
        {
            // 
            double rSphere = 7.0;
            double wireDiameter = 0.4;
            Vector origin = new Vector(0.0, 0.0, 0.0);
            Vector xHatNeg = new Vector(-1.0, 0.0, 0.0);
            Vector zHat = new Vector(0.0, 0.0, 1.0);
            posElements = new IShape[1];
            posElements[0] = new Sphere(origin, xHatNeg, rSphere, 90.0, -50.0, pdes);
            negElements = new IShape[2];
            negElements[0] = new Torus(origin, zHat, 0.2 * rSphere, wireDiameter, pdes);
            negElements[1] = new Post(
                start: new Vector(0.2 * rSphere, 0.0, 0.0),
                axis: new Vector(rSphere, 0.0, 0.0),
                wireDiameter: wireDiameter,
                pdes: pdes);
            WriteLine("Geometry (sphere post and ring): rSphere = {0:0.000}  d = {1:0.000}", rSphere, wireDiameter);
        }


        static bool ReadArgs(string[] args)
        {
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-a":
                            if (++i == args.Length)
                                throw new Exception("No argument for -a");
                            if (!double.TryParse(args[i], out a) || a <= 0.0)
                                throw new Exception("Invalid geometry parameter a");
                            break;
                        case "-b":
                            if (++i == args.Length)
                                throw new Exception("No argument for -b");
                            if (!double.TryParse(args[i], out b) || b <= 0.0)
                                throw new Exception("Invalid geometry parameter b");
                            break;
                        case "-c":
                            if (++i == args.Length)
                                throw new Exception("No argument for -c");
                            if (!double.TryParse(args[i], out c) || c <= 0.0)
                                throw new Exception("Invalid geometry parameter c");
                            break;
                        case "-d":
                            if (++i == args.Length)
                                throw new Exception("No argument for -d");
                            if (!double.TryParse(args[i], out wireDiameter) || wireDiameter <= 0.0)
                                throw new Exception("Invalid geometry parameter d");
                            break;
                        case "-f":
                            if (++i == args.Length)
                                throw new Exception("No argument for -f");
                            xmlFileName = args[i];
                            break;
                        case "-r":
                            if (++i == args.Length)
                                throw new Exception("No argument for -r");
                            if (!double.TryParse(args[i], out r) || r <= 0.0)
                                throw new Exception("Invalid geometry parameter r");
                            break;
                        case "-n":
                            if (++i == args.Length)
                                throw new Exception("No argument for -n");
                            if (!int.TryParse(args[i], out nParticles) || nParticles <= 0)
                                throw new Exception("Invalid number of particles");
                            break;
                        case "-out":
                            if (++i == args.Length)
                                throw new Exception("No argument for -out");
                            outFile = args[i];
                            writeToConsole = false;
                            break;
                        case "-reps":
                            if (++i == args.Length)
                                throw new Exception("No argument for -reps");
                            if (!int.TryParse(args[i], out nReps) || nReps <= 0)
                                throw new Exception("Invalid number of repetitions");
                            break;
                        case "-seed":
                            if (++i == args.Length)
                                throw new Exception("No argument for -seed");
                            if (!uint.TryParse(args[i], out seed))
                                throw new Exception("Invalid random number seed");
                            haveSeed = true;
                            break;
                        case "-tee":
                            if (++i == args.Length)
                                throw new Exception("No argument for -tee");
                            outFile = args[i];
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
@"efield [switches] - Calculate the electric field produced by an arrangement of charged conductors",
@" -a <distance>        - Provides the horizontal separation in cm between centers of two rings.",
@" -b <distance>        - Provides the vertical separation in cm between two rings. Rings are perpendicular to the z-axis.",
@" -c <cutoff>          - Provides a minimum cutoff in potential calculations. Experimental.",
@" -d <diameter>        - Provides the wire diameter for the rings.",
@" -f <xmlfile>         - The filename for the configuration.",
@" -n <particles>       - Sets the number of positive and negative particles.",
@" -out <file>          - Sends the output to a file instead of the console.",
@" -r <radius>          - The radius in cm of each ring.",
@" -reps <repetitions>  - The requested number of cycles looking at all particles.",
@" -seed <seed>         - Sets a specific random seed to debug a run.",
@" -tee <file>          - Sends the output to a file in addition to the console.",
@" -?  - Prints this usage message."
            };

            foreach (string s in usage)
                Console.WriteLine(s);
        }
    }
}
