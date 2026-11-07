using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LaserHero.Editor
{
    public static class LaserReferenceArchSetup
    {
        const string Folder = "Assets/LaserHero/Architecture";
        static Material plastic;
        static readonly Color Violet = new Color(.37f, .27f, 1), Blue = new Color(.33f, .3f, 1);
        public static string Create()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before creating the arch");
            var existing = GameObject.Find("Central LEGO Arch");
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                return "Existing arch selected";
            }

            var scene = SceneManager();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Backups/Lobby_BeforeReferenceArch.unity", true);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/LaserHero", "Architecture");
            var source = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Select(r => r.sharedMaterial).FirstOrDefault(m => m != null && m.shader.name == "LaserHero/Stud Plastic"
                && m.GetTexture("_MainTex") != null);
            if (source == null)
                throw new Exception("LEGO material missing");
            plastic = new Material(source)
            {
                name = "Arch LEGO Plastic"
            };
            plastic.SetFloat("_UseVertexColor", 0);
            plastic.SetFloat("_IsDynamic", 1);
            plastic.SetFloat("_UseStudAnchor", 0);
            plastic.SetColor("_Color", Color.white);
            plastic.SetFloat("_Scale", 1.5f);
            plastic.enableInstancing = true;
            AssetDatabase.CreateAsset(plastic, Folder + "/ArchLEGO.mat");
            var root = new GameObject("Central LEGO Arch");
            root.transform.position = new Vector3(0, 0, 14);
            Undo.RegisterCreatedObjectUndo(root, "Create reference LEGO arch");
            var supports = new GameObject("01 — Опоры");
            supports.transform.SetParent(root.transform, false);
            foreach (int side in new[]
            {
                -1,
                1
            }

            )
            {
                float x = side * 12.2f;
                string title = side < 0 ? "Левая" : "Правая";
                Box(supports.transform, title + " подошва", new Vector3(x, .25f, 0), new Vector3(4.1f, .5f,
                    4.1f), 0, Violet * .93f);
                for (int j = 0; j < 3; j++)
                    Box(supports.transform, title + " опора — блок " + (j + 1), new Vector3(x,
                        1.1f + j * 1.2f, 0), new Vector3(3.5f, 1.21f, 3.4f), 0, j % 2 == 0 ? Blue : Violet);
            }

            var ring = new GameObject("02 — Сегменты полукруга");
            ring.transform.SetParent(root.transform, false);
            const int count = 17;
            float delta = 180f / count;
            for (int i = 0; i < count; i++)
            {
                float a = (i + .5f) * delta;
                float radians = a * Mathf.Deg2Rad;
                bool crown = i == count / 2;
                Box(ring.transform, crown ? "09 — Центральный замковый блок"
                    : (i + 1).ToString("00") + " — Блок дуги", new Vector3(Mathf.Cos(radians) * 12.2f,
                    3.7f + Mathf.Sin(radians) * 12.2f + (crown ? .12f : 0), 0), new Vector3(crown ? 2.65f : 2.51f, crown
                    ? 3.85f : 3.6f, 3.4f + (crown ? .16f : 0)), a - 90, i % 2 == 0 ? Violet : Blue);
            }

            var infill = new GameObject("03 — Верхняя вставка");
            infill.transform.SetParent(root.transform, false);
            for (int i = 0; i < 9; i++)
            {
                float left = -9.9f + i * 2.2f, right = left + 2.2f;
                float topLeft = 3.7f + Mathf.Sqrt(11.35f * 11.35f - left * left), topRight =
                    3.7f + Mathf.Sqrt(11.35f * 11.35f - right * right);
                var go = new GameObject("Вставка — блок " + (i + 1), typeof(MeshFilter),
                    typeof(MeshRenderer), typeof(MeshCollider));
                go.transform.SetParent(infill.transform, false);
                Mesh mesh = Panel(left, right, 9.05f, topLeft, topRight);
                AssetDatabase.CreateAsset(mesh, Folder + "/ArchInfill" + i + ".asset");
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = plastic;
                go.GetComponent<MeshCollider>().sharedMesh = mesh;
                go.AddComponent<LaserArchBlock>().blockColor = Color.Lerp(Blue, Violet, .3f);
                go.GetComponent<LaserArchBlock>().ApplyColor();
            }

            PrefabUtility.SaveAsPrefabAssetAndConnect(root, Folder + "/CentralLEGOArch.prefab", InteractionMode.AutomatedAction);
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Reference arch saved: 34 independently recolourable blocks, 28m wide, 17.95m high; central lobby path (0,0,14)";
        }

        static UnityEngine.SceneManagement.Scene SceneManager() => UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        static void Box(Transform parent, string title, Vector3 position, Vector3 size, float angle, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = title;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0, 0, angle);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = plastic;
            var block = go.AddComponent<LaserArchBlock>();
            block.blockColor = color;
            block.ApplyColor();
        }

        static Mesh Panel(float left, float right, float bottom, float topLeft, float topRight)
        {
            // Separate trapezoid bricks close the curved top while keeping the opening straight.
            Vector3[] corners =
            {
                new Vector3(left, bottom, -1.35f),
                new Vector3(right, bottom, -1.35f),
                new Vector3(right, topRight, -1.35f),
                new Vector3(left, topLeft, -1.35f),
                new Vector3(left, bottom, 1.35f),
                new Vector3(right, bottom, 1.35f),
                new Vector3(right, topRight, 1.35f),
                new Vector3(left, topLeft, 1.35f)
            };
            int[] indices =
            {
                0,
                3,
                2,
                1,
                5,
                6,
                7,
                4,
                0,
                4,
                7,
                3,
                1,
                2,
                6,
                5,
                3,
                7,
                6,
                2,
                0,
                1,
                5,
                4
            };
            var vertices = new Vector3[24];
            var triangles = new int[36];
            for (int face = 0; face < 6; face++)
            {
                for (int k = 0; k < 4; k++)
                    vertices[face * 4 + k] = corners[indices[face * 4 + k]];
                int v = face * 4, t = face * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            var mesh = new Mesh
            {
                name = "Arch infill brick"
            };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
