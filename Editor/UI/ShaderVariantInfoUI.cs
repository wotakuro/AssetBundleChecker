using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


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

        public ShaderVariantInfoUI( ShaderVariantCollection collection)
        {
            this.variantCollection = collection;

            ConstructShaderList();
            this.InitUI();
        }

        private void InitUI()
        {
            var mainFold = new Foldout();
            mainFold.text = variantCollection.name;

            ObjectField objectField = new ObjectField("ShaderVaiantCollection");
            objectField.objectType = typeof(ShaderVariantCollection);
            objectField.value = variantCollection;
            mainFold.Add(objectField);

            foreach ( var shader in this.shaders)
            {
                var shaderFold = new Foldout();
                shaderFold.text = shader.name;
                shaderFold.style.paddingLeft = 10;


                ObjectField shaderObject = new ObjectField("Shader");
                shaderObject.objectType = typeof(Shader);
                shaderObject.value = shader;
                shaderFold.Add(shaderObject);

                ShaderVariants variants = null;
                if(this.shaderVariants.TryGetValue(shader,out variants)){
                    foreach( var keyword in variants.keywordNames)
                    {
                        var keywordLabel = new Label(keyword);
                        shaderFold.Add(keywordLabel);
                    }
                }
                mainFold.Add(shaderFold);
            }

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


    }
}
