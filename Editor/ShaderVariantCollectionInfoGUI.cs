using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UTJ
{
    public class ShaderVariantCollectionInfoGUI
    {
        public class ShaderVariants{
            public List<string> keywordNames = new List<string>();
        }

        private ShaderVariantCollection variantCollection;
        private SerializedObject serializedObject;

        private List<Shader> shaders = new List<Shader>();
        private Dictionary<Shader, ShaderVariants> shaderVariants = new Dictionary<Shader, ShaderVariants>();
        private Dictionary<Shader, bool> shaderVariantsFold = new Dictionary<Shader, bool>();

        public ShaderVariantCollectionInfoGUI(ShaderVariantCollection collection)
        {
            variantCollection = collection;
            serializedObject = new SerializedObject(collection);

            ConstructShaderList();
        }

        private void ConstructShaderList()
        {
            var shaderProperties = serializedObject.FindProperty("m_Shaders");
            if(shaderProperties == null) { return; }
            for (int i = 0; i < shaderProperties.arraySize; ++i)
            {
                var shaderProp = shaderProperties.GetArrayElementAtIndex(i);
                Shader shader = shaderProp.FindPropertyRelative("first").objectReferenceValue as Shader;
                ShaderVariants variants = new ShaderVariants();
                var variantsProp = shaderProp.FindPropertyRelative("second.variants");

                for( int j = 0;j < variantsProp.arraySize; ++j)
                {
                    var variantProp = variantsProp.GetArrayElementAtIndex(j).FindPropertyRelative("keywords");
                    variants.keywordNames.Add(variantProp.stringValue);
                }
                shaderVariants.Add(shader, variants);
                shaderVariantsFold.Add(shader, false);
                shaders.Add(shader);
            }
        }

        public void OnGUI()
        {
            if(variantCollection == null) { return; }
            EditorGUI.indentLevel++;
            EditorGUILayout.ObjectField(variantCollection, typeof(ShaderVariantCollection), false);
            foreach( var shader in shaders)
            {

                shaderVariantsFold[shader] = EditorGUILayout.Foldout(shaderVariantsFold[shader], shader.name);
                if (shaderVariantsFold[shader])
                {
                    OnGUIShaderVariants(shader);
                }
            }
            EditorGUI.indentLevel--;
        }

        private void OnGUIShaderVariants(Shader shader) {
            var variants = shaderVariants[shader];
            EditorGUI.indentLevel++;
            EditorGUILayout.ObjectField(shader, typeof(Shader), false);
            foreach ( var keywords in variants.keywordNames){
                EditorGUILayout.LabelField(keywords);
            }
            EditorGUI.indentLevel--;
        }
    }
}