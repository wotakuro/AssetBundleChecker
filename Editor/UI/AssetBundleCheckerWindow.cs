using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using System.Runtime.Remoting.Messaging;

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
        private ScrollView assetBunleItemBody;
        private ScrollView shaderItemBody;
        private ScrollView shaderVariantsItemBody;
        private List<AssetBundleItemUI> loadAbItemUIs = new List<AssetBundleItemUI>();
        private List<ShaderItemUI> loadShaderItems = new List<ShaderItemUI>();

        private Button loadAbButton;

        private Toolbar headerToolbar;

        private void OnEnable()
        {
            string windowLayoutPath = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/AssetBundleChecker.uxml";

            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(windowLayoutPath);
            var visualElement = CloneTree(tree);
            this.rootVisualElement.Add(visualElement);


            this.InitHeader();
            this.InitAssetBundleItems();
            this.InitShaderItems();
            this.InitShaderVariants();
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

        private void InitHeader()
        {
            this.headerToolbar = this.rootVisualElement.Q<Toolbar>("Header");
            headerToolbar.Q<ToolbarButton>("Assets").clickable.clicked += SetAssetFileMode;
            headerToolbar.Q<ToolbarButton>("Shaders").clickable.clicked += SetShaderMode;
            headerToolbar.Q<ToolbarButton>("ShaderVariants").clickable.clicked += SetShaderVariantMode;
        }
        private void SetAssetFileMode()
        {
            assetBunleItemBody.visible = true;
            shaderItemBody.visible = false;
            shaderVariantsItemBody.visible = false;
        }
        private void SetShaderMode()
        {
            assetBunleItemBody.visible = false;
            shaderItemBody.visible = true;
            shaderVariantsItemBody.visible = false;
        }
        private void SetShaderVariantMode()
        {
            assetBunleItemBody.visible = false;
            shaderItemBody.visible = false;
            shaderVariantsItemBody.visible = true;
        }


        private void InitAssetBundleItems()
        {
            string assetBuntleItemFile = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/AssetBundleFileItem.uxml";
            this.assetBundleTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetBuntleItemFile);

            this.assetBunleItemBody = this.rootVisualElement.Q<ScrollView>("AssetBundleItemBody");
            this.loadAbButton = new Button();
            assetBunleItemBody.Add(loadAbButton);
            loadAbButton.text = "Load AssetBundle";
            loadAbButton.clickable.clicked += SelectAssetBundleFile;
        }
        private void InitShaderItems()
        {
            this.shaderItemBody = this.rootVisualElement.Q<ScrollView>("ShaderItemBody");

        }
        private void InitShaderVariants()
        {
            shaderVariantsItemBody = this.rootVisualElement.Q<ScrollView>("VariantsItemBody");
        }

        private void SelectAssetBundleFile()
        {
            var file = EditorUtility.OpenFilePanel("Select AssetBundle", "", "");
            if(string.IsNullOrEmpty(file)) { return; }
            AssetBundle assetBundle = AssetBundle.LoadFromFile(file);
            if( assetBundle == null) { return; }
            var assetBundleItem = new AssetBundleItemUI(assetBundle, this.assetBundleTreeAsset,this.OnDeleteAssetBundleItem);
            assetBundleItem.InsertBefore(this.loadAbButton);
            loadAbItemUIs.Add(assetBundleItem);

            // Shaderリスト
            List<Shader> shaders = new List<Shader>();
            assetBundleItem.CollectAbObjectToList(shaders);
        }


        private void OnDeleteAssetBundleItem(AssetBundleItemUI item)
        {
            if(loadAbItemUIs != null)
            {
                loadAbItemUIs.Remove(item);
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


    }
}
