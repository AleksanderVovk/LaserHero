using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LaserHero.Editor
{
    public static class LaserHUDSceneSetup
    {
        public static string Apply()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Stop Play Mode");
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            var scene = hud.gameObject.scene;
            const string backup = "Assets/Scenes/Backups/Lobby_BeforeSceneHUD.unity";
            if (!File.Exists(backup))
                EditorSceneManager.SaveScene(scene, backup, true);
            hud.CreateSceneLayout();
            hud.name = "HUD Canvas";
            int count = 0;
            foreach (var button in hud.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if (button.GetComponent<LaserHUDButtonAction>() != null)
                    continue;
                var action = Resolve(hud, button, out int index);
                if (action == LaserHUDAction.None)
                    throw new Exception("Unmapped HUD button: " + button.name + " / " + button.transform.parent.name);
                button.onClick.RemoveAllListeners();
                var binding = button.gameObject.AddComponent<LaserHUDButtonAction>();
                binding.hud = hud;
                binding.action = action;
                binding.index = index;
                EditorUtility.SetDirty(binding);
                count++;
            }

            if (hud.transform.Find("Custom UI") == null)
            {
                var custom = new GameObject("Custom UI", typeof(RectTransform));
                custom.layer = 5;
                custom.transform.SetParent(hud.transform, false);
                var rect = (RectTransform)custom.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }

            EditorUtility.SetDirty(hud);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = hud.gameObject;
            EditorGUIUtility.PingObject(hud.gameObject);
            return "Scene HUD saved: persistent Canvas, all menus and mobile controls, " + count
                + " saved button bindings, Custom UI root";
        }

        static LaserHUDAction Resolve(LaserHeroHUD hud, UnityEngine.UI.Button button, out int index)
        {
            index = 0;
            string name = button.name, parent = button.transform.parent.name;
            if (name == "X" || name == "Отмена")
                return LaserHUDAction.CloseMenus;
            if (name == "Восстановить")
                return LaserHUDAction.Restore;
            if (name == "Взрыв [E]")
                return LaserHUDAction.Explosion;
            if (name == "Летать [F]")
                return LaserHUDAction.Flight;
            if (name == "Прыжок")
                return LaserHUDAction.Jump;
            if (name == "Удалить сохранение")
                return LaserHUDAction.Reset;
            if (button.transform.parent == hud.transform)
            {
                if (name == "Магазин")
                    return LaserHUDAction.ToggleStore;
                if (name == "Лазеры")
                    return LaserHUDAction.ToggleShop;
                if (name == "Ауры")
                    return LaserHUDAction.ToggleAuras;
                if (name == "Улучшения")
                    return LaserHUDAction.ToggleUpgrade;
                if (name == "Перерождение")
                    return LaserHUDAction.ToggleRebirth;
            }

            if (parent == "Rebirth" && name == "Перерождение")
                return LaserHUDAction.Rebirth;
            int space = parent.LastIndexOf(' ');
            if (space < 0 || !int.TryParse(parent.Substring(space + 1), out index))
                return LaserHUDAction.None;
            if (parent.StartsWith("Laser "))
                return name == "Premium purchase" ? LaserHUDAction.PremiumLaser : LaserHUDAction.BuyLaser;
            if (parent.StartsWith("Aura "))
                return name == "Premium purchase" ? LaserHUDAction.PremiumAura : LaserHUDAction.BuyAura;
            if (parent.StartsWith("Upgrade "))
                return LaserHUDAction.BuyUpgrade;
            if (parent.StartsWith("Drone "))
                return LaserHUDAction.Drone;
            if (parent.StartsWith("Category "))
                return LaserHUDAction.Category;
            return LaserHUDAction.None;
        }
    }
}
