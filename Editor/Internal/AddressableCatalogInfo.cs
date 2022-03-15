#if CHECKER_WITH_ADDRESSABLES
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UTJ
{
    public class AddressableCatalogInfo
    {
        private struct AddressableObjectCompare : IComparer<AddressableObject>
        {
            public int Compare(AddressableObject x, AddressableObject y)
            {
                int strComp = x.path.CompareTo(y.path);
                if (strComp != 0)
                {
                    return strComp;
                }
                int typeComp = x.objectType.FullName.CompareTo(y.objectType.FullName);
                if (typeComp != 0)
                {
                    return typeComp;
                }

                int abComp = x.assetBundleFile.CompareTo(y.assetBundleFile);
                if (abComp != 0)
                {
                    return abComp;
                }

                return 0;
            }
        }

        public struct AddressableObject
        {
            public string path;
            public System.Type objectType;
            public string assetBundleFile;
            public List<string> dependAssetBundleFiles;
        }
        private Dictionary<string, List<string>> assetBundleDepencyFile = new Dictionary<string, List<string>>();
        private List<string> assetBundles;



        private List<AddressableObject> addressableObjects;

        public void Load(string file)
        {

            var txt = System.IO.File.ReadAllText(file);
            ContentCatalogData catalogData = JsonUtility.FromJson<ContentCatalogData>(txt);

            ResourceLocationMap resourceLocationMap = catalogData.CreateLocator();          
            this.addressableObjects = GetObjectsInfo(resourceLocationMap);
            this.assetBundleDepencyFile = new Dictionary<string, List<string>>();

            foreach (var obj in this.addressableObjects)
            {
                this.AddAddressableObject(obj);
            }
        }

        public List<string> GetAssetBundleFiles()
        {
            if (assetBundles == null)
            {
                assetBundles = new List<string>(assetBundleDepencyFile.Count);
                foreach (var key in assetBundleDepencyFile.Keys)
                {
                    assetBundles.Add(key);
                }
            }
            return assetBundles;
        }

        public List<string> GetDependencies(string abFile)
        {
            List<string> list;
            if (assetBundleDepencyFile.TryGetValue(abFile, out list))
            {
                return list;
            }
            return null;
        }
        private void AddAddressableObject(AddressableObject obj)
        {
            var abFile = obj.assetBundleFile;
            if (string.IsNullOrEmpty(abFile)) { return; }

            List<string> list;
            if (!assetBundleDepencyFile.TryGetValue(abFile, out list))
            {
                list = new List<string>();
                foreach (var dependAb in obj.dependAssetBundleFiles)
                {
                    list.Add(dependAb);
                }

                assetBundleDepencyFile.Add(abFile, list);
                return;
            }
            int lastIndex = 0;
            foreach (var dependAb in obj.dependAssetBundleFiles)
            {
                int idx = list.IndexOf(dependAb);

                if (idx == -1)
                {
                    list.Insert(lastIndex, dependAb);
                    ++lastIndex;
                }
                else
                {
                    lastIndex = idx;
                }
            }
        }





        private static List<AddressableObject> GetObjectsInfo(ResourceLocationMap resourceLocationMap)
        {
            var list = new List<AddressableObject>();
            foreach (var location in resourceLocationMap.Locations)
            {

                foreach (var val in location.Value)
                {
                    if (!val.HasDependencies)
                    {
                        continue;
                    }

                    AddressableObject addressableObject = new AddressableObject();
                    addressableObject.path = val.InternalId;
                    if (val.Dependencies == null)
                    {
                        continue;
                    }
                    int depCount = val.Dependencies.Count;
                    if (depCount == 0)
                    {
                        continue;
                    }
                    addressableObject.objectType = val.ResourceType;

                    addressableObject.assetBundleFile = val.Dependencies[0].PrimaryKey;
                    addressableObject.dependAssetBundleFiles = new List<string>();

                    for (int i = depCount - 1; i > 0; --i)
                    {
                        addressableObject.dependAssetBundleFiles.Add(val.Dependencies[i].PrimaryKey);

                    }

                    list.Add(addressableObject);

                }
            }
            return list;

        }
    }
}

#endif