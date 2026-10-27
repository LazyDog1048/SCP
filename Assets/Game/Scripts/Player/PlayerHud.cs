using ScpParty.Combat;
using ScpParty.Weapons;
using Unity.Netcode;
using UnityEngine;

namespace ScpParty.Player
{
    /// <summary>
    /// 本地玩家的临时 HUD（IMGUI）：准星、命中标记、血量、武器与子弹、拾取提示、受伤闪红、死亡倒计时。
    /// </summary>
    public class PlayerHud : NetworkBehaviour
    {
        Health health;
        PlayerWeapons weapons;
        PlayerLife life;
        float hitMarkerUntil;
        float damageFlashUntil;
        static Texture2D white;
        static GUIStyle center, big;

        void Awake()
        {
            health = GetComponent<Health>();
            weapons = GetComponent<PlayerWeapons>();
            life = GetComponent<PlayerLife>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                Health.AnyDamaged += OnAnyDamaged;
        }

        public override void OnNetworkDespawn()
        {
            Health.AnyDamaged -= OnAnyDamaged;
        }

        void OnAnyDamaged(Health target, int amount, Vector3 point, Vector3 knockback, ulong attackerId)
        {
            if (target == health)
            {
                if (amount > 0)
                    damageFlashUntil = Time.time + 0.25f;
            }
            else if (attackerId == OwnerClientId)
            {
                hitMarkerUntil = Time.time + 0.15f;
            }
        }

        void OnGUI()
        {
            if (!IsOwner || !IsSpawned)
                return;

            white ??= Texture2D.whiteTexture;
            center ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, richText = true };
            big ??= new GUIStyle(center) { fontSize = 30 };

            float w = Screen.width, h = Screen.height;
            float s = Mathf.Max(1f, h / 720f);

            if (Time.time < damageFlashUntil)
                Fill(new Rect(0, 0, w, h), new Color(1f, 0f, 0f, 0.18f));

            // 准星
            var cc = new Vector2(w * 0.5f, h * 0.5f);
            Color crossColor = Time.time < hitMarkerUntil ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 1f, 1f, 0.85f);
            float gap = 4 * s, len = 7 * s, thick = 2 * s;
            if (Time.time < hitMarkerUntil) { gap = 6 * s; len = 10 * s; }
            Fill(new Rect(cc.x - thick / 2, cc.y - gap - len, thick, len), crossColor);
            Fill(new Rect(cc.x - thick / 2, cc.y + gap, thick, len), crossColor);
            Fill(new Rect(cc.x - gap - len, cc.y - thick / 2, len, thick), crossColor);
            Fill(new Rect(cc.x + gap, cc.y - thick / 2, len, thick), crossColor);

            // 血量
            float hp = health != null ? health.Current.Value : 0;
            float max = health != null ? health.Max : 100;
            var bar = new Rect(20 * s, h - 46 * s, 260 * s, 22 * s);
            Fill(bar, new Color(0f, 0f, 0f, 0.5f));
            Fill(new Rect(bar.x + 2 * s, bar.y + 2 * s, (bar.width - 4 * s) * Mathf.Clamp01(hp / max), bar.height - 4 * s),
                Color.Lerp(new Color(0.9f, 0.25f, 0.2f), new Color(0.35f, 0.85f, 0.4f), hp / max));
            GUI.Label(bar, $"{hp:0} / {max:0}", center);

            // 武器
            var def = weapons != null ? weapons.Held : null;
            var weaponRect = new Rect(w - 300 * s, h - 60 * s, 280 * s, 44 * s);
            string weaponText = def == null
                ? "空手 · 左键推人"
                : def.UsesAmmo
                    ? $"{def.displayName}  {(weapons.Reloading.Value ? "换弹中…" : $"{weapons.Ammo.Value}/{def.magazineSize}")}"
                    : def.displayName;
            GUI.Label(weaponRect, weaponText + (def != null ? "\n<size=13>R 换弹 · G 丢弃</size>" : ""), center);

            // 拾取提示
            if (weapons != null && weapons.LookedAtPickup != null && weapons.LookedAtPickup.Definition != null)
                GUI.Label(new Rect(cc.x - 200 * s, cc.y + 40 * s, 400 * s, 30 * s), $"E 拾取 {weapons.LookedAtPickup.Definition.displayName}", center);

            // 死亡
            if (health != null && health.IsDead.Value)
            {
                Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.35f));
                float remaining = life != null ? life.RespawnRemaining : 0f;
                GUI.Label(new Rect(0, h * 0.35f, w, 60 * s), $"你被打倒了\n<size=18>{remaining:0.0} 秒后复活</size>", big);
            }
        }

        static void Fill(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, white);
            GUI.color = old;
        }
    }
}
