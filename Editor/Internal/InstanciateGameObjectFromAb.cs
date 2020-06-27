using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UTJ
{
    internal class InstanciateGameObjectFromAb
    {
        public GameObject gameObject;
        private List<Renderer> allRenderers;
        private Dictionary<Renderer, Material[]> abOriginMaterials;
        private Dictionary<Renderer, Material[]> projShaderMaterials;


        public bool IsProjectShader { get; private set; }

        public InstanciateGameObjectFromAb(GameObject prefab)
        {
            gameObject = GameObject.Instantiate<GameObject>(prefab);
            allRenderers = new List<Renderer>(gameObject.GetComponentsInChildren<Renderer>(true));
            abOriginMaterials = new Dictionary<Renderer, Material[]>(allRenderers.Count);
            projShaderMaterials = new Dictionary<Renderer, Material[]>(allRenderers.Count);

            foreach (var renderer in allRenderers)
            {
                var materials = renderer.sharedMaterials;
                abOriginMaterials.Add(renderer, materials);
                var projectMaterials = CreateProjectShaderMaterials(materials);

                projShaderMaterials.Add(renderer, projectMaterials);
            }
            IsProjectShader = false;
        }

        private Material[] CreateProjectShaderMaterials(Material[] materials)
        {
            if (materials == null) { return null; }
            Material[] newMaterials = new Material[materials.Length];

            for (int i = 0; i < materials.Length; ++i)
            {
                var originMaterial = materials[i];
                if (originMaterial == null || originMaterial.shader == null)
                {
                    newMaterials[i] = null;
                    continue;
                }
                newMaterials[i] = new Material(originMaterial);
                newMaterials[i].shader = Shader.Find(originMaterial.shader.name);
            }

            return newMaterials;
        }

        public void SetAbOrigin()
        {
            SetMaterials(abOriginMaterials);
            IsProjectShader = false;
        }
        public void SetProjectOrigin()
        {
            SetMaterials(projShaderMaterials);
            IsProjectShader = true;
        }
        private void SetMaterials(Dictionary<Renderer, Material[]> setData)
        {
            foreach (var renderer in allRenderers)
            {
                renderer.materials = setData[renderer];
            }
        }

        public void Destroy()
        {
            Object.DestroyImmediate(gameObject);
        }

    }

}