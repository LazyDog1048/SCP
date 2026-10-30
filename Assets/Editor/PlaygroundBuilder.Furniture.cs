using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ScpParty.EditorTools
{
    public static partial class PlaygroundBuilder
    {
        const string FurnitureSource = "Assets/ThirdParty/KenneyFurniture/Models";
        const string FurnitureOutput = "Assets/Prefabs/Furniture";

        [MenuItem("SCP/用 Kenney 家具重搭房间")]
        public static void RebuildFurniture()
        {
            EditorSceneManager.OpenScene(ScenePath);
            FurnishRooms();
            AssetDatabase.SaveAssets();
            Debug.Log("[Furniture] COMPLETE");
        }

        public static void PreviewFurniture()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var camera=Camera.main;
            camera.fieldOfView=65;
            var rt=new RenderTexture(1440,900,24);
            camera.targetTexture=rt;
            var image=new Texture2D(1440,900,TextureFormat.RGB24,false);
            var positions=new[]{new Vector3(-11f,3.8f,-9f),new Vector3(1f,3.6f,12.5f)};
            var targets=new[]{new Vector3(0f,.9f,2f),new Vector3(0f,1f,20f)};
            for(int i=0;i<2;i++)
            {
                camera.transform.position=positions[i];camera.transform.LookAt(targets[i]);camera.Render();
                RenderTexture.active=rt; image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
                File.WriteAllBytes("Logs/FurnitureRoom"+(i==0?"A":"B")+".png",image.EncodeToPNG());
            }
            RenderTexture.active=null;camera.targetTexture=null;
            Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
        }

        public static void BuildFurnitureTest()
        {
            RebuildFurniture();
            PreviewFurniture();
            BuildWindows();
        }

        static void FurnishRooms()
        {
            Directory.CreateDirectory(FurnitureOutput);
            AssetDatabase.Refresh();
            foreach (var source in Directory.GetFiles(FurnitureSource, "*.fbx"))
            {
                string name = Path.GetFileNameWithoutExtension(source);
                var root = new GameObject(name);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source), root.transform);
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(original =>
                    {
                        string key = original == null ? "Default" : original.name.Replace('/', '_');
                        string path = FurnitureOutput + "/" + key + ".mat";
                        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (material == null)
                        {
                            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                            material.SetColor("_BaseColor", original != null && original.HasProperty("_Color") ? original.color : Color.white);
                            material.SetFloat("_Smoothness", .15f);
                            AssetDatabase.CreateAsset(material, path);
                        }
                        return material;
                    }).ToArray();
                }
                PrefabUtility.SaveAsPrefabAsset(root, FurnitureOutput + "/" + name + ".prefab");
                Object.DestroyImmediate(root);
            }
            var rooms = GameObject.Find("Rooms").transform;
            foreach (var transform in rooms.GetComponentsInChildren<Transform>().ToArray())
            {
                if (transform == null || !PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject)) continue;
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject);
                if (path.StartsWith(PropDir + "/")) Object.DestroyImmediate(transform.gameObject);
            }
            var old = GameObject.Find("Level"); if (old != null) Object.DestroyImmediate(old);
            old = GameObject.Find("FurnitureLayout"); if (old != null) Object.DestroyImmediate(old);
            old = GameObject.Find("TrainingPad"); if (old != null) Object.DestroyImmediate(old);
            var layout = new GameObject("FurnitureLayout").transform;
            var a = new GameObject("A_LoungeAndEquipment").transform; a.SetParent(layout);
            var b = new GameObject("B_Workroom").transform; b.SetParent(layout);

            Place("table", a, new Vector3(-2.4f, 0, -2.2f), new Vector3(2.4f,.95f,1.2f));
            Place("table", a, new Vector3(2.4f, 0, -2.2f), new Vector3(2.4f,.95f,1.2f));
            Place("rugRectangle", a, new Vector3(-8f,.01f,3.6f), new Vector3(6f,.02f,5f), false);
            Place("loungeSofa", a, new Vector3(-10f,0,4f), new Vector3(3f,1.1f,1.1f), true,90);
            Place("loungeChair", a, new Vector3(-6f,0,5.5f), new Vector3(1.2f,1.1f,1.2f),true,180);
            Place("tableCoffee", a, new Vector3(-7.9f,0,3.5f), new Vector3(2f,.5f,1f));
            Place("radio", a, new Vector3(-7.9f,.5f,3.5f), new Vector3(.5f,.3f,.2f),false);
            Place("bookcaseOpen", a, new Vector3(-12.3f,0,8.5f), new Vector3(2.4f,2.2f,.6f),true,90);
            for(int i=0;i<3;i++) Place("books",a,new Vector3(-12.3f,.38f+i*.58f,8.4f),new Vector3(.65f,.36f,.3f),false,90);
            Place("pottedPlant", a, new Vector3(-11.5f,0,1f), new Vector3(.8f,1.7f,.8f));
            Place("lampRoundFloor", a, new Vector3(-11.8f,0,5.8f), new Vector3(.65f,2f,.65f));
            Place("coatRackStanding", a, new Vector3(-11.5f,0,-8f),new Vector3(.8f,1.8f,.8f));
            Place("bookcaseClosedWide", a,new Vector3(7f,0,-10.5f),new Vector3(3f,2.1f,.7f));
            Place("cardboardBoxClosed",a,new Vector3(9.5f,0,-9.5f),new Vector3(.9f,.8f,.8f));
            Place("cardboardBoxOpen",a,new Vector3(10.7f,0,-9f),new Vector3(.8f,.7f,.8f));
            Place("trashcan",a,new Vector3(11.8f,0,-7f),new Vector3(.6f,.9f,.6f));
            Place("pottedPlant",a,new Vector3(4f,0,9.8f),new Vector3(.8f,1.7f,.8f));
            Place("rugRectangle",a,new Vector3(10.5f,.01f,0),new Vector3(3.5f,.02f,10.5f),false);

            for (int i=0;i<3;i++) Place(i==1?"kitchenSink":"kitchenCabinetDrawer",b,new Vector3(6.9f,0,16f+i*2.1f),new Vector3(2f,1f,.85f),true,-90);
            Place("kitchenFridge",b,new Vector3(6.7f,0,23.7f),new Vector3(1f,2f,1f),true,-90);
            Place("kitchenCoffeeMachine",b,new Vector3(6.9f,1f,16f),new Vector3(.5f,.6f,.5f),false,-90);
            for(int i=0;i<2;i++)
            {
                float x=-5.4f+i*3.2f;
                Place("desk",b,new Vector3(x,0,23.7f),new Vector3(2.5f,.95f,1.1f),true,180);
                Place("computerScreen",b,new Vector3(x,.95f,23.9f),new Vector3(.8f,.6f,.25f),false,180);
                Place("computerKeyboard",b,new Vector3(x,.95f,23.35f),new Vector3(.65f,.05f,.22f),false,180);
                Place("chairDesk",b,new Vector3(x,0,22.3f),new Vector3(.7f,1.2f,.7f));
            }
            Place("bookcaseClosedWide",b,new Vector3(-7.2f,0,17.5f),new Vector3(3f,2.2f,.6f),true,90);
            Place("bookcaseOpen",b,new Vector3(-7.2f,0,20.8f),new Vector3(2.4f,2.2f,.6f),true,90);
            for(int i=0;i<3;i++) Place("books",b,new Vector3(-7.2f,.38f+i*.58f,20.7f),new Vector3(.65f,.36f,.3f),false,90);
            Place("cardboardBoxClosed",b,new Vector3(-6f,0,13.4f),new Vector3(.9f,.8f,.9f));
            Place("cardboardBoxOpen",b,new Vector3(-4.8f,0,13.8f),new Vector3(.8f,.65f,.8f));
            Place("pottedPlant",b,new Vector3(-.5f,0,24f),new Vector3(.8f,1.6f,.8f));
            foreach(float z in new[]{-5f,5f}) foreach(float x in new[]{-6f,6f}) CeilingLamp(a,new Vector3(x,4.7f,z));
            CeilingLamp(b,new Vector3(0f,4.7f,16f)); CeilingLamp(b,new Vector3(0f,4.7f,22f));
            foreach (var renderer in rooms.GetComponentsInChildren<Renderer>())
            {
                string name=renderer.gameObject.name;
                if(name=="Floor") renderer.sharedMaterial=Mat("FurnishedFloor",new Color(.29f,.39f,.43f));
                else if(name.StartsWith("Wall")) renderer.sharedMaterial=Mat("FurnishedWall",new Color(.79f,.85f,.83f));
            }
            var mainLight=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l=>l.type==LightType.Directional);
            if(mainLight!=null) { mainLight.intensity=.8f; mainLight.transform.rotation=Quaternion.Euler(65f,-25f,0f); }
            EditorSceneManager.SaveScene(layout.gameObject.scene);
        }

        static GameObject Place(string name,Transform parent,Vector3 position,Vector3 size,bool solid=true,float yaw=0)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FurnitureOutput+"/"+name+".prefab"),parent);
            var renderers=root.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            root.transform.localScale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
            bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            var model=root.transform.GetChild(0);
            model.localPosition-=new Vector3(bounds.center.x/root.transform.localScale.x,bounds.min.y/root.transform.localScale.y,bounds.center.z/root.transform.localScale.z);
            if(solid) { var collider=root.AddComponent<BoxCollider>(); collider.center=new Vector3(0,size.y/2/root.transform.localScale.y,0); collider.size=new Vector3(size.x/root.transform.localScale.x,size.y/root.transform.localScale.y,size.z/root.transform.localScale.z); }
            root.transform.position=position; root.transform.rotation=Quaternion.Euler(0,yaw,0);
            return root;
        }

        static void CeilingLamp(Transform parent,Vector3 position)
        {
            var fixture=Place("lampSquareCeiling",parent,position,new Vector3(.8f,.25f,.8f),false);
            var light=new GameObject("WarmLight").AddComponent<Light>(); light.transform.SetParent(fixture.transform,false);
            light.transform.position=position+Vector3.down*.3f; light.type=LightType.Point;light.range=15;light.intensity=3;
            light.color=new Color(1,.88f,.7f);
        }
    }
}
