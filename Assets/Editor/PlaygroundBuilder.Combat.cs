using ScpParty.Combat;
using ScpParty.Weapons;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace ScpParty.EditorTools
{
    /// <summary>生成器的战斗部分：武器数据与模型、地上拾取物、水弹、训练木桩、训练区。</summary>
    public static partial class PlaygroundBuilder
    {
        const string WeaponModelDir = Root + "/Prefabs/Weapons";
        const string WeaponDataDir = Root + "/Data/Weapons";
        const string ResourcesDir = Root + "/Resources";
        const string PickupPrefabPath = Root + "/Prefabs/WeaponPickup.prefab";
        const string DummyPrefabPath = Root + "/Prefabs/TrainingDummy.prefab";

        struct CombatAssets
        {
            public WeaponPickup Pickup;
            public NetworkObject Dummy;
            public int WeaponCount;
        }

        static CombatAssets BuildCombatAssets()
        {
            // 特效材质（运行时用 Resources.Load 读取）
            Mat("Tracer", new Color(1f, 0.92f, 0.55f), 0f, ResourcesDir + "/FX", "Universal Render Pipeline/Unlit");
            Mat("Impact", new Color(1f, 0.75f, 0.3f), 0f, ResourcesDir + "/FX");
            var water = Mat("Splash", new Color(0.35f, 0.72f, 1f), 0.8f, ResourcesDir + "/FX");

            var wood = Mat("Wood", new Color(0.85f, 0.62f, 0.38f));
            var grip = Mat("Grip", new Color(0.18f, 0.14f, 0.12f));
            var metal = Mat("Metal", new Color(0.22f, 0.23f, 0.26f), 0.4f);
            var toy = Mat("ToyPink", new Color(1f, 0.45f, 0.62f));
            var toyTank = Mat("ToyCyan", new Color(0.35f, 0.85f, 0.9f), 0.5f);

            // 模型约定：握把在原点，枪口朝 +Z；近战沿 +Y，再由根节点前倾
            var bat = Model("Bat", new Vector3(40f, 0f, 0f), m =>
            {
                Part(PrimitiveType.Cylinder, "Grip", m, new Vector3(0f, 0.05f, 0f), new Vector3(0.045f, 0.16f, 0.045f), grip);
                Part(PrimitiveType.Cylinder, "Barrel", m, new Vector3(0f, 0.47f, 0f), new Vector3(0.085f, 0.28f, 0.085f), wood);
            });
            var pan = Model("Pan", new Vector3(25f, 0f, 0f), m =>
            {
                Part(PrimitiveType.Cube, "Handle", m, new Vector3(0f, 0.12f, 0f), new Vector3(0.04f, 0.32f, 0.03f), grip);
                var disc = Part(PrimitiveType.Cylinder, "Pan", m, new Vector3(0f, 0.45f, 0f), new Vector3(0.38f, 0.025f, 0.38f), metal);
                disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            });
            var pistol = Model("Pistol", Vector3.zero, m =>
            {
                Part(PrimitiveType.Cube, "Slide", m, new Vector3(0f, 0.04f, 0.1f), new Vector3(0.07f, 0.09f, 0.26f), metal);
                var g = Part(PrimitiveType.Cube, "Grip", m, new Vector3(0f, -0.06f, 0f), new Vector3(0.06f, 0.14f, 0.07f), grip);
                g.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
                Muzzle(m, new Vector3(0f, 0.05f, 0.25f));
            });
            var waterGun = Model("WaterGun", Vector3.zero, m =>
            {
                Part(PrimitiveType.Cube, "Body", m, new Vector3(0f, 0.03f, 0.1f), new Vector3(0.09f, 0.1f, 0.32f), toy);
                Part(PrimitiveType.Cylinder, "Tank", m, new Vector3(0f, 0.13f, 0.04f), new Vector3(0.14f, 0.06f, 0.14f), toyTank);
                var nozzle = Part(PrimitiveType.Cylinder, "Nozzle", m, new Vector3(0f, 0.04f, 0.3f), new Vector3(0.04f, 0.05f, 0.04f), toyTank);
                nozzle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var g = Part(PrimitiveType.Cube, "Grip", m, new Vector3(0f, -0.07f, 0f), new Vector3(0.06f, 0.14f, 0.07f), toy);
                g.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
                Muzzle(m, new Vector3(0f, 0.04f, 0.36f));
            });
            var waterBall = Model("WaterBall", Vector3.zero, m =>
                Part(PrimitiveType.Sphere, "Ball", m, Vector3.zero, Vector3.one * 0.22f, water));

            // 武器数据（数据库顺序 = 网络下标，不要随意调整）
            var weapons = new[]
            {
                Weapon("Bat", w =>
                {
                    w.displayName = "棒球棍"; w.kind = WeaponKind.Melee; w.model = bat;
                    w.damage = 25; w.knockback = 10f; w.knockbackUp = 3f; w.fireInterval = 0.55f;
                    w.range = 2.2f; w.meleeAngle = 100f;
                }),
                Weapon("Pan", w =>
                {
                    w.displayName = "平底锅"; w.kind = WeaponKind.Melee; w.model = pan;
                    w.damage = 15; w.knockback = 13f; w.knockbackUp = 4f; w.knockDownOnHit = true; w.fireInterval = 0.75f;
                    w.range = 2.0f; w.meleeAngle = 90f;
                }),
                Weapon("Pistol", w =>
                {
                    w.displayName = "手枪"; w.kind = WeaponKind.Hitscan; w.model = pistol;
                    w.damage = 15; w.knockback = 3f; w.knockbackUp = 0.5f; w.fireInterval = 0.25f;
                    w.range = 60f; w.magazineSize = 8; w.reloadTime = 1.2f; w.spread = 0.5f;
                }),
                Weapon("WaterGun", w =>
                {
                    w.displayName = "水枪"; w.kind = WeaponKind.Projectile; w.model = waterGun; w.projectileModel = waterBall;
                    w.damage = 8; w.knockback = 6f; w.knockbackUp = 1.5f; w.fireInterval = 0.15f; w.automatic = true;
                    w.range = 30f; w.magazineSize = 24; w.reloadTime = 1.5f; w.spread = 2f;
                    w.projectileSpeed = 22f; w.projectileGravity = 6f; w.projectileRadius = 0.12f; w.projectileLifetime = 3f;
                }),
            };

            string dbPath = ResourcesDir + "/WeaponDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<WeaponDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<WeaponDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }
            db.weapons = weapons;
            EditorUtility.SetDirty(db);

            return new CombatAssets
            {
                Pickup = BuildPickupPrefab(),
                Dummy = BuildDummyPrefab(),
                WeaponCount = weapons.Length,
            };
        }

        static GameObject Model(string name, Vector3 rootEuler, System.Action<Transform> build)
        {
            var root = new GameObject(name);
            root.transform.localRotation = Quaternion.Euler(rootEuler);
            build(root.transform);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{WeaponModelDir}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void Muzzle(Transform parent, Vector3 position)
        {
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(parent, false);
            muzzle.localPosition = position;
        }

        static WeaponDefinition Weapon(string name, System.Action<WeaponDefinition> setup)
        {
            string path = $"{WeaponDataDir}/{name}.asset";
            var def = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            setup(def);
            EditorUtility.SetDirty(def);
            return def;
        }

        static WeaponPickup BuildPickupPrefab()
        {
            var accent = Mat("PickupGlow", new Color(1f, 0.85f, 0.35f), 0.3f);
            var root = new GameObject("WeaponPickup");
            root.AddComponent<NetworkObject>();
            var pickup = root.AddComponent<WeaponPickup>();
            var modelRoot = new GameObject("ModelRoot").transform;
            modelRoot.SetParent(root.transform, false);
            modelRoot.localPosition = new Vector3(0f, 0.6f, 0f);
            Part(PrimitiveType.Cylinder, "Base", root.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.8f, 0.02f, 0.8f), accent);
            Set(pickup, "modelRoot", modelRoot);
            return SaveNetworkPrefab(root, PickupPrefabPath).GetComponent<WeaponPickup>();
        }

        static NetworkObject BuildDummyPrefab()
        {
            var straw = Mat("Straw", new Color(0.92f, 0.80f, 0.45f));
            var wood = Mat("Wood", new Color(0.85f, 0.62f, 0.38f));
            var dark = Mat("Grip", new Color(0.18f, 0.14f, 0.12f));
            var red = Mat("TargetRed", new Color(0.9f, 0.22f, 0.2f));
            var white = Mat("TargetWhite", new Color(0.95f, 0.95f, 0.92f));

            var root = new GameObject("TrainingDummy");
            root.AddComponent<NetworkObject>();
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.height = 2f;
            col.radius = 0.4f;

            var health = root.AddComponent<Health>();
            Set(health, "maxHealth", 1000);
            Set(health, "canDie", false);
            Set(health, "regenerateWhenIdle", true);
            Set(health, "regenerateDelay", 3f);
            var dummy = root.AddComponent<TrainingDummy>();

            Part(PrimitiveType.Cylinder, "Base", root.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.9f, 0.05f, 0.9f), dark);
            var pivot = new GameObject("WobblePivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, 0.1f, 0f);
            Part(PrimitiveType.Cylinder, "Post", pivot, new Vector3(0f, 0.45f, 0f), new Vector3(0.14f, 0.45f, 0.14f), wood);
            Part(PrimitiveType.Cylinder, "Torso", pivot, new Vector3(0f, 1.15f, 0f), new Vector3(0.7f, 0.42f, 0.7f), straw);
            Part(PrimitiveType.Cube, "Arms", pivot, new Vector3(0f, 1.35f, 0f), new Vector3(1.4f, 0.14f, 0.14f), wood);
            Part(PrimitiveType.Cube, "Head", pivot, new Vector3(0f, 1.82f, 0f), new Vector3(0.5f, 0.45f, 0.45f), straw);
            var ringOuter = Part(PrimitiveType.Cylinder, "TargetOuter", pivot, new Vector3(0f, 1.15f, 0.34f), new Vector3(0.5f, 0.02f, 0.5f), red);
            ringOuter.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var ringMid = Part(PrimitiveType.Cylinder, "TargetMid", pivot, new Vector3(0f, 1.15f, 0.355f), new Vector3(0.32f, 0.02f, 0.32f), white);
            ringMid.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var ringIn = Part(PrimitiveType.Cylinder, "TargetInner", pivot, new Vector3(0f, 1.15f, 0.37f), new Vector3(0.14f, 0.02f, 0.14f), red);
            ringIn.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var label = new GameObject("LabelAnchor").transform;
            label.SetParent(root.transform, false);
            label.localPosition = new Vector3(0f, 2.55f, 0f);

            Set(dummy, "wobblePivot", pivot);
            Set(dummy, "labelAnchor", label);
            return SaveNetworkPrefab(root, DummyPrefabPath).GetComponent<NetworkObject>();
        }

        static GameObject SaveNetworkPrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Debug.Log($"[PlaygroundBuilder] {path} GlobalObjectIdHash = {prefab.GetComponent<NetworkObject>().PrefabIdHash}");
            return prefab;
        }

        static void BuildTrainingArea(NetworkManager network, CombatAssets combat)
        {
            var stone = Mat("Stone", new Color(0.72f, 0.72f, 0.76f));

            var area = new GameObject("TrainingArea").transform;
            Block("TrainingPad", area, new Vector3(10.5f, 0.05f, 0f), new Vector3(3f, 0.1f, 10f), stone);

            var dummies = new GameObject("DummyPoints").transform;
            dummies.SetParent(area, false);
            for (int i = 0; i < 3; i++)
            {
                var point = new GameObject($"Dummy{i + 1}").transform;
                point.SetParent(dummies, false);
                point.position = new Vector3(10.5f, 0.1f, -3f + i * 3f);
                point.rotation = Quaternion.LookRotation(Vector3.left);
            }

            var weaponPoints = new GameObject("WeaponPoints").transform;
            weaponPoints.SetParent(area, false);
            for (int i = 0; i < combat.WeaponCount; i++)
            {
                var point = new GameObject($"WeaponSpawn{i + 1}");
                point.transform.SetParent(weaponPoints, false);
                point.transform.position = new Vector3(-3f + i * 2f, 0f, -2.6f);
                var spawn = point.AddComponent<WeaponSpawnPoint>();
                Set(spawn, "weaponIndex", i);
            }

            var spawner = area.gameObject.AddComponent<ServerSpawner>();
            Set(spawner, "network", network);
            Set(spawner, "dummyPrefab", combat.Dummy);
            Set(spawner, "pickupPrefab", combat.Pickup);
            Set(spawner, "dummyPoints", dummies);
            Set(spawner, "weaponPoints", weaponPoints);
        }

        static void RegisterNetworkPrefabs(params GameObject[] prefabs)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:NetworkPrefabsList"))
            {
                var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var prefab in prefabs)
                {
                    if (!list.Contains(prefab))
                        list.Add(new NetworkPrefab { Prefab = prefab });
                }
                EditorUtility.SetDirty(list);
                Debug.Log($"[PlaygroundBuilder] NetworkPrefabsList {list.name}: {list.PrefabList.Count} prefabs");
            }
        }

        static void Set(Object target, string property, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p == null)
            {
                Debug.LogError($"[PlaygroundBuilder] {target.GetType().Name}.{property} not found");
                return;
            }
            switch (value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
