using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

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
        private ScrollView itemBody;
        private List<AssetBundleItemUI> loadAbItemUIs = new List<AssetBundleItemUI>();
        private Button loadAbButton;

        private void OnEnable()
        {
            string windowLayoutPath = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/AssetBundleChecker.uxml";
            string assetBuntleItemFile = "Packages/com.utj.assetbundlechecker/Editor/UI/UXML/AssetBundleFileItem.uxml";

            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(windowLayoutPath);
            var visualElement = CloneTree(tree);
            this.rootVisualElement.Add(visualElement);
            this.itemBody = this.rootVisualElement.Q<ScrollView>("Body");

            this.assetBundleTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetBuntleItemFile);

            SetAssetBundleMode();
        }

        private void OnDisable()
        {
            if (loadAbItemUIs != null)
            {
                foreach (var abItem in loadAbItemUIs)
                {
                    abItem.Dispose();
                }
            }
            loadAbItemUIs = null;
        }

        private void SetAssetBundleMode()
        {
            this.loadAbButton = new Button();
            itemBody.Add(loadAbButton);
            loadAbButton.text = "Load AssetBundle";
            loadAbButton.clickable.clicked += SelectAssetBundleFile;
        }

        private void SelectAssetBundleFile()
        {
            var file = EditorUtility.OpenFilePanel("Select AssetBundle", "", "");
            if(string.IsNullOrEmpty(file)) { return; }
            AssetBundle assetBundle = AssetBundle.LoadFromFile(file);
            if( assetBundle == null) { return; }
            var ui = new AssetBundleItemUI(assetBundle, this.assetBundleTreeAsset,this.OnDeleteAssetBundleItem);
            ui.InsertBefore(this.loadAbButton);

            loadAbItemUIs.Add(ui);
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
