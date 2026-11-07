using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LaserHero
{
    [DefaultExecutionOrder(500)]
    public class LaserWorldRenderBatcher : MonoBehaviour
    {
        [Serializable]
        public class Group
        {
            public LaserVoxelBody[] bodies;
            public MeshFilter filter;
            public MeshRenderer renderer;
            [NonSerialized]
            public Mesh runtimeMesh;
        }

        public Group[] groups;
        public int RebuildCount { get; private set; }

        readonly Dictionary<LaserVoxelBody, Group> membership = new Dictionary<LaserVoxelBody, Group>();
        readonly HashSet<Group> dirty = new HashSet<Group>();
        readonly List<CombineInstance> instances = new List<CombineInstance>(32);
        void OnEnable()
        {
            membership.Clear();
            dirty.Clear();
            if (groups == null)
                return;
            foreach (var group in groups)
            {
                group.renderer.enabled = true;
                foreach (var body in group.bodies)
                    if (body != null)
                    {
                        membership[body] = group;
                        body.GetComponent<MeshRenderer>().enabled = false;
                    }

                // A dormant arena may have been repaired while we were unsubscribed.
                dirty.Add(group);
            }

            LaserVoxelBody.SurfaceChanged += Changed;
        }

        void Changed(LaserVoxelBody body)
        {
            if (body != null && membership.TryGetValue(body, out var group))
                dirty.Add(group);
        }

        void LateUpdate()
        {
            foreach (var group in dirty)
                Rebuild(group);
            dirty.Clear();
        }

        public void Flush()
        {
            LateUpdate();
        }

        public void Rebuild(Group group)
        {
            instances.Clear();
            Matrix4x4 local = group.filter.transform.worldToLocalMatrix;
            foreach (var body in group.bodies)
            {
                if (body == null || !body.isActiveAndEnabled)
                    continue;
                var mesh = body.GetComponent<MeshFilter>().sharedMesh;
                if (mesh == null || mesh.vertexCount == 0)
                    continue;
                body.GetComponent<MeshRenderer>().enabled = false;
                instances.Add(new CombineInstance { mesh = mesh, subMeshIndex = 0, transform =
                    local * body.transform.localToWorldMatrix });
            }

            if (group.runtimeMesh == null)
            {
                group.runtimeMesh = new Mesh
                {
                    name = "Destructible sector render surface",
                    indexFormat = IndexFormat.UInt32
                };
                group.runtimeMesh.MarkDynamic();
            }

            group.runtimeMesh.Clear();
            if (instances.Count > 0)
                group.runtimeMesh.CombineMeshes(instances.ToArray(), true, true, false);
            group.filter.sharedMesh = group.runtimeMesh;
            group.renderer.enabled = instances.Count > 0;
            RebuildCount++;
        }

        void OnDisable()
        {
            LaserVoxelBody.SurfaceChanged -= Changed;
            if (groups == null)
                return;
            foreach (var group in groups)
            {
                if (group.renderer != null)
                    group.renderer.enabled = false;
                foreach (var body in group.bodies)
                    if (body != null)
                        body.GetComponent<MeshRenderer>().enabled = true;
            }
        }

        void OnDestroy()
        {
            if (groups != null)
                foreach (var group in groups)
                    if (group.runtimeMesh != null)
                        Destroy(group.runtimeMesh);
        }
    }
}
