﻿using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

        private GUIContent[] toolBars = new GUIContent[]
        {
            new GUIContent("Load AssetBundles"),
            new GUIContent("AssetBundle Shaders"),
        };
        private int toolBarMode = 0;
        private List<Shader> assetBundleShaders = new List<Shader>();
        private Dictionary<Shader, bool> assetBundleShaderFold = new Dictionary<Shader, bool>();

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
                allRenderers = new List<Renderer>(gmo.GetComponentsInChildren<Renderer>(true));
                abOriginMaterials = new Dictionary<Renderer, Material[]>(allRenderers.Count);
                projShaderMaterials = new Dictionary<Renderer, Material[]>(allRenderers.Count);

                foreach (var renderer in allRenderers)
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
                if (materials == null) { return null; }
                Material[] newMaterials = new Material[materials.Length];

                for (int i = 0; i < materials.Length; ++i)
                {
                    var originMaterial = materials[i];
                    if (originMaterial == null || originMaterial.shader == null)
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
            if (GUILayout.Button("Clear", GUILayout.Width(100.0f)))
            {
                ClearAssetBundle();
            }
            EditorGUILayout.EndHorizontal();


            EditorGUI.indentLevel++;
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            toolBarMode = GUILayout.Toolbar(toolBarMode, toolBars);
            switch (toolBarMode)
            {
                case 0:
                    OnGUILoadedAssetBundles();
                    break;
                case 1:
                    OnGUIShaderDebug();
                    break;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(EditorGUI.indentLevel * 11f));
            if (GUILayout.Button("Load AssetBundle"))
            {
                var file = EditorUtility.OpenFilePanel("Select AssetBundle", "", "");
                LoadAssetBundle(file);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.EndScrollView();
            EditorGUI.indentLevel--;

        }

        private void OnGUIShaderDebug()
        {
            // Shader Debug
            foreach (var shader in this.assetBundleShaders)
            {
                assetBundleShaderFold[shader] = EditorGUILayout.Foldout(assetBundleShaderFold[shader], shader.name);

                if (assetBundleShaderFold[shader])
                {
                    EditorGUILayout.ObjectField(shader, typeof(Shader), false);

//                    EditorGUILayout.BeginVertical(GUILayout.Width(this.position.width / 2));
                    EditorGUILayout.LabelField("AssetBundle");
                    OnGUIShader(shader);
//                    EditorGUILayout.EndVertical();
#if false
                    var projectShader = Shader.Find(shader.name);
                    EditorGUILayout.BeginVertical(GUILayout.Width(this.position.width / 2));
                    EditorGUILayout.LabelField("Project");
                    OnGUIShader(projectShader);
                    EditorGUILayout.EndVertical();
#endif

                }
            }
        }

        private void OnGUILoadedAssetBundles()
        {
            AssetBundle unloadAssetBundle = null;
            foreach (var ab in loadedAssetBundles)
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
            // unload exec
            UnloadAssetBundle(unloadAssetBundle);
        }

        private void OnGUIAssetBundleDetail(AssetBundle ab)
        {
            EditorGUI.indentLevel++;
            var objects = loadedObjects[ab];


            EditorGUILayout.LabelField("Load Objects");
            EditorGUI.indentLevel++;
            foreach (var obj in objects)
            {
                EditorGUILayout.ObjectField(obj, obj.GetType(), true);
            }
            EditorGUI.indentLevel--;

            var instanciates = instanciatedGameObjectPerAb[ab];
            if (instanciates.Count > 0)
            {
                EditorGUILayout.LabelField("MaterialChange");

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
            if (advanceInfoFold[ab])
            {
                SerializedObject serializedObject = null;
                if (!serializedObjectPerAb.TryGetValue(ab, out serializedObject))
                {
                    serializedObject = new SerializedObject(ab);
                    serializedObjectPerAb.Add(ab, serializedObject);
                }
                EditorGUI.indentLevel++;
                DoDrawDefaultInspector(serializedObject);
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
            foreach (var ab in abs)
            {
                ab.Unload(true);
            }

        }

        private void OnDisable()
        {
            ClearAssetBundle();
        }

        private void LoadAssetBundle(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return;
            }
            AssetBundle ab = AssetBundle.LoadFromFile(file);
            if (ab == null)
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
                instancedObjects.Add(new InstanciatedGameObject(gmo));
            }
            instanciatedGameObjectPerAb.Add(ab, instancedObjects);

            // 
            this.AppendShaderListFromAb(ab);
        }

        private void UnloadAssetBundle(AssetBundle ab)
        {
            if (ab == null) { return; }
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
            assetBundleShaders.Clear();
            assetBundleShaderFold.Clear();
            ab.Unload(true);
            loadedAssetBundles.Remove(ab);

            // 
            ConstructAssetBundleShaderList();

        }

        private void ClearAssetBundle()
        {
            assetBundleShaders.Clear();
            assetBundleShaderFold.Clear();
            foreach (var objs in instanciatedGameObjectPerAb.Values)
            {
                foreach (var instanciatedGameObject in objs)
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

        private void OnGUIShader(Shader shader)
        {
            if (shader != null)
            {
                EditorGUI.indentLevel++;
                bool hasShaderCode = HasShaderCode(shader);
                EditorGUILayout.LabelField("isSupported:" + shader.isSupported);
                EditorGUILayout.LabelField("shaderCode:" + hasShaderCode);
                var shaderData = ShaderUtil.GetShaderData(shader);
                EditorGUILayout.LabelField("SubShader Count:" + shaderData.SubshaderCount);

                EditorGUILayout.LabelField("");
                for (int i = 0; i < shaderData.SubshaderCount; ++i)
                {
                    var subShader = shaderData.GetSubshader(i);
                    EditorGUILayout.LabelField("SubShader " + i + "(PassCount:" + subShader.PassCount + ")");
                    EditorGUI.indentLevel++;
                    for (int j = 0; j < subShader.PassCount; ++j)
                    {
                        var pass = subShader.GetPass(j);
                        EditorGUILayout.LabelField("PassName \"" + pass.Name + "\"");
                        EditorGUI.indentLevel++;

                        if ( !hasShaderCode || string.IsNullOrEmpty(pass.SourceCode))
                        {
                            EditorGUILayout.LabelField("No SourceCode");
                        }
                        else
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("", GUILayout.Width(EditorGUI.indentLevel * 11.0f));
                            if ( GUILayout.Button("CopySourceToClip"))
                            {
                                EditorGUIUtility.systemCopyBuffer = pass.SourceCode;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("",GUILayout.Width( EditorGUI.indentLevel * 11.0f ) );
                if( GUILayout.Button("Dump ShaderVaritansts"))
                {
                    var dumpInfo = new ShaderDumpInfo(shader);
                    string jsonString = JsonUtility.ToJson(dumpInfo);
                    string file = shader.name.Replace("/", "_") + ".json";
                    System.IO.File.WriteAllText(file, jsonString);
                    Debug.Log(jsonString);
                    EditorUtility.DisplayDialog("Saved", "Dump saved \"" + file +"\"", "ok");
                }
                EditorGUILayout.EndHorizontal();
                // Debug
                /*
                DoDrawDefaultInspector(new SerializedObject(shader));
                if(GUILayout.Button("Debug"))
                {
                    DebugSerializedObject(new SerializedObject(shader));
                }
                */
                EditorGUI.indentLevel--;
            }
        }



        internal static bool DebugSerializedObject(SerializedObject obj)
        {
            EditorGUI.BeginChangeCheck();
            obj.Update();

            // Loop through properties and create one field (including children) for each top level property.
            SerializedProperty property = obj.GetIterator();
            bool expanded = true;
            while (property.NextVisible(expanded))
            {
                Debug.Log(property.propertyPath);
            }
            return EditorGUI.EndChangeCheck();
        }





        private static bool HasShaderCode(Shader shader)
        {
            return (CallShaderUtilBoolFunc(shader, "HasShaderSnippets") ||
                 CallShaderUtilBoolFunc(shader, "HasSurfaceShaders") ||
                  CallShaderUtilBoolFunc(shader, "HasFixedFunctionShaders"));

        }

        private static bool CallShaderUtilBoolFunc(Shader shader, string func)
        {
            var method = typeof(ShaderUtil).GetMethod(func, BindingFlags.Static | BindingFlags.NonPublic);
            var obj = method.Invoke(null, new object[] { shader });
            System.Boolean val = (System.Boolean)obj;
            return val;
        }


        private void ConstructAssetBundleShaderList()
        {
            assetBundleShaderFold.Clear();
            assetBundleShaders.Clear();
            foreach (var ab in this.loadedAssetBundles)
            {
                AppendShaderListFromAb(ab);
            }
        }

        private void AppendShaderListFromAb(AssetBundle ab)
        {

            SerializedObject serializedObject = null;
            if (!serializedObjectPerAb.TryGetValue(ab, out serializedObject))
            {
                serializedObject = new SerializedObject(ab);
                serializedObjectPerAb.Add(ab, serializedObject);
            }

            var preloadTable = serializedObject.FindProperty("m_PreloadTable");
            var preloadInstancies = preloadTable.serializedObject.context;

            for (int i = 0; i < preloadTable.arraySize; ++i)
            {
                var elementProp = preloadTable.GetArrayElementAtIndex(i);
                var shader = elementProp.objectReferenceValue as Shader;
                AddShader(shader);
            }
        }

        private static string GetDependency(Shader shader, string name)
        {
            string func = "GetDependency";
            var method = typeof(ShaderUtil).GetMethod(func, BindingFlags.Static | BindingFlags.NonPublic);
            var obj = method.Invoke(null, new object[] { shader, name });
            return obj as string;
        }

        private void AddShader(Shader shader)
        {
            if (shader == null) { return; }
            if (assetBundleShaders.Contains(shader))
            {
                return;
            }
            assetBundleShaders.Add(shader);

            assetBundleShaderFold.Add(shader, false);
        }
        // future...
        //  -> Execute on runtime( For Android/Windows)
        //  -> LoadManifest file and dependencies auto resolved.

    }
}
