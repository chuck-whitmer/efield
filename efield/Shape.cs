using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Whitmer;

namespace Efield
{
    interface IShape
    {
        Vector RandomPoint();
    }

    abstract class Shape : IShape
    {
        protected string type;
        protected PseudoDES pd;

        protected Shape()
        { }

        static public Shape FromDictionary(Dictionary<string, string> dict, PseudoDES pd)
        {
            Shape shape = null;
            switch (dict["type"])
            {
                case "TorusSegment":
                    shape = new TorusSegment(dict);
                    break;
                default:
                    throw new Exception(String.Format("Unknown shape type {0}", dict["type"]));
            }
            shape.pd = pd;
            return shape;
        }

        protected double ReadDouble(Dictionary<string, string> dict, string arg)
        {
            if (!dict.ContainsKey(arg))
                throw new Exception(String.Format("A {0} requires a value for {1}",type,arg));
            double x;
            if (!double.TryParse(dict[arg],out x))
                throw new Exception(String.Format("Invalid value {0} for {1} in {2} definition.",dict[arg],arg,type));
            return x;
        }

        protected double ReadAngle(Dictionary<string, string> dict, string arg)
        {
            if (!dict.ContainsKey(arg))
                throw new Exception(String.Format("A {0} requires a value for {1}",type,arg));
            string angle = dict[arg];
            bool hasPi = false;

            string[] words = angle.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length==2)
            {
                if (String.Compare(words[0], "pi", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    angle = words[1];
                    hasPi = true;
                }
                else if (String.Compare(words[1], "pi", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    angle = words[0];
                    hasPi = true;
                }
                else
                {
                    throw new Exception(String.Format("Invalid value {0} for {1} in {2} definition.", dict[arg], arg, type));
                }
            }
            else if (words.Length != 1)
            {
                throw new Exception(String.Format("Invalid value {0} for {1} in {2} definition.", dict[arg], arg, type));
            }
            double x;
            if (!double.TryParse(angle,out x))
                throw new Exception(String.Format("Invalid value {0} for {1} in {2} definition.",dict[arg],arg,type));
            if (hasPi) x *= Math.PI;
            return x;
        }

        public abstract Vector RandomPoint();
    }

    class TorusSegment : Shape
    {
        double radius, radius2, theta, phi, phi2, phi3, x, y, z;

        public TorusSegment(Dictionary<string, string> dict)
        {
            type = "TorusSegment";
            radius = ReadDouble(dict, "radius");
            radius2 = ReadDouble(dict, "radius2");
            theta = ReadAngle(dict, "theta");
            phi = ReadAngle(dict, "phi");
            phi2 = ReadAngle(dict, "phi2");
            phi3 = ReadAngle(dict, "phi3");
            x = ReadDouble(dict, "x");
            y = ReadDouble(dict, "y");
            z = ReadDouble(dict, "z");
        }

        public override Vector RandomPoint()
        {
            double a1 = 2.0 * Math.PI * pd.RandomDouble();
            double a2 = (phi3-phi2) * pd.RandomDouble() + phi2;
            double r1 = radius + radius2 * Math.Cos(a2);
            double ax = r1 * Math.Cos(a1);
            double ay = r1 * Math.Sin(a1);
            double az = radius2 * Math.Sin(a2);
            Vector v = new Vector(ax, ay, az);
            v.Rotate(theta, phi);
            v.Offset(x, y, z);
            return v;
        }
    }

    class Torus : IShape
    {
        public Vector Center { get; private set; }
        public Vector Axis { get; private set; }
        public double Radius { get; private set; }
        public double WireRadius { get; private set; }
        Vector perpX, perpY;
        PseudoDES pd;

        public Torus(Vector center, Vector axis, double radius, double wireDiameter, PseudoDES pdes)
        {
            Center = center;
            Radius = radius;
            WireRadius = wireDiameter / 2.0;
            pd = pdes;
            Vector[] perps = axis.GetPerpendiculars();
            Axis = axis.Normalize();
            perpX = perps[0];
            perpY = perps[1];
        }

