using RhythmCore.Data;
using RhythmCore.Logic;
using UnityEngine;
using UnityEngine.UI;
using static RhythmCore.Logic.Judgement;

namespace RhythmCore.Gameplay
{
    public class NoteController : MonoBehaviour
    {
        [Header("Data")]
        public NoteData Data;

        [Header("Visual")]
        public GameObject tapVisual;
        public GameObject dragVisual;
        public GameObject blastVisual;

        public GameObject holdHeadVisual;
        public GameObject holdTailVisual;
        public GameObject holdBodyVisual;


        public RectTransform bodyRectTransform;
        private Image _bodyRenderer;
        private RectTransform rectTransform => GetComponent<RectTransform>();
        private GameObject _activeHoldVFX;

        [Header("State")]
        public bool IsHit { get; private set; } = false;
        public bool IsHoldingState { get; private set; } = false;
        private bool _isMissed = false;  
        private double _missBeatPoint;

        private int _lastTickBeat = -1;

        private float _holdTimeCounter = 0f;
        private const float Grace_Period = 0.15f;

        private const float BLAST_FIXED_SPEED = 50f;
        private const float WARNING_VIEW_DISTANCE = 30f;
        private bool _hasSpawnedWarning = false;

        [Header("Score Config")]
        public int scorePerfect = 100;
        public int scoreGreat = 50;
        public int scoreMiss = 0;
        public bool breaksComboOnMiss = true;

        [Header("References")]
        private LaneController _parentLane;

        private bool _isInitialized = false;

        public void Initialize(NoteData data, LaneController lane)
        {
            Data = data;
            _parentLane = lane;
            IsHit = false;
            IsHoldingState = false;
            _isMissed = false;
            _lastTickBeat = (int) Data.beat;

            _isInitialized = true;
            
            
            _parentLane.RegisterNote(this);

            if (Data.type != NoteType.Hold) rectTransform.anchoredPosition = new Vector2(ScrollManager.Instance.GetNotePosition(Data.beat), 0);

            if (holdBodyVisual != null)
            {
                _bodyRenderer = holdBodyVisual.GetComponent<Image>();
            }

            SetupVisuals();
        }

        private void SetupVisuals()
        {
            if (tapVisual) tapVisual.SetActive(false);
            if (dragVisual) dragVisual.SetActive(false);
            if (blastVisual) blastVisual.SetActive(false);
            if (holdHeadVisual) holdHeadVisual.SetActive(false);
            if (holdTailVisual) holdTailVisual.SetActive(false);
            if (holdBodyVisual) holdBodyVisual.SetActive(false);

            switch (Data.type)
            {
                case NoteType.Tap:
                    if (tapVisual) tapVisual.SetActive(true);
                    break;
                case NoteType.Drag:
                    if (dragVisual) dragVisual.SetActive(true);
                    break;
                case NoteType.Blast:
                    if (blastVisual) blastVisual.SetActive(true);
                    break;
                case NoteType.Hold:
                    if (holdHeadVisual) holdHeadVisual.SetActive(true);
                    if (holdTailVisual) holdTailVisual.SetActive(true);
                    if (holdBodyVisual) holdBodyVisual.SetActive(true);
                    break;
            }
        }

        public bool IsInTapWindow()
        {
            if (IsHit) return false;
            // Drag không ăn bằng Tap (trừ khi bạn muốn cơ chế giống Cytus, nhưng thiết kế mới là Hold Drag)
            if (Data.type == NoteType.Drag) return false;

            double diff = System.Math.Abs(Conductor.Instance.SongPosition - Conductor.Instance.BeatToTime(Data.beat));
            return diff <= Judgement.WINDOW_GREAT;
        }

        // 2. Kiểm tra xem Drag có nằm trong vùng cho phép ăn không?
        public bool IsInDragWindow()
        {
            if (IsHit) return false;
            double diff = System.Math.Abs(Conductor.Instance.SongPosition - Conductor.Instance.BeatToTime(Data.beat));
            return diff <= Judgement.WINDOW_PERFECT;
        }

        public bool TryHit(double inputDSPTime)
        {
            if (IsHit) return false;

            double noteDSPTime = Conductor.Instance.BeatToTime(Data.beat);
            var result = Judgement.EvaluateHit(noteDSPTime, inputDSPTime);

            if (result == HitResult.None)
                return false;

            Debug.Log($"Input: {inputDSPTime} | Note: {noteDSPTime} | Diff: {inputDSPTime - noteDSPTime} | Result: {result}");
            
            CommitResult(result);

            if (Data.type == NoteType.Hold)
                ProcessHoldHeadHit(result);
            else
                ProcessNormalHit(result);

            return true;
        }

