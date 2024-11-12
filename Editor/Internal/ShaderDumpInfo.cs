using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Text;

namespace UTJ
{

    public class ShaderDumpInfo
    {
        [Serializable]
        public class GpuProgramInfo
        {
            [SerializeField]
            public int tierIndex;

            [SerializeField]
            public string gpuProgramType;
            [SerializeField]
            public List<int> keywordIndecies;
            [SerializeField]
            public List<int> localKeywordIndecies;
            [SerializeField]
            public List<string> keywords;

            [NonSerialized]
            public string ConbinedKeyword;

            public GpuProgramInfo(SerializedProperty serializedProperty, int tier)
            {
                var gpuProgramTypeProp = serializedProperty.FindPropertyRelative("m_GpuProgramType");
                this.tierIndex = tier;
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
                   // Debug.Log("keywordIndex[" + i + "]" + keywordIndex);
                }
                var localKeywords = serializedProperty.FindPropertyRelative("m_LocalKeywordIndices");
                if (localKeywords != null)
                {
                    localKeywordIndecies = new List<int>();
                    for (int i = 0; i < localKeywords.arraySize; ++i)
                    {
                        int keywordIndex = localKeywords.GetArrayElementAtIndex(i).intValue;
                        localKeywordIndecies.Add(keywordIndex);
                    }
                }
            }
#if UNITY_2021_3_OR_NEWER
            public void ResolveKeywordName(List<string> keywordNames)
            {
                keywords = new List<string>(keywordIndecies.Count);
                for (int i = 0; i < keywordIndecies.Count; ++i)
                {
                    int index = keywordIndecies[i];
                    if (index < keywordNames.Count)
                    {
                        keywords.Add(keywordNames[index]);
                    }
                    else
                    {
                        Debug.LogError("ResolveKeywordName failed " + index + "::" + keywordIndecies.Count);
                    }
                }
                if (localKeywordIndecies != null)
                {
                    for (int i = 0; i < this.localKeywordIndecies.Count; ++i)
                    {
                        int index = localKeywordIndecies[i];
                        if (index < keywordNames.Count)
                        {
                            keywords.Add(keywordNames[index]);
                        }
                        else
                        {
                            Debug.LogError("ResolveKeywordName failed " + index + "::" + keywordIndecies.Count);
                        }
                    }
                }

                this.ResolvedConbinedKeyword();
            }
#else
            public void ResolveKeywordName(Dictionary<int, string> dictionary)
            {
                keywords = new List<string>(keywordIndecies.Count);
                for (int i = 0; i < keywordIndecies.Count; ++i)
                {
                    int index = keywordIndecies[i];
                    string val = null;
                    if (dictionary.TryGetValue(index, out val))
                    {
                        keywords.Add(val);
                    }
                }
                if (localKeywordIndecies != null)
                {
                    for (int i = 0; i < this.localKeywordIndecies.Count; ++i)
                    {
                        int index = localKeywordIndecies[i];
                        string val = null;
                        if (dictionary.TryGetValue(index, out val))
                        {
                            keywords.Add(val);
                        }
                    }
                }

                this.ResolvedConbinedKeyword();
            }

#endif

