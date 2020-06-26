using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using System.Runtime.Remoting.Messaging;
using System.Linq.Expressions;

namespace UTJ
{

    public class AssetBundleCheckerWindow : EditorWindow
    {
        [MenuItem("Tools/UTJ/AssetBundleCheck")]
        public static void Create()
        {
            EditorWindow.GetWindow<AssetBundleCheckerWindow>();
        }

        private VisualTreeAsset assetBundleTreeAsset;
        private VisualTreeAsset shaderTreeAsset;

        private List<AssetBundleItemUI> loadAbItemUIs = new List<AssetBundleItemUI>();
        private Dictionary<AssetBundleItemUI, List<ShaderItemUI>> loadShaderItems = new Dictionary<AssetBundleItemUI, List<ShaderItemUI>>();
        private Dictionary<AssetBundleItemUI, List<ShaderVariantInfoUI>> loadVariantItems = new Dictionary<AssetBundleItemUI, List<ShaderVariantInfoUI>>();

        private VisualElement bodyElement;
        private ScrollView assetBunleItemBody;
        private ScrollView shaderItemBody;
        private ScrollView shaderVariantsItemBody;
        private string openDateStr;

        private Button loadAbButton;
        private Toggle isResolveDepencies;
        private IEnumerator dumpExecute;


        private void OnEnable()
        {
            string windowLayoutPath = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/AssetBundleChecker.uxml";
            var now = System.DateTime.Now;
            openDateStr = now.ToString("yyyyMMdd_tthhmmss");

            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(windowLayoutPath);
            var visualElement = CloneTree(tree);
            this.rootVisualElement.Add(visualElement);



            this.InitHeader();

            this.bodyElement = this.rootVisualElement.Q<VisualElement>("BodyItems");
            this.InitAssetBundleItems();
            this.InitShaderItems();
            this.InitShaderVariants();

            this.SetAssetFileMode();
        }
        private void OnDisable()
        {
            if (loadAbItemUIs != null)
            {
                foreach (var abItem in loadAbItemUIs)
                {
                    abItem.DisposeFromOnDisable();
                }
            }
            loadAbItemUIs = null;
        }

        private void Update()
        {
            if(dumpExecute != null)
            {
                if( !dumpExecute.MoveNext())
                {
                    dumpExecute = null;
                }
            }
        }

        private void InitHeader()
        {
            // load btn
            this.loadAbButton = this.rootVisualElement.Q<Button>("LoadAssetBundle");
            loadAbButton.clickable.clicked += SelectAssetBundleFile;
            // dump all
            var dumpAllBtn = this.rootVisualElement.Q<Button>("DumpAllShader");
            dumpAllBtn.clickable.clicked += DumpAllShader;
            //clear all
            var unloadAllBtn = this.rootVisualElement.Q<Button>("UnloadAll");
            unloadAllBtn.clickable.clicked += UnloadAll;
            //
            isResolveDepencies = this.rootVisualElement.Q<Toggle>("AutoLoadDependencies");
            isResolveDepencies.value = true;
            //
            var headerToolbar = this.rootVisualElement.Q<VisualElement>("Header");
            headerToolbar.Q<ToolbarButton>("Assets").clickable.clicked += SetAssetFileMode;
            headerToolbar.Q<ToolbarButton>("Shaders").clickable.clicked += SetShaderMode;
            headerToolbar.Q<ToolbarButton>("ShaderVariants").clickable.clicked += SetShaderVariantMode;
        }
        private void SetAssetFileMode()
        {
            SetVisibility(assetBunleItemBody, true);
            SetVisibility(shaderItemBody, false);
            SetVisibility(shaderVariantsItemBody, false);
        }
        private void SetShaderMode()
        {
            SetVisibility(assetBunleItemBody, false);
            SetVisibility(shaderItemBody, true);
            SetVisibility(shaderVariantsItemBody, false);
        }
        private void SetShaderVariantMode()
        {
            SetVisibility(assetBunleItemBody, false);
            SetVisibility(shaderItemBody, false);
            SetVisibility(shaderVariantsItemBody, true);
        }

        private void SetVisibility(ScrollView itemBody,bool flag)
        {
            if (flag)
            {
                if (!bodyElement.Contains(itemBody))
                {
                    bodyElement.Add(itemBody);
                }
            }
            else
            {
                if (bodyElement.Contains(itemBody))
                {
                    bodyElement.Remove(itemBody);
                }
            }
        }

        private void InitAssetBundleItems()
        {
            string assetBuntleItemFile = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/AssetBundleFileItem.uxml";
            this.assetBundleTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetBuntleItemFile);

