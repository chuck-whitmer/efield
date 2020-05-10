using System;

namespace Efield
{
    struct Vector
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
            double z1 = z*c-x*s;
            double x1 = z*s+x*c;
            double y1 = y;
            c = Math.Cos(phi);
            s = Math.Sin(phi);
            x = x1*c-y1*s;
            y = x1*s+y1*c;
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

        public double Length()
        {
            return Math.Sqrt(x * x + y * y + z * z);
        }

        // Normalize
        //  Return a vector which is parallel to the given vector, but with length 1.
        public Vector Normalize()
        {
            double len = Length();
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

        public Vector VectorTo(Vector p)
        {
            return new Vector(p.x - x, p.y - y, p.z - z);
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

        public static Vector operator *(Vector v, double e) => new Vector(e * v.x, e * v.y, e * v.z);
        public static Vector operator *(double e, Vector v) => new Vector(e * v.x, e * v.y, e * v.z);
        public static Vector operator +(Vector a, Vector b) => new Vector(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector operator -(Vector a, Vector b) => new Vector(a.x - b.x, a.y - b.y, a.z - b.z);


        public override string ToString()
        {
            return String.Format("({0,13:0.0000000000},{1,13:0.0000000000},{2,13:0.0000000000})", x, y, z);
        }
    }
}
