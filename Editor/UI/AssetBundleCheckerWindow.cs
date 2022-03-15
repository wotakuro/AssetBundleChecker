using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif
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
        private Button loadCatalogBtn;

        private IEnumerator dumpExecute;

        private void OnEnable()
        {

            string windowLayoutPath = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/AssetBundleChecker.uxml";
            var now = System.DateTime.Now;
            openDateStr = now.ToString("yyyyMMdd_HHmmss");

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
            if (dumpExecute != null)
            {
                if (!dumpExecute.MoveNext())
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


            this.loadCatalogBtn = this.rootVisualElement.Q<Button>("LoadCatalogBtn");
            //

            // 

#if CHECKER_WITH_ADDRESSABLES

            loadCatalogBtn.clickable.clicked += LoadCatalog;
#else
            this.loadCatalogBtn.parent.Remove(this.loadCatalogBtn);
#endif
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
        private void SeAddressableCatalogMode()
        {
            SetVisibility(assetBunleItemBody, false);
            SetVisibility(shaderItemBody, false);
            SetVisibility(shaderVariantsItemBody, false);
        }

        private void SetVisibility(VisualElement itemBody, bool flag)
        {
            if(itemBody == null) { return; }
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
                var fileList = GetDependFiles(file);
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

        private void LoadAssetBundle(string file)
        {
            if (AlreadyLoadedAssetBundle(file))
            {
                return;
            }
            var assetBundleItem = new AssetBundleItemUI(file, this.assetBundleTreeAsset, this.OnDeleteAssetBundleItem);
            if (!assetBundleItem.Validate()) { return; }
            assetBundleItem.AddToElement(this.assetBunleItemBody);
            loadAbItemUIs.Add(assetBundleItem);

            // Shaderリスト
            List<Shader> shaders = new List<Shader>();
            assetBundleItem.CollectAbObjectToList(shaders);
            List<ShaderItemUI> shaderItems = new List<ShaderItemUI>();
            foreach (var shader in shaders)
            {
                var shaderItem = new ShaderItemUI(shader, assetBundleItem.AssetBundleFilePath, shaderTreeAsset, this.openDateStr);
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
                var variantItem = new ShaderVariantInfoUI(variantCollection,this.openDateStr);
                variantItem.AddToElement(this.shaderVariantsItemBody);
                variantItems.Add(variantItem);
            }
            this.loadVariantItems.Add(assetBundleItem, variantItems);
        }


        private void OnDeleteAssetBundleItem(AssetBundleItemUI item)
        {
            if (loadAbItemUIs != null)
            {
                loadAbItemUIs.Remove(item);
            }
            if (loadShaderItems != null)
            {
                List<ShaderItemUI> shaderItems;
                if (loadShaderItems.TryGetValue(item, out shaderItems))
                {
                    foreach (var shaderItem in shaderItems)
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
            return asset.CloneTree();
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
            var variantItems = new List<ShaderVariantInfoUI>();
            var allItem = new List<ShaderItemUI>();
            foreach (var items in this.loadShaderItems.Values)
            {
                if (items != null)
                {
                    allItem.AddRange(items);
                }
            }
            foreach (var items in this.loadVariantItems.Values)
            {
                if (items != null)
                {
                    variantItems.AddRange(items);
                }
            }
            yield return null;

            foreach (var item in allItem)
            {
                item.DumpStart();
                while (!item.IsDumpComplete())
                {
                    yield return null;
                }
            }
            foreach (var item in variantItems)
            {
                item.DumpToJson();
                yield return null;
            }
        }

            
        private void UnloadAll()
        {
            var delList = new List<AssetBundleItemUI>(this.loadAbItemUIs);
            foreach (var del in delList)
            {
                del.RemoveFormParent();
                del.Dispose();
            }
            this.loadAbItemUIs.Clear();
        }

        private bool AlreadyLoadedAssetBundle(string file)
        {
            foreach( var loadAbItem in loadAbItemUIs)
            {
                if(loadAbItem.AssetBundleFilePath == file){
                    Debug.Log("Already load " + file);
                    return true;
                }
            }
            return false;
        }

        private List<string> GetDependFiles(string file)
        {
#if CHECKER_WITH_ADDRESSABLES
            var inCatalogResult = GetDependInCatalog(file);
            if(inCatalogResult != null && inCatalogResult.Count > 1) {
                return inCatalogResult; 
            }
#endif
            var fileList = new List<string>();
            AssetBundleManifestResolver.GetLoadFiles(file, fileList);

            return fileList;
        }

        // with Addressables
#if CHECKER_WITH_ADDRESSABLES
        private AddressableCatalogInfo catalogInfo;

        private void LoadCatalog()
        {
            var file = EditorUtility.OpenFilePanel("Select AssetBundle", "Library/com.unity.addressables/aa", "json");
            catalogInfo = new AddressableCatalogInfo();
            catalogInfo.Load(file);

        }

        private List<string> GetDependInCatalog(string file)
        {
            if(catalogInfo == null) { return null; }
            int idx = file.LastIndexOf('/');
            string fileOnly = file.Substring(idx+1);
            string dir = file.Substring(0, idx);
            Debug.Log("fileOnly::" + fileOnly);
            var dependencies =  catalogInfo.GetDependencies(fileOnly);
            if(dependencies == null) { return null; }
            var result = new List<string>( dependencies.Count);
            foreach(var depend in dependencies)
            {
                result.Add(System.IO.Path.Combine(dir, depend));
            }
            result.Add(file);
            return result;
        }
#endif

    }
}