            private void ResolvedConbinedKeyword()
            {
                var sortedKeywords = new List<string>(keywords);
                sortedKeywords.Sort();
                int length = 0;
                foreach (var word in sortedKeywords)
                {
                    length += word.Length + 1;
                }
                StringBuilder builder = new StringBuilder(length);
                foreach (var word in sortedKeywords)
                {
                    builder.Append(word).Append(' ');
                }
                if (builder.Length <= 1)
                {
                    this.ConbinedKeyword = "<none>";
                }
                else
                {
                    this.ConbinedKeyword = builder.ToString();
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

                tags = new List<ShaderTagInfo>(tagsProp.arraySize);

                for (int i = 0; i < tagsProp.arraySize; ++i)
                {
                    var tagInfo = new ShaderTagInfo(tagsProp.GetArrayElementAtIndex(i));
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
        public class KeywordDictionaryInfo
        {
            [SerializeField]
            public int idx;
            [SerializeField]
            public string keyword;
            public KeywordDictionaryInfo(int index, string key)
            {
                this.idx = index;
                this.keyword = key;
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
            public List<KeywordDictionaryInfo> keywordInfos;

            [SerializeField]
            public List<GpuProgramInfo> vertInfos;
            [SerializeField]
            public List<GpuProgramInfo> fragmentInfos;

            private SerializedProperty serializedProperty;
            private IEnumerator execute;
            private Dictionary<int, string> keywordDictionary;
            private DumpYieldCheck yieldChk;
            private ShaderDumpInfo dumpInfoObject;

            public PassInfo(ShaderDumpInfo dumpInfo, SerializedProperty prop, DumpYieldCheck yieldCheck)
            {
                this.dumpInfoObject = dumpInfo;
                this.serializedProperty = prop;
                this.yieldChk = yieldCheck;
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
#if !UNITY_2021_3_OR_NEWER
                SetupKeywordDictionary(serializedProperty);
#endif
                var shaderExecute = ExecuteShader();
                while (shaderExecute.MoveNext())
                {
                    yield return null;
                }
                yield return null;
            }
#if UNITY_2021_3_OR_NEWER
            public IEnumerator ExecuteShader()
            {
                var progVertex = serializedProperty.FindPropertyRelative("progVertex.m_PlayerSubPrograms");
                var progFragment = serializedProperty.FindPropertyRelative("progFragment.m_PlayerSubPrograms");
                yield return null;
                int vertexNum = GetSubProgramNum(progVertex);
                int fragmentNum = GetSubProgramNum(progFragment);
                yieldChk.SetVertexNum(vertexNum);
                yieldChk.SetFragmentNum(fragmentNum);
                vertInfos = new List<GpuProgramInfo>(vertexNum);
                fragmentInfos = new List<GpuProgramInfo>(fragmentNum);

                int vertTierNum = progVertex.arraySize;
                for (int tierIndex = 0; tierIndex < vertTierNum; ++tierIndex)
                {
                    var tierPrograms = progVertex.GetArrayElementAtIndex(tierIndex);
                    var vertExec = ExecuteGPUPrograms(vertInfos, tierIndex, tierPrograms, tierPrograms.arraySize, yieldChk.CompleteVertIdx);
                    while (vertExec.MoveNext())
                    {
                        yield return null;
                    }
                }
                int fragTierNum = progFragment.arraySize;
                for (int tierIndex = 0; tierIndex < fragTierNum; ++tierIndex)
                {
                    var tierPrograms = progFragment.GetArrayElementAtIndex(tierIndex);
                    var fragExec = ExecuteGPUPrograms(fragmentInfos, tierIndex, tierPrograms, tierPrograms.arraySize, yieldChk.CompleteFragIdx);
                    while (fragExec.MoveNext())
                    {
                        yield return null;
                    }
                }
            }

            private int GetSubProgramNum(SerializedProperty prop)
            {
                int num = 0;
                int tierNum = prop.arraySize;
                for(int i = 0; i < tierNum; i++)
                {
                    var tierPrograms = prop.GetArrayElementAtIndex(i);
                    num += tierPrograms.arraySize;
                }
                return num;
            }
#else

            public IEnumerator ExecuteShader()
            {
                var progVertex = serializedProperty.FindPropertyRelative("progVertex.m_SubPrograms");
                var progFragment = serializedProperty.FindPropertyRelative("progFragment.m_SubPrograms");

                int vertNum = progVertex.arraySize;
                int fragNum = progFragment.arraySize;


                vertInfos = new List<GpuProgramInfo>(vertNum);
                fragmentInfos = new List<GpuProgramInfo>(fragNum);
                // vertex
                yieldChk.SetVertexNum(vertNum);
                var vertExec = ExecuteGPUPrograms(vertInfos, -1 ,progVertex, vertNum, yieldChk.CompleteVertIdx);
                while (vertExec.MoveNext())
                {
                    yield return null;
                }
                // fragment
                yieldChk.SetFragmentNum(fragNum);
                var fragExec = ExecuteGPUPrograms(fragmentInfos, -1 , progFragment, fragNum, yieldChk.CompleteFragIdx);
                while (fragExec.MoveNext())
                {
                    yield return null;
                }
            }

#endif
            private IEnumerator ExecuteGPUPrograms(List<GpuProgramInfo> programs, int tier, SerializedProperty props, int num, Action<int> onCompleteIndex)
            {
                for (int i = 0; i < num; ++i)
                {
                    var gpuProgram = new GpuProgramInfo(props.GetArrayElementAtIndex(i), tier);
#if UNITY_2021_3_OR_NEWER
                    gpuProgram.ResolveKeywordName( this.dumpInfoObject.keywordNames );
#else
                    gpuProgram.ResolveKeywordName(keywordDictionary);
#endif
                    programs.Add(gpuProgram);
                    // yield
                    onCompleteIndex(i);
                    if (yieldChk.ShouldYield())
                    {
                        yield return null;
                    }
                }

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
                for (int i = 0; i < tagsProp.arraySize; ++i)
                {
                    var tagInfo = new ShaderTagInfo(serializedProperty.GetArrayElementAtIndex(i));
                    tags.Add(tagInfo);
                }
            }


            private void SetupNameInfo(SerializedProperty serializedProperty)
            {
                useName = serializedProperty.FindPropertyRelative("m_UseName").stringValue;
                name = serializedProperty.FindPropertyRelative("m_Name").stringValue;
            }

#if !UNITY_2021_3_OR_NEWER
            private void SetupKeywordDictionary(SerializedProperty serializedProperty)
            {
                var nameIndices = serializedProperty.FindPropertyRelative("m_NameIndices");
                int nameSize = nameIndices.arraySize;
                this.keywordDictionary = new Dictionary<int, string>(nameSize);
                this.keywordInfos = new List<KeywordDictionaryInfo>(nameSize);
                for (int k = 0; k < nameSize; ++k)
                {
                    var currentNameIndecies = nameIndices.GetArrayElementAtIndex(k).FindPropertyRelative("first");
                    var nameIndex = nameIndices.GetArrayElementAtIndex(k).FindPropertyRelative("second");


                    if (!keywordDictionary.ContainsKey(nameIndex.intValue))
                    {
                        int idxVal = nameIndex.intValue;
                        string strVal = currentNameIndecies.stringValue;
                        keywordDictionary.Add(idxVal, strVal);
                        keywordInfos.Add(new KeywordDictionaryInfo(idxVal, strVal));
                    }
                }

            }
#endif
        }

        [Serializable]
        public class SubShaderInfo
        {
            [SerializeField]
            public List<PassInfo> passes;

            private SerializedProperty serializedProperty;
            private IEnumerator execute;
            private DumpYieldCheck yieldChk;

            private ShaderDumpInfo dumpInfoObject;

            public SubShaderInfo(ShaderDumpInfo dumpInfo,SerializedProperty prop, DumpYieldCheck yieldCheck)
            {
                this.dumpInfoObject = dumpInfo;
                this.serializedProperty = prop;
                this.yieldChk = yieldCheck;
                this.execute = Execute();
            }
            public bool MoveNext()
            {
                return this.execute.MoveNext();
            }

            private IEnumerator Execute()
            {
                var passesProp = serializedProperty.FindPropertyRelative("m_Passes");
                int passCnt = passesProp.arraySize;
                passes = new List<PassInfo>(passCnt);
                this.yieldChk.SetPassCount(passCnt);
                for (int i = 0; i < passesProp.arraySize; ++i)
                {
                    var currentPassProp = passesProp.GetArrayElementAtIndex(i);
                    var passInfo = new PassInfo(this.dumpInfoObject,currentPassProp, yieldChk);
                    while (passInfo.MoveNext())
                    {
                        yield return null;
                    }
                    passes.Add(passInfo);
                    // yield
                    yieldChk.CompletePassIdx(i);
                    if (yieldChk.ShouldYield())
                    {
                        yield return null;
                    }
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

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
        public List<string> keywordNames;
        [SerializeField]
        public List<int> keywordFlags;

#endif

        private SerializedObject serializedObject;
        private IEnumerator executeProgress;
        public bool IsComplete { get; set; } = false;
        public float Progress
        {
            get
            {
                if (this.yieldChk == null) { return 0.0f; }
                return this.yieldChk.Progress;
            }
        }
        public string ProgressStr
        {
            get
            {
                if (this.yieldChk == null) { return ""; }
                return this.yieldChk.CurrentState;
            }

        }


        private DumpYieldCheck yieldChk = new DumpYieldCheck();

        public ShaderDumpInfo(Shader sh)
        {
            this.serializedObject = new SerializedObject(sh);
            executeProgress = Execute();
        }


        public List<string> CollectKeywords()
        {
            HashSet<string> hashedKeywords = new HashSet<string>();
            foreach (var subshader in this.subShaderInfos)
            {
                foreach (var pass in subshader.passes)
                {
                    foreach (var gpuProgram in pass.fragmentInfos)
                    {
                        hashedKeywords.Add(gpuProgram.ConbinedKeyword);
                    }
                    foreach (var gpuProgram in pass.vertInfos)
                    {
                        hashedKeywords.Add(gpuProgram.ConbinedKeyword);
                    }
                }
            }
            var retList = new List<string>(hashedKeywords);
            retList.Sort();
            return retList;
        }

        public bool MoveNext()
        {
            return executeProgress.MoveNext();
        }

        public void SetYieldCheckTime()
        {
            this.yieldChk.SetYieldCheckTime();
        }

#if UNITY_2021_3_OR_NEWER
        private void ExecuteKeywordInfos()
        {
            // names
            SerializedProperty keywordNamesProp = serializedObject.FindProperty("m_ParsedForm.m_KeywordNames");
            int keywordNameNum = keywordNamesProp.arraySize;
            this.keywordNames = new List<string>(keywordNameNum);
            for(int i = 0;i< keywordNameNum; ++i)
            {
                keywordNames.Add(keywordNamesProp.GetArrayElementAtIndex(i).stringValue);
            }
            // flags
            SerializedProperty flagsProp = serializedObject.FindProperty("m_ParsedForm.m_KeywordFlags");
            int flagsNum = flagsProp.arraySize;
            this.keywordFlags = new List<int>(flagsNum);
            for (int i = 0; i < flagsNum; ++i)
            {
                this.keywordFlags.Add(flagsProp.GetArrayElementAtIndex(i).intValue);
            }
        }
#endif

        private IEnumerator Execute()
        {

            //EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            /*
            var prop = serializedObject.GetIterator();
            while (prop.Next(true))
            {
                Debug.Log(prop.name + "::" + prop.stringValue);
            }
            yield return null;
            */
            // name
            SerializedProperty nameProp = serializedObject.FindProperty("m_ParsedForm.m_Name");
            this.name = nameProp.stringValue;
            //fallback
            SerializedProperty fallbackProp = serializedObject.FindProperty("m_ParsedForm.m_FallbackName");
            this.fallback = fallbackProp.stringValue;

            // props
            SerializedProperty propsproperty = serializedObject.FindProperty("m_ParsedForm.m_PropInfo.m_Props");
            propInfos = new List<PropInfo>(propsproperty.arraySize);
            for (int i = 0; i < propsproperty.arraySize; ++i)
            {
                var prop = propsproperty.GetArrayElementAtIndex(i);
                var propInfo = new PropInfo(prop);
                propInfos.Add(propInfo);
            }
            yield return null;

            // keyword names
#if UNITY_2021_3_OR_NEWER
            ExecuteKeywordInfos();
            yield return null;
#endif
            // subShaders
            SerializedProperty subShadersProp = serializedObject.FindProperty("m_ParsedForm.m_SubShaders");
            int subShaderNum = subShadersProp.arraySize;
            subShaderInfos = new List<SubShaderInfo>(subShaderNum);
            yieldChk.SetSubshaderCount(subShaderNum);

            for (int i = 0; i < subShadersProp.arraySize; ++i)
            {
                var currentSubShaderProp = subShadersProp.GetArrayElementAtIndex(i);
                var info = new SubShaderInfo(this,currentSubShaderProp, yieldChk);
                while (info.MoveNext())
                {
                    yield return null;
                }
                this.subShaderInfos.Add(info);
                // yield chk
                yieldChk.CompleteSubShaderIdx(i);
                if (yieldChk.ShouldYield())
                {
                    yield return null;
                }

            }
            this.IsComplete = true;
        }
    }

}