        // 4. Xử lý giữ phím (Cho Hold Body và Drag)
        public void OnKeepHolding()
        {
            // A. Xử lý DRAG: Chỉ cần giữ phím khi nó lướt qua
            if (Data.type == NoteType.Drag && !IsHit)
            {
                if (IsInDragWindow())
                {
                    Debug.Log("DRAG HIT!");
                    CommitResult(HitResult.Perfect); // Drag luôn Perfect nếu trúng
                    IsHit = true;
                    VFXManager.Instance.PlayTapVFX(GetVFXSpawnPosition());
                    _parentLane.UnregisterNote(this);
                    Destroy(gameObject);
                }
                return;
            }

            // B. Xử lý HOLD: Duy trì trạng thái giữ
            if (Data.type == NoteType.Hold && IsHoldingState)
            {
                _holdTimeCounter = 0f; // Reset bộ đếm ân xá (Grace Period)

                // Logic tính điểm theo Tick (Beat)
                double currentBeat = Conductor.Instance.CurrentBeat;
                if ((int)currentBeat > _lastTickBeat)
                {
                    _lastTickBeat = (int)currentBeat;
                    // GameManager.AddScore(10)...
                    Debug.Log("Hold Tick!");
                }
            }
        }

        // 5. Xử lý thả tay (Input Manager báo)
        public void OnReleaseHold()
        {
            // Chỉ quan tâm nếu đang giữ Hold Note
            if (Data.type == NoteType.Hold && IsHoldingState)
            {
                // Tăng bộ đếm ân xá, chưa chết ngay
                // (Logic này xử lý trong Update, hàm này chỉ để thông báo sự kiện nếu cần)
            }
        }

        private void ProcessNormalHit(HitResult result)
        {
            IsHit = true;
            VFXManager.Instance.PlayTapVFX(GetVFXSpawnPosition());
            Destroy(gameObject);
        }

        private void ProcessHoldHeadHit(HitResult result)
        {
            // Nếu đánh trúng đầu Hold (kể cả Great/Miss mà chưa quá tệ)
            if (result != HitResult.Miss)
            {
                IsHit = true; // Đầu đã xong
                IsHoldingState = true; // Chuyển sang giai đoạn giữ
                _holdTimeCounter = 0f;

                VFXManager.Instance.PlayTapVFX(GetVFXSpawnPosition());
                if (holdHeadVisual != null)
                {
                    _activeHoldVFX = VFXManager.Instance.StartHoldVFX(transform.position, holdHeadVisual.transform);
                }
            }
            else
            {
                // Nếu Miss cái đầu -> Miss cả dây
                HandleMiss();
            }
        }

        void Update()
        {
            if (!_isInitialized) return;


            UpdateVisual();
            UpdateHoldVisual(Conductor.Instance.CurrentBeat);

            if (IsHit && !IsHoldingState) return;

            HandleBlast();

            if (Data.type == NoteType.Hold)
            {
                HandleHolding();
            }
            else CheckMissCondition();
        }

        public void UpdateVisual()
        {
            if (Data.type == NoteType.Hold) return; // Hold xử lý riêng

            float visualDistance = ScrollManager.Instance.GetNotePosition(Data.beat);

            if (Data.type == NoteType.Blast)
            {
                visualDistance = visualDistance / ScrollManager.Instance.BaseNoteSpeed * BLAST_FIXED_SPEED;
            }

            rectTransform.anchoredPosition = new Vector2(visualDistance, 0);
        }

        private void HandleHolding()
        {
            double currentSongBeat = Conductor.Instance.CurrentBeat;
            double currentSongTime = Conductor.Instance.SongPosition;
            double headTime = Conductor.Instance.BeatToTime(Data.beat);

            if (IsHoldingState)
            {
                _holdTimeCounter += Time.deltaTime;
                if (_holdTimeCounter > Grace_Period)
                {
                    if (!_parentLane.IsHoldingInput)
                    {
                        HandleHoldBreak(currentSongBeat);
                    }
                }
            }
            else if (!_isMissed && !IsHoldingState && (currentSongTime - headTime > Judgement.WINDOW_MISS))
            {
                HandleHoldBreak(Data.beat);
            }
            
            if (IsHoldingState && currentSongBeat >= Data.beat + Data.duration)
            {
                FinishHold();
                return;
            }

            if (IsHoldingState)
            {
                int currentIntBeat = (int)currentSongBeat;
                if (currentIntBeat > _lastTickBeat)
                {
                    _lastTickBeat = currentIntBeat;
                    Debug.Log($"Lane {Data.lane} HOLD TICK +100 Score");
                    // TODO: GameManager.AddScore(100);
                }
            }
        }

