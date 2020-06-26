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
    internal class InstanciateGameObjectUI:System.IDisposable
    {
        private InstanciateGameObjectFromAb instanciateGameObject;
        private Button flipShaderButton;

        internal InstanciateGameObjectUI(InstanciateGameObjectFromAb inst)
        {
            this.instanciateGameObject = inst;
        }

        internal void AddToParent(VisualElement parent)
        {
            VisualElement element = new VisualElement();
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            element.style.flexDirection = FlexDirection.Row;
#endif

            Label label = new Label(instanciateGameObject.gameObject.name);
            flipShaderButton = new Button();
            flipShaderButton.text = "AssetBundle Shader";
            flipShaderButton.clickable.clicked += FlipAbProjectShader;

            element.Add(label);
            element.Add(flipShaderButton);
            parent.Add(element);
        }

        void FlipAbProjectShader()
        {
            if( this.instanciateGameObject.IsProjectShader)
            {
                this.instanciateGameObject.SetAbOrigin();
                flipShaderButton.text = "AssetBundle Shader";
            }
            else
            {
                this.instanciateGameObject.SetProjectOrigin();
                flipShaderButton.text = "Project Shader";
            }
        }

        public void Dispose()
        {
        }
    }
}
