using System;
using System.Collections;
using System.Linq;
using ScpParty.Combat;
using ScpParty.Player;
using ScpParty.Weapons;
using Unity.Netcode;
using UnityEngine;

namespace ScpParty.DevTools
{
    /// <summary>
    /// 自动化战斗自测（仅在主机带 -combatTest 参数启动时运行，不影响正常游戏）：
    /// 依次用每把武器打木桩、测试捡武器、用手枪把一名客户端玩家打倒并等待复活，结果写入日志 [CombatTest]。
    /// 用法：SCP.exe -batchmode -nographics -host -combatTest -quitAfter 60
    /// </summary>
    public class CombatSelfTest : MonoBehaviour
    {
        int failures;

        void Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-combatTest") >= 0)
                StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            var network = NetworkManager.Singleton;
            yield return new WaitUntil(() => network != null && network.IsServer && network.LocalClient?.PlayerObject != null);
            Log("server started, waiting for a client (up to 10s)");
            float waitUntil = Time.time + 10f;
            yield return new WaitUntil(() => network.ConnectedClientsIds.Count >= 2 || Time.time > waitUntil);
            yield return new WaitForSeconds(1.5f);

            var me = network.LocalClient.PlayerObject;
            var controller = me.GetComponent<PlayerController>();
            var weapons = me.GetComponent<PlayerWeapons>();
            var dummy = FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None).OrderBy(d => d.transform.position.z).FirstOrDefault();
            Check(dummy != null, "training dummy spawned");
            Check(WeaponPickup.Active.Count >= WeaponDatabase.Count, $"weapon pickups spawned ({WeaponPickup.Active.Count})");
            if (dummy == null)
                yield break;
            var dummyHealth = dummy.GetComponent<Health>();

            // 每把武器打木桩
            for (int i = 0; i < WeaponDatabase.Count; i++)
            {
                var def = WeaponDatabase.Get(i);
                float distance = def.kind == WeaponKind.Melee ? 1.4f : 6f;
                Vector3 toward = (dummy.transform.position - new Vector3(0f, 0f, 0f)).normalized;
                Vector3 standAt = dummy.transform.position - new Vector3(toward.x, 0f, toward.z).normalized * distance;
                controller.TeleportTo(standAt, Quaternion.LookRotation(dummy.transform.position - standAt));
                yield return new WaitForSeconds(0.5f);

                weapons.ServerGive(i, def.magazineSize);
                int before = dummyHealth.Current.Value;
                for (int shot = 0; shot < 3; shot++)
                {
                    Vector3 origin = controller.Head.position;
                    weapons.ServerAttack(origin, (dummy.transform.position + Vector3.up * 1.1f - origin).normalized);
                    yield return new WaitForSeconds(def.fireInterval + 0.05f);
                }
                yield return new WaitForSeconds(def.kind == WeaponKind.Projectile ? 1.2f : 0.2f);
                int dealt = before - dummyHealth.Current.Value;
                Check(dealt > 0, $"{def.name} ({def.kind}) damaged dummy by {dealt}, ammo left {weapons.Ammo.Value}");
                weapons.ServerDrop(standAt);
                yield return new WaitForSeconds(3.5f); // 等木桩回满
            }

            // 捡武器
            var pickup = WeaponPickup.Active.FirstOrDefault(p => p.WeaponIndex.Value == 2);
            if (pickup != null)
            {
                controller.TeleportTo(pickup.transform.position + Vector3.back * 1f, Quaternion.identity);
                yield return new WaitForSeconds(0.5f);
                Check(weapons.ServerTryPickup(pickup) && weapons.HeldIndex.Value == 2, "picked up weapon from ground");
            }

            // PvP：用手枪打倒一名客户端
            var victim = network.ConnectedClientsList.Select(c => c.PlayerObject).FirstOrDefault(p => p != null && p != me);
            if (victim == null)
            {
                Log("no client connected, skip PvP");
            }
            else
            {
                var victimHealth = victim.GetComponent<Health>();
                controller.TeleportTo(victim.transform.position + victim.transform.forward * 4f, Quaternion.LookRotation(-victim.transform.forward));
                yield return new WaitForSeconds(0.6f);
                int pistol = Enumerable.Range(0, WeaponDatabase.Count).First(i => WeaponDatabase.Get(i).kind == WeaponKind.Hitscan);
                int shots = 0;
                while (!victimHealth.IsDead.Value && shots < 30)
                {
                    weapons.ServerGive(pistol, 99);
                    Vector3 origin = controller.Head.position;
                    weapons.ServerAttack(origin, (victim.transform.position + Vector3.up * 1.0f - origin).normalized);
                    shots++;
                    yield return new WaitForSeconds(WeaponDatabase.Get(pistol).fireInterval + 0.05f);
                }
                Check(victimHealth.IsDead.Value, $"client player died after {shots} shots");
                yield return new WaitForSeconds(4f);
                Check(!victimHealth.IsDead.Value && victimHealth.Current.Value == victimHealth.Max, "client player revived with full health");
            }

            Log(failures == 0 ? "ALL PASSED" : $"FAILED ({failures})");
        }

        void Check(bool ok, string what)
        {
            if (!ok)
                failures++;
            Log($"{(ok ? "PASS" : "FAIL")} {what}");
        }

        static void Log(string message) => Debug.Log($"[CombatTest] {message}");
    }
}
