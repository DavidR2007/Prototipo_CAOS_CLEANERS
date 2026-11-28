using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

/// <summary>
/// Genera y muestra en un RawImage el código QR con la URL WebSocket y el código de sala
/// para que los móviles se conecten escaneándolo desde la app Controller_CC.
/// </summary>
public class QRCodeDisplay : MonoBehaviour
{
    [Header("Componentes UI")]
    [Tooltip("RawImage del Canvas donde se dibujará el código QR")]
    [SerializeField] private RawImage qrRawImage;

    [Tooltip("Texto opcional para mostrar el código de sala (ej. SALA: K7P4Q2)")]
    [SerializeField] private TMP_Text roomCodeText;

    [Tooltip("Texto opcional para mostrar la dirección IP y puerto (ej. ws://192.168.1.43:3000)")]
    [SerializeField] private TMP_Text serverUrlText;

    [Tooltip("Texto opcional para mostrar cuántos mandos móviles hay conectados")]
    [SerializeField] private TMP_Text playersCountText;

    [Header("Configuración del QR")]
    [SerializeField] private int qrSize = 256;

    private Texture2D qrTexture;

    private void Start()
    {
        RefreshDisplay();

        if (NetworkServer.Instance != null)
        {
            NetworkServer.Instance.OnPlayersChanged += UpdatePlayersCount;
        }
    }

    private void OnDestroy()
    {
        if (NetworkServer.Instance != null)
        {
            NetworkServer.Instance.OnPlayersChanged -= UpdatePlayersCount;
        }

        if (qrTexture != null)
        {
            Destroy(qrTexture);
        }
    }

    public void RefreshDisplay()
    {
        var server = NetworkServer.Instance;
        if (server == null)
        {
            Debug.LogWarning("[QRCodeDisplay] No se encontró NetworkServer.Instance en la escena.");
            return;
        }

        if (qrRawImage != null)
        {
            qrTexture = GenerateQRTexture(server.QRPayload, qrSize, qrSize);
            qrRawImage.texture = qrTexture;
        }

        if (roomCodeText != null)
        {
            roomCodeText.text = $"SALA: {server.RoomCode}";
        }

        if (serverUrlText != null)
        {
            serverUrlText.text = server.ConnectionURL;
        }

        UpdatePlayersCount();
    }

    private void UpdatePlayersCount()
    {
        if (playersCountText == null || NetworkServer.Instance == null) return;
        playersCountText.text = $"Mandos conectados: {NetworkServer.Instance.ConnectedPlayersCount} / 4";
    }

    private static Texture2D GenerateQRTexture(string content, int width, int height)
    {
        var writer = new QRCodeWriter();
        BitMatrix matrix = writer.encode(content, BarcodeFormat.QR_CODE, width, height);

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };

        Color32[] pixels = new Color32[width * height];
        Color32 black = new Color32(0, 0, 0, 255);
        Color32 white = new Color32(255, 255, 255, 255);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Invertir eje Y para que coincida con coordenadas de Texture2D en Unity
                bool isDark = matrix[x, height - 1 - y];
                pixels[y * width + x] = isDark ? black : white;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}
