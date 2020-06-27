using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif


namespace UTJ
{
    public class ShaderVariantInfoUI
    {
        public class ShaderVariants
        {
            public List<string> keywordNames = new List<string>();
        }
        private ShaderVariantCollection variantCollection;
        private VisualElement element;
        private List<Shader> shaders = new List<Shader>();
        private Dictionary<Shader, ShaderVariants> shaderVariants = new Dictionary<Shader, ShaderVariants>();
        private DumpInfo dumpInfo;
        private string dateStr;


        [System.Serializable]
        private class DumpShaderInfo
        {
            [SerializeField]
            public string shaderName;
            [SerializeField]
            public List<string> keywords;
        }

        [System.Serializable]
        private class DumpInfo
        {
            [SerializeField]
            public string collectionName;
            [SerializeField]
            public List<DumpShaderInfo> shaderInfos;
        }

        public ShaderVariantInfoUI( ShaderVariantCollection collection,string date)
        {
            this.variantCollection = collection;
            this.dateStr = date;

            ConstructShaderList();
            this.InitUI();
        }

        private void InitUI()
        {
            var mainFold = new Foldout();
            mainFold.text = variantCollection.name;

            ObjectField objectField = null;

#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            objectField = new ObjectField("ShaderVaiantCollection");
#else
            objectField = new ObjectField();
#endif
            objectField.objectType = typeof(ShaderVariantCollection);
            objectField.value = variantCollection;
            mainFold.Add(objectField);

            foreach ( var shader in this.shaders)
            {
                var shaderFold = new Foldout();
                shaderFold.text = shader.name;
                shaderFold.style.paddingLeft = 10;


#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
                ObjectField shaderObject = new ObjectField("Shader");
#else
                ObjectField shaderObject = new ObjectField();
#endif
                shaderObject.objectType = typeof(Shader);
                shaderObject.value = shader;
                shaderFold.Add(shaderObject);
                shaderFold.value = false;


                ShaderVariants variants = null;
                if(this.shaderVariants.TryGetValue(shader,out variants)){
                    Foldout keywordsFold = new Foldout();
                    keywordsFold.text = "keywords(" + variants.keywordNames.Count + ")";
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
                    keywordsFold.style.left = 20;
#else
                    keywordsFold.style.positionLeft = 20;
#endif
                    keywordsFold.value = false;
                    foreach ( var keyword in variants.keywordNames)
                    {
                        string str = keyword;
                        if(str == "") { str = "<none>"; }
                        var keywordLabel = new Label(str);
                        keywordsFold.Add(keywordLabel);
                    }
                    shaderFold.Add(keywordsFold);
                }
                mainFold.Add(shaderFold);
            }
            var dumpButton = new Button();
            dumpButton.text = "Dump To Json";
            dumpButton.clickable.clicked += () =>
            {
                this.DumpToJson();
                dumpButton.parent.Remove(dumpButton);
            };
            mainFold.Add(dumpButton);

            this.element = new VisualElement();
            this.element.Add(mainFold);
        }

        private void ConstructShaderList()
        {
            var serializedObject = new SerializedObject(this.variantCollection);
            var shaderProperties = serializedObject.FindProperty("m_Shaders");
            if (shaderProperties == null) { return; }
            for (int i = 0; i < shaderProperties.arraySize; ++i)
            {
                var shaderProp = shaderProperties.GetArrayElementAtIndex(i);
                Shader shader = shaderProp.FindPropertyRelative("first").objectReferenceValue as Shader;
                ShaderVariants variants = new ShaderVariants();
                var variantsProp = shaderProp.FindPropertyRelative("second.variants");

                for (int j = 0; j < variantsProp.arraySize; ++j)
                {
                    var variantProp = variantsProp.GetArrayElementAtIndex(j).FindPropertyRelative("keywords");
                    variants.keywordNames.Add(variantProp.stringValue);
                }
                shaderVariants.Add(shader, variants);
                shaders.Add(shader);
            }
        }

        public void AddToElement(VisualElement parent)
        {
            parent.Add(this.element);
        }
        public void Remove()
        {
            this.element.parent.Remove(this.element);
        }

        public void DumpToJson()
        {
            if(dumpInfo != null)
            {
                return;
            }
            dumpInfo = new DumpInfo();
            dumpInfo.collectionName = this.variantCollection.name;
            dumpInfo.shaderInfos = new List<DumpShaderInfo>();

            foreach( var shader in this.shaders)
            {
                ShaderVariants variants;
                DumpShaderInfo shaderInfo = new DumpShaderInfo();
                shaderInfo.shaderName = shader.name;
                if ( shaderVariants.TryGetValue(shader , out variants) ){
                    shaderInfo.keywords = new List<string>(variants.keywordNames);
                    shaderInfo.keywords.Sort();
                }
                this.dumpInfo.shaderInfos.Add(shaderInfo);
            }

            string str = JsonUtility.ToJson(this.dumpInfo);
            string dir = ShaderItemUI.SaveDIR + '/' + this.dateStr + "/variants" ;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string jsonFile = Path.Combine(dir, this.variantCollection.name + ".json");
            File.WriteAllText(jsonFile, str);
        }

    }
}
