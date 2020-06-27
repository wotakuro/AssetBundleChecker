using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace UTJ
{
    public class AssetBundleManifestResolver
    {
        public static void GetLoadFiles(string file, List<string> list)
        {
            var dependencies = GetDependenciesFromManifest(file);
            if (dependencies != null)
            {
                foreach (var dependFile in dependencies)
                {
                    GetLoadFiles(dependFile, list);
                }
            }
            if (!list.Contains(file))
            {
                list.Add(file);
            }
        }



        private static List<string> GetDependenciesFromManifest(string file)
        {
            string manifest = file + ".manifest";
            if(!File.Exists(manifest))
            {
                return null;
            }
            List<string> dependencies = new List<string>();
            var lines = File.ReadAllLines(manifest);
            bool isDependFile = false;
            for(int i=0;i<lines.Length;++i)
            {
                if (isDependFile)
                {
                    string dependFile = lines[i].Substring(2).Trim();
                    if (!string.IsNullOrEmpty(dependFile))
                    {
//                        Debug.Log(file + "\n=>" + dependFile);
                        dependencies.Add(dependFile);
                    }
                }
                if (lines[i] == "Dependencies:")
                {
                    isDependFile = true;
                }
            }

            return dependencies;
        }


    }
}
