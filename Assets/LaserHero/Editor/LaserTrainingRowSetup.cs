using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LaserHero.Editor
{
    public static class LaserTrainingRowSetup
    {
        public static string Apply()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play Mode");
            var all = UnityEngine.Object.FindObjectsByType<LaserTrainingTarget>(FindObjectsSortMode.None);
            var source = all.Single(t => t.name == "Training target x1");
            var scene = source.gameObject.scene;
            const string backup = "Assets/Scenes/Backups/Lobby_BeforeTrainingRow.unity";
            if (!File.Exists(backup))
                EditorSceneManager.SaveScene(scene, backup, true);
            var group = GameObject.Find("Training Lanes");
            if (group == null)
            {
                group = new GameObject("Training Lanes");
                group.transform.SetParent(source.transform.parent, true);
                Undo.RegisterCreatedObjectUndo(group, "Create training lane row");
            }

            Vector3 start = source.transform.position;
            float[] multipliers =
            {
                1.5f,
                2,
                4,
                6,
                8,
                10,
                15,
                25,
                100
            };
            int[] thresholds =
            {
                0,
                2,
                6,
                8,
                10,
                0,
                0,
                0,
                0
            };
            double[] prices =
            {
                0,
                0,
                0,
                0,
                0,
                5.99,
                7.99,
                9.99,
                19.99
            };
            for (int i = 0; i < 9; i++)
            {
                var target = i == 0 ? source : all.SingleOrDefault(t => t.tier == i);
                if (target == null)
                {
                    var clone = UnityEngine.Object.Instantiate(source.gameObject);
                    target = clone.GetComponent<LaserTrainingTarget>();
                    Undo.RegisterCreatedObjectUndo(clone, "Add training lane");
                }

                Undo.RecordObject(target, "Configure training lane");
                Undo.RecordObject(target.transform, "Place training lane");
                Undo.SetTransformParent(target.transform, group.transform, "Group training lanes");
                if (i > 0)
                    target.name = "Training target x" + multipliers[i].ToString("0",
                        System.Globalization.CultureInfo.InvariantCulture);
                target.transform.SetPositionAndRotation(start + Vector3.left * (i * 6), Quaternion.Euler(0, 180, 0));
                target.tier = i;
                target.trainingMultiplier = multipliers[i];
                target.requiredRebirths = thresholds[i];
                target.access = i == 0 ? LaserTrainingAccess.Free : i <= 4 ? LaserTrainingAccess.Rebirths : LaserTrainingAccess.IAP;
                if (i >= 5)
                {
                    target.iapProductId = "laser_hero_training_" + multipliers[i].ToString("0",
                        System.Globalization.CultureInfo.InvariantCulture) + "x";
                    target.iapPrice = prices[i];
                }

                target.RefreshPlate();
                EditorUtility.SetDirty(target);
            }

            var lanes = UnityEngine.Object.FindObjectsByType<LaserTrainingTarget>(FindObjectsSortMode.None);
            var catalog = UnityEngine.Object.FindFirstObjectByType<LaserIAPStore>().catalog;
            LaserIAPStore.ConfigureTrainingProducts(catalog, lanes);
            if (!catalog.IsValid(out var reason))
                throw new Exception(reason);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = group;
            EditorGUIUtility.PingObject(group);
            return "9 matching training lanes saved in one row, 6m spacing, requirements 2/6/8/10, IAP 5.99/7.99/9.99/19.99; 17 store products";
        }
    }
}
