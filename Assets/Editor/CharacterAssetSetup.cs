using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ScpParty.EditorTools
{
    public static class CharacterAssetSetup
    {
        const string Kenney = "Assets/ThirdParty/KenneyProtagonists";
        const string Output = "Assets/Models/Characters";

        [MenuItem("SCP/模型/导入玩家与怪物")]
        public static void Setup()
        {
            Directory.CreateDirectory(Output);
            AssetDatabase.Refresh();
            foreach (var path in Directory.GetFiles("Assets/ThirdParty", "*.fbx", SearchOption.AllDirectories))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = true;
                importer.importCameras = importer.importLights = false;
                var settings = importer.defaultClipAnimations;
                foreach (var clip in settings)
                    clip.loopTime = clip.name.EndsWith("Idle") || clip.name.EndsWith("Run") || clip.name.EndsWith("Walk");
                importer.clipAnimations = settings;
                importer.SaveAndReimport();
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__")).ToArray();
                Debug.Log($"[CharacterAssets] {path}: {string.Join(", ", clips.Select(c => c.name))}");
            }
            var skin = MakeMaterial("PlayerSkin", Kenney + "/Skins/skaterMaleA.png");
            var player = MakePrefab(Kenney + "/Model/characterMedium.fbx", "KenneyPlayer", skin, 1.8f);
            var controllerPath = Output + "/Player.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if (controller.layers.Length == 0 || controller.layers[0].stateMachine.states.Length == 0)
            {
                if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
                controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
                var machine = controller.layers[0].stateMachine;
                var idle = machine.AddState("Idle");
                idle.motion = Clip(Kenney + "/Animations/idle.fbx");
                var run = machine.AddState("Run");
                run.motion = Clip(Kenney + "/Animations/run.fbx");
                machine.defaultState = idle;
                var start = idle.AddTransition(run); start.hasExitTime = false; start.duration = .15f;
                start.AddCondition(AnimatorConditionMode.If, 0, "Moving");
                var stop = run.AddTransition(idle); stop.hasExitTime = false; stop.duration = .15f;
                stop.AddCondition(AnimatorConditionMode.IfNot, 0, "Moving");
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(player);
            var animator = instance.GetComponentInChildren<Animator>();
            if (animator == null) animator = instance.transform.GetChild(0).gameObject.AddComponent<Animator>();
            foreach (var state in controller.layers[0].stateMachine.states)
                state.state.motion = Clip(Kenney + "/Animations/" + (state.state.name == "Run" ? "run" : "idle") + ".fbx");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(instance, Output + "/KenneyPlayer.prefab");
            Object.DestroyImmediate(instance);
            var atlas = MakeMaterial("Monsters", "Assets/ThirdParty/QuaterniusMonsters/Atlas_Monsters.png");
            foreach (var path in Directory.GetFiles("Assets/ThirdParty/QuaterniusMonsters", "*.fbx", SearchOption.AllDirectories))
                MakePrefab(path, new DirectoryInfo(Path.GetDirectoryName(path)).Parent.Name + "_" + Path.GetFileNameWithoutExtension(path), atlas, 1.8f);
            ApplyPlayerVisual();
            AssetDatabase.SaveAssets();
            Debug.Log("[CharacterAssets] SETUP COMPLETE");
        }

        static AnimationClip Clip(string path)
        {
            var source = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .First(c => !c.name.StartsWith("__preview__") && c.name.EndsWith("|" + Path.GetFileNameWithoutExtension(path), System.StringComparison.OrdinalIgnoreCase));
            Debug.Log($"[CharacterAssets] Clip {source.name} curves={AnimationUtility.GetCurveBindings(source).Length}: {string.Join(",", AnimationUtility.GetCurveBindings(source).Take(8).Select(b => b.path + ":" + b.propertyName))}");
            string output = Output + "/" + Path.GetFileNameWithoutExtension(path) + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(output);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, output); }
            var copy = Object.Instantiate(source);
            EditorUtility.CopySerialized(copy, clip);
            Object.DestroyImmediate(copy);
            // Animation-only FBXs omit the model container. Bind their tracks
            // beneath the model's Root armature, preserving the container scale.
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var curves = bindings.Select(b => AnimationUtility.GetEditorCurve(clip, b)).ToArray();
            foreach (var binding in bindings) AnimationUtility.SetEditorCurve(clip, binding, null);
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                binding.path = string.IsNullOrEmpty(binding.path) ? "Root" : "Root/" + binding.path;
                AnimationUtility.SetEditorCurve(clip, binding, curves[i]);
            }
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static Material MakeMaterial(string name, string texture)
        {
            string path = Output + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", .1f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject MakePrefab(string source, string name, Material material, float height)
        {
            var root = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source), root.transform);
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) { bounds.Encapsulate(r.bounds); r.sharedMaterials = r.sharedMaterials.Select(_ => material).ToArray(); }
            model.transform.localScale *= height / bounds.size.y;
            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            model.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Debug.Log($"[CharacterAssets] {name} height={bounds.size.y} skinned={model.GetComponentsInChildren<SkinnedMeshRenderer>().Length}");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Output + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static void ApplyPlayerVisual()
        {
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/KenneyPlayer.prefab");
            if (visual == null) return;
            const string path = "Assets/Prefabs/Player.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var body = root.transform.Find("Body");
                for (int i = body.childCount - 1; i >= 0; i--) Object.DestroyImmediate(body.GetChild(i).gameObject);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(visual, body);
                var so = new SerializedObject(root.GetComponent<ScpParty.Player.PlayerVisuals>());
                so.FindProperty("tintedRenderers").arraySize = 0;
                so.FindProperty("animator").objectReferenceValue = model.GetComponentInChildren<Animator>();
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void RenderPreview()
        {
            var player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/KenneyPlayer.prefab"));
            player.GetComponentInChildren<Animator>().enabled = false;
            foreach (var t in player.GetComponentsInChildren<Transform>().Take(20)) Debug.Log("[Rig] " + AnimationUtility.CalculateTransformPath(t, player.transform));
            Clip(Kenney + "/Animations/idle.fbx").SampleAnimation(player.transform.GetChild(0).gameObject, .5f);
            var monster = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/Big_Cactoro.prefab"));
            monster.transform.position = new Vector3(2f, 0f, 0f);
            var light = new GameObject("PreviewLight").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(40f, 180f, 0f);
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.transform.position = new Vector3(1f, 1.2f, 6f);
            camera.transform.LookAt(new Vector3(1f, .9f, 0f));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.3f, .33f, .38f);
            var rt = new RenderTexture(1024, 640, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(1024, 640, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1024, 640), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/CharacterPreview.png", image.EncodeToPNG());
            Clip(Kenney + "/Animations/run.fbx").SampleAnimation(player.transform.GetChild(0).gameObject, .2f);
            camera.Render();
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 1024, 640), 0, 0);
            image.Apply();
            File.WriteAllBytes("Logs/CharacterRunPreview.png", image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            foreach (var obj in new Object[] { player, monster, light.gameObject, camera.gameObject, rt, image }) Object.DestroyImmediate(obj);
        }
    }
}
