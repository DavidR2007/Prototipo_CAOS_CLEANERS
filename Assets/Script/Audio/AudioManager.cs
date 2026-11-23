using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor Central de Audio (Singleton) para Chaos Cleaners.
/// - Menú Principal: Reproduce 2 o más canciones EN ORDEN secuencial y en bucle infinito.
/// - In-Game: Reproduce las canciones de combate de forma ALEATORIA en bucle continuo.
/// - Transición suave (Crossfade) automática al cambiar entre canciones y entre escenas.
/// - Enrutamiento automático a AudioMixer (MusicVolume y SfxVolume).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("AudioMixer Principal")]
    [Tooltip("Arrastra aquí tu MAIN_MIXER")]
    [SerializeField] private AudioMixer mainMixer;

    [Tooltip("Grupo del mixer para música de fondo")]
    [SerializeField] private AudioMixerGroup musicGroup;

    [Tooltip("Grupo del mixer para efectos de sonido (SFX)")]
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Música del Menú Principal (Secuencial en Bucle)")]
    [Tooltip("Canciones que sonarán en el Menú Principal en orden (Pista 1 -> Pista 2 -> Pista 1...)")]
    public AudioClip[] mainMenuTracks;

    [Tooltip("Nombre de la escena del Menú (detecta si contiene 'Menu' o coincide con este nombre)")]
    [SerializeField] private string menuSceneName = "01_MainMenu";

    [Header("Música In-Game (Playlist Aleatoria en Bucle)")]
    [Tooltip("Soundtracks que rotarán aleatoriamente durante la partida en la arena")]
    public AudioClip[] inGameSoundtracks;

    [Header("Ajustes de Música")]
    [Tooltip("Volumen general de la música")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    [Tooltip("Segundos de transición suave (Fade Out / Fade In) entre canciones")]
    [SerializeField] private float crossfadeDuration = 1.2f;

    [Header("Sonidos de UI por Defecto")]
    [Tooltip("Sonido al pasar el ratón o seleccionar un botón (Hover)")]
    public AudioClip defaultButtonHover;
    [Range(0f, 1f)] public float defaultHoverVolume = 0.5f;

    [Tooltip("Sonido al pulsar un botón (Click)")]
    public AudioClip defaultButtonClick;
    [Range(0f, 1f)] public float defaultClickVolume = 0.6f;

    // Fuentes internas (se crean solas automáticamente)
    [SerializeField, HideInInspector] private AudioSource musicSource;
    [SerializeField, HideInInspector] private AudioSource sfxSource;

    private int currentMenuTrackIndex = 0;
    private int lastInGameTrackIndex = -1;
    private Coroutine currentMusicRoutine;
    private bool isInGameMode = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        EvaluateSceneMusic(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EvaluateSceneMusic(scene.name);
    }

    private void EvaluateSceneMusic(string sceneName)
    {
        bool isMenu = IsMenuScene(sceneName);

        if (isMenu)
        {
            if (isInGameMode || !musicSource.isPlaying)
            {
                isInGameMode = false;
                StartMenuPlaylist();
            }
        }
        else
        {
            if (!isInGameMode || !musicSource.isPlaying)
            {
                isInGameMode = true;
                StartInGamePlaylist();
            }
        }
    }

    private bool IsMenuScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(menuSceneName) && sceneName.Equals(menuSceneName, System.StringComparison.OrdinalIgnoreCase))
            return true;

        return sceneName.ToLower().Contains("menu");
    }

    private void InitializeAudioSources()
    {
        if (musicSource == null)
        {
            GameObject mObj = new GameObject("Music_Source");
            mObj.transform.SetParent(transform);
            musicSource = mObj.AddComponent<AudioSource>();
        }

        musicSource.loop = false;
        musicSource.playOnAwake = false;
        if (musicGroup != null)
        {
            musicSource.outputAudioMixerGroup = musicGroup;
        }

        if (sfxSource == null)
        {
            GameObject sObj = new GameObject("SFX_Source");
            sObj.transform.SetParent(transform);
            sfxSource = sObj.AddComponent<AudioSource>();
        }

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        if (sfxGroup != null)
        {
            sfxSource.outputAudioMixerGroup = sfxGroup;
        }
    }

    #region Control de Música (Menú Secuencial vs In-Game Aleatorio)

    /// <summary>
    /// Inicia la reproducción en orden secuencial (Pista 1 -> Pista 2 -> ...) de las canciones del Menú.
    /// </summary>
    public void StartMenuPlaylist()
    {
        if (mainMenuTracks == null || mainMenuTracks.Length == 0) return;

        StopCurrentMusicRoutine();
        currentMusicRoutine = StartCoroutine(MenuPlaylistRoutine());
    }

    /// <summary>
    /// Inicia la playlist aleatoria de canciones durante la partida en la arena.
    /// </summary>
    public void StartInGamePlaylist()
    {
        if (inGameSoundtracks == null || inGameSoundtracks.Length == 0) return;

        StopCurrentMusicRoutine();
        currentMusicRoutine = StartCoroutine(InGamePlaylistRoutine());
    }

    private void StopCurrentMusicRoutine()
    {
        if (currentMusicRoutine != null)
        {
            StopCoroutine(currentMusicRoutine);
            currentMusicRoutine = null;
        }
    }

    private IEnumerator MenuPlaylistRoutine()
    {
        // Fade Out previo si venía sonando otra cosa
        if (musicSource.isPlaying)
        {
            yield return StartCoroutine(FadeMusicVolume(musicSource.volume, 0f, crossfadeDuration));
        }

        musicSource.loop = false;

        while (true)
        {
            if (mainMenuTracks == null || mainMenuTracks.Length == 0)
            {
                yield return new WaitForSecondsRealtime(1f);
                continue;
            }

            // Reproducir en orden estricto
            AudioClip currentTrack = mainMenuTracks[currentMenuTrackIndex];

            if (currentTrack != null)
            {
                musicSource.clip = currentTrack;
                musicSource.Play();

                // Fade In suave
                yield return StartCoroutine(FadeMusicVolume(0f, musicVolume, crossfadeDuration));

                // Esperar a que la canción termine (restando tiempo de Fade Out)
                float waitTime = Mathf.Max(0.5f, currentTrack.length - crossfadeDuration);
                yield return new WaitForSecondsRealtime(waitTime);

                // Fade Out suave
                yield return StartCoroutine(FadeMusicVolume(musicVolume, 0f, crossfadeDuration));
            }
            else
            {
                yield return new WaitForSecondsRealtime(1f);
            }

            // Siguiente canción en orden (vuelve a la primera al terminar la lista)
            currentMenuTrackIndex = (currentMenuTrackIndex + 1) % mainMenuTracks.Length;
        }
    }

    private IEnumerator InGamePlaylistRoutine()
    {
        // Fade Out previo si venía sonando el menú
        if (musicSource.isPlaying)
        {
            yield return StartCoroutine(FadeMusicVolume(musicSource.volume, 0f, crossfadeDuration));
        }

        musicSource.loop = false;

        while (true)
        {
            if (inGameSoundtracks == null || inGameSoundtracks.Length == 0)
            {
                yield return new WaitForSecondsRealtime(1f);
                continue;
            }

            // Elegir pista aleatoria sin repetir la anterior
            int nextIndex = PickNextRandomInGameIndex();
            AudioClip currentTrack = inGameSoundtracks[nextIndex];
            lastInGameTrackIndex = nextIndex;

            if (currentTrack != null)
            {
                musicSource.clip = currentTrack;
                musicSource.Play();

                // Fade In
                yield return StartCoroutine(FadeMusicVolume(0f, musicVolume, crossfadeDuration));

                // Esperar a que la canción termine
                float waitTime = Mathf.Max(0.5f, currentTrack.length - crossfadeDuration);
                yield return new WaitForSecondsRealtime(waitTime);

                // Fade Out
                yield return StartCoroutine(FadeMusicVolume(musicVolume, 0f, crossfadeDuration));
            }
            else
            {
                yield return new WaitForSecondsRealtime(1f);
            }
        }
    }

    private int PickNextRandomInGameIndex()
    {
        if (inGameSoundtracks.Length <= 1) return 0;

        int newIndex = Random.Range(0, inGameSoundtracks.Length);
        int attempts = 0;

        while (newIndex == lastInGameTrackIndex && attempts < 10)
        {
            newIndex = Random.Range(0, inGameSoundtracks.Length);
            attempts++;
        }

        return newIndex;
    }

    private IEnumerator FadeMusicVolume(float from, float to, float duration)
    {
        if (duration <= 0.05f)
        {
            musicSource.volume = to;
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            musicSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }
        musicSource.volume = to;
    }

    #endregion

    #region Reproducción de SFX y UI

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public void PlayButtonHover(AudioClip customClip = null, float volume = -1f, float pitch = 1f)
    {
        AudioClip clipToPlay = customClip != null ? customClip : defaultButtonHover;
        float vol = volume >= 0f ? volume : defaultHoverVolume;

        if (clipToPlay != null)
        {
            PlaySFX(clipToPlay, vol, pitch);
        }
    }

    public void PlayButtonClick(AudioClip customClip = null, float volume = -1f, float pitch = 1f)
    {
        AudioClip clipToPlay = customClip != null ? customClip : defaultButtonClick;
        float vol = volume >= 0f ? volume : defaultClickVolume;

        if (clipToPlay != null)
        {
            PlaySFX(clipToPlay, vol, pitch);
        }
    }

    #endregion
}
