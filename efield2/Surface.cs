using System;
using System.Collections.Generic;
using Whitmer;

namespace Efield
{
    struct SurfTriangle
    {
        // This is a surface triangle.
        // Dimensions are assumed to be meters.
        Vector A;
        Vector AtoB;
        Vector AtoC;

        // Process a triangle given a face from an STL file.
        public SurfTriangle(Face f)
        {
            // We assume STL files are in millimeters.
            A = f.Pt1 * 0.001;
            AtoB = f.Pt2 * 0.001 - A;
            AtoC = f.Pt3 * 0.001 - A;
        }

        // Compute the area of the triangle.
        public double Area { get { return Vector.Cross(AtoB, AtoC).Length() / 2.0; } }

        // Return a random point on the triangle given a random generator.
        public Vector RandomPoint(PseudoDES rand)
        {
            double r1 = rand.RandomDouble();
            double r2 = rand.RandomDouble();
            if (r1 + r2 > 1.0)
            {
                r1 = 1.0 - r1;
                r2 = 1.0 - r2;
            }
            return A + r1 * AtoB + r2 * AtoC;
        }
    }

    // Represents a surface in a 3D space.
    // We load it from an STL file.
    // We are able to generate a random point on the surface.
    class Surface
    {
        SurfTriangle[] triangles;
        double[] cummulativeAreas;
        PseudoDES rand;

        public int TriangleCount { get { return triangles.Length; } }
        public double TotalArea { get; private set; }

        public Surface(StlFile stl, PseudoDES rand)
        {
            this.rand = rand;
            List<SurfTriangle> triangleList = new List<SurfTriangle>();
            foreach (Face f in stl)
            {
                triangleList.Add(new SurfTriangle(f));
            }
            triangles = triangleList.ToArray();
            double sum = 0.0;
            List<double> areas = new List<double>();
            foreach (SurfTriangle t in triangles)
            {
                sum += t.Area;
                areas.Add(sum);
            }
            cummulativeAreas = areas.ToArray();
            TotalArea = sum;
        }

        public Vector RandomPoint()
        {
            double area = TotalArea * rand.RandomDouble();
            int idx = Array.BinarySearch(cummulativeAreas, area);
            if (idx < 0) idx = ~idx;
            return triangles[idx].RandomPoint(rand);
        }
    }
}
