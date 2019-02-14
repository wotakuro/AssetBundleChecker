using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UTJ
{

    public class AssetBundleChecker : EditorWindow
    {
        private List<AssetBundle> loadedAssetBundles = new List<AssetBundle>();
        private Dictionary<AssetBundle, List<UnityEngine.Object>> loadedObjects = new Dictionary<AssetBundle, List<Object>>();
        private Dictionary<AssetBundle, List<InstanciatedGameObject>> instanciatedGameObjectPerAb = new Dictionary<AssetBundle, List<InstanciatedGameObject>>();
        private Dictionary<AssetBundle, bool> assetBundleFold = new Dictionary<AssetBundle, bool>();
        private Dictionary<AssetBundle, bool> advanceInfoFold = new Dictionary<AssetBundle, bool>();

        private Dictionary<AssetBundle, SerializedObject> serializedObjectPerAb = new Dictionary<AssetBundle, SerializedObject>();

        private Vector2 scrollPos;

        private class InstanciatedGameObject
        {
            public GameObject gameObject;
            private List<Renderer> allRenderers;
            private Dictionary<Renderer, Material[]> abOriginMaterials;
            private Dictionary<Renderer, Material[]> projShaderMaterials;

            public bool IsProjectShader { get; private set; }

            public InstanciatedGameObject(GameObject gmo)
            {
                gameObject = gmo;
                allRenderers = new List<Renderer>( gmo.GetComponentsInChildren<Renderer>(true) );
                abOriginMaterials = new Dictionary<Renderer, Material[]>(allRenderers.Count);
                projShaderMaterials = new Dictionary<Renderer, Material[]>(allRenderers.Count);

                foreach( var renderer in allRenderers)
                {
                    var materials = renderer.sharedMaterials;
                    abOriginMaterials.Add(renderer, materials);
                    var projectMaterials = CreateProjectShaderMaterials(materials);

                    projShaderMaterials.Add(renderer, projectMaterials);
                }
                IsProjectShader = false;
            }

            private Material[] CreateProjectShaderMaterials(Material[] materials)
            {
                if(materials == null) { return null; }
                Material[] newMaterials = new Material[materials.Length];

                for (int i = 0; i < materials.Length; ++i)
                {
                    var originMaterial = materials[i];
                    if ( originMaterial == null || originMaterial.shader == null)
                    {
                        newMaterials[i] = null;
                        continue;
                    }
                    newMaterials[i] = new Material(originMaterial);
                    newMaterials[i].shader = Shader.Find(originMaterial.shader.name);
                }

                return newMaterials;
            }

            public void SetAbOrigin()
            {
                SetMaterials(abOriginMaterials);
                IsProjectShader = false;
            }
            public void SetProjectOrigin()
            {
                SetMaterials(projShaderMaterials);
                IsProjectShader = true;
            }
            private void SetMaterials(Dictionary<Renderer, Material[]> setData)
            {
                foreach (var renderer in allRenderers)
                {
                    renderer.materials = setData[renderer];
                }
            }

            public void Destroy()
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [MenuItem("UTJ/Tools/AssetBundleChecker")]
        public static void Create()
        {
            EditorWindow.GetWindow<AssetBundleChecker>();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Loaded Assetbundles");
            if (GUILayout.Button("Clear",GUILayout.Width(100.0f) ))
            {
                ClearAssetBundle();
            }
            EditorGUILayout.EndHorizontal();


            EditorGUI.indentLevel++;
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            AssetBundle unloadAssetBundle = null;
            foreach ( var ab in loadedAssetBundles)
            {
                EditorGUILayout.BeginHorizontal();
                assetBundleFold[ab] = EditorGUILayout.Foldout(assetBundleFold[ab], ab.name);

                if (GUILayout.Button("X", GUILayout.Width(20.0f)))
                {
                    unloadAssetBundle = ab;
                }
                EditorGUILayout.EndHorizontal();

                if (assetBundleFold[ab])
                {
                    OnGUIAssetBundleDetail(ab);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(EditorGUI.indentLevel * 11f));            
            if (GUILayout.Button("Load AssetBundle"))
            {
                var file = EditorUtility.OpenFilePanel("Select AssetBundle", "", "");
                LoadAssetBundle(file);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUI.indentLevel--;
            // unload exec
            UnloadAssetBundle(unloadAssetBundle);

        }

        private void OnGUIAssetBundleDetail(AssetBundle ab)
        {
            EditorGUI.indentLevel++;
            var objects = loadedObjects[ab];


            EditorGUILayout.LabelField("Load Objects");
            EditorGUI.indentLevel++;
            foreach (var obj in objects) {
                EditorGUILayout.ObjectField(obj,obj.GetType(),true);
            }
            EditorGUI.indentLevel--;

            var instanciates = instanciatedGameObjectPerAb[ab];
            if (instanciates.Count > 0)
            {
                EditorGUILayout.LabelField("MaterialChange" );

                EditorGUI.indentLevel++;
                foreach (var instanciate in instanciates)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(instanciate.gameObject.name);
                    if (!instanciate.IsProjectShader)
                    {
                        if (GUILayout.Button("AssetBundle Shader"))
                        {
                            instanciate.SetProjectOrigin();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Project Shader"))
                        {
                            instanciate.SetAbOrigin();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            //AdvanceInfo
            advanceInfoFold[ab] = EditorGUILayout.Foldout(advanceInfoFold[ab], "Advanced");
            if( advanceInfoFold[ab])
            {
                SerializedObject serializedObject = null;
                if( !serializedObjectPerAb.TryGetValue(ab,out serializedObject) ){
                    serializedObject = new SerializedObject(ab);
                    serializedObjectPerAb.Add(ab, serializedObject);
                }
                EditorGUI.indentLevel++;
                DoDrawDefaultInspector( serializedObject );
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;

        }
        internal static bool DoDrawDefaultInspector(SerializedObject obj)
        {
            EditorGUI.BeginChangeCheck();
            obj.Update();

            // Loop through properties and create one field (including children) for each top level property.
            SerializedProperty property = obj.GetIterator();
            bool expanded = true;
            while (property.NextVisible(expanded))
            {
                using (new EditorGUI.DisabledScope("m_Script" == property.propertyPath))
                {
                    EditorGUILayout.PropertyField(property, true);
                }
                expanded = false;
            }

            obj.ApplyModifiedProperties();
            return EditorGUI.EndChangeCheck();
        }

        static void SuperCleanAssetBundle()
        {
            var abs = Resources.FindObjectsOfTypeAll<AssetBundle>();
            foreach( var ab in abs)
            {
                ab.Unload(true);
            }

        }

        private void OnDisable()
        {
            ClearAssetBundle();
        }

        void LoadAssetBundle(string file)
        {
            if(string.IsNullOrEmpty(file))
            {
                return;
            }
            AssetBundle ab = AssetBundle.LoadFromFile(file);
            if( ab == null)
            {
                return;
            }
            loadedAssetBundles.Add(ab);
            // load objects
            var allLoadObjects = ab.LoadAllAssets<UnityEngine.Object>();
            loadedObjects.Add(ab, new List<UnityEngine.Object>(allLoadObjects));
            // fold
            assetBundleFold.Add(ab, true);

            advanceInfoFold.Add(ab, false);

            // prefabs
            var prefabs = ab.LoadAllAssets<GameObject>();
            List<InstanciatedGameObject> instancedObjects = new List<InstanciatedGameObject>(prefabs.Length);

            foreach (var prefab in prefabs)
            {
                var gmo = GameObject.Instantiate(prefab);
                instancedObjects.Add( new InstanciatedGameObject( gmo ) );
            }
            instanciatedGameObjectPerAb.Add(ab, instancedObjects );            
        }

        void UnloadAssetBundle(AssetBundle ab)
        {
            if( ab == null) { return; }
            var objs = instanciatedGameObjectPerAb[ab];
            foreach (var instanciatedObject in objs)
            {
                instanciatedObject.Destroy();
            }
            instanciatedGameObjectPerAb.Remove(ab);
            assetBundleFold.Remove(ab);
            advanceInfoFold.Remove(ab);
            serializedObjectPerAb.Remove(ab);
            loadedObjects.Remove(ab);
            ab.Unload(true);
            loadedAssetBundles.Remove(ab);
        }

        void ClearAssetBundle()
        {
            foreach( var objs in instanciatedGameObjectPerAb.Values)
            {
                foreach( var instanciatedGameObject in objs)
                {
                    instanciatedGameObject.Destroy();
                }
            }
            instanciatedGameObjectPerAb.Clear();
            assetBundleFold.Clear();
            advanceInfoFold.Clear();
            loadedObjects.Clear();
            serializedObjectPerAb.Clear();
            foreach (var assetBundle in loadedAssetBundles)
            {
                assetBundle.Unload(true);
            }
            loadedAssetBundles.Clear();
        }
        // future...
        //  -> Execute on runtime( For Android/Windows)
        //  -> LoadManifest file and dependencies auto resolved.

    }
}
