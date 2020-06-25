using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using System.Collections;

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

                if (keywords == null)
                {
                    keywords = serializedProperty.FindPropertyRelative("m_GlobalKeywordIndices");
                }
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
        public class ShaderState
        {
            [SerializeField]
            public string name;
            [SerializeField]
            public List<ShaderTagInfo> tags;
            public ShaderState(SerializedProperty serializedProperty)
            {
                this.name = serializedProperty.FindPropertyRelative("m_Name").stringValue;
                var tagsProp = serializedProperty.FindPropertyRelative("m_Tags.tags");

                tags = new List<ShaderTagInfo>( tagsProp.arraySize );

                for ( int i = 0; i< tagsProp.arraySize; ++i)
                {
                    var tagInfo = new ShaderTagInfo( tagsProp.GetArrayElementAtIndex(i) );
                    tags.Add(tagInfo);
                }
            }
        }

        [Serializable]
        public class ShaderTagInfo
        {
            [SerializeField]
            public string key;
            [SerializeField]
            public string value;

            public ShaderTagInfo(SerializedProperty serializedProperty)
            {
                var firstProp = serializedProperty.FindPropertyRelative("first");
                var secondProp = serializedProperty.FindPropertyRelative("second");

                this.key = firstProp.stringValue;
                this.value = secondProp.stringValue;

//                Debug.Log("tag::" + firstProp.stringValue + ":" + secondProp.stringValue);

            }
        }

        [Serializable]
        public class PassInfo
        {
            [SerializeField]
            public string useName;
            [SerializeField]
            public string name;

            [SerializeField]
            public ShaderState state;

            [SerializeField]
            public List<ShaderTagInfo> tags;
            [SerializeField]
            public List<GpuProgramInfo> vertInfos;
            [SerializeField]
            public List<GpuProgramInfo> fragmentInfos;

            private SerializedProperty serializedProperty;
            private IEnumerator execute;
            private Dictionary<int, string> keywordDictionary;


            public PassInfo(SerializedProperty prop) {
                this.serializedProperty = prop;
                this.execute = Execute();
            }

            public bool MoveNext()
            {
                return execute.MoveNext();
            }

            public IEnumerator Execute()
            {
                SetupShaderStage(serializedProperty);
                SetupTags(serializedProperty);
                SetupNameInfo(serializedProperty);
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
                yield return null;
            }
            private void SetupShaderStage(SerializedProperty serializedProperty)
            {
                var stateProp = serializedProperty.FindPropertyRelative("m_State");
                this.state = new ShaderState(stateProp);
            }

            private void SetupTags(SerializedProperty serializedProperty)
            {
                var tagsProp = serializedProperty.FindPropertyRelative("m_Tags.tags");

                tags = new List<ShaderTagInfo>(tagsProp.arraySize);
                for ( int i = 0; i < tagsProp.arraySize; ++i)
                {
                    var tagInfo = new ShaderTagInfo( serializedProperty.GetArrayElementAtIndex(i) );
                    tags.Add(tagInfo);
                }
            }


            private  void SetupNameInfo(SerializedProperty serializedProperty)
            {
                useName = serializedProperty.FindPropertyRelative("m_UseName").stringValue;
                name = serializedProperty.FindPropertyRelative("m_Name").stringValue;
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

            private SerializedProperty serializedProperty;
            private IEnumerator execute;

            public SubShaderInfo(SerializedProperty prop)
            {
                this.serializedProperty = prop;
                this.execute = Execute();
            }
            public bool MoveNext()
            {
                return this.execute.MoveNext();
            }

            private IEnumerator Execute() { 
                var passesProp = serializedProperty.FindPropertyRelative("m_Passes");
                passes = new List<PassInfo>(passesProp.arraySize);
                for (int i = 0; i < passesProp.arraySize; ++i)
                {
                    var currentPassProp = passesProp.GetArrayElementAtIndex(i);
                    var passInfo = new PassInfo(currentPassProp);
                    while (passInfo.MoveNext()) { }
                    passes.Add(passInfo);
                }
                yield return null;

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

        private const int PropSycle = 50;
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

        private SerializedObject serializedObject;
        private IEnumerator executeProgress;
        public bool IsComplete { get; set; } = false;

        public ShaderDumpInfo(Shader sh)
        {
            this.serializedObject = new SerializedObject(sh);
            executeProgress = Execute();
        }


        public bool MoveNext()
        {
            return executeProgress.MoveNext();
        }

        private IEnumerator Execute() { 

            //EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            // name
            SerializedProperty nameProp = serializedObject.FindProperty("m_ParsedForm.m_Name");
            this.name = nameProp.stringValue;
            //fallback
            SerializedProperty fallbackProp = serializedObject.FindProperty("m_ParsedForm.m_FallbackName");
            this.fallback = fallbackProp.stringValue;
            yield return null;

            // props
            SerializedProperty propsproperty = serializedObject.FindProperty( "m_ParsedForm.m_PropInfo.m_Props");
            propInfos = new List<PropInfo>(propsproperty.arraySize);
            for ( int i = 0; i < propsproperty.arraySize; ++i)
            {
                var prop = propsproperty.GetArrayElementAtIndex(i);
                var propInfo = new PropInfo(prop);
                propInfos.Add(propInfo);
            }
            yield return null;
            // subShaders
            SerializedProperty subShadersProp = serializedObject.FindProperty("m_ParsedForm.m_SubShaders");
            subShaderInfos = new List<SubShaderInfo>(subShadersProp.arraySize);
            for (int i = 0; i < subShadersProp.arraySize; ++i)
            {
                var currentSubShaderProp = subShadersProp.GetArrayElementAtIndex(i);
                var info = new SubShaderInfo(currentSubShaderProp);
                while (info.MoveNext()) { }
                this.subShaderInfos.Add(info);
            }
            this.IsComplete = true;
        }
    }

}
