using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LaserHero.Editor
{
    public static class LaserMarketStallSetup
    {
        const string Folder = "Assets/LaserHero/Architecture/MarketStall";
        static Material plastic, fire, green, ice;
        static Mesh gem;
        static readonly Color Wood = new Color(.77f, .38f, .075f), Oak = new Color(1, .58f, .16f), LightWood =
            new Color(.9f, .68f, .25f), Cyan = new Color(.27f, .83f, 1), Blue = new Color(.08f, .5f, .95f);
        static Transform Group(Transform root, string title)
        {
            var g = new GameObject(title);
            g.transform.SetParent(root, false);
            return g.transform;
        }

        static GameObject Box(Transform parent, string title, Vector3 position, Vector3 size, Color color,
            Vector3 rotation = default)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = title;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.transform.localRotation = Quaternion.Euler(rotation);
            go.GetComponent<Renderer>().sharedMaterial = plastic;
            var block = go.AddComponent<LaserArchBlock>();
            block.blockColor = color;
            block.ApplyColor();
            return go;
        }

        static GameObject Cylinder(Transform parent, string title, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = title;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = plastic;
            var block = go.AddComponent<LaserArchBlock>();
            block.blockColor = color;
            block.ApplyColor();
            return go;
        }

        static Material Glow(string name, Color color)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = name
            };
            m.SetColor("_BaseColor", color);
            m.SetColor("_EmissionColor", color * 3);
            m.EnableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(m, Folder + "/" + name + ".mat");
            return m;
        }

        public static string Create()
        {
            if (Application.isPlaying)
                throw new Exception("Stop Play Mode");
            var existing = GameObject.Find("LEGO Market Stall");
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                return "Existing stall selected";
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Backups/Lobby_BeforeMarketStall.unity", true);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/LaserHero/Architecture", "MarketStall");
            plastic = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/LaserHero/Architecture/ArchLEGO.mat"))
            {
                name = "Market LEGO Plastic"
            };
            plastic.SetFloat("_Scale", .8f);
            plastic.SetFloat("_UseVertexColor", 0);
            AssetDatabase.CreateAsset(plastic, Folder + "/MarketLEGO.mat");
            fire = Glow("Amber Magic", new Color(1, .32f, .015f));
            green = Glow("Lime Magic", new Color(.2f, 1, .015f));
            ice = Glow("Cyan Magic", new Color(.025f, .8f, 1));
            gem = Gem();
            AssetDatabase.CreateAsset(gem, Folder + "/MagicGem.asset");
            var root = new GameObject("LEGO Market Stall");
            Undo.RegisterCreatedObjectUndo(root, "Create LEGO market stall");
            var baseParts = Group(root.transform, "01 — Площадка");
            Box(baseParts, "Лавандовая LEGO-площадка", new Vector3(0, .1f, 0), new Vector3(10.8f, .2f, 5.6f),
                new Color(.55f, .56f, .9f));
            var frame = Group(root.transform, "02 — Деревянный каркас");
            foreach (int x in new[]
            {
                -1,
                1
            }

            )
                foreach (int z in new[]
                {
                    -1,
                    1
                }

                )
                {
                    Vector3 p = new Vector3(x * 2.9f, 0, z * 1.35f);
                    Box(frame, "Башмак стойки", p + Vector3.up * .31f, new Vector3(.85f, .22f, .8f), Wood);
                    Box(frame, "Опорный столб", p + Vector3.up * 2.12f, new Vector3(.36f, 3.62f, .38f), Oak);
                    Box(frame, "Нижний хомут стойки", p + Vector3.up * 1.85f, new Vector3(.49f, .2f, .5f), LightWood);
                    Box(frame, "Верхний хомут стойки", p + Vector3.up * 3.86f, new Vector3(.52f, .24f, .54f), Wood);
                }

            foreach (int z in new[]
            {
                -1,
                1
            }

            )
                Box(frame, "Поперечная балка", new Vector3(0, 3.94f, z * 1.35f), new Vector3(6.25f, .23f, .3f), Wood);
            foreach (int x in new[]
            {
                -1,
                1
            }

            )
                Box(frame, "Боковая связь", new Vector3(x * 2.9f, 3.4f, 0), new Vector3(.21f, .18f, 2.7f), Oak);
            var counter = Group(root.transform, "03 — Прилавок");
            for (int i = 0; i < 7; i++)
                Box(counter, "Доска прилавка " + (i + 1), new Vector3(-2.7f + i * .9f, 1.48f, -.83f),
                    new Vector3(.895f, .2f, 1.38f), i % 2 == 0 ? Oak : Wood);
            Box(counter, "Передний борт", new Vector3(0, 1.29f, -1.55f), new Vector3(6.6f, .4f, .24f), Oak);
            Box(counter, "Тёмная вставка переднего борта", new Vector3(0, 1.31f, -1.68f), new Vector3(5.95f, .12f, .035f), Wood);
            foreach (int x in new[]
            {
                -1,
                1
            }

            )
                Box(counter, "Ножка прилавка", new Vector3(x * 2.45f, .78f, -.9f), new Vector3(.55f, 1.16f, .6f), Wood);
            var canopy = Group(root.transform, "04 — Полосатый голубой навес");
            for (int i = 0; i < 9; i++)
                foreach (int side in new[]
                {
                    -1,
                    1
                }

                )
                {
                    Color color = i % 3 == 1 ? Blue : Cyan;
                    Box(canopy, "Полоса навеса " + (i + 1) + (side < 0 ? " — перед"
                        : " — зад"), new Vector3((i - 4) * .86f, 4.19f, side * 1.08f), new Vector3(.865f, .17f, 2.22f),
                        color, new Vector3(side * 6, 0, 0));
                }

            Box(canopy, "Конёк крыши", new Vector3(0, 4.34f, 0), new Vector3(7.9f, .11f, .24f), Cyan);
            var merchandise = Group(root.transform, "05 — Светящиеся товары");
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 1.7f;
                var tray = Group(merchandise, "Лоток " + (i + 1));
                tray.localPosition = new Vector3(x, 1.6f, -.85f);
                Box(tray, "Дно", Vector3.zero, new Vector3(1.35f, .12f, .88f), Wood);
                foreach (int side in new[]
                {
                    -1,
                    1
                }

                )
                {
                    Box(tray, "Борт", new Vector3(side * .63f, .19f, 0), new Vector3(.14f, .3f, .9f), LightWood);
                    Box(tray, "Борт", new Vector3(0, .19f, side * .4f), new Vector3(1.3f, .3f, .13f), LightWood);
                }

                Box(tray, "Пряжка", new Vector3(0, .2f, -.48f), new Vector3(.22f, .16f, .05f), Oak);
                var surface = Box(tray, "Магический свет", new Vector3(0, .18f, 0), new Vector3(1.08f, .06f, .6f), Color.white);
                UnityEngine.Object.DestroyImmediate(surface.GetComponent<LaserArchBlock>());
                surface.GetComponent<Renderer>().sharedMaterial = i == 0 ? fire : i == 1 ? green : ice;
                UnityEngine.Object.DestroyImmediate(surface.GetComponent<Collider>());
                if (i != 1)
                    for (int j = 0; j < 3; j++)
                    {
                        var stone = new GameObject(i == 0 ? "Огненный кристалл"
                            : "Ледяной кристалл", typeof(MeshFilter), typeof(MeshRenderer));
                        stone.transform.SetParent(tray, false);
                        stone.transform.localPosition = new Vector3((j - 1) * .26f, .45f, j == 1 ? .1f : 0);
                        stone.transform.localScale = new Vector3(.33f, j == 1 ? .68f : .44f, .33f);
                        stone.transform.localRotation = Quaternion.Euler(0, j * 35, (j - 1) * 15);
                        stone.GetComponent<MeshFilter>().sharedMesh = gem;
                        stone.GetComponent<Renderer>().sharedMaterial = i == 0 ? fire : ice;
                    }
            }

            var crate = Group(root.transform, "06 — Ящик слева");
            crate.localPosition = new Vector3(-4.2f, .95f, -.8f);
            Box(crate, "Корпус ящика", Vector3.zero, new Vector3(1.5f, 1.5f, 1.4f), LightWood);
            foreach (int x in new[]
            {
                -1,
                1
            }

            )
                Box(crate, "Угол ящика", new Vector3(x * .69f, 0, -.74f), new Vector3(.16f, 1.58f, .12f), Wood);
            foreach (int y in new[]
            {
                -1,
                1
            }

            )
                Box(crate, "Кромка ящика", new Vector3(0, y * .68f, -.75f), new Vector3(1.55f, .17f, .13f), Oak);
            Box(crate, "Диагональ X", new Vector3(0, 0, -.83f), new Vector3(.16f, 1.9f, .12f), Wood, new Vector3(0, 0, 45));
            Box(crate, "Диагональ X", new Vector3(0, 0, -.84f), new Vector3(.16f, 1.9f, .12f), Wood, new Vector3(0, 0, -45));
            Box(crate, "Коробочка на ящике", new Vector3(0, .86f, 0), new Vector3(.8f, .25f, .65f), Oak);
            Box(crate, "Синий камень в коробочке", new Vector3(0, 1.02f, 0), new Vector3(.42f, .08f, .34f), Blue);
            var barrel = Group(root.transform, "07 — Бочка и голубой фонарь");
            barrel.localPosition = new Vector3(4.15f, 0, -.7f);
            Cylinder(barrel, "Бочка", new Vector3(0, 1.12f, 0), new Vector3(1.55f, .9f, 1.55f), LightWood);
            for (int i = 0; i < 3; i++)
                Cylinder(barrel, "Обруч бочки " + (i + 1), new Vector3(0, .3f + i * .82f, 0),
                    new Vector3(1.66f, .065f, 1.66f), Oak);
            Cylinder(barrel, "Крышка бочки", new Vector3(0, 2.04f, 0), new Vector3(1.65f, .055f, 1.65f), Oak);
            Lantern(barrel, new Vector3(0, 2.14f, 0), ice, .9f);
            var lamps = Group(root.transform, "08 — Подвесные фонари");
            foreach (int side in new[]
            {
                -1,
                1
            }

            )
            {
                Box(lamps, "Подвес фонаря", new Vector3(side * 1.95f, 3.65f, .95f), new Vector3(.09f, .6f, .09f), Wood);
                Lantern(lamps, new Vector3(side * 1.95f, 2.82f, .95f), fire, .65f);
            }

            var mushrooms = Group(root.transform, "09 — LEGO-грибочки");
            Mushroom(mushrooms, new Vector3(-3.3f, .24f, -2.0f), new Color(1, .1f, .07f));
            Mushroom(mushrooms, new Vector3(3.25f, .24f, -2.0f), Blue);
            root.AddComponent<LaserStallRenderBatch>().sourceMaterial = plastic;
            root.transform.position = new Vector3(-11, 0, -8);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, Folder + "/LEGOMarketStall.prefab", InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            return "LEGO stall created without vendor; editable prefab saved, lobby position (-11,0,-8)";
        }

        static void Lantern(Transform parent, Vector3 position, Material light, float scale)
        {
            var root = Group(parent, "Фонарь");
            root.localPosition = position;
            root.localScale = Vector3.one * scale;
            Box(root, "Основание фонаря", Vector3.zero, new Vector3(.7f, .13f, .7f), Wood);
            Box(root, "Крышка фонаря", new Vector3(0, .84f, 0), new Vector3(.65f, .13f, .65f), Oak);
            foreach (int x in new[]
            {
                -1,
                1
            }

            )
                foreach (int z in new[]
                {
                    -1,
                    1
                }

                )
                    Box(root, "Рамка фонаря", new Vector3(x * .24f, .42f, z * .24f), new Vector3(.1f, .8f, .1f), Oak);
            var pane = Box(root, "Светящееся стекло", new Vector3(0, .42f, 0), new Vector3(.4f, .7f, .4f), Color.white);
            UnityEngine.Object.DestroyImmediate(pane.GetComponent<LaserArchBlock>());
            pane.GetComponent<Renderer>().sharedMaterial = light;
            UnityEngine.Object.DestroyImmediate(pane.GetComponent<Collider>());
        }

        static void Mushroom(Transform parent, Vector3 point, Color color)
        {
            for (int j = 0; j < 2; j++)
            {
                Vector3 p = point + new Vector3(j * .4f, 0, j * .15f);
                Box(parent, "Ножка грибочка", p + Vector3.up * .14f, new Vector3(.13f, .28f, .13f), Color.white);
                Box(parent, "Шляпка грибочка", p + Vector3.up * .32f, new Vector3(.43f, .16f, .38f), color);
                Box(parent, "Пятнышко грибочка", p + new Vector3(.07f, .41f, 0), new Vector3(.12f, .025f, .1f), Color.white);
            }
        }

        static Mesh Gem()
        {
            var mesh = new Mesh
            {
                name = "Faceted magic gem"
            };
            Vector3[] p =
            {
                new Vector3(0, .7f, 0),
                new Vector3(0, -.5f, 0),
                new Vector3(.5f, 0, 0),
                new Vector3(0, 0, .5f),
                new Vector3(-.5f, 0, 0),
                new Vector3(0, 0, -.5f)
            };
            int[] face =
            {
                0,
                3,
                2,
                0,
                4,
                3,
                0,
                5,
                4,
                0,
                2,
                5,
                1,
                2,
                3,
                1,
                3,
                4,
                1,
                4,
                5,
                1,
                5,
                2
            };
            mesh.vertices = face.Select(i => p[i]).ToArray();
            mesh.triangles = Enumerable.Range(0, 24).ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
