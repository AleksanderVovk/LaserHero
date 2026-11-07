using System.Collections.Generic;
using UnityEngine;

namespace LaserHero
{
    // Keep the source bricks editable in the prefab; combine only their render geometry in Play Mode.
    public class LaserStallRenderBatch : MonoBehaviour
    {
        public Material sourceMaterial;
        readonly List<MeshRenderer> sources = new List<MeshRenderer>();
        Mesh mesh;
        Material material;
        GameObject batch;
        void Start()
        {
            var instances = new List<CombineInstance>();
            var temporary = new List<Mesh>();
            foreach (var part in GetComponentsInChildren<LaserArchBlock>())
            {
                var renderer = part.GetComponent<MeshRenderer>();
                var filter = part.GetComponent<MeshFilter>();
                if (renderer == null || filter == null || renderer.sharedMaterial != sourceMaterial)
                    continue;
                var copy = Instantiate(filter.sharedMesh);
                var colors = new Color[copy.vertexCount];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = part.blockColor;
                copy.colors = colors;
                for (int s = 0; s < copy.subMeshCount; s++)
                    instances.Add(new CombineInstance
                    {
                        mesh = copy,
                        subMeshIndex = s,
                        transform = transform.worldToLocalMatrix * part.transform.localToWorldMatrix
                    });
                temporary.Add(copy);
                sources.Add(renderer);
            }

            if (instances.Count == 0)
                return;
            mesh = new Mesh
            {
                name = "Market stall combined plastic",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            mesh.CombineMeshes(instances.ToArray());
            foreach (var copy in temporary)
                Destroy(copy);
            material = new Material(sourceMaterial);
            material.SetFloat("_UseVertexColor", 1);
            material.SetFloat("_IsDynamic", 1);
            material.SetVector("_ObjectScale", new Vector4(transform.lossyScale.x, transform.lossyScale.y,
                transform.lossyScale.z, 1));
            material.SetColor("_Color", Color.white);
            batch = new GameObject("Combined stall render");
            batch.transform.SetParent(transform, false);
            batch.AddComponent<MeshFilter>().sharedMesh = mesh;
            batch.AddComponent<MeshRenderer>().sharedMaterial = material;
            foreach (var source in sources)
                source.enabled = false;
        }

        void OnDisable()
        {
            if (batch != null)
                batch.SetActive(false);
            foreach (var source in sources)
                if (source != null)
                    source.enabled = true;
        }

        void OnEnable()
        {
            if (batch != null)
            {
                batch.SetActive(true);
                foreach (var source in sources)
                    if (source != null)
                        source.enabled = false;
            }
        }

        void OnDestroy()
        {
            if (mesh != null)
                Destroy(mesh);
            if (material != null)
                Destroy(material);
        }
    }
}
