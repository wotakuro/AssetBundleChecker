using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;


namespace UTJ
{

    public class ShaderDumpInfo 
    {
        [Serializable]
        public class GpuProgramInfo
        {
            [SerializeField]
            public string gpuProgramType;
            [SerializeField]
            public List<int> keywordIndecies;
            [SerializeField]
            public List<string> keywords;
            public GpuProgramInfo(SerializedProperty serializedProperty)
            {
                var gpuProgramTypeProp = serializedProperty.FindPropertyRelative("m_GpuProgramType");
                this.gpuProgramType = ((ShaderGpuProgramType)gpuProgramTypeProp.intValue).ToString();

                var keywords = serializedProperty.FindPropertyRelative("m_KeywordIndices");
                keywordIndecies = new List<int>(keywords.arraySize);
                for (int i = 0; i < keywords.arraySize; ++i)
                {
                    int keywordIndex = keywords.GetArrayElementAtIndex(i).intValue;
                    keywordIndecies.Add(keywordIndex);
                }
            }
            public void ResolveKeywordName(Dictionary<int,string> dictionary)
            {
                keywords = new List<string>(keywordIndecies.Count);
                for( int i = 0; i < keywordIndecies.Count; ++i)
                {
                    int index = keywordIndecies[i];
                    string val = null;
                    if( dictionary.TryGetValue(index,out val))
                    {
                        keywords.Add(val); 
                    }
                }
            }
        }
        [Serializable]
        public class PassInfo
        {
            [SerializeField]
            public List<GpuProgramInfo> vertInfos;
            [SerializeField]
            public List<GpuProgramInfo> fragmentInfos;

            private Dictionary<int, string> keywordDictionary;
            public PassInfo(SerializedProperty serializedProperty)
            {
                SetupKeywordDictionary(serializedProperty);

                var progVertex = serializedProperty.FindPropertyRelative("progVertex.m_SubPrograms");
                var progFragment = serializedProperty.FindPropertyRelative("progFragment.m_SubPrograms");

                vertInfos = new List<GpuProgramInfo>(progVertex.arraySize);
                fragmentInfos = new List<GpuProgramInfo>(progFragment.arraySize);

                for (int i = 0; i < progVertex.arraySize; ++i)
                {
                    var gpuProgram = new GpuProgramInfo(progVertex.GetArrayElementAtIndex(i));
                    gpuProgram.ResolveKeywordName(keywordDictionary);
                    vertInfos.Add(gpuProgram);
                }
                for (int i = 0; i < progFragment.arraySize; ++i)
                {
                    var gpuProgram = new GpuProgramInfo(progFragment.GetArrayElementAtIndex(i));
                    gpuProgram.ResolveKeywordName(keywordDictionary);
                    fragmentInfos.Add(gpuProgram);
                }

            }
            private void SetupKeywordDictionary (SerializedProperty serializedProperty)
            {
                keywordDictionary = new Dictionary<int, string>();
                var nameIndices = serializedProperty.FindPropertyRelative("m_NameIndices");

                for (int k = 0; k < nameIndices.arraySize; ++k)
                {
                    var currentNameIndecies = nameIndices.GetArrayElementAtIndex(k).FindPropertyRelative("first");
                    var nameIndex = nameIndices.GetArrayElementAtIndex(k).FindPropertyRelative("second");

//                    Debug.Log(currentNameIndecies.stringValue + "::" + nameIndex.intValue);

                    if (!keywordDictionary.ContainsKey(nameIndex.intValue))
                    {
                        keywordDictionary.Add(nameIndex.intValue, currentNameIndecies.stringValue);
                    }
                }

            }
        }

        [Serializable]
        public class SubShaderInfo
        {
            [SerializeField]
            public List<PassInfo> passes;
            public SubShaderInfo(SerializedProperty serializedProperty)
            {
                var passesProp = serializedProperty.FindPropertyRelative("m_Passes");
                passes = new List<PassInfo>(passesProp.arraySize);
                for (int j = 0; j < passesProp.arraySize; ++j)
                {
                    var currentPassProp = passesProp.GetArrayElementAtIndex(j);
                    var passInfo = new PassInfo(currentPassProp);
                    passes.Add(passInfo);
                }

            }
        }

        [Serializable]
        public class PropInfo
        {
            [SerializeField]
            public string name;

            public PropInfo(SerializedProperty serializedProperty)
            {
                var propNameProperty = serializedProperty.FindPropertyRelative("m_Name");
                if (propNameProperty != null)
                {
                    name = propNameProperty.stringValue;
                }
            }
        }

        public enum ShaderGpuProgramType : int
        {
            GpuProgramUnknown = 0,

            GpuProgramGLLegacy_Removed = 1,
            GpuProgramGLES31AEP = 2,
            GpuProgramGLES31 = 3,
            GpuProgramGLES3 = 4,
            GpuProgramGLES = 5,
            GpuProgramGLCore32 = 6,
            GpuProgramGLCore41 = 7,
            GpuProgramGLCore43 = 8,
            GpuProgramDX9VertexSM20_Removed = 9,
            GpuProgramDX9VertexSM30_Removed = 10,
            GpuProgramDX9PixelSM20_Removed = 11,
            GpuProgramDX9PixelSM30_Removed = 12,
            GpuProgramDX10Level9Vertex_Removed = 13,
            GpuProgramDX10Level9Pixel_Removed = 14,
            GpuProgramDX11VertexSM40 = 15,
            GpuProgramDX11VertexSM50 = 16,
            GpuProgramDX11PixelSM40 = 17,
            GpuProgramDX11PixelSM50 = 18,
            GpuProgramDX11GeometrySM40 = 19,
            GpuProgramDX11GeometrySM50 = 20,
            GpuProgramDX11HullSM50 = 21,
            GpuProgramDX11DomainSM50 = 22,
            GpuProgramMetalVS = 23,
            GpuProgramMetalFS = 24,

            GpuProgramSPIRV = 25,

            GpuProgramConsole = 26,
            GpuProgramCount
        }

        [SerializeField]
        public string name;

        [SerializeField]
        public string fallback;

        [SerializeField]
        public List<PropInfo> propInfos;

        [SerializeField]
        public List<SubShaderInfo> subShaderInfos;



        public ShaderDumpInfo(Shader shader)
        {
            var obj = new SerializedObject(shader);

            //EditorGUI.BeginChangeCheck();
            obj.Update();

            // name
            SerializedProperty nameProp = obj.FindProperty("m_ParsedForm.m_Name");
            this.name = nameProp.stringValue;
            //fallback
            SerializedProperty fallbackProp = obj.FindProperty("m_ParsedForm.m_FallbackName");
            this.fallback = fallbackProp.stringValue;

            // props
            SerializedProperty propsproperty = obj.FindProperty( "m_ParsedForm.m_PropInfo.m_Props");
            propInfos = new List<PropInfo>(propsproperty.arraySize);
            for ( int i = 0; i < propsproperty.arraySize; ++i)
            {
                var prop = propsproperty.GetArrayElementAtIndex(i);
                var propInfo = new PropInfo(prop);
                propInfos.Add(propInfo);
            }

            // subShaders
            SerializedProperty subShadersProp = obj.FindProperty("m_ParsedForm.m_SubShaders");
            subShaderInfos = new List<SubShaderInfo>(subShadersProp.arraySize);
            for (int i = 0; i < subShadersProp.arraySize; ++i)
            {
                var currentSubShaderProp = subShadersProp.GetArrayElementAtIndex(i);
                var info = new SubShaderInfo(currentSubShaderProp);
                this.subShaderInfos.Add(info);
            }
        }
    }

}
