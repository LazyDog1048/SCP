using System.Collections;
using UnityEngine;

namespace ScpParty.Weapons
{
    /// <summary>简单的本地特效：弹道线、命中小方块。材质放在 Resources/FX。</summary>
    public static class CombatFx
    {
        static Material tracerMaterial;
        static Material impactMaterial;
        static Material splashMaterial;
        static FxRunner runner;

        public static void Tracer(Vector3 from, Vector3 to)
        {
            tracerMaterial ??= Resources.Load<Material>("FX/Tracer");
            var go = new GameObject("Tracer");
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = tracerMaterial;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = 0.035f;
            line.endWidth = 0.015f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.Destroy(go, 0.06f);
        }

        public static void Impact(Vector3 point, WeaponKind kind)
        {
            bool water = kind == WeaponKind.Projectile;
            Material mat = water
                ? splashMaterial ??= Resources.Load<Material>("FX/Splash")
                : impactMaterial ??= Resources.Load<Material>("FX/Impact");
            int pieces = water ? 6 : 3;
            for (int i = 0; i < pieces; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(go.GetComponent<Collider>());
                go.transform.position = point;
                go.transform.localScale = Vector3.one * (water ? 0.12f : 0.07f);
                go.transform.rotation = Random.rotation;
                go.GetComponent<Renderer>().sharedMaterial = mat;
                Runner.StartCoroutine(Fly(go.transform, Random.insideUnitSphere * 2.5f + Vector3.up * 2f, 0.35f));
            }
        }

        static FxRunner Runner
        {
            get
            {
                if (runner == null)
                {
                    var go = new GameObject("CombatFx");
                    Object.DontDestroyOnLoad(go);
                    runner = go.AddComponent<FxRunner>();
                }
                return runner;
            }
        }

        static IEnumerator Fly(Transform t, Vector3 velocity, float duration)
        {
            Vector3 startScale = t.localScale;
            for (float age = 0f; age < duration && t != null; age += Time.deltaTime)
            {
                velocity += Vector3.down * 9.81f * Time.deltaTime;
                t.position += velocity * Time.deltaTime;
                t.localScale = startScale * (1f - age / duration);
                yield return null;
            }
            if (t != null)
                Object.Destroy(t.gameObject);
        }

        class FxRunner : MonoBehaviour { }
    }
}
