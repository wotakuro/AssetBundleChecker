using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

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
            element.style.flexDirection = FlexDirection.Row;

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
