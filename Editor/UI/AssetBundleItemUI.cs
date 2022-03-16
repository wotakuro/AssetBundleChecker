using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace UTJ
{
    public class AssetBundleItemUI:System.IDisposable
    {
        public delegate void OnDeleteAsset(AssetBundleItemUI itemUi);

        private string assetBundleFilePath;
        private AssetBundle assetBundle;
        private VisualElement element;
        private List<UnityEngine.Object> assetBundleObjects;
        private List<InstanciateGameObjectFromAb> instanciateObjects;

        private Foldout advancedFold;

        private SerializedObject serializedObject;
        private OnDeleteAsset onDeleteAsset;

        public string AssetBundleFilePath { get { return assetBundleFilePath; } }

        public AssetBundleItemUI(string abFilePath, VisualTreeAsset tree, OnDeleteAsset onDelete)
        {
            this.assetBundleFilePath = abFilePath;
            this.assetBundle =AssetBundle.LoadFromFile(abFilePath);
            if(this.assetBundle == null) { return; }
            this.serializedObject = new SerializedObject(this.assetBundle);
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            this.element = tree.CloneTree();
#else
            this.element = tree.CloneTree(null);
#endif
            this.onDeleteAsset = onDelete;
            if (!IsStreamSceneAsset(this.serializedObject))
            {
                var allObjects = this.assetBundle.LoadAllAssets<UnityEngine.Object>();
                this.assetBundleObjects = new List<UnityEngine.Object>(allObjects);
            }
            else
            {
                this.assetBundleObjects = new List<UnityEngine.Object>();
                var scenes = this.assetBundle.GetAllScenePaths();
                foreach(var scene in scenes)
                {
                    if (EditorApplication.isPlaying)
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene(scene,UnityEngine.SceneManagement.LoadSceneMode.Additive);
                    }
                }
            }
            this.InitElement();
        }

        private bool IsStreamSceneAsset(SerializedObject obj)
        {
            var prop = obj.FindProperty("m_IsStreamedSceneAssetBundle") ;
            if( prop == null) { Debug.LogError("m_IsStreamedSceneAssetBundle is null"); }
            return prop.boolValue;
        }

        public bool Validate()
        {
            return (this.assetBundle != null);
        }

        private void InitElement()
        {
            if (!string.IsNullOrEmpty(this.assetBundle.name))
            {
                this.element.Q<Label>("AssetBundleName").text = "Name:"+this.assetBundle.name;
            }
            this.element.Q<Foldout>("AssetBundleItem").text = System.IO.Path.GetFileName( assetBundleFilePath );
            
            this.element.Q<Foldout>("AssetBundleItem").value = false;

#if !UNITY_2019_1_OR_NEWER && !UNITY_2019_OR_NEWER
            this.element.Q<Foldout>("AssetBundleItem").style.minWidth = 400;
#endif

            var loadObjectBody = this.element.Q<VisualElement>("LoadObjectBody");
            foreach( var abObject in assetBundleObjects)
            {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
                var field = new ObjectField(abObject.name);
#else
                var field = new ObjectField();
#endif
                field.allowSceneObjects = true;
                loadObjectBody.Add(field);
                field.objectType = abObject.GetType();
                field.value = abObject;
            }

            // instanciate...
            var instanciateBody = this.element.Q<VisualElement>("MaterialChangeBody");
            instanciateObjects = new List<InstanciateGameObjectFromAb>();
            foreach (var abObject in assetBundleObjects)
            {
                var prefab = abObject as GameObject;
                if( prefab == null) { continue; }
                var instanciateObject = new InstanciateGameObjectFromAb(prefab);
                instanciateObjects.Add(instanciateObject);

                var instanceUI = new InstanciateGameObjectUI(instanciateObject);
                instanceUI.AddToParent(instanciateBody);
            }

            // advanced 
            advancedFold = this.element.Q<Foldout>("Advanced");
            var advancedBody = new IMGUIContainer(OnAdvancedGUI);
            advancedFold.Add(advancedBody);

            // Close Btn

#if !UNITY_2019_1_OR_NEWER && !UNITY_2019_OR_NEWER
#endif
            this.element.Q<Button>("CloseBtn").clickable.clicked += OnClickClose;
        }

        private void OnClickClose()
        {
            this.RemoveFormParent();
            this.Dispose();
        }

        private void OnAdvancedGUI()
        {
            if( !this.advancedFold.value)
            {
                return;
            }
            DoDrawDefaultInspector(this.serializedObject);
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

        public void AddToElement(VisualElement parent)
        {
            if (this.element != null)
            {
                parent.Add(this.element);
            }
        }

        public void CollectAbObjectToList<T>(List<T> items) where T :class
        {
            var preloadTable = serializedObject.FindProperty("m_PreloadTable");
            var preloadInstancies = preloadTable.serializedObject.context;

            for (int i = 0; i < preloadTable.arraySize; ++i)
            {
                var elementProp = preloadTable.GetArrayElementAtIndex(i);
                var item = elementProp.objectReferenceValue as T;
                if (item != null && !items.Contains(item))
                {
                    items.Add(item);
                }
            }
            foreach( var abItem in this.assetBundleObjects)
            {
                var item = abItem as T;
                if (item != null && !items.Contains(item))
                {
                    items.Add(item);
                }
            }
        }

        public void RemoveFormParent()
        {
            if( this.element.parent == null) { return; }
            this.element.parent.Remove(this.element);
        }

        public void DisposeFromOnDisable()
        {
            this.onDeleteAsset = null; 
            this.Dispose();
        }

        public void Dispose()
        {
            if( this.onDeleteAsset != null)
            {
                this.onDeleteAsset(this);
            }
            if(instanciateObjects != null)
            {
                foreach( var instanciateObj in instanciateObjects)
                {
                    instanciateObj.Destroy();
                }
            }
            if (assetBundle != null)
            {
                assetBundle.Unload(true);
                assetBundle = null;
            }
        }
    }
}
