using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Whitmer;

namespace Efield
{
    interface IElement
    {
        Vector RandomPoint();
    }

    class Ring : IElement
    {
        public Vector Center { get; private set; }
        public Vector Axis { get; private set; }
        public double Radius { get; private set; }
        Vector perpX, perpY;
        PseudoDES pd;

        public Ring(Vector center, Vector axis, double radius, PseudoDES pdes)
        {
            Center = center;
            Axis = axis;
            Radius = radius;
            pd = pdes;
            Vector[] perps = Axis.GetPerpendiculars();
            perpX = perps[0];
            perpY = perps[1];
        }

        public Vector RandomPoint()
        {
            double theta = 2.0 * Math.PI * pd.RandomDouble();
            double c = Radius * Math.Cos(theta);
            double s = Radius * Math.Sin(theta);
            return new Vector(Center.x + c * perpX.x + s * perpY.x,
                              Center.y + c * perpX.y + s * perpY.y,
                              Center.z + c * perpX.z + s * perpY.z);
        }
    }

    class Torus : IElement
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

    class Post : IElement
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
    class Sphere : IElement
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
