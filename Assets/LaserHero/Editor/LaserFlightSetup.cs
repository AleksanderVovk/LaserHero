using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserFlightSetup
    {
        const string Folder = "Assets/LaserHero/Flight";
        static LaserHeroHUD hud;
        static Sprite circle;
        public static string Apply()
        {
            if (Application.isPlaying)
                throw new Exception("Exit Play Mode");
            hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            var hero = hud.hero;
            if (hud.GetComponent<LaserFlightControls>() != null)
                throw new Exception("Flight controls already configured");
            if (!File.Exists("Assets/Scenes/Backups/Lobby_BeforeFlightControls.unity"))
                EditorSceneManager.SaveScene(hero.gameObject.scene, "Assets/Scenes/Backups/Lobby_BeforeFlightControls.unity", true);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/LaserHero", "Flight");
            var motor = hero.GetComponent<LaserFlightMotor>();
            if (motor == null)
                motor = Undo.AddComponent<LaserFlightMotor>(hero.gameObject);
            motor.hero = hero;
            hero.flightMotor = motor;
            foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.name == "Lobby island")
                {
                    motor.lobbyGround = c;
                    break;
                }

            if (motor.lobbyGround == null)
                throw new Exception("Lobby boundary missing");
            foreach (var button in hud.GetComponentsInChildren<LaserHUDButtonAction>(true))
                if (button.action == LaserHUDAction.Flight)
                    Undo.DestroyObjectImmediate(button.gameObject);
            foreach (var r in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,
                FindObjectsSortMode.None))
                if (r.name == "Jump")
                {
                    circle = r.GetComponent<UnityEngine.UI.Image>()?.sprite;
                    break;
                }

            if (circle == null)
                throw new Exception("Jump button circle sprite missing");
            var root = new GameObject("Flight Controls", typeof(RectTransform));
            root.transform.SetParent(hud.transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var controls = Undo.AddComponent<LaserFlightControls>(hud.gameObject);
            controls.motor = motor;
            var toggle = Button(root.transform, "Flight Toggle", new Vector2(-82, 200), 128, "Взлететь", out var label);
            controls.toggle = toggle.gameObject;
            controls.toggleLabel = label;
            var binding = toggle.gameObject.AddComponent<LaserHUDButtonAction>();
            binding.hud = hud;
            binding.action = LaserHUDAction.Flight;
            var up = Button(root.transform, "Ascend", new Vector2(-80, 390), 112, "Вверх", out _);
            var hold = up.gameObject.AddComponent<LaserFlightHoldButton>();
            hold.motor = motor;
            hold.up = true;
            controls.up = up.gameObject;
            var down = Button(root.transform, "Descend", new Vector2(-210, 390), 112, "Вниз", out _);
            hold = down.gameObject.AddComponent<LaserFlightHoldButton>();
            hold.motor = motor;
            hold.up = false;
            down.GetComponentInChildren<LaserFlightArrow>().down = true;
            controls.down = down.gameObject;
            up.gameObject.SetActive(false);
            down.gameObject.SetActive(false);
            CreateAnimation(hero.animator);
            foreach (var obj in new UnityEngine.Object[]
            {
                hero,
                motor,
                controls,
                hero.animator
            }

            )
                EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(hero.gameObject.scene);
            EditorSceneManager.SaveScene(hero.gameObject.scene);
            Selection.activeGameObject = root;
            return "Flight controls saved; independent vertical hold inputs, lobby excluded, humanoid hover animation added";
        }

        static UnityEngine.UI.Button Button(Transform parent, string name, Vector2 pos, float size, string caption,
            out TMP_Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(1, 0);
            r.pivot = new Vector2(.5f, .5f);
            r.anchoredPosition = pos;
            r.sizeDelta = Vector2.one * size;
            var image = go.GetComponent<UnityEngine.UI.Image>();
            image.sprite = circle;
            image.color = Color.white;
            image.raycastTarget = true;
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var icon = new GameObject("Arrow", typeof(RectTransform), typeof(LaserFlightArrow));
            icon.layer = 5;
            icon.transform.SetParent(r, false);
            var ir = (RectTransform)icon.transform;
            ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(.5f, .5f);
            ir.anchoredPosition = new Vector2(0, 10);
            ir.sizeDelta = Vector2.one * (size * .48f);
            icon.GetComponent<LaserFlightArrow>().raycastTarget = false;
            var textGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.layer = 5;
            textGO.transform.SetParent(r, false);
            var tr = (RectTransform)textGO.transform;
            tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(.5f, 0);
            tr.anchoredPosition = new Vector2(0, 12);
            tr.sizeDelta = new Vector2(size, 30);
            label = textGO.GetComponent<TextMeshProUGUI>();
            label.font = hud.font;
            label.fontSharedMaterial = hud.outlinedText;
            label.fontSize = size > 120 ? 22 : 20;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.text = caption;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return button;
        }

        static void CreateAnimation(Animator animator)
        {
            if (animator == null || !animator.isHuman)
                throw new Exception("A humanoid avatar is required");
            string controllerPath = Folder + "/PlayerWithFlight.controller";
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController), controllerPath))
                throw new Exception("Could not copy player animator controller");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            controller.AddParameter("LaserFlying", AnimatorControllerParameterType.Bool);
            var clip = new AnimationClip
            {
                name = "Superhero Hover Idle",
                frameRate = 30
            };
            foreach (string muscle in HumanTrait.MuscleName)
            {
                float value = 0, wave = 0;
                if (muscle.EndsWith("Arm Down-Up"))
                {
                    value = -.68f;
                    wave = .025f;
                }
                else if (muscle.EndsWith("Arm Front-Back"))
                    value = .06f;
                else if (muscle.EndsWith("Shoulder Down-Up"))
                    value = -.12f;
                else if (muscle.EndsWith("Forearm Stretch"))
                    value = .78f;
                else if (muscle.EndsWith("Upper Leg In-Out"))
                    value = .04f;
                else if (muscle.EndsWith("Upper Leg Front-Back"))
                {
                    value = .65f;
                    wave = .01f;
                }
                else if (muscle.EndsWith("Lower Leg Stretch"))
                    value = .95f;
                else if (muscle == "Spine Front-Back")
                {
                    value = -.04f;
                    wave = .015f;
                }

                var curve = new AnimationCurve(new Keyframe(0, value), new Keyframe(1.2f, value + wave), new Keyframe(2.4f, value));
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), curve);
            }

            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = true;
            clipSettings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            AssetDatabase.CreateAsset(clip, Folder + "/SuperheroHover.anim");
            var mask = new AvatarMask
            {
                name = "Flight Body Mask"
            };
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, i != (int)AvatarMaskBodyPart.Root);
            AssetDatabase.CreateAsset(mask, Folder + "/FlightBody.mask");
            controller.AddLayer("Flight Pose");
            var layers = controller.layers;
            var layer = layers[layers.Length - 1];
            layer.defaultWeight = 0;
            layer.avatarMask = mask;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            var empty = layer.stateMachine.AddState("Ground animations");
            empty.writeDefaultValues = false;
            layer.stateMachine.defaultState = empty;
            var flying = layer.stateMachine.AddState("Hover idle");
            flying.motion = clip;
            flying.writeDefaultValues = false;
            var enter = empty.AddTransition(flying);
            enter.hasExitTime = false;
            enter.duration = .22f;
            enter.AddCondition(AnimatorConditionMode.If, 0, "LaserFlying");
            var exit = flying.AddTransition(empty);
            exit.hasExitTime = false;
            exit.duration = .18f;
            exit.AddCondition(AnimatorConditionMode.IfNot, 0, "LaserFlying");
            layers[layers.Length - 1] = layer;
            controller.layers = layers;
            animator.runtimeAnimatorController = controller;
        }
    }
}
