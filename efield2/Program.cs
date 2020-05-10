using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whitmer;

namespace Efield
{
    class Program
    {
        static string posFileName = "anode.stl";
        static string negFileName = "cathode.stl";
        static int nParticles = 100;
        static string outFile = null;
        static TextWriter tw;
        static bool writeToConsole = true;
        static int nReps = 100;
        static uint seed;
        static bool haveSeed = false;
        static bool cancelWasReceived = false;
        static double x0, x1;
        static int xN = 0;

        static void Main(string[] args)
        {
            if (!ReadArgs(args)) return;
            try
            {
                if (outFile != null) tw = new StreamWriter(outFile);
                //WriteLine("Command line: efield {0}", String.Join(" ", args));
                WriteLine("Command line: {0}", Environment.CommandLine);
                WriteLine("Run by: {0}\\{1} on {2}", Environment.UserDomainName, Environment.UserName, Environment.MachineName);

                DateTime startTime = DateTime.Now;
                WriteLine(startTime.ToString());

                if (!haveSeed) seed = (uint)startTime.Ticks;
                PseudoDES rand = new PseudoDES(0,seed);
                WriteLine("Seed = {0}", seed);

                if (!File.Exists(posFileName))
                    throw new Exception(string.Format("Anode description file {0} not found", posFileName));
                if (!File.Exists(negFileName))
                    throw new Exception(string.Format("Cathode description file {0} not found", negFileName));

                Surface anode = new Surface(new StlFile(posFileName), rand);
                Surface cathode = new Surface(new StlFile(negFileName), rand);

                WriteLine("Anode: {0}  Triangles: {1}   Area: {2:0.0000} m^2", posFileName, anode.TriangleCount, anode.TotalArea);
                WriteLine("Cathode: {0}  Triangles: {1}   Area: {2:0.0000} m^2", negFileName, cathode.TriangleCount, cathode.TotalArea);

                // Trap ^C
                Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

                // Create the Balancer.
                Balancer baal = new Balancer(nParticles, anode, cathode);

                // Let's see the starting potentials.
                double[] pots = baal.GetAveragePotentials();
                WriteLine("Steps: {0,5}   Potentials: {1:0.00e00} {2:0.00e00}  {3:0.00e00} {4:0.00e00}", 0, pots[0], pots[1], pots[2], pots[3]);

                int batch = 10;
                for (int nDone=0; nDone<nReps; nDone+=batch)
                {
                    // Run some steps.
                    baal.DoSteps(Math.Min(batch,nReps-nDone));
                    // And report.
                    pots = baal.GetAveragePotentials();
                    WriteLine("Steps: {0,5}   Potentials: {1:0.00e00} {2:0.00e00}  {3:0.00e00} {4:0.00e00}", 
                        Math.Min(nDone+batch,nReps), pots[0], pots[1], pots[2], pots[3]);
                    if (cancelWasReceived) break;
                }
                DateTime endTime = DateTime.Now;
                TimeSpan runTime = endTime - startTime;

                if (cancelWasReceived)
                    WriteLine("Received ^C, terminating early!");

                WriteLine("Run time = {0:0.000} minutes", runTime.TotalMinutes);
                double potentialDifference = pots[0] - pots[2];
                double posSdev = 100.0 * pots[1] / Math.Abs(pots[0]);
                double negSdev = 100.0 * pots[3] / Math.Abs(pots[2]);
                double capacitance = 1e12 / potentialDifference; // in picoFarads
                WriteLine("Final potentials: {0:0.00e00} ({1:0.0}%)  {2:0.00e00} ({3:0.0}%)", pots[0], posSdev, pots[2], negSdev);
                WriteLine("Final potential difference: {0:0.00e00}", potentialDifference);
                WriteLine("Capacitance: {0:0.0} pF", capacitance);

                // If writing to a file, dump the final particle positions
                if (tw != null)
                {
                    if (xN > 0)
                    {
                        double step = (x1 - x0) / xN;
                        double charge = 1.0 / baal.posParticles.Length;
                        tw.WriteLine("Scan along X-axis");
                        for (int i=0; i<=xN; i++)
                        {
                            double x = x0 + i * step; // x in mm
                            double phi = EField.GetPotential(new Vector(x*0.001, 0.0, 0.0), baal.posParticles, baal.posParticles, charge);
                            tw.WriteLine("{0,10:0.00}  {1:0.000e00}", x, phi);
                        }
                    }


                    int n = baal.posParticles.Length;
                    tw.WriteLine("particles {0} {0}", n);
                    foreach (Vector x in baal.posParticles)
                        tw.WriteLine("{0,10:0.0000}{1,10:0.0000}{2,10:0.0000}", x.x, x.y, x.z);
                    foreach (Vector x in baal.negParticles)
                        tw.WriteLine("{0,10:0.0000}{1,10:0.0000}{2,10:0.0000}", x.x, x.y, x.z);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }
            if (tw != null) tw.Close();
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            cancelWasReceived = true;
            e.Cancel = true; // Resumes calculation.
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
                        case "-fpos":
                            if (++i == args.Length)
                                throw new Exception("No argument for -fpos");
                            posFileName = args[i];
                            break;
                        case "-fneg":
                            if (++i == args.Length)
                                throw new Exception("No argument for -fneg");
                            negFileName = args[i];
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
                            if (!int.TryParse(args[i], out nReps) || nReps < 0)
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
                        case "-X":
                            if (++i == args.Length)
                                throw new Exception("No argument for -X");
                            string[] limits = args[i].Split(new char[] { ',' });
                            x0 = double.Parse(limits[0]);
                            x1 = double.Parse(limits[1]);
                            xN = int.Parse(limits[2]);
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
@" -fpos <stlfile>      - STL description of the anode.",
@" -fneg <stlfile>      - STL description of the cathode.",
@" -n <particles>       - Sets the number of positive and negative particles.",
@" -out <file>          - Sends the output to a file instead of the console.",
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
