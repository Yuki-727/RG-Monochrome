using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace MainLobby.UI
{
    public class MagneticScrollView : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        public static MagneticScrollView Instance;
        public ScrollRect scrollRect;
        public float snapSpeed = 10f;

        // Sự kiện khi Visual thay đổi (dùng để update UI, scale, info)
        public event Action<int> OnVisualSelectionChanged;
        // Sự kiện khi dừng hẳn (dùng để Play nhạc)
        public event Action<int> OnSelectionSettled;

        private float _stride;
        private int _totalItems;
        private bool _isScrolling = false;
        private float _targetY;

        private int _currentVisualIndex = -1; // Index hiện tại về mặt hình ảnh

        private void Awake() => Instance = this;

        public void Init(float height, float spacing, int count)
        {
            _stride = height + spacing;
            _totalItems = count;
            scrollRect.inertia = false;
        }

        private void Update()
        {
            // 1. Logic Snap (chạy khi thả tay)
            if (!_isScrolling && scrollRect.content != null)
            {
                Vector2 currentPos = scrollRect.content.anchoredPosition;
                float newY = Mathf.Lerp(currentPos.y, _targetY, Time.deltaTime * snapSpeed);
                if (Mathf.Abs(newY - _targetY) < 0.1f) newY = _targetY;
                scrollRect.content.anchoredPosition = new Vector2(currentPos.x, newY);
            }

            // 2. Logic Visual Realtime (Fix #5)
            // Tính toán xem bài nào đang ở giữa NGAY LÚC NÀY
            if (_stride > 0)
            {
                float currentY = scrollRect.content.anchoredPosition.y;
                int rawIndex = Mathf.RoundToInt(currentY / _stride);
                int clampedIndex = Mathf.Clamp(rawIndex, 0, _totalItems - 1);

                // Nếu Index thay đổi so với frame trước
                if (clampedIndex != _currentVisualIndex)
                {
                    _currentVisualIndex = clampedIndex;
                    OnVisualSelectionChanged?.Invoke(_currentVisualIndex);

                    // KHI KÉO QUA BÀI KHÁC -> FADE OUT NHẠC NGAY
                    if (_isScrolling && GlobalAudioManager.Instance != null)
                    {
                        //GlobalAudioManager.Instance.FadeOutCurrentPreview();
                    }
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isScrolling = true;

            // FIX LỖI 6: Fade Out nhạc ngay khi bắt đầu kéo để tạo cảm giác phản hồi nhanh
            if (GlobalAudioManager.Instance != null)
            {
                //GlobalAudioManager.Instance.OnStartDragging();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isScrolling = false;
            SnapToNearest();
        }

        public void SnapToNearest()
        {
            if (_stride == 0) return;
            ScrollTo(_currentVisualIndex); // Snap vào cái đang hiển thị
        }

        public void ScrollTo(int index)
        {
            index = Mathf.Clamp(index, 0, _totalItems - 1);
            _targetY = index * _stride;

            // Gọi sự kiện "Chốt đơn" -> Lúc này mới Play nhạc
            OnSelectionSettled?.Invoke(index);
        }
    }
}