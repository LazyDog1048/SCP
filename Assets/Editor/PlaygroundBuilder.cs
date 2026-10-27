using System.IO;
using System.Linq;
using ScpParty.Combat;
using ScpParty.DevTools;
using ScpParty.Interaction;
using ScpParty.Network;
using ScpParty.Player;
using ScpParty.Weapons;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ScpParty.EditorTools
{
    /// <summary>
    /// 生成联机底子所需的 lowpoly 材质、玩家 prefab、武器与木桩（PlaygroundBuilder.Combat.cs）、房间与门（PlaygroundBuilder.Rooms.cs）和 Playground 测试场景。
    /// 可重复执行：会覆盖这里列出的生成资源，不会触碰其他资源。
    /// 批处理：Unity.exe -batchmode -quit -projectPath . -executeMethod ScpParty.EditorTools.PlaygroundBuilder.BuildAll
    /// </summary>
    public static partial class PlaygroundBuilder
    {
        const string Root = "Assets";
        const string MaterialDir = Root + "/Materials";
        const string PrefabPath = Root + "/Prefabs/Player.prefab";
        const string ScenePath = Root + "/Scenes/Playground.unity";
        const string WindowsBuildPath = "Builds/Windows/SCP.exe";

        static readonly Color SkyColor = new(0.62f, 0.80f, 0.95f);

        [MenuItem("SCP/重新生成 Playground 场景与玩家 Prefab")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            foreach (string dir in new[] { MaterialDir, Root + "/Prefabs", Root + "/Scenes", WeaponModelDir, WeaponDataDir, ResourcesDir + "/FX" })
                Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();

            var combat = BuildCombatAssets();
            var door = BuildDoorPrefab();
            var player = BuildPlayerPrefab(combat.Pickup);
            BuildScene(player, combat, door);
            RegisterNetworkPrefabs(player, combat.Pickup.gameObject, combat.Dummy.gameObject, door.gameObject);
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[PlaygroundBuilder] 完成：" + ScenePath);
        }

        [MenuItem("SCP/构建 Windows 测试包")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[PlaygroundBuilder] Windows 构建结果：{report.summary.result}，输出 {WindowsBuildPath}");
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        // ---------- 材质 ----------

        static Material Mat(string name, Color color, float smoothness = 0.05f, string dir = MaterialDir, string shader = "Universal Render Pipeline/Lit")
        {
            string path = $"{dir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------- 玩家 Prefab ----------

        static GameObject BuildPlayerPrefab(WeaponPickup pickupPrefab)
        {
            var bodyMat = Mat("PlayerBody", Color.white);
            var visorMat = Mat("PlayerVisor", new Color(0.12f, 0.13f, 0.18f), 0.6f);

            var root = new GameObject("Player");
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.4f;
            cc.skinWidth = 0.04f;

            root.AddComponent<NetworkObject>();
            var nt = root.AddComponent<NetworkTransform>();
            nt.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            nt.SyncRotAngleX = false;
            nt.SyncRotAngleZ = false;
            nt.SyncScaleX = nt.SyncScaleY = nt.SyncScaleZ = false;

            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            var torso = Part(PrimitiveType.Cylinder, "Torso", body, new Vector3(0f, 0.6f, 0f), new Vector3(0.75f, 0.6f, 0.75f), bodyMat);
            var headMesh = Part(PrimitiveType.Cube, "HeadMesh", body, new Vector3(0f, 1.48f, 0f), new Vector3(0.62f, 0.52f, 0.56f), bodyMat);
            Part(PrimitiveType.Cube, "Visor", body, new Vector3(0f, 1.52f, 0.27f), new Vector3(0.46f, 0.16f, 0.06f), visorMat);
            var handL = Part(PrimitiveType.Cube, "HandL", body, new Vector3(-0.48f, 0.75f, 0.1f), new Vector3(0.18f, 0.18f, 0.18f), bodyMat);
            var handR = Part(PrimitiveType.Cube, "HandR", body, new Vector3(0.48f, 0.75f, 0.1f), new Vector3(0.18f, 0.18f, 0.18f), bodyMat);

            var head = new GameObject("Head").transform;
            head.SetParent(root.transform, false);
            head.localPosition = new Vector3(0f, 1.55f, 0f);
            var weaponAnchor = new GameObject("WeaponAnchor").transform;
            weaponAnchor.SetParent(head, false);
            weaponAnchor.localPosition = new Vector3(0.3f, -0.3f, 0.5f);

            root.AddComponent<Health>();
            var controller = root.AddComponent<PlayerController>();
            root.AddComponent<PlayerInteractor>();
            root.AddComponent<PlayerPush>();
            var visuals = root.AddComponent<PlayerVisuals>();
            var weapons = root.AddComponent<PlayerWeapons>();
            root.AddComponent<PlayerLife>();
            root.AddComponent<PlayerHud>();

            Set(weapons, "weaponAnchor", weaponAnchor);
            Set(weapons, "pickupPrefab", pickupPrefab);

            var so = new SerializedObject(controller);
            so.FindProperty("head").objectReferenceValue = head;
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(visuals);
            so.FindProperty("body").objectReferenceValue = body;
            var tinted = so.FindProperty("tintedRenderers");
            var renderers = new Renderer[] { torso, headMesh, handL, handR };
            tinted.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
                tinted.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            // NetworkObject 的 GlobalObjectIdHash 在导入时生成，强制导入一次确保有值
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            uint hash = prefab.GetComponent<NetworkObject>().PrefabIdHash;
            Debug.Log($"[PlaygroundBuilder] Player prefab GlobalObjectIdHash = {hash}");
            return prefab;
        }

        static Renderer Part(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = mat;
            return renderer;
        }

        // ---------- 场景 ----------

        static void BuildScene(GameObject playerPrefab, CombatAssets combat, NetworkObject doorPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.86f, 0.98f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.66f, 0.70f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.34f, 0.30f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = SkyColor;
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 90f;

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 75f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraHome>();
            // 菜单时的俯瞰视角：房间 A 西南角
            camGo.transform.position = new Vector3(-11.5f, 3.8f, -9.5f);
            camGo.transform.LookAt(new Vector3(2f, 0.5f, 2f));

            var doorPoints = BuildRooms(out var weaponPoints);
            BuildLevel();
            BuildSpawnPoints();

            var netGo = new GameObject("NetworkManager");
            var network = netGo.AddComponent<NetworkManager>();
            var transport = netGo.AddComponent<UnityTransport>();
            transport.ConnectTimeoutMS = 1000;
            transport.MaxConnectAttempts = 10;
            network.NetworkConfig ??= new NetworkConfig();
            network.NetworkConfig.NetworkTransport = transport;
            network.NetworkConfig.PlayerPrefab = playerPrefab;
            network.NetworkConfig.ConnectionApproval = true;
            netGo.AddComponent<LanSession>();
            netGo.AddComponent<CombatSelfTest>();

            BuildTrainingArea(network, combat, doorPrefab, doorPoints, weaponPoints);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static void BuildLevel()
        {
            var stone = Mat("Stone", new Color(0.72f, 0.72f, 0.76f));
            var wood = Mat("Wood", new Color(0.85f, 0.62f, 0.38f));
            var accent = Mat("Accent", new Color(0.98f, 0.55f, 0.45f));

            // 房间 A 里的障碍物（房间外壳见 PlaygroundBuilder.Rooms.cs）
            var level = new GameObject("Level").transform;

            Block("CenterPad", level, new Vector3(0f, 0.1f, 0f), new Vector3(4f, 0.2f, 4f), accent);

            // 箱子堆
            Block("Crate1", level, new Vector3(6f, 0.5f, 5f), Vector3.one, wood);
            Block("Crate2", level, new Vector3(7.1f, 0.5f, 5f), Vector3.one, wood);
            Block("Crate3", level, new Vector3(6.55f, 1.5f, 5f), Vector3.one, wood, 15f);
            Block("Crate4", level, new Vector3(-8f, 0.5f, -6f), Vector3.one, wood, 30f);

            // 高台 + 坡道
            Block("Platform", level, new Vector3(-7f, 0.75f, 6f), new Vector3(4f, 1.5f, 4f), stone);
            var ramp = Block("Ramp", level, new Vector3(-7f, 0.66f, 2.1f), new Vector3(3f, 0.25f, 4.2f), stone);
            ramp.localRotation = Quaternion.Euler(-20f, 0f, 0f);

            // 矮墙与柱子
            Block("LowWall", level, new Vector3(0f, 0.5f, -8f), new Vector3(6f, 1f, 0.6f), stone);
            Pillar(level, new Vector3(8f, 0f, -5f), stone);
            Pillar(level, new Vector3(-3f, 0f, 9f), stone);
        }

        static Transform Block(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, float yaw = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.isStatic = true;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go.transform;
        }

        static void Pillar(Transform parent, Vector3 basePosition, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Pillar";
            go.isStatic = true;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = basePosition + Vector3.up * 1.25f;
            go.transform.localScale = new Vector3(1.2f, 1.25f, 1.2f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }


        static void BuildSpawnPoints()
        {
            var root = new GameObject("SpawnPoints");
            root.AddComponent<SpawnPoints>();
            const int count = 8;
            const float radius = 5f;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                var pos = new Vector3(Mathf.Sin(angle) * radius, 0.05f, -Mathf.Cos(angle) * radius);
                var point = new GameObject($"Spawn{i + 1}").transform;
                point.SetParent(root.transform, false);
                point.localPosition = pos;
                point.localRotation = Quaternion.LookRotation(new Vector3(-pos.x, 0f, -pos.z));
            }
        }

        static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
