using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UTJ
{
    public class ShaderItemUI
    {
        private VisualElement element;

        private ShaderDumpInfo shaderDumpInfo;

        private Shader shader;

        public ShaderItemUI (Shader sh,VisualTreeAsset treeAsset)
        {
            this.shader = sh;
            var shaderData = ShaderUtil.GetShaderData(sh);
            element = treeAsset.CloneTree();

            element.Q<Foldout>("ShaderFold").text = sh.name;
            // shader value
            element.Q<ObjectField>("ShaderVal").objectType = typeof(Shader);
            element.Q<ObjectField>("ShaderVal").value = sh;

            var shaderSubShadersFold = element.Q<Foldout>("SubShaders");
            shaderSubShadersFold.text = "SubShaders " + shaderData.SubshaderCount;
            shaderSubShadersFold.value = false;

            for (int i = 0; i < shaderData.SubshaderCount; ++i)
            {
                Foldout subShaderFold = new Foldout();
                var subShader = shaderData.GetSubshader(i);

                CreateSubShaderMenu(subShaderFold,i, subShader);
                shaderSubShadersFold.Add(subShaderFold);
            }
            // DumpBtn
            element.Q<Button>("DumpButton").clickable.clicked += () =>
            {
                DumpStart();
            };
        }

        public void AddToElement(VisualElement parent)
        {
            parent.Add(this.element);
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

        public void Remove()
        {
            this.element.parent.Remove(this.element);
        }


        public void DumpStart()
        {
            if(shaderDumpInfo != null) { return; }
            this.shaderDumpInfo = new ShaderDumpInfo(this.shader);
            EditorApplication.update += Update;


        }

        private void Update() {
            shaderDumpInfo.SetYieldCheckTime();
            if (!shaderDumpInfo.MoveNext())
            {
                string jsonString = JsonUtility.ToJson(shaderDumpInfo);
                string file = shader.name.Replace("/", "_") + ".json";
                System.IO.File.WriteAllText(file, jsonString);
                EditorApplication.update -= this.Update;
                EditorUtility.DisplayDialog("DumpComplete", "Dump", "ok");
            }
        }

    }
}