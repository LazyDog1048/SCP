using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScpParty.Network
{
    /// <summary>
    /// 局域网联机入口：创建主机 / 按 IP 加入、连接审批与出生点分配、简易 IMGUI 菜单。
    /// 命令行：-host、-client [ip]、-port [端口]、-quitAfter [秒]（自动测试用）。
    /// </summary>
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public class LanSession : MonoBehaviour
    {
        public const ushort DefaultPort = 7777;

        [SerializeField] int maxPlayers = 8;

        NetworkManager network;
        UnityTransport transport;

        string addressField = "127.0.0.1";
        string portField = DefaultPort.ToString();
        string status = "";
        ulong nextSpawnIndex;
        float quitAt = -1f;
        List<string> localAddresses;

        void Awake()
        {
            network = GetComponent<NetworkManager>();
            transport = GetComponent<UnityTransport>();
            localAddresses = GetLocalIPv4Addresses();
        }

        void OnEnable()
        {
            network.ConnectionApprovalCallback += ApproveConnection;
            network.OnClientConnectedCallback += OnClientConnected;
            network.OnClientDisconnectCallback += OnClientDisconnected;
            network.OnTransportFailure += OnTransportFailure;
        }

        void OnDisable()
        {
            if (network == null)
                return;
            network.ConnectionApprovalCallback -= ApproveConnection;
            network.OnClientConnectedCallback -= OnClientConnected;
            network.OnClientDisconnectCallback -= OnClientDisconnected;
            network.OnTransportFailure -= OnTransportFailure;
        }

        void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string next = i + 1 < args.Length ? args[i + 1] : null;
                switch (args[i])
                {
                    case "-port" when next != null: portField = next; break;
                    case "-client" when next != null && !next.StartsWith("-"): addressField = next; break;
                    case "-quitAfter" when next != null && float.TryParse(next, out float seconds): quitAt = seconds; break;
                }
            }
            if (Array.IndexOf(args, "-host") >= 0)
                StartHost();
            else if (Array.IndexOf(args, "-client") >= 0)
                StartClient();
        }

        void Update()
        {
            if (quitAt > 0f && Time.realtimeSinceStartup >= quitAt)
            {
                Debug.Log($"[LanSession] quitAfter reached. connectedClients={(network.IsServer ? network.ConnectedClientsIds.Count : -1)} isConnectedClient={network.IsConnectedClient}");
                foreach (var p in FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None))
                    Debug.Log($"[LanSession] player owner={p.OwnerClientId} pos={p.transform.position}");
                Application.Quit();
                quitAt = -1f;
            }

            if (!network.IsListening)
                return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                InputGate.SetCursorLocked(false);
        }

        void LateUpdate()
        {
            // 放在 LateUpdate：避免这一帧的点击同时触发推人
            if (!network.IsListening || InputGate.GameplayEnabled || GUIUtility.hotControl != 0)
                return;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !IsPointerOverPanel())
                InputGate.SetCursorLocked(true);
        }

        public void StartHost()
        {
            if (!TryParsePort(out ushort port))
                return;
            nextSpawnIndex = 0;
            network.NetworkConfig.ConnectionApproval = true;
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            status = network.StartHost() ? $"主机已启动，端口 {port}" : "主机启动失败（端口可能被占用）";
            Debug.Log($"[LanSession] {status}");
        }

        public void StartClient()
        {
            if (!TryParsePort(out ushort port))
                return;
            string address = addressField.Trim();
            if (!IPAddress.TryParse(address, out IPAddress ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            {
                status = "请输入有效的 IPv4 地址，例如 192.168.1.23";
                return;
            }
            network.NetworkConfig.ConnectionApproval = true;
            transport.SetConnectionData(address, port);
            status = network.StartClient() ? $"正在连接 {address}:{port} ..." : "客户端启动失败";
            Debug.Log($"[LanSession] {status}");
        }

        public void Disconnect()
        {
            network.Shutdown();
            InputGate.SetCursorLocked(false);
            status = "已断开";
        }

        void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            if (network.ConnectedClientsIds.Count >= maxPlayers)
            {
                response.Approved = false;
                response.Reason = "房间已满";
                return;
            }

            Transform spawn = SpawnPoints.Get(nextSpawnIndex++);
            response.Approved = true;
            response.CreatePlayerObject = true;
            if (spawn != null)
            {
                response.Position = spawn.position;
                response.Rotation = spawn.rotation;
            }
        }

        void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[LanSession] client connected: {clientId} (local={network.LocalClientId}, server={network.IsServer})");
            if (clientId == network.LocalClientId && !network.IsServer)
                status = "已连接";
        }

        void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[LanSession] client disconnected: {clientId}");
            if (network.IsServer && clientId != network.LocalClientId)
                return;

            string reason = network.DisconnectReason;
            status = string.IsNullOrEmpty(reason) ? "连接已断开或连接失败" : $"连接被拒绝：{reason}";
            InputGate.SetCursorLocked(false);
        }

        void OnTransportFailure()
        {
            status = "网络传输失败";
            Debug.LogWarning("[LanSession] transport failure");
            InputGate.SetCursorLocked(false);
        }

        bool TryParsePort(out ushort port)
        {
            if (ushort.TryParse(portField.Trim(), out port) && port > 0)
                return true;
            status = "端口无效（1-65535）";
            return false;
        }

        // ---------- IMGUI 菜单（临时界面，后续可替换为正式 UI） ----------

        Rect panelRect;

        bool IsPointerOverPanel()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return false;
            Vector2 p = mouse.position.ReadValue();
            return panelRect.Contains(new Vector2(p.x, Screen.height - p.y));
        }

        void OnGUI()
        {
            float scale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (!network.IsListening)
                DrawLobby();
            else
                DrawInGame();

            panelRect = new Rect(panelRect.x * scale, panelRect.y * scale, panelRect.width * scale, panelRect.height * scale);
        }

        void DrawLobby()
        {
            panelRect = new Rect(20, 20, 340, 270);
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("<b>SCP 派对 · 局域网联机</b>", RichLabel());
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label("主机 IP", GUILayout.Width(60));
            addressField = GUILayout.TextField(addressField, 40);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("端口", GUILayout.Width(60));
            portField = GUILayout.TextField(portField, 5);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("创建主机", GUILayout.Height(32)))
                StartHost();
            if (GUILayout.Button("加入", GUILayout.Height(32)))
                StartClient();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("本机局域网 IP：" + (localAddresses.Count > 0 ? string.Join("  ", localAddresses) : "未找到"));
            if (!string.IsNullOrEmpty(status))
                GUILayout.Label(status);
            GUILayout.EndArea();
        }

        void DrawInGame()
        {
            panelRect = new Rect(20, 20, 340, 120);
            GUILayout.BeginArea(panelRect, GUI.skin.box);

            string role = network.IsHost ? "主机" : "客户端";
            if (network.IsServer)
                GUILayout.Label($"{role} · 玩家 {network.ConnectedClientsIds.Count}/{maxPlayers} · 端口 {portField}");
            else
                GUILayout.Label($"{role} · {status}");
            if (network.IsHost && localAddresses.Count > 0)
                GUILayout.Label("让朋友加入：" + string.Join("  ", localAddresses));

            GUILayout.Label(InputGate.GameplayEnabled
                ? "Esc 释放鼠标 · 左键推人 · 冲刺(Shift)中左键扑倒"
                : "点击画面继续游戏");

            if (GUILayout.Button("断开"))
                Disconnect();
            GUILayout.EndArea();
        }

        static GUIStyle richLabel;

        static GUIStyle RichLabel()
        {
            richLabel ??= new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 };
            return richLabel;
        }

        static List<string> GetLocalIPv4Addresses()
        {
            var result = new List<string>();
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;
                    foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                            result.Add(unicast.Address.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LanSession] 获取本机 IP 失败：{e.Message}");
            }
            return result;
        }
    }
}
