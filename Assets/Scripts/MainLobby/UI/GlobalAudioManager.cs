using UnityEngine;
using System.Collections;
using RhythmCore.Data;
using System.Linq; 

public class GlobalAudioManager : MonoBehaviour
{
    public static GlobalAudioManager Instance;

    [Header("--- AUDIO SOURCES ---")]
    public AudioSource lobbySource;  
    public AudioSource previewSource;  

    [Header("--- SETTINGS ---")]
    public float fadeDuration = 1f;

    private Coroutine _fadeJob;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Mặc định vào game bật nhạc Lobby
        PlayLobbyMusic();
    }

    // --- CÁC HÀM GỌI TỪ BÊN NGOÀI ---

    // 1. Gọi khi vào Song Select hoặc chọn bài mới
    // (Logic delay sẽ nằm ở Controller, thằng này chỉ biết "Bảo tao phát là tao phát")
    public void PlayPreview(MainLobby.Data.SongEntry song)
    {
        if (song == null) return;

        // Lấy Metadata (Hard hoặc cái đầu tiên)
        var chart = song.GetMetadata("Hard"); // Hàm ông đã có trong SongEntry
        if (chart == null && song.Charts.Count > 0) chart = song.Charts.Values.FirstOrDefault()?.metadata;
        if (chart == null) return;

        StartCoroutine(RoutinePlayPreview(chart));
    }

    // 2. Gọi khi thoát Song Select (Back về Lobby)
    public void ReturnToLobby()
    {
        StartCoroutine(RoutineSwitchToLobby());
    }

    // 3. Gọi khi vào Game Play (Tắt hết)
    public void StopAllMusic()
    {
        if (lobbySource) lobbySource.Stop();
        if (previewSource) previewSource.Stop();
    }

    // 4. Chỉ bật Lobby (dùng lúc Start game)
    public void PlayLobbyMusic()
    {
        if (lobbySource && !lobbySource.isPlaying)
        {
            lobbySource.volume = 1f;
            lobbySource.Play();
        }
    }

    // --- LOGIC CHUYỂN ĐỔI (FADE) ---

    private IEnumerator RoutinePlayPreview(ChartMetadata meta)
    {
        // 1. Tải Audio Clip từ Resources
        string cleanPath = PathHelper.FixResourcePath(meta.audioPath);
        ResourceRequest request = Resources.LoadAsync<AudioClip>(cleanPath);
        yield return request;

        AudioClip clip = request.asset as AudioClip;
        if (clip == null)
        {
            Debug.LogWarning($"Không tìm thấy Audio tại: {cleanPath}");
            yield break;
        }

        // 2. Tính thời điểm bắt đầu (Preview Start Time)
        // Công thức: Time (s) = (Beat * 60) / BPM
        double startTime = 0;
        if (meta.bpm > 0)
        {
            startTime = (meta.previewStartBeat * 60.0) / meta.bpm;
        }

        // 3. Fade Out Lobby (Nếu đang chạy)
        if (lobbySource.isPlaying)
        {
            yield return StartCoroutine(FadeVolume(lobbySource, 0f, 0.3f));
            lobbySource.Stop();
        }

        // 4. Setup Preview Source
        previewSource.Stop();
        previewSource.clip = clip;
        previewSource.time = (float)startTime; // Tua đến đoạn điệp khúc
        previewSource.volume = 0f; // Để tí fade in
        previewSource.Play();

        // 5. Fade In Preview
        yield return StartCoroutine(FadeVolume(previewSource, 1f, fadeDuration));
    }

    private IEnumerator RoutineSwitchToLobby()
    {
        // 1. Fade Out Preview
        if (previewSource.isPlaying)
        {
            yield return StartCoroutine(FadeVolume(previewSource, 0f, 0.3f));
            previewSource.Stop();
            previewSource.clip = null; // Giải phóng bộ nhớ nhẹ
        }

        // 2. Fade In Lobby
        if (lobbySource)
        {
            if (!lobbySource.isPlaying) lobbySource.Play();
            yield return StartCoroutine(FadeVolume(lobbySource, 1f, fadeDuration));
        }
    }

    // Helper Fade volume thủ công (Không cần DOTween)
    private IEnumerator FadeVolume(AudioSource source, float targetVol, float duration)
    {
        float startVol = source.volume;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            source.volume = Mathf.Lerp(startVol, targetVol, t);
            yield return null;
        }
        source.volume = targetVol;
    }
}