        private void UpdateHoldVisual(double currentSongBeat)
        {
            if (Data.type != NoteType.Hold) return;

            double effectiveHeadBeat;

            if (IsHoldingState)
            {
                effectiveHeadBeat = currentSongBeat;
            }
            else if (_isMissed)
            {
                effectiveHeadBeat = _missBeatPoint;
            }
            else
            {
                effectiveHeadBeat = Data.beat;
            }

            double tailBeat = Data.beat + Data.duration;
            if (effectiveHeadBeat > tailBeat) effectiveHeadBeat = tailBeat;

            float xHead = ScrollManager.Instance.GetNotePosition(effectiveHeadBeat);
            float xTail = ScrollManager.Instance.GetNotePosition(tailBeat);

            if (holdHeadVisual) holdHeadVisual.transform.localPosition = new Vector3(xHead, 0, 0);
            if (holdTailVisual) holdTailVisual.transform.localPosition = new Vector3(xTail, 0, 0);

            float startX = Mathf.Min(xHead, xTail);
            float distance = Mathf.Abs(xTail - xHead);

            holdBodyVisual.transform.localPosition = new Vector3(startX, 0, 0);
            
            float currentHeight = bodyRectTransform.sizeDelta.y;

            bodyRectTransform.sizeDelta = new Vector2(distance, currentHeight);
        }

        private void HandleHoldBreak(double missAtBeat)
        {
            Debug.Log("HOLD MISS / BREAK!");
            IsHoldingState = false;
            _isMissed = true;
            _missBeatPoint = missAtBeat;
            
            var color = Color.gray;
            color.a = 0.5f;

            _parentLane.UnregisterNote(this);

            VFXManager.Instance.StopHoldVFX(_activeHoldVFX);

            CommitResult(HitResult.Miss);

            Destroy(gameObject, 5.0f);
        }

        private void FinishHold()
        {
            Debug.Log("HOLD COMPLETE!");
            IsHoldingState = false;
            IsHit = true;

            _parentLane.UnregisterNote(this);

            VFXManager.Instance.PlayTapVFX(GetVFXSpawnPosition());
            VFXManager.Instance.StopHoldVFX(_activeHoldVFX);

            CommitResult(HitResult.Perfect);

            Destroy(gameObject);
        }

        private void HandleBlast()
        {
            if (Data.type == NoteType.Blast && !_hasSpawnedWarning)
            {
                double currentBeat = Conductor.Instance.CurrentBeat;
                double targetBeat = Data.beat;

                float currentDistance = (float)(targetBeat - currentBeat) * ScrollManager.Instance.BaseNoteSpeed;

                if (currentDistance <= WARNING_VIEW_DISTANCE && (targetBeat - currentBeat) > 0)
                {
                    _hasSpawnedWarning = true;

                    _parentLane.SpawnWarning(currentBeat, targetBeat);
                    Debug.Log($"BLAST WARNING Spawned for note at beat {Data.beat}");
                }
            }
        }

        public void ForceMiss()
        {
            if (IsHit) return;

            IsHit = true;
            CommitResult(HitResult.Miss);

            Destroy(gameObject, 0.3f);
        }
        private void CheckMissCondition()
        {
            // Nếu đã Hit rồi thì thôi
            if (IsHit) return;

            double currentSongTime = Conductor.Instance.SongPosition;
            double noteTime = Conductor.Instance.BeatToTime(Data.beat);

            // Nếu thời gian hiện tại > thời gian note + thời gian cho phép trễ (Miss Window)
            if (currentSongTime > noteTime + Judgement.WINDOW_MISS)
            {
                HandleMiss();
            }
        }

        private void HandleMiss()
        {
            Debug.Log($"MISS Note type {Data.type} at beat {Data.beat}");
            _parentLane.UnregisterNote(this);
            IsHit = true;

            CommitResult(HitResult.Miss);

            Destroy(gameObject, 0.5f);
        }

        private void CommitResult(HitResult result)
        {
            int scoreValue = 0;
            bool breakCombo = false;

            switch (result)
            {
                case HitResult.Perfect:
                    scoreValue = scorePerfect;
                    break;
                case HitResult.Great:
                    scoreValue = scoreGreat;
                    break;
                case HitResult.Miss:
                    scoreValue = scoreMiss;
                    breakCombo = breaksComboOnMiss;
                    break;
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ProcessHit(result, this, scoreValue);
            }
        }

        //VFX 
        private Vector3 GetVFXSpawnPosition()
        {
            // 1. Nếu ông dùng Canvas Overlay (phổ biến nhất):
            // Cần chuyển từ Screen Point (Pixel) sang World Point (Unit)

            if (Camera.main == null) return transform.position; // Fallback nếu quên gắn Tag MainCamera

            // Lấy vị trí trên màn hình của cái Note
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, this.transform.position);

            // Quan trọng: Đặt Z (khoảng cách từ Camera đến điểm nổ). 
            // Giả sử Camera ở Z=-10, thì đặt Z=9 để nó nổ ngay trước mặt Camera (cách 9 đơn vị)
            screenPos.z = 10.0f; // Số này tuỳ chỉnh độ to nhỏ, càng gần càng to

            return Camera.main.ScreenToWorldPoint(screenPos);
        }
    }
}