using System.Linq;
using ScpParty.Combat;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ScpParty.EditorTools
{
    public static partial class PlaygroundBuilder
    {
        const string EnemyPath = "Assets/Prefabs/StationaryEnemy.prefab";

        [MenuItem("SCP/添加静止怪物到 Playground")]
        public static void SetupEnemy()
        {
            EditorSceneManager.OpenScene(ScenePath);
            InstallEnemy();
            AssetDatabase.SaveAssets();
            Debug.Log("[EnemySetup] COMPLETE");
        }

        public static void BuildEnemyTest()
        {
            SetupEnemy();
            BuildWindows();
        }

        public static void PreviewEnemy()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath));
            var animator = instance.GetComponentInChildren<Animator>();
            animator.enabled = false;
            var clip = AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/QuaterniusMonsters/Big/FBX/Cactoro.fbx")
                .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__") && c.name.EndsWith("|Death"));
            foreach (var t in instance.GetComponentsInChildren<Transform>().Take(8)) Debug.Log("[EnemyRig] " + AnimationUtility.CalculateTransformPath(t, animator.transform));
            Debug.Log("[EnemyCurves] " + string.Join(",", AnimationUtility.GetCurveBindings(clip).Take(8).Select(b => b.path)));
            clip.SampleAnimation(animator.gameObject, clip.length * .95f);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(45f, 180f, 0f);
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1.5f, 4f);
            camera.transform.LookAt(Vector3.up * .7f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.gray;
            var rt = new RenderTexture(640, 640, 24); camera.targetTexture = rt; camera.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
            System.IO.File.WriteAllBytes("Logs/EnemyDeathPreview.png", image.EncodeToPNG());
            RenderTexture.active = null; camera.targetTexture = null;
            foreach (var obj in new Object[] { instance, light.gameObject, camera.gameObject, rt, image }) Object.DestroyImmediate(obj);
        }

        static void InstallEnemy()
        {
            const string controllerPath = "Assets/Models/Characters/StationaryEnemy.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if (controller.layers.Length == 0 || controller.layers[0].stateMachine.states.Length == 0)
            {
                if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
                controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
                controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
                var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/QuaterniusMonsters/Big/FBX/Cactoro.fbx")
                    .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
                var machine = controller.layers[0].stateMachine;
                var idle = machine.AddState("Idle"); idle.motion = clips.First(c => c.name.EndsWith("|Idle"));
                var hit = machine.AddState("Hit"); hit.motion = clips.First(c => c.name.EndsWith("|HitReact"));
                var death = machine.AddState("Death"); death.motion = clips.First(c => c.name.EndsWith("|Death"));
                machine.defaultState = idle;
                var die = machine.AddAnyStateTransition(death);
                die.hasExitTime = false; die.duration = .05f; die.canTransitionToSelf = false;
                die.AddCondition(AnimatorConditionMode.If, 0, "Dead");
                var hurt = machine.AddAnyStateTransition(hit);
                hurt.hasExitTime = false; hurt.duration = .05f; hurt.canTransitionToSelf = true;
                hurt.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
                hurt.AddCondition(AnimatorConditionMode.If, 0, "Hit");
                var recover = hit.AddTransition(idle); recover.hasExitTime = true; recover.exitTime = 1; recover.duration = .1f;
            }
            var root = new GameObject("StationaryEnemy");
            root.AddComponent<NetworkObject>();
            root.AddComponent<Health>();
            var collider = root.AddComponent<CapsuleCollider>();
            collider.height = 1.8f; collider.radius = .5f; collider.center = Vector3.up * .9f;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Big_Cactoro.prefab"), root.transform);
            var animator = visual.GetComponentInChildren<Animator>();
            if (animator == null) animator = visual.transform.GetChild(0).gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            var enemy = root.AddComponent<StationaryEnemy>();
            Set(enemy, "animator", animator); Set(enemy, "hitCollider", collider);
            PrefabUtility.SaveAsPrefabAsset(root, EnemyPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(EnemyPath, ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
            RegisterNetworkPrefabs(prefab);

            var spawner = Object.FindFirstObjectByType<ServerSpawner>();
            var points = GameObject.Find("EnemyPoints")?.transform;
            if (points == null)
            {
                points = new GameObject("EnemyPoints").transform;
                var point = new GameObject("StationaryMonster").transform;
                point.SetParent(points, false);
                point.position = new Vector3(3f, .05f, 19f);
                point.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            var so = new SerializedObject(spawner);
            var groups = so.FindProperty("groups");
            int index = -1;
            for (int i = 0; i < groups.arraySize; i++)
                if (groups.GetArrayElementAtIndex(i).FindPropertyRelative("points").objectReferenceValue == points) index = i;
            if (index < 0) { index = groups.arraySize; groups.arraySize++; }
            var group = groups.GetArrayElementAtIndex(index);
            group.FindPropertyRelative("prefab").objectReferenceValue = prefab.GetComponent<NetworkObject>();
            group.FindPropertyRelative("points").objectReferenceValue = points;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(spawner.gameObject.scene);
        }
    }
}
