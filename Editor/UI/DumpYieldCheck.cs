using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace UTJ
{
    public class DumpYieldCheck
    {
        public bool ShouldYield()
        {
            double time = EditorApplication.timeSinceStartup;
            if(time - this.startTime > 0.03f)
            {
                return true;
            }
            return false;
        }

        public float Progress {
            get
            {
                float subShader = (subShaderIdx+1) / (float)subShaderCount;
                return subShader;
            }
        }
        public string CurrentState
        {
            get
            {
                return "";
            }
        }

        private double startTime;

        private int subShaderCount;
        private int subShaderIdx;
        private int passCount;
        private int passIdx;
        private int vertCount;
        private int vertIdx;
        private int fragCount;
        private int fragIdx;



        public void SetSubShaderIdx(int idx)
        {
            subShaderIdx = idx;
        }

        public void SetSubshaderCount(int subCnt)
        {
            subShaderCount = subCnt;
        }

        public void SetPassIdx(int idx)
        {
            passIdx = idx;
        }
        public void SetPassCount(int passCnt)
        {
            passCount = passCnt;
        }

        public void SetVertexIdx(int idx)
        {
            vertIdx = idx;
        }
        public void SetVertexNum(int cnt)
        {
            vertCount = cnt;
        }

        public void SetFragmentIdx(int idx)
        {
            fragIdx = idx;
        }
        public void SetFragmentNum(int cnt)
        {
            fragCount = cnt;
        }

        public void SetYieldCheckTime()
        {
            startTime = EditorApplication.timeSinceStartup;
        }
    }
}