using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LaserHero.Editor
{
    public static class LaserIAPSetup
    {
        public const string Path = "Assets/LaserHero/Settings/GooglePlayCatalog.asset";
        public static string Apply()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            var progress = UnityEngine.Object.FindFirstObjectByType<LaserProgress>();
            EditorSceneManager.SaveScene(progress.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeGoogleIAP.unity", true);
            if (!AssetDatabase.IsValidFolder("Assets/LaserHero/Settings"))
                AssetDatabase.CreateFolder("Assets/LaserHero", "Settings");
            var catalog = AssetDatabase.LoadAssetAtPath<LaserStoreCatalog>(Path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LaserStoreCatalog>();
                string[] lasers =
                {
                    "fire",
                    "yellow",
                    "cyan",
                    "indigo",
                    "green",
                    "pink"
                }, auras =
                {
                    "flame",
                    "blue",
                    "green",
                    "pink",
                    "void"
                };
                double[] laserPrices =
                {
                    .99,
                    1.99,
                    2.99,
                    4.99,
                    6.99,
                    9.99
                }, auraPrices =
                {
                    1.99,
                    3.99,
                    5.99,
                    7.99,
                    9.99
                };
                catalog.items = lasers.Select((name, i) => new LaserStoreItem
                {
                    kind = LaserStoreKind.Laser,
                    index = i,
                    productId = "laser_hero_laser_" + name,
                    previewPrice = laserPrices[i]
                }).Concat(auras.Select((name, i) => new LaserStoreItem
                {
                    kind = LaserStoreKind.Aura,
                    index = i,
                    productId = "laser_hero_aura_" + name,
                    previewPrice = auraPrices[i]
                })).ToArray();
                AssetDatabase.CreateAsset(catalog, Path);
            }

            if (!catalog.IsValid(out var error))
                throw new InvalidOperationException(error);
            var store = progress.GetComponent<LaserIAPStore>();
            if (store == null)
                store = progress.gameObject.AddComponent<LaserIAPStore>();
            store.catalog = catalog;
            store.progress = progress;
            EditorUtility.SetDirty(store);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(progress.gameObject.scene);
            EditorSceneManager.SaveScene(progress.gameObject.scene);
            return "Google Play IAP connected to scene, 11 non-consumables configured; public key pending";
        }
    }

    [CustomEditor(typeof(LaserStoreCatalog))]
    public class LaserStoreCatalogInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Цены previewPrice — предпросмотр. Реальная цена задаётся в Google Play Console и загружается из магазина. Каждый Product ID должен точно совпадать с товаром Google Play. Товары — постоянные Non-Consumable.", MessageType.Info);
            DrawDefaultInspector();
            var catalog = (LaserStoreCatalog)target;
            if (!catalog.IsValid(out var reason))
                EditorGUILayout.HelpBox(reason, MessageType.Error);
        }
    }
}