            this.assetBunleItemBody = new ScrollView();
            assetBunleItemBody.style.overflow = Overflow.Hidden;
        }
        private void InitShaderItems()
        {
            string shaderItem = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/ShaderItem.uxml";
            this.shaderItemBody = new ScrollView();
            shaderItemBody.style.overflow = Overflow.Hidden;
            this.shaderTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(shaderItem);
        }
        private void InitShaderVariants()
        {
            this.shaderVariantsItemBody = new ScrollView();
            shaderVariantsItemBody.style.overflow = Overflow.Hidden;
        }

        private void SelectAssetBundleFile()
        {
            var file = EditorUtility.OpenFilePanel("Select AssetBundle", "", "");
            if (string.IsNullOrEmpty(file)) { return; }
            bool isResolveDependencies = isResolveDepencies.value;

            if (isResolveDependencies)
            {
                var fileList = new List<string>();
                AssetBundleManifestResolver.GetLoadFiles(file, fileList);
                foreach (var loadFile in fileList)
                {
                    LoadAssetBundle(loadFile);
                }
            }
            else
            {
                LoadAssetBundle(file);
            }
        }

        private void LoadAssetBundle(string file) { 
            var assetBundleItem = new AssetBundleItemUI(file, this.assetBundleTreeAsset,this.OnDeleteAssetBundleItem);
            if( !assetBundleItem.Validate()) { return; }
            assetBundleItem.AddToElement(this.assetBunleItemBody);
            loadAbItemUIs.Add(assetBundleItem);

            // Shaderリスト
            List<Shader> shaders = new List<Shader>();
            assetBundleItem.CollectAbObjectToList(shaders);
            List<ShaderItemUI> shaderItems = new List<ShaderItemUI>();
            foreach( var shader in shaders)
            {
                var shaderItem = new ShaderItemUI(shader, shaderTreeAsset,this.openDateStr);
                shaderItem.AddToElement(this.shaderItemBody);
                shaderItems.Add(shaderItem);
            }
            loadShaderItems.Add(assetBundleItem, shaderItems);
            // shaderVariantCollectionリスト
            List<ShaderVariantCollection> variantCollections = new List<ShaderVariantCollection>();
            assetBundleItem.CollectAbObjectToList(variantCollections);
            List<ShaderVariantInfoUI> variantItems = new List<ShaderVariantInfoUI>();
            foreach (var variantCollection in variantCollections)
            {
                var variantItem = new ShaderVariantInfoUI(variantCollection);
                variantItem.AddToElement(this.shaderVariantsItemBody);
                variantItems.Add(variantItem);
            }
            this.loadVariantItems.Add(assetBundleItem, variantItems);
        }


        private void OnDeleteAssetBundleItem(AssetBundleItemUI item)
        {
            if(loadAbItemUIs != null)
            {
                loadAbItemUIs.Remove(item);
            }
            if (loadShaderItems != null)
            {
                List<ShaderItemUI> shaderItems;
                if(loadShaderItems.TryGetValue(item,out shaderItems))
                {
                    foreach( var shaderItem in shaderItems)
                    {
                        shaderItem.Remove();
                    }
                    loadShaderItems.Remove(item);
                }
            }
            if (loadVariantItems != null)
            {
                List<ShaderVariantInfoUI> variantItems;
                if (loadVariantItems.TryGetValue(item, out variantItems))
                {
                    foreach (var variantItem in variantItems)
                    {
                        variantItem.Remove();
                    }
                    loadVariantItems.Remove(item);
                }
            }
        }
        private static VisualElement CloneTree(VisualTreeAsset asset)
        {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            return asset.CloneTree();
#else
            return asset.CloneTree(null);
#endif
        }

        private void DumpAllShader()
        {
            if (dumpExecute == null)
            {
                dumpExecute = this.ExecuteDumpAll();
            }
        }

        private IEnumerator ExecuteDumpAll()
        {
            List<ShaderItemUI> allItem = new List<ShaderItemUI>();
            foreach(var items in this.loadShaderItems.Values)
            {
                if (items != null)
                {
                    allItem.AddRange(items);
                }
            }
            yield return null;

            foreach( var item in allItem)
            {
                item.DumpStart();
                while (!item.IsDumpComplete() ) {
                    yield return null;
                }
            }
        }

        private void UnloadAll()
        {
            var delList = new List<AssetBundleItemUI>(this.loadAbItemUIs);
            foreach( var del in delList)
            {
                del.RemoveFormParent();
                del.Dispose();
            }
        }
    }
}
