using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace UTJ
{
    public class ShaderInfoGUI
    {
        Shader shader;
        public ShaderInfoGUI(Shader sh)
        {
            shader = sh;
        }
        public void OnGUIShader()
        {
            if (shader == null)
            {
                return;
            }
            EditorGUI.indentLevel++;
            EditorGUILayout.ObjectField(shader, typeof(Shader), false);

            bool hasShaderCode = HasShaderCode(shader);
            EditorGUILayout.LabelField("isSupported:" + shader.isSupported);
            EditorGUILayout.LabelField("shaderCode:" + hasShaderCode);
            var shaderData = ShaderUtil.GetShaderData(shader);
            EditorGUILayout.LabelField("SubShader Count:" + shaderData.SubshaderCount);

            EditorGUILayout.LabelField("");
            for (int i = 0; i < shaderData.SubshaderCount; ++i)
            {
                var subShader = shaderData.GetSubshader(i);
                EditorGUILayout.LabelField("SubShader " + i + "(PassCount:" + subShader.PassCount + ")");
                EditorGUI.indentLevel++;
                for (int j = 0; j < subShader.PassCount; ++j)
                {
                    var pass = subShader.GetPass(j);
                    EditorGUILayout.LabelField("PassName \"" + pass.Name + "\"");
                    EditorGUI.indentLevel++;

                    if (!hasShaderCode || string.IsNullOrEmpty(pass.SourceCode))
                    {
                        EditorGUILayout.LabelField("No SourceCode");
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(EditorGUI.indentLevel * 11.0f));
                        if (GUILayout.Button("CopySourceToClip"))
                        {
                            EditorGUIUtility.systemCopyBuffer = pass.SourceCode;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(EditorGUI.indentLevel * 11.0f));
            if (GUILayout.Button("Dump ShaderVaritansts"))
            {
                var dumpInfo = new ShaderDumpInfo(shader);
                while(dumpInfo.MoveNext()) { }
                string jsonString = JsonUtility.ToJson(dumpInfo);
                string file = shader.name.Replace("/", "_") + ".json";
                System.IO.File.WriteAllText(file, jsonString);
                Debug.Log(jsonString);
                EditorUtility.DisplayDialog("Saved", "Dump saved \"" + file + "\"", "ok");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        private static bool HasShaderCode(Shader shader)
        {
            return (CallShaderUtilBoolFunc(shader, "HasShaderSnippets") ||
                 CallShaderUtilBoolFunc(shader, "HasSurfaceShaders") ||
                  CallShaderUtilBoolFunc(shader, "HasFixedFunctionShaders"));

        }
        
        private static bool CallShaderUtilBoolFunc(Shader shader, string func)
        {
            var method = typeof(ShaderUtil).GetMethod(func, BindingFlags.Static | BindingFlags.NonPublic);
            var obj = method.Invoke(null, new object[] { shader });
            System.Boolean val = (System.Boolean)obj;
            return val;
        }

    }
}