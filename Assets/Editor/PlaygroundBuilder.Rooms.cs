using ScpParty.Interaction;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ScpParty.EditorTools
{
    /// <summary>生成器的房间部分：房间 A/B 外壳、连接门、工作台等道具 prefab 与摆放。</summary>
    public static partial class PlaygroundBuilder
    {
        const string PropDir = Root + "/Prefabs/Props";
        const string DoorPrefabPath = Root + "/Prefabs/Door.prefab";

        const float WallThickness = 0.4f;
        const float RoomHeight = 5f;
        const float DoorGapWidth = 1.8f;
        const float DoorGapHeight = 2.8f;

        // 房间内部范围（xz）。A 在南，B 在北，共用 A 的北墙（z = 11 ~ 11.4），门在 x = 0。
        static readonly Rect RoomA = Rect.MinMaxRect(-13f, -11f, 13f, 11f);
        static readonly Rect RoomB = Rect.MinMaxRect(-8f, 11f + WallThickness, 8f, 25.4f);
        static readonly Vector3 DoorPosition = new(0f, 0f, 11f + WallThickness * 0.5f);

        struct Props
        {
            public GameObject Workbench, WallWorkbench, Shelf, Crate, Lamp;
        }

        /// <summary>生成两个房间并摆放道具，返回门的生成点（由服务器生成联网的门）。</summary>
        static Transform BuildRooms(out Transform weaponPoints)
        {
            var floorA = Mat("FloorA", new Color(0.80f, 0.74f, 0.62f));
            var floorB = Mat("FloorB", new Color(0.66f, 0.50f, 0.36f));
            var wallA = Mat("WallA", new Color(0.93f, 0.90f, 0.82f));
            var wallB = Mat("WallB", new Color(0.78f, 0.86f, 0.80f));
            var ceiling = Mat("Ceiling", new Color(0.95f, 0.95f, 0.93f));

            var props = BuildPropPrefabs();
            var rooms = new GameObject("Rooms").transform;

            // ---- 房间 A：出生点、木桩、武器工作台 ----
            var a = new GameObject("RoomA").transform;
            a.SetParent(rooms, false);
            Shell(a, RoomA, floorA, wallA, ceiling, northDoorX: 0f, buildSouth: true);
            Prop(props.Workbench, a, new Vector3(-2.4f, 0f, -2.2f), Vector3.back);
            Prop(props.Workbench, a, new Vector3(2.4f, 0f, -2.2f), Vector3.back);
            Prop(props.WallWorkbench, a, new Vector3(-8f, 0f, -10.45f), Vector3.forward);
            Prop(props.Shelf, a, new Vector3(-12.7f, 0f, -6f), Vector3.right);
            Prop(props.Crate, a, new Vector3(12.3f, 0f, -10.3f), Vector3.forward, 20f);
            Prop(props.Lamp, a, new Vector3(-6f, RoomHeight, 0f), Vector3.forward);
            Prop(props.Lamp, a, new Vector3(6f, RoomHeight, 0f), Vector3.forward);

            weaponPoints = new GameObject("WeaponPoints").transform;
            weaponPoints.SetParent(a, false);
            float benchTop = 0.95f;
            float[] weaponX = { -3.0f, -1.8f, 1.8f, 3.0f };
            for (int i = 0; i < weaponX.Length; i++)
            {
                var point = new GameObject($"WeaponSpawn{i + 1}");
                point.transform.SetParent(weaponPoints, false);
                point.transform.position = new Vector3(weaponX[i], benchTop, -2.2f);
                var spawn = point.AddComponent<Weapons.WeaponSpawnPoint>();
                Set(spawn, "weaponIndex", i);
            }

            // ---- 房间 B：工作间 ----
            var b = new GameObject("RoomB").transform;
            b.SetParent(rooms, false);
            Shell(b, RoomB, floorB, wallB, ceiling, northDoorX: null, buildSouth: false);
            Prop(props.WallWorkbench, b, new Vector3(7.45f, 0f, 15f), Vector3.left);
            Prop(props.WallWorkbench, b, new Vector3(7.45f, 0f, 19.5f), Vector3.left);
            Prop(props.Workbench, b, new Vector3(-1f, 0f, 19f), Vector3.back);
            Prop(props.Shelf, b, new Vector3(-1.5f, 0f, 25.1f), Vector3.back);
            Prop(props.Shelf, b, new Vector3(-4.5f, 0f, 25.1f), Vector3.back);
            Prop(props.Crate, b, new Vector3(-6.6f, 0f, 13.2f), Vector3.forward, 10f);
            Prop(props.Crate, b, new Vector3(-5.5f, 0f, 13.4f), Vector3.forward, -15f);
            Prop(props.Crate, b, new Vector3(-6.1f, 0.9f, 13.3f), Vector3.forward, 35f);
            Prop(props.Crate, b, new Vector3(-6.8f, 0f, 22.5f), Vector3.forward, 5f);
            Prop(props.Lamp, b, new Vector3(0f, RoomHeight, 15f), Vector3.forward);
            Prop(props.Lamp, b, new Vector3(0f, RoomHeight, 21.5f), Vector3.forward);

            // ---- 门 ----
            var doorPoints = new GameObject("DoorPoints").transform;
            doorPoints.SetParent(rooms, false);
            var doorPoint = new GameObject("DoorAB").transform;
            doorPoint.SetParent(doorPoints, false);
            doorPoint.position = DoorPosition;
            return doorPoints;
        }

        /// <summary>地板、四面墙（北墙可开门洞）、屋顶。屋顶不投影，阳光能照进来。</summary>
        static void Shell(Transform parent, Rect r, Material floor, Material wall, Material ceilingMat, float? northDoorX, bool buildSouth)
        {
            float t = WallThickness, h = RoomHeight;
            Vector2 c = r.center;
            float outerW = r.width + t * 2f, outerD = r.height + t * 2f;

            Block("Floor", parent, new Vector3(c.x, -0.25f, c.y), new Vector3(outerW, 0.5f, outerD), floor);
            var roof = Block("Ceiling", parent, new Vector3(c.x, h + 0.15f, c.y), new Vector3(outerW, 0.3f, outerD), ceilingMat);
            roof.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            Block("WallWest", parent, new Vector3(r.xMin - t / 2f, h / 2f, c.y), new Vector3(t, h, r.height), wall);
            Block("WallEast", parent, new Vector3(r.xMax + t / 2f, h / 2f, c.y), new Vector3(t, h, r.height), wall);
            if (buildSouth)
                Block("WallSouth", parent, new Vector3(c.x, h / 2f, r.yMin - t / 2f), new Vector3(outerW, h, t), wall);

            float northZ = r.yMax + t / 2f;
            if (northDoorX is float doorX)
            {
                float left = r.xMin - t, right = r.xMax + t;
                float gapL = doorX - DoorGapWidth / 2f, gapR = doorX + DoorGapWidth / 2f;
                Block("WallNorthL", parent, new Vector3((left + gapL) / 2f, h / 2f, northZ), new Vector3(gapL - left, h, t), wall);
                Block("WallNorthR", parent, new Vector3((gapR + right) / 2f, h / 2f, northZ), new Vector3(right - gapR, h, t), wall);
                Block("WallNorthLintel", parent, new Vector3(doorX, (DoorGapHeight + h) / 2f, northZ), new Vector3(DoorGapWidth, h - DoorGapHeight, t), wall);
            }
            else
            {
                Block("WallNorth", parent, new Vector3(c.x, h / 2f, northZ), new Vector3(outerW, h, t), wall);
            }
        }

        static void Prop(GameObject prefab, Transform parent, Vector3 position, Vector3 facing, float extraYaw = 0f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(facing) * Quaternion.Euler(0f, extraYaw, 0f);
        }

        // ---------- 道具 prefab（正面朝 +Z，原点在地面中心） ----------

        static Props BuildPropPrefabs()
        {
            System.IO.Directory.CreateDirectory(PropDir);
            var wood = Mat("Wood", new Color(0.85f, 0.62f, 0.38f));
            var darkWood = Mat("DarkWood", new Color(0.45f, 0.32f, 0.22f));
            var grip = Mat("Grip", new Color(0.18f, 0.14f, 0.12f));
            var metal = Mat("Metal", new Color(0.22f, 0.23f, 0.26f), 0.4f);
            var red = Mat("ToolRed", new Color(0.85f, 0.25f, 0.22f));
            var blue = Mat("ToolBlue", new Color(0.25f, 0.45f, 0.80f));
            var peg = Mat("Pegboard", new Color(0.88f, 0.76f, 0.56f));
            var shade = Mat("LampShade", new Color(0.30f, 0.45f, 0.40f));
            var bulb = Mat("Bulb", new Color(1f, 0.92f, 0.70f), 0f, MaterialDir, "Universal Render Pipeline/Unlit");

            var props = new Props();

            props.Workbench = PropPrefab("Workbench", new Vector3(2.4f, 0.95f, 1f), root =>
            {
                BenchBody(root, wood, darkWood);
                Part(PrimitiveType.Cube, "Vise", root, new Vector3(0.95f, 1.03f, 0.3f), new Vector3(0.2f, 0.15f, 0.25f), metal);
                Part(PrimitiveType.Cube, "ToolBox", root, new Vector3(-0.85f, 1.05f, -0.3f), new Vector3(0.45f, 0.2f, 0.22f), red);
            });

            props.WallWorkbench = PropPrefab("WallWorkbench", new Vector3(2.4f, 0.95f, 1f), root =>
            {
                BenchBody(root, wood, darkWood);
                Part(PrimitiveType.Cube, "Pegboard", root, new Vector3(0f, 1.6f, -0.47f), new Vector3(2.4f, 1.2f, 0.05f), peg);
                Part(PrimitiveType.Cube, "Hammer", root, new Vector3(-0.7f, 1.6f, -0.42f), new Vector3(0.06f, 0.4f, 0.04f), grip);
                Part(PrimitiveType.Cube, "HammerHead", root, new Vector3(-0.7f, 1.82f, -0.42f), new Vector3(0.18f, 0.07f, 0.06f), metal);
                Part(PrimitiveType.Cube, "Wrench", root, new Vector3(-0.3f, 1.55f, -0.42f), new Vector3(0.05f, 0.45f, 0.03f), metal);
                Part(PrimitiveType.Cube, "Saw", root, new Vector3(0.45f, 1.6f, -0.43f), new Vector3(0.6f, 0.2f, 0.02f), metal);
                Part(PrimitiveType.Cube, "ToolBox", root, new Vector3(0.8f, 1.05f, 0.1f), new Vector3(0.45f, 0.2f, 0.22f), blue);
                Part(PrimitiveType.Cube, "Plank", root, new Vector3(-0.4f, 0.98f, 0.15f), new Vector3(0.9f, 0.05f, 0.25f), peg);
            });

            props.Shelf = PropPrefab("Shelf", new Vector3(2f, 2.2f, 0.5f), root =>
            {
                Part(PrimitiveType.Cube, "SideL", root, new Vector3(-0.97f, 1.1f, 0f), new Vector3(0.06f, 2.2f, 0.5f), darkWood);
                Part(PrimitiveType.Cube, "SideR", root, new Vector3(0.97f, 1.1f, 0f), new Vector3(0.06f, 2.2f, 0.5f), darkWood);
                Part(PrimitiveType.Cube, "Back", root, new Vector3(0f, 1.1f, -0.23f), new Vector3(1.9f, 2.2f, 0.04f), wood);
                for (int i = 0; i < 4; i++)
                    Part(PrimitiveType.Cube, $"Board{i}", root, new Vector3(0f, 0.1f + i * 0.65f, 0f), new Vector3(1.9f, 0.05f, 0.48f), darkWood);
                Part(PrimitiveType.Cube, "BoxA", root, new Vector3(-0.5f, 0.3f, 0.02f), new Vector3(0.5f, 0.35f, 0.38f), red);
                Part(PrimitiveType.Cube, "BoxB", root, new Vector3(0.45f, 0.95f, 0.02f), new Vector3(0.6f, 0.3f, 0.38f), blue);
                Part(PrimitiveType.Cube, "BoxC", root, new Vector3(-0.3f, 1.6f, 0.02f), new Vector3(0.4f, 0.4f, 0.36f), wood);
                Part(PrimitiveType.Cylinder, "Can", root, new Vector3(0.55f, 1.68f, 0.02f), new Vector3(0.2f, 0.12f, 0.2f), metal);
            });

            props.Crate = PropPrefab("Crate", new Vector3(0.9f, 0.9f, 0.9f), root =>
            {
                Part(PrimitiveType.Cube, "Box", root, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.9f), wood);
                Part(PrimitiveType.Cube, "BandFront", root, new Vector3(0f, 0.45f, 0.455f), new Vector3(0.92f, 0.12f, 0.02f), darkWood);
                Part(PrimitiveType.Cube, "BandSide", root, new Vector3(0.455f, 0.45f, 0f), new Vector3(0.02f, 0.12f, 0.92f), darkWood);
                Part(PrimitiveType.Cube, "BandBack", root, new Vector3(0f, 0.45f, -0.455f), new Vector3(0.92f, 0.12f, 0.02f), darkWood);
                Part(PrimitiveType.Cube, "BandSide2", root, new Vector3(-0.455f, 0.45f, 0f), new Vector3(0.02f, 0.12f, 0.92f), darkWood);
            });

            // 吊灯：原点在天花板
            props.Lamp = PropPrefab("Lamp", Vector3.zero, root =>
            {
                Part(PrimitiveType.Cylinder, "Cord", root, new Vector3(0f, -0.4f, 0f), new Vector3(0.03f, 0.4f, 0.03f), grip);
                Part(PrimitiveType.Cylinder, "Shade", root, new Vector3(0f, -0.9f, 0f), new Vector3(0.6f, 0.12f, 0.6f), shade);
                var b = Part(PrimitiveType.Sphere, "Bulb", root, new Vector3(0f, -1.02f, 0f), Vector3.one * 0.2f, bulb);
                b.shadowCastingMode = ShadowCastingMode.Off;
                var lightGo = new GameObject("Light");
                lightGo.transform.SetParent(root, false);
                lightGo.transform.localPosition = new Vector3(0f, -1.15f, 0f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.85f, 0.65f);
                light.range = 14f;
                light.intensity = 2.5f;
                light.shadows = LightShadows.None;
            });

            return props;
        }

        static void BenchBody(Transform root, Material top, Material legs)
        {
            Part(PrimitiveType.Cube, "Top", root, new Vector3(0f, 0.9f, 0f), new Vector3(2.4f, 0.1f, 1f), top);
            Part(PrimitiveType.Cube, "Shelf", root, new Vector3(0f, 0.25f, 0f), new Vector3(2.2f, 0.05f, 0.85f), top);
            foreach (var x in new[] { -1.1f, 1.1f })
                foreach (var z in new[] { -0.42f, 0.42f })
                    Part(PrimitiveType.Cube, "Leg", root, new Vector3(x, 0.425f, z), new Vector3(0.1f, 0.85f, 0.1f), legs);
        }

        /// <summary>道具 prefab：子物体只有外观，根节点一个盒子碰撞体（size 为 0 则不加）。</summary>
        static GameObject PropPrefab(string name, Vector3 colliderSize, System.Action<Transform> build)
        {
            var root = new GameObject(name);
            build(root.transform);
            if (colliderSize != Vector3.zero)
            {
                var box = root.AddComponent<BoxCollider>();
                box.size = colliderSize;
                box.center = new Vector3(0f, colliderSize.y / 2f, 0f);
            }
            GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PropDir}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------- 门 ----------

        static NetworkObject BuildDoorPrefab()
        {
            var doorWood = Mat("DoorWood", new Color(0.55f, 0.36f, 0.24f));
            var frame = Mat("DarkWood", new Color(0.45f, 0.32f, 0.22f));
            var metal = Mat("Metal", new Color(0.22f, 0.23f, 0.26f), 0.4f);

            var root = new GameObject("Door");
            root.AddComponent<NetworkObject>();
            var door = root.AddComponent<Door>();

            float half = DoorGapWidth / 2f;
            // 门框：碰撞体保留，填满门洞两侧
            Solid(PrimitiveType.Cube, "FrameL", root.transform, new Vector3(-half + 0.05f, DoorGapHeight / 2f, 0f), new Vector3(0.1f, DoorGapHeight, WallThickness + 0.04f), frame);
            Solid(PrimitiveType.Cube, "FrameR", root.transform, new Vector3(half - 0.05f, DoorGapHeight / 2f, 0f), new Vector3(0.1f, DoorGapHeight, WallThickness + 0.04f), frame);
            Solid(PrimitiveType.Cube, "FrameTop", root.transform, new Vector3(0f, DoorGapHeight - 0.05f, 0f), new Vector3(DoorGapWidth, 0.1f, WallThickness + 0.04f), frame);

            float panelWidth = DoorGapWidth - 0.2f, panelHeight = DoorGapHeight - 0.12f;
            var hinge = new GameObject("Hinge").transform;
            hinge.SetParent(root.transform, false);
            hinge.localPosition = new Vector3(-half + 0.1f, 0f, 0f);
            Solid(PrimitiveType.Cube, "Panel", hinge, new Vector3(panelWidth / 2f, panelHeight / 2f + 0.01f, 0f), new Vector3(panelWidth, panelHeight, 0.1f), doorWood);
            Part(PrimitiveType.Cube, "HandleFront", hinge, new Vector3(panelWidth - 0.15f, 1.1f, 0.08f), new Vector3(0.18f, 0.05f, 0.05f), metal);
            Part(PrimitiveType.Cube, "HandleBack", hinge, new Vector3(panelWidth - 0.15f, 1.1f, -0.08f), new Vector3(0.18f, 0.05f, 0.05f), metal);

            Set(door, "hinge", hinge);
            return SaveNetworkPrefab(root, DoorPrefabPath).GetComponent<NetworkObject>();
        }

        /// <summary>带碰撞体的部件（Part 会删掉碰撞体）。</summary>
        static Renderer Solid(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = mat;
            return renderer;
        }
    }
}
