using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Orbit
{
    class Json
    {
        public static T[] ReadArray<T>(string filename) where T : new()
        {
            System.Reflection.FieldInfo[] fields = typeof(T).GetFields();
            string[] names = fields.Select(x => x.Name).ToArray();
            Type[] types = fields.Select(x => x.FieldType).ToArray();
            int n = names.Length;

            List<T> tlist = new List<T>();
            JToken token = JToken.Parse(File.ReadAllText(filename));
            if (!(token is JArray))
                throw new Exception("Json file is not an array");

            foreach (JObject jj in token)
            {
                T t = new T();
                for (int i=0; i<n; i++)
                {
                    fields[i].SetValue(t, Convert.ChangeType(jj[names[i]], types[i]));
                }
                tlist.Add(t);
            }
            return tlist.ToArray();
        }
    }
}
