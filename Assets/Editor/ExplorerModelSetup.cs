using System.IO;
using UnityEditor;
using UnityEngine;

namespace ScpParty.EditorTools
{
    /// <summary>
    /// 导入 Tripo 生成的探险小人（Assets/Models/Explorer）：设置导入参数、建 URP 材质，
    /// 生成脚底在原点、面朝 +Z、身高与玩家胶囊一致的 Explorer.prefab，并渲染正面/侧面预览图到 Logs/（不进 Git）。
    /// 模型目前不带骨骼（Tripo 免费账号导不出骨骼），只用作静态外观。
    /// 批处理：Unity.exe -batchmode -quit -projectPath . -executeMethod ScpParty.EditorTools.ExplorerModelSetup.Setup
    /// </summary>
    public static class ExplorerModelSetup
    {
        const string Dir = "Assets/Models/Explorer";
        const string ModelPath = Dir + "/Explorer.fbx";
        const string BaseColorPath = Dir + "/Textures/Explorer_BaseColor.jpg";
        const string NormalPath = Dir + "/Textures/Explorer_Normal.png";
        const string MaterialPath = Dir + "/Explorer.mat";
        const string PrefabPath = Dir + "/Explorer.prefab";

        // 与玩家 CharacterController 高度一致（PlaygroundBuilder：height 1.8）。
        const float TargetHeight = 1.8f;
        // Tripo 导出的 FBX 是 Z 轴朝上、趴着的（头朝 +Z、脸朝 -Y），绕 X 转 -90° 后站立且脸朝 +Z（已用预览图确认）。
        static readonly Vector3 ModelEuler = new(-90f, 0f, 0f);

        [MenuItem("SCP/模型/导入探险小人 Explorer")]
        public static void Setup()
        {
            ConfigureImporters();
            var material = BuildMaterial();
            BuildPrefab(material);
            AssetDatabase.SaveAssets();
            RenderPreviews();
        }

        static void ConfigureImporters()
        {
            var model = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            model.materialImportMode = ModelImporterMaterialImportMode.None;
            model.importAnimation = false;
            model.animationType = ModelImporterAnimationType.None;
            model.importCameras = false;
            model.importLights = false;
            model.importBlendShapes = false;
            model.SaveAndReimport();

            var normal = (TextureImporter)AssetImporter.GetAtPath(NormalPath);
            normal.textureType = TextureImporterType.NormalMap;
            normal.maxTextureSize = 2048;
            normal.SaveAndReimport();

            var baseColor = (TextureImporter)AssetImporter.GetAtPath(BaseColorPath);
            baseColor.sRGBTexture = true;
            baseColor.maxTextureSize = 2048;
            baseColor.SaveAndReimport();
        }

        static Material BuildMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath);
            material.SetTexture("_BaseMap", baseMap);
            material.SetTexture("_MainTex", baseMap);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
            material.EnableKeyword("_NORMALMAP");
            // 派对风格：不要金属感，略带哑光。
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void BuildPrefab(Material material)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var root = new GameObject("Explorer");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            model.name = "Model";
            model.transform.localRotation = Quaternion.Euler(ModelEuler);

            foreach (var r in model.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = material;

            var bounds = WorldBounds(model);
            float scale = TargetHeight / bounds.size.y;
            model.transform.localScale *= scale;
            bounds = WorldBounds(model);
            model.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            bounds = WorldBounds(model);
            int triangles = 0;
            foreach (var f in model.GetComponentsInChildren<MeshFilter>())
                triangles += f.sharedMesh.triangles.Length / 3;
            Debug.Log($"[ExplorerSetup] scale={scale:F4} size={bounds.size} min={bounds.min} triangles={triangles}");

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers)
                b.Encapsulate(r.bounds);
            return b;
        }

        // 从 +Z（应看到脸）和 +X 两个方向各拍一张，供人工确认朝向。
        static void RenderPreviews()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var instance = Object.Instantiate(prefab);
            var lightGo = new GameObject("PreviewLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(35f, 200f, 0f);

            var camGo = new GameObject("PreviewCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.35f, 0.38f, 0.42f);
            cam.fieldOfView = 30f;

            Directory.CreateDirectory("Logs");
            Shoot(cam, new Vector3(0f, 0.95f, 4.2f), "Logs/ExplorerPreview_front.png");
            Shoot(cam, new Vector3(4.2f, 0.95f, 0f), "Logs/ExplorerPreview_side.png");

            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(lightGo);
            Object.DestroyImmediate(instance);
        }

        static void Shoot(Camera cam, Vector3 position, string path)
        {
            cam.transform.position = position;
            cam.transform.LookAt(new Vector3(0f, 0.9f, 0f));
            var rt = new RenderTexture(512, 512, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(512, 512, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Debug.Log($"[ExplorerSetup] preview {path}");
        }
    }
}
