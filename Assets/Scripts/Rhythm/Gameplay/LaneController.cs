using RhythmCore.Data;
using RhythmCore.Logic;
using RhythmCore.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmCore.Gameplay
{
    [RequireComponent(typeof(RectTransform))]
    public class LaneController : MonoBehaviour
    {
        //public SpriteRenderer visualRenderer;

        [Header("Identity")]
        public int LaneID;

        [Header("Visual Settings")]
        // Tỉ lệ màn hình chuẩn thiết kế (1920x1080 -> 16:9)
        public Vector2 referenceResolution = new Vector2(1920, 1080);

        // Scale: 1 đơn vị Logic (trong Editor) = bao nhiêu Pixel?
        public float basePixelScale = 120f;

        private RectTransform _rectTransform;
        private List<LaneEvent> _events;
        private LaneData _defaultDef;
        private float _refAspectRatio;

        public float CurrentRotation { get; private set; }
        public bool IsAutoplay { get; private set; }

        public bool IsHoldingInput { get; set; } = false;

        [Header("References")]
        public Transform noteContainer;
        private List<NoteController> _activeNotes = new List<NoteController>();

        public Transform JudgementPoint; 

        [Header("Blast Config")]
        public Transform warningContainer; 
        public GameObject warningPrefab;   

        private int _activeBlastCount = 0;
        private Color _originalLaneColor;

        private Image _targetImage;
        private CanvasGroup _canvasGroup;

        [Header("References")]
        // Kéo cái object "Visual" vào đây
        public Image laneVisualImage;

        // Kéo cái object "Judgement Line" vào đây (nếu muốn đổi màu nó lun)
        public Image judgementLineImage; // Hoặc Image tùy ông dùng cái gì

        //private RectTransform _rectTransform;
        //private CanvasGroup _canvasGroup;
        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            // Tính tỉ lệ chuẩn (1.777...)
            _refAspectRatio = referenceResolution.x / referenceResolution.y;
        }

        public void Initialize(int id, LaneData def, List<LaneEvent> allEvents)
        {
            LaneID = id;
            gameObject.name = $"Lane_{id}";
            _defaultDef = def;

            // Lọc và sort event
            _events = allEvents
                .Where(e => e.lane_id == LaneID)
                .OrderBy(e => e.beat)
                .ToList();

            // Set vị trí ban đầu (Beat 0)
            UpdateLaneState(0);
        }

        // Dùng LateUpdate hoặc Update thường để di chuyển Lane
        void Update()
        {
            // Lấy thời gian từ Conductor (nếu đang chạy nhạc)
            if (Conductor.Instance != null && Conductor.Instance.IsPlaying)
            {
                UpdateLaneState(Conductor.Instance.CurrentBeat);
                CheckMiss(); // Logic cũ
            }
        }

        // --- LOGIC DI CHUYỂN LANE (QUAN TRỌNG NHẤT) ---
        public void UpdateLaneState(double currentBeat)
        {
            if (_defaultDef == null) return;

            // 1. KHỞI TẠO GIÁ TRỊ MẶC ĐỊNH (Theo Lane Definition)
            float currentX = _defaultDef.pos_x;
            float currentY = _defaultDef.pos_y;
            float currentRot = _defaultDef.rotation;
            float currentAlpha = _defaultDef.alpha;
            IsAutoplay = _defaultDef.autoplay;

            // 2. TÌM KEYFRAME TRƯỚC VÀ SAU (Logic giống Editor)
            LaneEvent prev = null;
            LaneEvent next = null;

            // Vì list _events đã sort theo beat, ta tìm thủ công cho nhanh
            // (Hoặc dùng LINQ như Editor cũng được, nhưng Loop tối ưu hơn cho Game Loop)
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].beat <= currentBeat)
                {
                    prev = _events[i];
                }
                else
                {
                    next = _events[i];
                    break; // Tìm thấy cái next đầu tiên là dừng
                }
            }

            // 3. TÍNH TOÁN LOGIC
            if (prev != null)
            {
                // Trạng thái cơ bản luôn lấy theo Keyframe gần nhất đã qua
                IsAutoplay = prev.autoplay;

                if (next != null)
                {
                    // --- CÓ CẢ PREV VÀ NEXT -> NỘI SUY (INTERPOLATE) ---
                    float duration = (float)(next.beat - prev.beat);

                    if (duration > 0.0001f) // Tránh chia cho 0
                    {
                        // Tính tiến độ t (Linear 0 -> 1)
                        float t = (float)((currentBeat - prev.beat) / duration);

                        // Parse chuỗi easing từ data thành Enum
                        EasingType easeType = prev.ease;

                        // Dùng EasingHelper để nội suy
                        // LƯU Ý: Hàm Interpolate của ông đã bao gồm Evaluate(t), nên ta truyền t thuần vào
                        currentX = EasingHelper.Interpolate(prev.pos_x, next.pos_x, t, easeType);
                        currentY = EasingHelper.Interpolate(prev.pos_y, next.pos_y, t, easeType);
                        currentRot = EasingHelper.Interpolate(prev.rotation, next.rotation, t, easeType);
                        currentAlpha = EasingHelper.Interpolate(prev.alpha, next.alpha, t, easeType);
                    }
                    else
                    {
                        // Thời gian quá ngắn -> Nhảy cóc luôn
                        currentX = prev.pos_x;
                        currentY = prev.pos_y;
                        currentRot = prev.rotation;
                        currentAlpha = prev.alpha;
                    }
                }
                else
                {
                    // --- CHỈ CÓ PREV (HẾT BÀI HOẶC KEYFRAME CUỐI) ---
                    // Giữ nguyên trạng thái của Prev
                    currentX = prev.pos_x;
                    currentY = prev.pos_y;
                    currentRot = prev.rotation;
                    currentAlpha = prev.alpha;
                }
            }
            // Nếu prev == null (chưa tới event đầu tiên), nó sẽ dùng giá trị Default ở bước 1.

            // 4. MAPPING LOGIC -> SCREEN (Giữ nguyên logic Visual)

            // Tính tỉ lệ màn hình
            float currentAspectRatio = (float)Screen.width / Screen.height;
            float widthMultiplier = currentAspectRatio / _refAspectRatio;

            // Tính pixel
            float finalPixelX = currentX * basePixelScale * widthMultiplier;
            float finalPixelY = currentY * basePixelScale;

            // 5. APPLY VÀO UNITY
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = new Vector2(finalPixelX, finalPixelY);
                // Đảo dấu rotation để khớp Visual Editor (-currentRot)
                _rectTransform.localRotation = Quaternion.Euler(0, 0, -currentRot);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = currentAlpha;
            }

            // Lưu rotation (đảo dấu) cho Note bay
            CurrentRotation = -currentRot;
        }

        public void RegisterNote(NoteController note)
        {
            _activeNotes.Add(note);
            _activeNotes.Sort((a, b) => a.Data.beat.CompareTo(b.Data.beat));
        }

        public void UnregisterNote(NoteController note)
        {
            if (_activeNotes.Contains(note))
                _activeNotes.Remove(note);
        }

        public List<NoteController> GetTapCandidates()
        {
            List<NoteController> list = new List<NoteController>();

            foreach (var note in _activeNotes)
            {
                if (note == null) continue;
                if (note.IsHit) continue;
                if (note.IsInTapWindow())
                {
                    list.Add(note);
                }
                else break;
            }

            return list;
        }

        public List<NoteController> GetHoldCandidates()
        {
            List<NoteController> list = new List<NoteController>();

            foreach (var note in _activeNotes)
            {
                if (note == null) continue;

                // Drag
                if (note.Data.type == NoteType.Drag && note.IsInDragWindow())
                {
                    list.Add(note);
                }
                // Hold body
                else if (note.Data.type == NoteType.Hold && note.IsHoldingState)
                {
                    list.Add(note);
                }
            }

            return list;
        }


        public void OnInputDown(double inputDSPTime)
        {
            IsHoldingInput = true;

            // đảm bảo note sớm nhất được xét trước
            _activeNotes.Sort((a, b) => a.Data.beat.CompareTo(b.Data.beat));

            foreach (var note in _activeNotes)
            {
                if (note == null || note.IsHit) continue;

                if (note.TryHit(inputDSPTime))
                {
                    _activeNotes.Remove(note);
                    break;
                }
            }
        }

        public void SetHolding(bool isHolding)
        {
            IsHoldingInput = isHolding;
        }

        public void CheckMiss()
        {
            double currentTime = Conductor.Instance.SongPosition;

            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                var note = _activeNotes[i];
                if (note == null || note.IsHit) continue;

                double noteTime = Conductor.Instance.BeatToTime(note.Data.beat);

                if (currentTime - noteTime > Judgement.WINDOW_MISS)
                {
                    note.ForceMiss();
                    _activeNotes.RemoveAt(i);
                }
            }
        }

        public void SpawnWarning(double startBeat, double targetBeat)
        {
            if (warningPrefab == null || warningContainer == null) return;

            var obj = Instantiate(warningPrefab, warningContainer);
            obj.transform.localPosition = Vector3.zero;

            var ctrl = obj.GetComponent<BlastWarningController>();

            float currentSize = 1f;
            if (JudgementPoint != null) currentSize = JudgementPoint.localScale.x;

            ctrl.Initialize(startBeat, targetBeat, currentSize);

            _activeBlastCount++;

            StartCoroutine(RemoveBlastCountDelay((float)(targetBeat - startBeat)));
        }    

        private System.Collections.IEnumerator RemoveBlastCountDelay(float durationInBeats)
        {
            double seconds = durationInBeats * (60f / Conductor.Instance.bpm);

            yield return new WaitForSeconds((float)seconds);
            _activeBlastCount = Mathf.Max(0, _activeBlastCount - 1);
        }

    }
}