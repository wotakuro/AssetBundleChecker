using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UTJ
{
    internal class InstanciateGameObjectUI:System.IDisposable
    {
        InstanciateGameObjectFromAb instanciateGameObject;

        internal InstanciateGameObjectUI(InstanciateGameObjectFromAb inst)
        {
            this.instanciateGameObject = inst;
        }

        internal void AddToParent(VisualElement parent)
        {
            VisualElement element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;

            Label label = new Label(instanciateGameObject.gameObject.name);
            Button btn = new Button();
            btn.text = "aa";


            element.Add(label);
            element.Add(btn);

            parent.Add(element);
        }

        public void Dispose()
        {
        }
    }
}
