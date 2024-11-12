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
        private IEnumerator dumpExecute;


        private void OnEnable()
        {

#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            string windowLayoutPath = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/AssetBundleChecker.uxml";
#else
            string windowLayoutPath = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML2018/AssetBundleChecker.uxml";
#endif
            var now = System.DateTime.Now;
            openDateStr = now.ToString("yyyyMMdd_HHmmss");

            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(windowLayoutPath);
            var visualElement = CloneTree(tree);
            this.rootVisualElement.Add(visualElement);


#if !UNITY_2019_1_OR_NEWER && !UNITY_2019_OR_NEWER
            this.lastHeight = -1.0f;
#endif

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
#if !UNITY_2019_1_OR_NEWER && !UNITY_2019_OR_NEWER
            this.SetupScrollViewHeight();
#endif
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

        private void SetVisibility(ScrollView itemBody, bool flag)
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
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            string assetBuntleItemFile = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/AssetBundleFileItem.uxml";
#else
            string assetBuntleItemFile = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML2018/AssetBundleFileItem.uxml";
#endif
            this.assetBundleTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetBuntleItemFile);

            this.assetBunleItemBody = new ScrollView();
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            assetBunleItemBody.style.overflow = Overflow.Hidden;
#endif
        }
        private void InitShaderItems()
        {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            string shaderItem = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/ShaderItem.uxml";
#else
            string shaderItem = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML2018/ShaderItem.uxml";
#endif
            this.shaderItemBody = new ScrollView();

#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            shaderItemBody.style.overflow = Overflow.Hidden;
#endif
            this.shaderTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(shaderItem);
        }
        private void InitShaderVariants()
        {
            this.shaderVariantsItemBody = new ScrollView();
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            shaderVariantsItemBody.style.overflow = Overflow.Hidden;
#endif
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

        private void LoadAssetBundle(string file)
        {
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
                var shaderItem = new ShaderItemUI(shader, shaderTreeAsset, this.openDateStr);
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

            EditorUtility.RevealInFinder(ShaderItemUI.GetSavedDir(this.openDateStr));
        }

            
        private void UnloadAll()
        {
            var delList = new List<AssetBundleItemUI>(this.loadAbItemUIs);
            foreach (var del in delList)
            {
                del.RemoveFormParent();
                del.Dispose();
            }
        }

#if !UNITY_2019_1_OR_NEWER && !UNITY_2019_OR_NEWER
        private VisualElement rootVisualElement
        {
            get
            {
                return this.GetRootVisualContainer();
            }
        }
        private float lastHeight = -1.0f;
        private float lastWidth = -1.0f;
        private void SetupScrollViewHeight()
        {
            if (lastHeight == this.position.height && lastWidth == this.position.width)
            {
                return;
            }
            this.assetBunleItemBody.style.width = this.position.width;
            this.shaderItemBody.style.width = this.position.width;
            this.shaderVariantsItemBody.style.width = this.position.width;

            this.assetBunleItemBody.style.height = this.position.height - 100;
            this.shaderItemBody.style.height = this.position.height - 100;
            this.shaderVariantsItemBody.style.height = this.position.height - 100;

            lastHeight = this.position.height;
            lastWidth = this.position.width;
        }
#endif
    }
}