        public Vector RandomPoint()
        {
            // Here's the math. Theta and phi are random angles.
            // unitRadial = cos(theta)*perpX + sin(theta)*perpY
            // result = center + Radius*unitRadial + WireRadius*(cos(phi)*unitRadial+sin(phi)*Axis)
            double theta = 2.0 * Math.PI * pd.RandomDouble();
            double phi = 2.0 * Math.PI * pd.RandomDouble();

            double r1 = Radius + WireRadius * Math.Cos(phi);
            double ax = r1 * Math.Cos(theta);
            double ay = r1 * Math.Sin(theta);
            double az = WireRadius * Math.Sin(phi);

            return new Vector(Center.x + ax * perpX.x + ay * perpY.x + az * Axis.x,
                              Center.y + ax * perpX.y + ay * perpY.y + az * Axis.y,
                              Center.z + ax * perpX.z + ay * perpY.z + az * Axis.z);
        }
    }

    class Post : IShape
    {
        public Vector Start { get; private set; }
        public Vector Axis { get; private set; }
        public double WireRadius { get; private set; }
        Vector perpX, perpY;
        PseudoDES pd;

        public Post(Vector start, Vector axis, double wireDiameter, PseudoDES pdes)
        {
            Start = start;
            WireRadius = wireDiameter / 2.0;
            pd = pdes;
            Vector[] perps = axis.GetPerpendiculars();
            Axis = axis;
            perpX = perps[0];
            perpY = perps[1];
        }

        public Vector RandomPoint()
        {
            double az = pd.RandomDouble();
            double phi = 2.0 * Math.PI * pd.RandomDouble();

            double ax = WireRadius * Math.Cos(phi);
            double ay = WireRadius * Math.Sin(phi);

            return new Vector(Start.x + ax * perpX.x + ay * perpY.x + az * Axis.x,
                              Start.y + ax * perpX.y + ay * perpY.y + az * Axis.y,
                              Start.z + ax * perpX.z + ay * perpY.z + az * Axis.z);
        }
    }

    // Sphere - a section of a sphere.
    //
    // The axis points toward the north pole from the center.  The length of the axis is not used.
    // The north and south parameters describe the extent of the latitude of the spherical shell, in degrees.
    // Setting north:90 and south:-90 gets a whole sphere.  
    // Setting north:90 and south:0 get a hemisphere.  And so on.
    class Sphere : IShape
    {
        public Vector Center { get; private set; }
        public Vector Axis { get; private set; }
        public double Radius { get; private set; }
        public double North { get; private set; }
        public double South { get; private set; }
        Vector perpX, perpY;
        PseudoDES pd;
        double cos1, cos2; // Extents of cos(theta) where theta is measured from the north pole.

        public Sphere(Vector center, Vector axis, double radius, double north, double south, PseudoDES pdes)
        {
            Center = center;
            Radius = radius;
            pd = pdes;
            Vector[] perps = axis.GetPerpendiculars();
            Axis = axis.Normalize();
            perpX = perps[0];
            perpY = perps[1];
            if (north > 90.0 || south < -90.0 || south >= north)
                throw new ArgumentOutOfRangeException("Invalid latitude cutoff");
            cos1 = (south == -90.0) ? -1.0 : Math.Sin(south * Math.PI / 180.0);
            cos2 = (north == 90.0) ? 1.0 : Math.Sin(north * Math.PI / 180.0);
        }

        public Vector RandomPoint()
        {
            double cosTheta = cos1 + (cos2 - cos1) * pd.RandomDouble();
            double sinTheta = Math.Sqrt(1.0 - cosTheta * cosTheta);
            double phi = 2.0 * Math.PI * pd.RandomDouble();

            double ax = Radius * Math.Cos(phi) * sinTheta;
            double ay = Radius * Math.Sin(phi) * sinTheta;
            double az = Radius * cosTheta;

            return new Vector(Center.x + ax * perpX.x + ay * perpY.x + az * Axis.x,
                              Center.y + ax * perpX.y + ay * perpY.y + az * Axis.y,
                              Center.z + ax * perpX.z + ay * perpY.z + az * Axis.z);
        }
    }
}
