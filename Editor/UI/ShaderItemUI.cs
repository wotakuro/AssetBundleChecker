using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UTJ
{
    public class ShaderItemUI
    {
        private VisualElement element;

        public ShaderItemUI (Shader sh,VisualTreeAsset treeAsset)
        {

            var shaderData = ShaderUtil.GetShaderData(sh);
            element = treeAsset.CloneTree();

            element.Q<Foldout>("ShaderFold").text = sh.name;

            var subShaderFoldout = element.Q<Foldout>("SubShaders");
            subShaderFoldout.text = "SubShaders " + shaderData.SubshaderCount;
            subShaderFoldout.value = false;

            for (int i = 0; i < shaderData.SubshaderCount; ++i)
            {
                Foldout subShaderFold = new Foldout();
                var subShader = shaderData.GetSubshader(i);

                CreateSubShaderMenu(subShaderFold,i, subShader);
            }
        }
        private void CreateSubShaderMenu(Foldout subShaderFold,int idx,ShaderData.Subshader subShader)
        {
            subShaderFold.text = "SubShader " + idx + " PassNum:" + subShader.PassCount;

            for (int i = 0; i < subShader.PassCount; ++i)
            {
                var pass = subShader.GetPass(i);
                var label = new Label("PassName:" + pass.Name);
                subShaderFold.Add(label);
            }
        }
    }
}