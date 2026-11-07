using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaserHero.Editor
{
    public static class LaserInterfaceAudioSetup
    {
        public static string Apply()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first");
            var progress = UnityEngine.Object.FindFirstObjectByType<LaserProgress>();
            var hud = UnityEngine.Object.FindFirstObjectByType<LaserHeroHUD>();
            var audio = UnityEngine.Object.FindFirstObjectByType<LaserInterfaceAudio>();
            if (audio == null)
            {
                var go = new GameObject("Interface Audio");
                go.transform.SetParent(hud.transform.parent, false);
                audio = go.AddComponent<LaserInterfaceAudio>();
            }

            audio.progress = progress;
            audio.buyTheme = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/BuyTheme1.ogg");
            audio.levelUp = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LevelUp.mp3");
            audio.buyFail = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/BuyFail.ogg");
            audio.click = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/ClickSfx - Creator Store.ogg");
            if (audio.buyTheme == null || audio.levelUp == null || audio.buyFail == null || audio.click == null)
                throw new Exception("Missing UI audio clip");
            audio.resultVoice = Voice(audio, "Purchase results");
            audio.levelVoice = Voice(audio, "Level up");
            audio.clickVoice = Voice(audio, "Button clicks");
            audio.controlRoots = hud.GetComponentsInChildren<Transform>(true).Where(t =>
                t.name == "Controllers" || t.name == "Mobile controls" || t.name == "Flight Controls").ToArray();
            EditorUtility.SetDirty(audio);
            EditorSceneManager.MarkSceneDirty(audio.gameObject.scene);
            EditorSceneManager.SaveScene(audio.gameObject.scene);
            return "Interface Audio ready: BuyTheme1, LevelUp, BuyFail, ClickSfx; control roots excluded="
                + audio.controlRoots.Length;
        }

        static AudioSource Voice(LaserInterfaceAudio owner, string name)
        {
            var child = owner.transform.Find(name);
            if (child == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(owner.transform, false);
                child = go.transform;
            }

            var source = child.GetComponent<AudioSource>();
            if (source == null)
                source = child.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
            source.volume = 1;
            source.ignoreListenerVolume = false;
            source.ignoreListenerPause = true;
            source.priority = 32;
            EditorUtility.SetDirty(source);
            return source;
        }
    }
}
