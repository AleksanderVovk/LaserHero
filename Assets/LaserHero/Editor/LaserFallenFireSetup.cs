using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public static class LaserFallenFireSetup
{
    public static object Run()
    {
        if (Application.isPlaying)
            throw new System.InvalidOperationException("Configure FallenFire in Edit Mode.");
        AssetDatabase.Refresh();
        string folder = "Assets/LaserHero/Fire";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/LaserHero", "Fire");
        var audio = AssetImporter.GetAtPath("Assets/LaserHero/Audio/FallenFireLoop.wav") as AudioImporter;
        audio.forceToMono = true;
        var sample = audio.defaultSampleSettings;
        sample.preloadAudioData = true;
        sample.loadType = AudioClipLoadType.DecompressOnLoad;
        sample.compressionFormat = AudioCompressionFormat.Vorbis;
        sample.quality = .7f;
        audio.defaultSampleSettings = sample;
        audio.SaveAndReimport();
        var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FallenFire.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(original);
        go.name = "FallenFirePhysical";
        var converted = new System.Collections.Generic.Dictionary<Material, Material>();
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.gameObject.layer = 2;
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.None;
            main.maxParticles = Mathf.Min(main.maxParticles, 96);
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var old = renderer.sharedMaterial;
            if (!converted.TryGetValue(old, out var material))
            {
                string path = folder + "/" + old.name + "_URP.mat";
                material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                    AssetDatabase.CreateAsset(material, path);
                }

                material.SetTexture("_BaseMap", old.GetTexture("_MainTex"));
                material.SetColor("_BaseColor", old.HasProperty("_Color") ? old.GetColor("_Color") : Color.white);
                bool additive = old.name.Contains("ADD");
                material.SetFloat("_Surface", 1);
                material.SetFloat("_Blend", additive ? 2 : 0);
                material.SetFloat("_Cull", 0);
                material.SetFloat("_ZWrite", 0);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = 3000;
                EditorUtility.SetDirty(material);
                converted.Add(old, material);
            }

            renderer.sharedMaterial = material;
        }

        var physics = new PhysicsMaterial("FallenFire bounce")
        {
            dynamicFriction = .65f,
            staticFriction = .65f,
            bounciness = .4f,
            bounceCombine = PhysicsMaterialCombine.Maximum
        };
        string physicsPath = folder + "/FireBounce.physicMaterial";
        var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(physicsPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(physics, existing);
            UnityEngine.Object.DestroyImmediate(physics);
            physics = existing;
        }
        else
            AssetDatabase.CreateAsset(physics, physicsPath);
        var sphere = go.AddComponent<SphereCollider>();
        sphere.radius = .2f;
        sphere.center = new Vector3(0, .15f, 0);
        sphere.sharedMaterial = physics;
        sphere.excludeLayers = 1 << 2;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = .35f;
        rb.linearDamping = .4f;
        rb.angularDamping = 2;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var fire = go.AddComponent<LaserHero.LaserFallenFire>();
        fire.fireLoop = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LaserHero/Audio/FallenFireLoop.wav");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, folder + "/FallenFirePhysical.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        var feedback = UnityEngine.Object.FindFirstObjectByType<LaserHero.LaserImpactFeedback>();
        feedback.fallenFirePrefab = prefab.GetComponent<LaserHero.LaserFallenFire>();
        feedback.laserFireChance = .06f;
        feedback.firePoolSize = 12;
        EditorUtility.SetDirty(feedback);
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(feedback.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(feedback.gameObject.scene);
        return "FallenFire variant connected, 3 URP materials, spatial loop and physics configured; scene saved";
    }
}
