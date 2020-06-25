using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UTJ
{
    public class AssetBundleItemUI:System.IDisposable
    {
        public delegate void OnDeleteAsset(AssetBundleItemUI itemUi); 

        private AssetBundle assetBundle;
        private VisualElement element;
        private List<UnityEngine.Object> assetBundleObjects;
        private List<InstanciateGameObjectFromAb> instanciateObjects;

        private Foldout advancedFold;

        private SerializedObject serializedObject;
        private OnDeleteAsset onDeleteAsset;

        public AssetBundleItemUI( AssetBundle bundle, VisualTreeAsset tree, OnDeleteAsset onDelete)
        {
            this.assetBundle = bundle;
            this.serializedObject = new SerializedObject(this.assetBundle);
            this.element = tree.CloneTree();
            this.onDeleteAsset = onDelete;
            var allObjects = this.assetBundle.LoadAllAssets<UnityEngine.Object>();
            this.assetBundleObjects = new List<UnityEngine.Object>(allObjects);
            this.InitElement();
        }

        private void InitElement()
        {
            this.element.Q<Foldout>("AssetBundleItem").text =  this.assetBundle.name;
            var loadObjectBody = this.element.Q<VisualElement>("LoadObjectBody");
            foreach( var abObject in assetBundleObjects)
            {
                var field = new ObjectField(abObject.name);
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
            parent.Add(this.element);
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
