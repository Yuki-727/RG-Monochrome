using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using RhythmCore.Gameplay;

namespace RhythmCore.Logic
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance;

        [Header("References")]
        public Transform laneContainer;

        private Queue<double> _inputTokens = new Queue<double>();

        // Đổi từ Array sang List để dễ dàng thêm bớt động
        private List<Key> _watchedKeys = new List<Key>();

        void Awake()
        {
            Instance = this;
            SetupKeys(); // Gọi hàm cài đặt phím
        }

        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        // Hàm này sẽ tự động add tất cả các nút ông cần vào List
        void SetupKeys()
        {
            _watchedKeys.Clear();

            // 1. Thêm tự động từ A đến Z (Dùng vòng lặp cho gọn code)
            for (int i = (int)Key.A; i <= (int)Key.Z; i++)
            {
                _watchedKeys.Add((Key)i);
            }

            // 2. Thêm 4 phím mũi tên
            _watchedKeys.Add(Key.UpArrow);
            _watchedKeys.Add(Key.DownArrow);
            _watchedKeys.Add(Key.LeftArrow);
            _watchedKeys.Add(Key.RightArrow);

            // 3. Thêm các ký tự đặc biệt []\;',./
            // Unity đặt tên hơi khác ký tự trên phím một chút, tui map sẵn ở đây:
            _watchedKeys.Add(Key.LeftBracket);  // [
            _watchedKeys.Add(Key.RightBracket); // ]
            _watchedKeys.Add(Key.Backslash);    // \
            _watchedKeys.Add(Key.Semicolon);    // ;
            _watchedKeys.Add(Key.Quote);        // '
            _watchedKeys.Add(Key.Comma);        // ,
            _watchedKeys.Add(Key.Period);       // .
            _watchedKeys.Add(Key.Slash);        // /

            // Thêm phím Space nữa cho đủ bộ
            _watchedKeys.Add(Key.Space);
        }

        void Update()
        {
            // --- 1. XỬ LÝ MOBILE (TOUCH) ---
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    OnTap();
                }
            }

            // --- 2. XỬ LÝ PC (KEYBOARD) ---
            if (Keyboard.current != null)
            {
                // Duyệt qua cái List mình vừa tạo ở trên
                foreach (var key in _watchedKeys)
                {
                    // Check từng phím một xem frame này có nhấn không
                    if (Keyboard.current[key].wasPressedThisFrame)
                    {
                        OnTap();
                    }
                }
            }

            ConsumeInputTokens();
            HandleHolding();
        }

        void OnTap()
        {
            _inputTokens.Enqueue(Conductor.Instance.SongPosition);
        }

        void ConsumeInputTokens()
        {
            while (_inputTokens.Count > 0)
            {
                double inputTime = _inputTokens.Dequeue();
                NoteController target = FindBestTapNote(inputTime);

                if (target != null)
                {
                    target.TryHit(inputTime);
                }
            }
        }

        void HandleHolding()
        {
            bool isHolding = false;

            // Check Mobile
            if (Touch.activeTouches.Count > 0) isHolding = true;

            // Check PC (Dùng anyKey check tổng cho lẹ, hoặc check từng phím nếu muốn kỹ)
            if (Keyboard.current != null && Keyboard.current.anyKey.isPressed) isHolding = true;

            // Logic tìm lane hold notes (giữ nguyên)
            foreach (Transform laneTransform in laneContainer)
            {
                var lane = laneTransform.GetComponent<LaneController>();
                if (lane == null) continue;

                var holdNotes = lane.GetHoldCandidates();

                if (isHolding)
                    foreach (var note in holdNotes) note.OnKeepHolding();
                else
                    foreach (var note in holdNotes) note.OnReleaseHold();
            }
        }

        NoteController FindBestTapNote(double inputTime)
        {
            return FindBestTapNoteInternal(inputTime);
        }


        private NoteController FindBestTapNoteInternal(double inputTime)
        {
            NoteController bestNote = null;
            double bestAbsDiff = double.MaxValue;

            foreach (Transform laneTransform in laneContainer)
            {
                var lane = laneTransform.GetComponent<LaneController>();
                if (lane == null) continue;

                var candidates = lane.GetTapCandidates();

                foreach (var note in candidates)
                {
                    double noteTime = Conductor.Instance.BeatToTime(note.Data.beat);
                    double diff = inputTime - noteTime;

                    // Quá sớm hoặc quá trễ → bỏ
                    if (diff > Judgement.WINDOW_GREAT) continue;
                    if (diff < -Judgement.WINDOW_MISS) continue;

                    double abs = System.Math.Abs(diff);

                    if (abs < bestAbsDiff)
                    {
                        bestAbsDiff = abs;
                        bestNote = note;
                    }
                    else if (abs == bestAbsDiff && bestNote != null)
                    {
                        // Ưu tiên note xuất hiện sớm hơn nếu timing bằng nhau
                        if (note.Data.beat < bestNote.Data.beat)
                        {
                            bestNote = note;
                        }
                    }
                }
            }

            return bestNote;
        }
    }
}