using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif
using System.IO;

namespace UTJ
{
    public class ShaderItemUI
    {
        private VisualElement element;
        private DumpProgressUI dumpProgress;

        private ShaderDumpInfo shaderDumpInfo;        
        private Shader shader;
        private string dateTimeStr;

        internal static readonly string SaveDIR = "ShaderVariants/AssetBundles";

        public bool IsSame(Shader sh)
        {
            return (sh.GetInstanceID() == this.shader.GetInstanceID());
        }

        public ShaderItemUI (Shader sh,string abFilePath,VisualTreeAsset treeAsset,string date)
        {
            this.shader = sh;
            this.dateTimeStr = date;
            var shaderData = ShaderUtil.GetShaderData(sh);
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            element = treeAsset.CloneTree();
#else
            element = treeAsset.CloneTree(null);
#endif
            element.Q<Label>("AssetBundlePath").text = System.IO.Path.GetFileName(abFilePath);

            element.Q<Foldout>("ShaderFold").text = sh.name;
            element.Q<Foldout>("ShaderFold").value = false;
            // shader value
            element.Q<ObjectField>("ShaderVal").objectType = typeof(Shader);
            element.Q<ObjectField>("ShaderVal").value = sh;

            var shaderSubShadersFold = element.Q<Foldout>("SubShaders");
            shaderSubShadersFold.text = "SubShaders(" + shaderData.SubshaderCount + ")";
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
            this.shaderDumpInfo = null;
            this.element.parent.Remove(this.element);
        }

        public bool IsDumpComplete()
        {
            if (shaderDumpInfo == null) { return false; }
            return shaderDumpInfo.IsComplete;
        }

        public void DumpStart()
        {
            if(shaderDumpInfo != null) { return; }
            this.shaderDumpInfo = new ShaderDumpInfo(this.shader);

            var dumpBtn = this.element.Q<Button>("DumpButton");
            // add progress
            dumpProgress = new DumpProgressUI();
            dumpProgress.style.width = 200;
            dumpBtn.parent.Add(dumpProgress);
            // 
            dumpBtn.parent.Remove(dumpBtn);
            EditorApplication.update += Update;
        }

        private void Update() {
            if(shaderDumpInfo == null)
            {
                EditorApplication.update -= this.Update;
                return;
            }
            shaderDumpInfo.SetYieldCheckTime();
            if (!shaderDumpInfo.MoveNext())
            {
                OnDumpComplete();
            }
            else
            {
                dumpProgress.value = shaderDumpInfo.Progress * 100.0f;
                dumpProgress.text = shaderDumpInfo.ProgressStr;
            }
        }

        private void OnDumpComplete()
        {
            string dir = SaveDIR + '/' + this.dateTimeStr;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var parent = dumpProgress.parent;
            parent.Remove(dumpProgress);
            CreateKeywordList(parent);

            string jsonString = JsonUtility.ToJson(this.shaderDumpInfo);
            string file = Path.Combine(dir,shader.name.Replace("/", "_") + ".json");
            System.IO.File.WriteAllText(file, jsonString);
            EditorApplication.update -= this.Update;
        }

        private void CreateKeywordList( VisualElement parent)
        {
            var keywords = shaderDumpInfo.CollectKeywords();
            var keywordFold = new Foldout();
            keywordFold.text = "Keywords(" + keywords.Count + ")";

#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            keywordFold.style.left = 20;
#else
            keywordFold.style.positionLeft = 20;
#endif
            keywordFold.value = false;
            foreach (var keyword in keywords)
            {
                Label keywordLabel = new Label(keyword);
                keywordFold.Add(keywordLabel);
            }
            parent.Add(keywordFold);
        }

    }
}