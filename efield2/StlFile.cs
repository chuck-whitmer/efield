using System;
using System.Collections.Generic;
using System.IO;

namespace Efield
{
    struct Face
    {
        public Vector Normal;
        public Vector Pt1;
        public Vector Pt2;
        public Vector Pt3;
        public UInt16 Attribute;

        public Face(Vector n, Vector p1, Vector p2, Vector p3, UInt16 a)
        {
            Normal = n;
            Pt1 = p1;
            Pt2 = p2;
            Pt3 = p3;
            Attribute = a;
        }
    }

    class StlFile : IEnumerable<Face>
    {
        public bool IsAscii { get; private set; }
        public long Count { get; private set; }
        public string Title { get; private set; }
        public char[] HeaderChars { get; private set; }

        BinaryReader binary;
        StreamReader text;

        public StlFile(string filename) : this(new FileStream(filename, FileMode.Open))
        {
        }

        public StlFile(Stream s)
        {
            BinaryReader br = new BinaryReader(s);
            string h = new string(br.ReadChars(6));
            IsAscii = false;
            if (h.Substring(0, 6) == "solid ")
            {
                StreamReader sr = new StreamReader(s);
                string line1 = sr.ReadLine();
                if (line1.Length < 80)
                {
                    string line2 = sr.ReadLine();
                    if (line2.Trim().StartsWith("facet "))
                        IsAscii = true;
                }
            }

            br.BaseStream.Seek(0, SeekOrigin.Begin);
            if (IsAscii)
            {
                SetupAsciiFile(s);
            }
            else
            {
                SetupBinaryFile(s);
            }
        }

        void SetupAsciiFile(Stream s)
        {
            text = new StreamReader(s);
            Title = text.ReadLine();
            Count = 0;
        }

        void SetupBinaryFile(Stream s)
        {
            binary = new BinaryReader(s);
            HeaderChars = binary.ReadChars(80);
            int headerLen = 0;
            for (; headerLen < HeaderChars.Length; headerLen++)
            {
                if (HeaderChars[headerLen] == '\0') break;
            }
            Title = new string(HeaderChars, 0, headerLen);
            Count = binary.ReadUInt32();
        }

        #region IEnumerable<Face> Members

        public IEnumerator<Face> GetEnumerator()
        {
            if (IsAscii)
                return new AsciiFileEnumerator(text);
            else
                return new BinaryFileEnumerator(binary, Count);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            if (IsAscii)
                return (System.Collections.IEnumerator)new AsciiFileEnumerator(text);
            else
                return (System.Collections.IEnumerator)new BinaryFileEnumerator(binary, Count);
        }

        #endregion
    }

    class AsciiFileEnumerator : IEnumerator<Face>
    {
        StreamReader text;
        bool AtEOF = false;
        int count = 0;
        Face face;
        long fileSize;
        long roughPosition;
        public double PercentComplete { get; private set; }

        public AsciiFileEnumerator(StreamReader sr)
        {
            text = sr;
            roughPosition = 0;
            fileSize = text.BaseStream.Length;
        }

        public Face Current { get { return face; } }

        public void Dispose()
        {
        }

        object System.Collections.IEnumerator.Current
        {
            get { return (object)Current; }
        }

        char[] space = { ' ' };

        public bool MoveNext()
        {
            bool success = false;

            if (AtEOF) return false;
            try
            {
                Vector[] pts = new Vector[3];
                double x, y, z;
                // facet normal ni nj nk
                string line = text.ReadLine();
                roughPosition += line.Length;
                string[] words = line.Split(space, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length <= 2 && words[0] == "endsolid")
                {
                    line = text.ReadLine();
                    if (line == null)
                    {
                        AtEOF = true;
                        return false;
                    }
                    words = line.Split(space, StringSplitOptions.RemoveEmptyEntries);

                    if (words.Length >= 1 && words[0] == "solid")
                    {
                        line = text.ReadLine();
                        words = line.Split(space, StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        AtEOF = true;
                        return false;
                    }
                }

                if (words.Length != 5 || words[0] != "facet" || words[1] != "normal"
                    || !double.TryParse(words[2], out x)
                    || !double.TryParse(words[3], out y)
                    || !double.TryParse(words[4], out z)
                    )
                    throw (new Exception("File format error"));
                face.Normal.x = x;
                face.Normal.y = y;
                face.Normal.z = z;

                // outer loop
                line = text.ReadLine();
                roughPosition += line.Length;
                words = line.Split(space, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length != 2 || words[0] != "outer" || words[1] != "loop")
                    throw (new Exception("File format error"));

                // vertex v1x v1y v1z
                for (int i = 0; i < 3; i++)
                {
                    line = text.ReadLine();
                    roughPosition += line.Length;
                    words = line.Split(space, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length != 4 || words[0] != "vertex"
                        || !double.TryParse(words[1], out x)
                        || !double.TryParse(words[2], out y)
                        || !double.TryParse(words[3], out z)
                        )
                        throw (new Exception("File format error"));
                    pts[i].x = x;
                    pts[i].y = y;
                    pts[i].z = z;
                }
                face.Pt1 = pts[0];
                face.Pt2 = pts[1];
                face.Pt3 = pts[2];

                // endloop
                // endfacet
                if (text.ReadLine().Trim() != "endloop" || text.ReadLine().Trim() != "endfacet")
                    throw (new Exception("File format error"));
                roughPosition += 20;

                face.Attribute = 0;
                success = true;
                count++;
                PercentComplete = ((double)roughPosition) / fileSize;
            }
            catch (EndOfStreamException)
            {
                AtEOF = true;
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("After {0} facets: ", count), e);
            }
            return success;
        }

        public void Reset()
        {
            text.BaseStream.Seek(0, SeekOrigin.Begin);
            text.DiscardBufferedData();
            text.ReadLine();
            count = 0;
            AtEOF = false;
            roughPosition = 0;
            PercentComplete = 0.0;
        }
    }

    class BinaryFileEnumerator : IEnumerator<Face>
    {
        BinaryReader binary;
        long count;
        long totalCount;
        Face face;
        public double PercentComplete { get; private set; }

        public Face Current { get { return face; } }

        public BinaryFileEnumerator(BinaryReader br, long total)
        {
            binary = br;
            totalCount = total;
            count = 0;
            PercentComplete = 0.0;
        }

        public void Dispose()
        {
        }

        object System.Collections.IEnumerator.Current
        {
            get { return (object)Current; }
        }

        public bool MoveNext()
        {
            bool success = false;

            if (count >= totalCount) return false;
            try
            {
                face.Normal = ReadVector();
                face.Pt1 = ReadPoint();
                face.Pt2 = ReadPoint();
                face.Pt3 = ReadPoint();
                face.Attribute = binary.ReadUInt16();
                count++;
                success = true;
                PercentComplete = ((double)count) / totalCount;
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("After {0} facets: ", count), e);
            }
            return success;
        }

        public void Reset()
        {
            binary.BaseStream.Seek(0, SeekOrigin.Begin);
            binary.ReadBytes(80);
            binary.ReadUInt32();
            count = 0;
            PercentComplete = 0.0;
        }

        private Vector ReadPoint()
        {
            return new Vector(binary.ReadSingle(), binary.ReadSingle(), binary.ReadSingle());
        }

        private Vector ReadVector()
        {
            return new Vector(binary.ReadSingle(), binary.ReadSingle(), binary.ReadSingle());
        }



    }
}

