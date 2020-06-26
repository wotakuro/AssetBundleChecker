using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace UTJ
{
    public class DumpProgressUI:VisualElement
    {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
        private ProgressBar progressBar;
#endif
        private Label label;
        public float value
        {
            set
            {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
                progressBar.value = value;
#endif
            }
        }
        public string text
        {
            set
            {
                this.label.text = value;
            }
        }

        public DumpProgressUI()
        {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            this.progressBar = new ProgressBar();
            this.Add(this.progressBar);
#endif

            this.label = new Label();
            this.Add(this.label);
        }
    }
}