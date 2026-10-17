using MainLobby.Data;
using RhythmCore.Data;
using RhythmCore.Logic;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RhythmCore.Gameplay
{
    public class GameLoader : MonoBehaviour
    {
        [Header("Chart Settings")]
        public string chartFileName = "New Song";
        public bool autoStart = true;
        public float startDelayTime = 10f;

        [Header("References")]
        public LaneController lanePrefab;
        public NoteController notePrefab;
        public Transform trackRoot;

        [Header("Environment")] 
        public SpriteRenderer backgroundRenderer;

        private Dictionary<int, LaneController> _spawnedLanes = new Dictionary<int, LaneController>();

        System.Collections.IEnumerator Start()
        {
            yield return null; // Chờ 1 frame

            // 1. Check dependency hệ thống
            if (!CheckDependencies()) yield break;

            // 2. --- [LOGIC MỚI] CHECK VALI TỪ LOBBY ---
            if (GlobalDataManager.Instance != null)
            {
                var session = GlobalDataManager.Instance.CurrentSession;

                // Kiểm tra SelectedSong trước (vì nó chứa tên file)
                if (session != null && session.SelectedSong != null)
                {
                    // 👇 SỬA DÒNG NÀY: Lấy từ SelectedSong chứ không phải SelectedChart
                    string rawName = session.SelectedSong.JsonFileName;

                    if (!string.IsNullOrEmpty(rawName))
                    {
                        // Dùng Path để cắt đuôi .json
                        chartFileName = System.IO.Path.GetFileNameWithoutExtension(rawName);
                        Debug.Log($"[GameLoader] 🎒 Đã nhận hàng: {chartFileName}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[GameLoader] ⚠️ Không thấy GlobalDataManager (Chắc là đang chạy test Scene lẻ). Dùng tên mặc định: " + chartFileName);
            }
            // ------------------------------------------

            // 3. Tiến hành load
            LoadAndSetupChart();
        }


        bool CheckDependencies()
        {
            if (ScrollManager.Instance == null || Conductor.Instance == null)
            {
                Debug.LogError("CRITICAL: Thiếu ScrollManager hoặc Conductor trong Scene!");
                return false;
            }
            return true;
        }

        void LoadAndSetupChart()
        {
            // 1. LOAD JSON
            // Lưu ý: File JSON phải nằm trong Assets/Resources/Charts/
            TextAsset jsonFile = Resources.Load<TextAsset>($"Charts/{chartFileName}");
            if (jsonFile == null)
            {
                Debug.LogError($"[GameLoader] Không tìm thấy file Chart tại: Resources/Charts/{chartFileName}");
                return;
            }

            ChartData chart = JsonUtility.FromJson<ChartData>(jsonFile.text);

            // 2. LOAD ASSETS (Audio & BG)
            // Xử lý đường dẫn kỹ hơn trước khi Load
            LoadAudioResource(chart.metadata.audioPath);
            LoadBackgroundResource(chart.metadata.backgroundPath);

            // 3. SETUP LOGIC
            Conductor.Instance.Initialize(chart.metadata);
            Conductor.Instance.startDelay = startDelayTime;

            // Sắp xếp sự kiện theo thời gian (beat) để tránh lỗi cuộn
            chart.sv_events.Sort((a, b) => a.beat.CompareTo(b.beat));
            ScrollManager.Instance.Initialize(chart.sv_events);

            SetupLanes(chart);
            SetupNotes(chart.notes);

            if (InputManager.Instance != null)
            {
                if (InputManager.Instance.laneContainer == null)
                    InputManager.Instance.laneContainer = trackRoot;
            }

            if (autoStart)
            {
                Conductor.Instance.PlayMusic();
            }
        }

        // --- HÀM LOAD AUDIO ĐÃ FIX ---
        void LoadAudioResource(string rawPath)
        {
            // Làm sạch đường dẫn
            string cleanPath = GetCleanPath(rawPath);

            if (string.IsNullOrEmpty(cleanPath)) return;

            // Load trực tiếp cleanPath (vì GetCleanPath đã xử lý folder rồi)
            AudioClip clip = Resources.Load<AudioClip>(cleanPath);

            if (clip != null)
            {
                Debug.Log($"[GameLoader] Loaded Audio: {cleanPath}");
                Conductor.Instance.SetMusic(clip);
            }
            else
            {
                // Thử fallback: Nếu người dùng chỉ lưu tên file, thử tìm trong folder Audio mặc định
                string fallbackPath = $"Audio/{cleanPath}";
                clip = Resources.Load<AudioClip>(fallbackPath);

                if (clip != null)
                {
                    Debug.Log($"[GameLoader] Loaded Audio (Fallback): {fallbackPath}");
                    Conductor.Instance.SetMusic(clip);
                }
                else
                {
                    Debug.LogError($"[GameLoader] KHÔNG TÌM THẤY NHẠC! Đã thử load tại: '{cleanPath}' và '{fallbackPath}'");
                }
            }
        }

        // --- HÀM LOAD BACKGROUND ĐÃ FIX ---
        void LoadBackgroundResource(string rawPath)
        {
            string cleanPath = GetCleanPath(rawPath);
            if (string.IsNullOrEmpty(cleanPath) || backgroundRenderer == null) return;

            // Load Texture trước
            Texture2D tex = Resources.Load<Texture2D>(cleanPath);
            if (tex == null) tex = Resources.Load<Texture2D>($"Backgrounds/{cleanPath}");

            if (tex != null)
            {
                // Tạo Sprite từ Texture
                // Rect(0, 0, w, h): Lấy toàn bộ ảnh
                // Vector2(0.5f, 0.5f): Tâm ảnh nằm giữa (Pivot Center)
                Sprite newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

                backgroundRenderer.sprite = newSprite;

                // Tùy chọn: Chỉnh scale background cho vừa màn hình nếu cần
                // backgroundRenderer.transform.localScale = new Vector3(...);

                Debug.Log($"[GameLoader] Loaded Background Sprite: {cleanPath}");
            }
            else
            {
                Debug.LogWarning($"[GameLoader] Không tìm thấy Background: {cleanPath}");
            }
        }
        
        // --- HÀM XỬ LÝ PATH QUAN TRỌNG (FIX LOGIC) ---
        string GetCleanPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            // 1. Chuẩn hóa dấu gạch chéo
            path = path.Replace("\\", "/");

            // 2. Nếu đường dẫn chứa "Resources/", cắt bỏ phần đầu đi
            // Ví dụ: "Assets/Resources/Audio/Song.mp3" -> "Audio/Song.mp3"
            int resIndex = path.IndexOf("/Resources/");
            if (resIndex >= 0)
            {
                path = path.Substring(resIndex + 11); // 11 là độ dài của "/Resources/"
            }
            else if (path.StartsWith("Resources/"))
            {
                path = path.Substring(10);
            }

            // 3. Xóa đuôi file (.mp3, .wav, .png...)
            int dotIndex = path.LastIndexOf('.');
            if (dotIndex >= 0)
            {
                path = path.Substring(0, dotIndex);
            }

            return path;
        }

        void SetupLanes(ChartData currentChart)
        {
            // Clear lane cũ
            foreach (Transform child in trackRoot) Destroy(child.gameObject);
            _spawnedLanes.Clear();

            var definitions = currentChart.laneDefinitions;

            // Sắp xếp (giữ nguyên logic của ông)
            definitions.Sort((a, b) => a.lane_id.CompareTo(b.lane_id));

            foreach (var def in definitions)
            {
                // Tạo object từ prefab
                var laneObj = Instantiate(lanePrefab, trackRoot);

                // Lấy script controller ra (để chắc chắn)
                var controller = laneObj.GetComponent<LaneController>();

                // --- QUAN TRỌNG ---
                // 1. Không cần set localPosition bằng tay ở đây nữa (dòng cũ của ông).
                //    Vì trong Initialize -> UpdateLaneState(0) nó sẽ tự tính vị trí rồi.

                // 2. Gọi Initialize với 3 tham số: ID, Definition, và List Events
                controller.Initialize(def.lane_id, def, currentChart.lane_events);

                // Lưu vào dictionary quản lý
                _spawnedLanes.Add(def.lane_id, controller);
            }
        }

        void SetupNotes(List<NoteData> notes)
        {
            foreach (var noteData in notes)
            {
                if (_spawnedLanes.TryGetValue(noteData.lane, out LaneController targetLane))
                {
                    var noteObj = Instantiate(notePrefab, targetLane.noteContainer);
                    noteObj.transform.localPosition = Vector3.zero;
                    noteObj.Initialize(noteData, targetLane);
                }
            }
        }
    }
}