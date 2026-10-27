using UnityEngine;

namespace ScpParty.Combat
{
    /// <summary>受击点上浮的伤害数字（纯本地表现）。</summary>
    public class DamagePopup : MonoBehaviour
    {
        const float Lifetime = 0.9f;

        static Font font;

        TextMesh text;
        Color baseColor;
        float age;
        Vector3 drift;

        public static void Spawn(Vector3 position, int amount)
        {
            var go = new GameObject("DamagePopup");
            go.transform.position = position + Random.insideUnitSphere * 0.15f;
            var popup = go.AddComponent<DamagePopup>();
            popup.text = CreateText(go, amount.ToString(), amount >= 20 ? new Color(1f, 0.35f, 0.25f) : new Color(1f, 0.9f, 0.3f), 0.06f);
            popup.baseColor = popup.text.color;
            popup.drift = new Vector3(Random.Range(-0.3f, 0.3f), 1.4f, 0f);
        }

        /// <summary>创建面向相机的世界空间文字（木桩信息也用它）。</summary>
        public static TextMesh CreateText(GameObject go, string content, Color color, float characterSize)
        {
            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var text = go.AddComponent<TextMesh>();
            text.font = font;
            text.text = content;
            text.color = color;
            text.fontSize = 48;
            text.characterSize = characterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            go.AddComponent<FaceCamera>();
            return text;
        }

        void Update()
        {
            age += Time.deltaTime;
            transform.position += drift * Time.deltaTime;
            drift.y = Mathf.Max(0.2f, drift.y - 2f * Time.deltaTime);
            var c = baseColor;
            c.a = 1f - Mathf.Clamp01((age - Lifetime * 0.5f) / (Lifetime * 0.5f));
            text.color = c;
            if (age >= Lifetime)
                Destroy(gameObject);
        }
    }

    /// <summary>让物体始终朝向主相机。</summary>
    public class FaceCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
