using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Efield
{
    /*
    class XmlElement
    {
        public string Name { get; private set; }
        public string Text { get; private set; }

        public bool IsLeaf
        {
            get
            {
                return Text != null && myElements.Length == 0;
            }
        }

        public XmlElement[] Elements
        {
            get
            {
                return (XmlElement[]) myElements.Clone();
            }
        }

        Dictionary<string, string> simple;
        bool madeSimple = false;

        Dictionary<string, string> MakeSimpleObject()
        {
            if (myElements.Length == 0) return null;
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (XmlElement e in myElements)
            {
                if (!e.IsLeaf) return null;
                if (dict.ContainsKey(e.Name)) return null;
                dict.Add(e.Name, e.Text);
            }
            return dict;
        }

        public Dictionary<string, string> SimpleObject
        {
            get
            {
                if (!madeSimple)
                {
                    simple = MakeSimpleObject();
                    madeSimple = true;
                }
                return simple;
            }
        }

        XmlElement[] myElements;

        public XmlElement(XmlReader r)
        {
            List<XmlElement> elements = new List<XmlElement>();
            Name = r.Name;
            try
            {
                while (r.Read())
                {
                    if (r.NodeType == XmlNodeType.EndElement) break;

                    switch (r.NodeType)
                    {
                        case XmlNodeType.Element:
                            elements.Add(new XmlElement(r));
                            break;
                        case XmlNodeType.Text:
                            Text = r.Value;
                            break;
                        default:
                            throw new Exception("Unknown node type in " + Name);
                    }
                }
                myElements = elements.ToArray();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + " in " + Name);
            }
        }
    }
    */
    class XmlFile
    {
        public XmlElement[] Elements
        {
            get
            {
                return (XmlElement[])myElements.Clone();
            }
        }
        XmlElement[] myElements;

        //public XmlFile(string filename)
        //{
        //    List<XmlElement> elements = new List<XmlElement>();
        //    XmlTextReader r = new XmlTextReader(filename);
        //    while (r.Read())
        //    {
        //        switch (r.NodeType)
        //        {
        //            case XmlNodeType.Element:
        //                elements.Add(new XmlElement(r));
        //                break;
        //            default:
        //                throw new Exception("Unknown node type in " + filename);
        //        }
        //    }
        //    myElements = elements.ToArray();
        //}

    }
}
