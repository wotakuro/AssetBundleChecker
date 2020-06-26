using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UTJ
{
    public class DumpProgressUI:VisualElement
    {
        private ProgressBar progressBar;
        private Label label;
        public float value
        {
            set
            {
                progressBar.value = value;
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
            this.progressBar = new ProgressBar();
            this.Add(this.progressBar);

            this.label = new Label();
            this.Add(this.label);
        }
    }
}