using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Servidor WebSocket embebido en Unity para conectar hasta 4 móviles como mandos (Controller_CC).
/// Implementa RFC 6455 sobre TcpListener sin dependencias externas.
/// Persiste entre escenas (DontDestroyOnLoad) para mantener la conexión desde el menú hasta la partida.
/// </summary>
public class NetworkServer : MonoBehaviour
{
    public static NetworkServer Instance { get; private set; }

    [Header("Configuración de Red")]
    [Tooltip("Puerto TCP donde escucha el servidor WebSocket (debe coincidir con la app Android)")]
    [SerializeField] private int port = 3000;

    [Tooltip("Longitud del código de sala generado al iniciar")]
    [SerializeField] private int roomCodeLength = 6;

    public string LocalIP { get; private set; } = "127.0.0.1";
    public int Port => port;
    public string RoomCode { get; private set; } = "";
    public string ConnectionURL => $"ws://{LocalIP}:{port}";
    public string QRPayload => $"{ConnectionURL}|{RoomCode}";
    public bool IsRunning { get; private set; }

    public int ConnectedPlayersCount
    {
        get
        {
            lock (clientsLock)
            {
                int count = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (playerSlots[i] != null) count++;
                }
                return count;
            }
        }
    }

    public event Action OnPlayersChanged;

    private TcpListener listener;
    private Thread listenerThread;
    private volatile bool stopRequested;

    private readonly object clientsLock = new object();
    private readonly ClientConnection[] playerSlots = new ClientConnection[4];
    private readonly List<ClientConnection> allClients = new List<ClientConnection>();

    private readonly Vector2[] remoteMoveInputs = new Vector2[4];
    private readonly bool[] remoteActionPending = new bool[4];
    private bool remotePauseRequested;

    private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
    private float nextMenuNavTime;

    private class ClientConnection
    {
        public TcpClient Tcp;
        public NetworkStream Stream;
        public int PlayerIndex = -1; // 0..3 cuando ya hizo join
        public readonly object SendLock = new object();
        public volatile bool Closed;
    }

    [Serializable]
    private class IncomingMessage
    {
        public string type = "";
        public string room = "";
        public int player = 0;
        public int preferredPlayer = 0;
        public float x = 0f;
        public float y = 0f;
        public bool pressed = false;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LocalIP = DetectLocalIPv4();
        RoomCode = GenerateRoomCode(roomCodeLength);
        StartServer();
    }

    private void Update()
    {
        while (mainThreadQueue.TryDequeue(out Action action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkServer] Error en cola principal: {ex.Message}");
            }
        }

        HandleMenuNavigation();
    }

    private bool IsInMenuContext()
    {
        if (EventSystem.current == null) return false;
        if (Time.timeScale == 0f) return true;
        if (GameTimer.Instance == null) return true; // En escenas de menú (MAIN_MENU)
        return GameTimer.Instance.PartidaTerminada;
    }

    private void HandleMenuNavigation()
    {
        if (!IsInMenuContext()) return;

        // Cualquier mando conectado puede navegar por los menús
        Vector2 nav = Vector2.zero;
        bool submitPressed = false;

        for (int i = 0; i < 4; i++)
        {
            if (remoteMoveInputs[i].sqrMagnitude > nav.sqrMagnitude)
            {
                nav = remoteMoveInputs[i];
            }
            if (ConsumeAction(i))
            {
                submitPressed = true;
            }
        }

        var eventSystem = EventSystem.current;
        GameObject current = eventSystem.currentSelectedGameObject;

        // Si no hay ningún elemento seleccionado o se desactivó, seleccionar el primer botón activo
        if (current == null || !current.activeInHierarchy)
        {
            if (nav.sqrMagnitude > 0.3f || submitPressed)
            {
                Button firstBtn = FindFirstActiveButton();
                if (firstBtn != null)
                {
                    eventSystem.SetSelectedGameObject(firstBtn.gameObject);
                    current = firstBtn.gameObject;
                }
            }
            if (current == null) return;
        }

        if (submitPressed)
        {
            ExecuteEvents.Execute(current, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
            return;
        }

        if (Time.unscaledTime < nextMenuNavTime || nav.sqrMagnitude < 0.3f) return;

        Selectable selectable = current.GetComponent<Selectable>();
        if (selectable == null) return;

        // Si es un Slider y se mueve izquierda/derecha, ajustar su valor
        if (selectable is Slider slider && Mathf.Abs(nav.x) > Mathf.Abs(nav.y))
        {
            float step = (slider.maxValue - slider.minValue) * (slider.wholeNumbers ? 1f : 0.08f);
            if (slider.wholeNumbers && step < 1f) step = 1f;
            slider.value += nav.x > 0 ? step : -step;
            nextMenuNavTime = Time.unscaledTime + 0.14f;
            return;
        }

        Selectable next = null;
        if (Mathf.Abs(nav.y) >= Mathf.Abs(nav.x))
        {
            next = nav.y > 0 ? selectable.FindSelectableOnUp() : selectable.FindSelectableOnDown();
        }
        else
        {
            next = nav.x > 0 ? selectable.FindSelectableOnRight() : selectable.FindSelectableOnLeft();
        }

        if (next != null && next.IsInteractable())
        {
            eventSystem.SetSelectedGameObject(next.gameObject);
            nextMenuNavTime = Time.unscaledTime + 0.22f;
        }
    }

    private static Button FindFirstActiveButton()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            if (btn != null && btn.gameObject.activeInHierarchy && btn.IsInteractable())
            {
                return btn;
            }
        }
        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            StopServer();
            Instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        StopServer();
    }

    public Vector2 GetMoveInput(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= 4) return Vector2.zero;
        return remoteMoveInputs[playerIndex];
    }

    public bool HasRemotePlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= 4) return false;
        lock (clientsLock)
        {
            return playerSlots[playerIndex] != null;
        }
    }

    public bool ConsumeAction(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= 4) return false;
        if (!remoteActionPending[playerIndex]) return false;
        remoteActionPending[playerIndex] = false;
        return true;
    }

    public bool ConsumePauseRequest()
    {
        if (!remotePauseRequested) return false;
        remotePauseRequested = false;
        return true;
    }

    public void BroadcastPaused(bool paused)
    {
        string type = paused ? "paused" : "resumed";
        BroadcastJson($"{{\"type\":\"{type}\"}}");
    }

    public void BroadcastStart()
    {
        BroadcastJson("{\"type\":\"start\"}");
    }

    private void StartServer()
    {
        try
        {
            stopRequested = false;
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            IsRunning = true;

            listenerThread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "CC_WebSocketListener"
            };
            listenerThread.Start();

            Debug.Log($"[NetworkServer] Servidor WebSocket activo en {ConnectionURL} | Sala: {RoomCode}");
        }
        catch (Exception ex)
        {
            IsRunning = false;
            Debug.LogError($"[NetworkServer] No se pudo iniciar en el puerto {port}: {ex.Message}");
        }
    }

    private void StopServer()
    {
        stopRequested = true;
        IsRunning = false;

        try { listener?.Stop(); } catch { }

        lock (clientsLock)
        {
            foreach (var client in allClients)
            {
                CloseClientInternal(client);
            }
            allClients.Clear();
            for (int i = 0; i < 4; i++)
            {
                playerSlots[i] = null;
                remoteMoveInputs[i] = Vector2.zero;
                remoteActionPending[i] = false;
            }
        }
    }

    private void AcceptLoop()
    {
        while (!stopRequested)
        {
            try
            {
                TcpClient tcp = listener.AcceptTcpClient();
                tcp.NoDelay = true;

                Thread clientThread = new Thread(() => HandleClient(tcp))
                {
                    IsBackground = true,
                    Name = "CC_WebSocketClient"
                };
                clientThread.Start();
            }
            catch (SocketException)
            {
                if (stopRequested) break;
            }
            catch (Exception ex)
            {
                if (!stopRequested)
                {
                    Debug.LogWarning($"[NetworkServer] Error aceptando cliente: {ex.Message}");
                }
            }
        }
    }

    private void HandleClient(TcpClient tcp)
    {
        ClientConnection conn = null;
        try
        {
            NetworkStream stream = tcp.GetStream();
            if (!PerformWebSocketHandshake(stream))
            {
                tcp.Close();
                return;
            }

            conn = new ClientConnection
            {
                Tcp = tcp,
                Stream = stream
            };

            lock (clientsLock)
            {
                allClients.Add(conn);
            }

            while (!stopRequested && !conn.Closed && tcp.Connected)
            {
                string message = ReadWebSocketTextFrame(stream, out bool isCloseFrame);
                if (isCloseFrame)
                {
                    break;
                }
                if (message != null)
                {
                    ProcessClientMessage(conn, message);
                }
            }
        }
        catch
        {
            // Desconexión normal o pérdida de red
        }
        finally
        {
            if (conn != null)
            {
                RemoveClient(conn);
            }
            else
            {
                try { tcp.Close(); } catch { }
            }
        }
    }

    private void ProcessClientMessage(ClientConnection conn, string json)
    {
        IncomingMessage msg;
        try
        {
            msg = JsonUtility.FromJson<IncomingMessage>(json);
        }
        catch
        {
            return;
        }

        if (msg == null || string.IsNullOrEmpty(msg.type)) return;

        switch (msg.type)
        {
            case "join":
                HandleJoin(conn, msg.room, msg.preferredPlayer);
                break;

            case "select_player":
                if (conn.PlayerIndex >= 0)
                {
                    HandleSelectPlayer(conn, msg.player);
                }
                break;

            case "move":
                if (conn.PlayerIndex >= 0 && conn.PlayerIndex < 4)
                {
                    Vector2 vec = new Vector2(msg.x, msg.y);
                    if (vec.sqrMagnitude > 1f) vec.Normalize();
                    int idx = conn.PlayerIndex;
                    mainThreadQueue.Enqueue(() => remoteMoveInputs[idx] = vec);
                }
                break;

            case "action":
                if (conn.PlayerIndex >= 0 && conn.PlayerIndex < 4 && msg.pressed)
                {
                    int idx = conn.PlayerIndex;
                    mainThreadQueue.Enqueue(() => remoteActionPending[idx] = true);
                }
                break;

            case "pause":
                if (conn.PlayerIndex >= 0 && conn.PlayerIndex < 4)
                {
                    mainThreadQueue.Enqueue(() => remotePauseRequested = true);
                }
                break;
        }
    }

    private void HandleJoin(ClientConnection conn, string room, int preferredPlayer)
    {
        string normalized = (room ?? "").Trim().ToUpperInvariant();
        if (normalized != RoomCode)
        {
            SendText(conn, "{\"type\":\"error\",\"message\":\"Código de sala incorrecto\"}");
            CloseClientInternal(conn);
            return;
        }

        int assignedSlot = -1;
        bool preferredBusy = false;

        lock (clientsLock)
        {
            if (conn.PlayerIndex >= 0)
            {
                assignedSlot = conn.PlayerIndex;
            }
            else if (preferredPlayer >= 1 && preferredPlayer <= 4)
            {
                int targetSlot = preferredPlayer - 1;
                if (playerSlots[targetSlot] == null)
                {
                    playerSlots[targetSlot] = conn;
                    conn.PlayerIndex = targetSlot;
                    assignedSlot = targetSlot;
                }
                else
                {
                    preferredBusy = true;
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (playerSlots[i] == null)
                    {
                        playerSlots[i] = conn;
                        conn.PlayerIndex = i;
                        assignedSlot = i;
                        break;
                    }
                }
            }
        }

        if (preferredBusy)
        {
            SendText(conn, $"{{\"type\":\"error\",\"message\":\"El Jugador {preferredPlayer} ya está ocupado\"}}");
            CloseClientInternal(conn);
            return;
        }

        if (assignedSlot < 0)
        {
            SendText(conn, "{\"type\":\"error\",\"message\":\"Sala llena (máximo 4 jugadores)\"}");
            CloseClientInternal(conn);
            return;
        }

        int playerNumber = assignedSlot + 1;
        SendText(conn, $"{{\"type\":\"joined\",\"player\":{playerNumber}}}");

        mainThreadQueue.Enqueue(() =>
        {
            Debug.Log($"[NetworkServer] Móvil conectado como Jugador {playerNumber}");
            OnPlayersChanged?.Invoke();
        });
    }

    private void HandleSelectPlayer(ClientConnection conn, int requestedPlayer)
    {
        if (requestedPlayer < 1 || requestedPlayer > 4) return;

        int targetSlot = requestedPlayer - 1;
        int previousSlot = -1;
        bool switched = false;

        lock (clientsLock)
        {
            if (conn.PlayerIndex == targetSlot)
            {
                switched = true;
            }
            else if (playerSlots[targetSlot] == null)
            {
                previousSlot = conn.PlayerIndex;
                if (previousSlot >= 0 && previousSlot < 4 && playerSlots[previousSlot] == conn)
                {
                    playerSlots[previousSlot] = null;
                }
                playerSlots[targetSlot] = conn;
                conn.PlayerIndex = targetSlot;
                switched = true;
            }
        }

        if (!switched)
        {
            SendText(conn, $"{{\"type\":\"player_busy\",\"message\":\"El Jugador {requestedPlayer} ya está ocupado\"}}");
            return;
        }

        SendText(conn, $"{{\"type\":\"joined\",\"player\":{requestedPlayer}}}");

        mainThreadQueue.Enqueue(() =>
        {
            if (previousSlot >= 0 && previousSlot < 4)
            {
                remoteMoveInputs[previousSlot] = Vector2.zero;
                remoteActionPending[previousSlot] = false;
            }
            Debug.Log($"[NetworkServer] Móvil cambió a Jugador {requestedPlayer}");
            OnPlayersChanged?.Invoke();
        });
    }

    private void RemoveClient(ClientConnection conn)
    {
        int freedIndex = -1;
        lock (clientsLock)
        {
            allClients.Remove(conn);
            if (conn.PlayerIndex >= 0 && conn.PlayerIndex < 4 && playerSlots[conn.PlayerIndex] == conn)
            {
                freedIndex = conn.PlayerIndex;
                playerSlots[freedIndex] = null;
            }
            CloseClientInternal(conn);
        }

        if (freedIndex >= 0)
        {
            int idx = freedIndex;
            mainThreadQueue.Enqueue(() =>
            {
                remoteMoveInputs[idx] = Vector2.zero;
                remoteActionPending[idx] = false;
                Debug.Log($"[NetworkServer] Jugador {idx + 1} desconectado");
                OnPlayersChanged?.Invoke();
            });
        }
    }

    private void CloseClientInternal(ClientConnection conn)
    {
        if (conn.Closed) return;
        conn.Closed = true;
        try { conn.Stream?.Close(); } catch { }
        try { conn.Tcp?.Close(); } catch { }
    }

    private void BroadcastJson(string json)
    {
        List<ClientConnection> snapshot;
        lock (clientsLock)
        {
            snapshot = new List<ClientConnection>(allClients);
        }

        foreach (var client in snapshot)
        {
            if (client.PlayerIndex >= 0 && !client.Closed)
            {
                SendText(client, json);
            }
        }
    }

    private bool PerformWebSocketHandshake(NetworkStream stream)
    {
        byte[] buffer = new byte[4096];
        int totalRead = 0;

        while (totalRead < buffer.Length)
        {
            int read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
            if (read <= 0) return false;
            totalRead += read;

            string headers = Encoding.UTF8.GetString(buffer, 0, totalRead);
            if (headers.Contains("\r\n\r\n"))
            {
                Match match = Regex.Match(headers, "Sec-WebSocket-Key:\\s*(.*)", RegexOptions.IgnoreCase);
                if (!match.Success) return false;

                string key = match.Groups[1].Value.Trim();
                string acceptSeed = key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(acceptSeed));
                string acceptKey = Convert.ToBase64String(hash);

                string response =
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";

                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                stream.Write(responseBytes, 0, responseBytes.Length);
                return true;
            }
        }

        return false;
    }

    private string ReadWebSocketTextFrame(NetworkStream stream, out bool isCloseFrame)
    {
        isCloseFrame = false;

        int b0 = stream.ReadByte();
        int b1 = stream.ReadByte();
        if (b0 < 0 || b1 < 0)
        {
            isCloseFrame = true;
            return null;
        }

        int opcode = b0 & 0x0F;
        if (opcode == 0x8) // Close
        {
            isCloseFrame = true;
            return null;
        }

        bool masked = (b1 & 0x80) != 0;
        long payloadLen = b1 & 0x7F;

        if (payloadLen == 126)
        {
            byte[] lenBytes = ReadExact(stream, 2);
            if (lenBytes == null) { isCloseFrame = true; return null; }
            payloadLen = (lenBytes[0] << 8) | lenBytes[1];
        }
        else if (payloadLen == 127)
        {
            byte[] lenBytes = ReadExact(stream, 8);
            if (lenBytes == null) { isCloseFrame = true; return null; }
            Array.Reverse(lenBytes);
            payloadLen = BitConverter.ToInt64(lenBytes, 0);
        }

        if (payloadLen < 0 || payloadLen > 1024 * 64)
        {
            isCloseFrame = true;
            return null;
        }

        byte[] maskKey = null;
        if (masked)
        {
            maskKey = ReadExact(stream, 4);
            if (maskKey == null) { isCloseFrame = true; return null; }
        }

        byte[] payload = ReadExact(stream, (int)payloadLen);
        if (payload == null) { isCloseFrame = true; return null; }

        if (masked)
        {
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = (byte)(payload[i] ^ maskKey[i % 4]);
            }
        }

        if (opcode == 0x9) // Ping -> responder Pong
        {
            return null;
        }

        if (opcode == 0x1 || opcode == 0x0) // Text
        {
            return Encoding.UTF8.GetString(payload);
        }

        return null;
    }

    private byte[] ReadExact(NetworkStream stream, int count)
    {
        byte[] data = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(data, offset, count - offset);
            if (read <= 0) return null;
            offset += read;
        }
        return data;
    }

    private void SendText(ClientConnection conn, string text)
    {
        if (conn == null || conn.Closed) return;

        byte[] payload = Encoding.UTF8.GetBytes(text);
        byte[] frame;

        if (payload.Length <= 125)
        {
            frame = new byte[2 + payload.Length];
            frame[0] = 0x81; // FIN + Text
            frame[1] = (byte)payload.Length;
            Buffer.BlockCopy(payload, 0, frame, 2, payload.Length);
        }
        else
        {
            frame = new byte[4 + payload.Length];
            frame[0] = 0x81;
            frame[1] = 126;
            frame[2] = (byte)((payload.Length >> 8) & 0xFF);
            frame[3] = (byte)(payload.Length & 0xFF);
            Buffer.BlockCopy(payload, 0, frame, 4, payload.Length);
        }

        lock (conn.SendLock)
        {
            if (conn.Closed) return;
            try
            {
                conn.Stream.Write(frame, 0, frame.Length);
            }
            catch
            {
                CloseClientInternal(conn);
            }
        }
    }

    private static string DetectLocalIPv4()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                {
                    return endPoint.Address.ToString();
                }
            }
        }
        catch { }

        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
        }
        catch { }

        return "127.0.0.1";
    }

    private static string GenerateRoomCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new System.Random();
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            sb.Append(chars[rng.Next(chars.Length)]);
        }
        return sb.ToString();
    }
}
