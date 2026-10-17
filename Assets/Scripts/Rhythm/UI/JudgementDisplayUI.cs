using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using RhythmCore.Logic;

namespace RhythmCore.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class JudgementDisplayUI : MonoBehaviour
    {
        [Header("References")]
        public Image judgementImage;

        [Header("Sprites")]
        public Sprite perfectSprite;
        public Sprite greatSprite;
        public Sprite missSprite;

        [Header("Animation Settings")]
        [Tooltip("Đường cong độ nảy. Mẹo: Vẽ nó vọt lên > 1 rồi vòng xuống 1")]
        public AnimationCurve scaleCurve = new AnimationCurve(
            new Keyframe(0, 0.5f),  // Bắt đầu nhỏ
            new Keyframe(0.1f, 1.2f), // Phóng to quá đà (1.2 lần)
            new Keyframe(0.3f, 1.0f)  // Trở về bình thường
        );

        [Range(0, 20)] public float floatSpeed = 5.0f;   // Tốc độ nhấp nhô
        [Range(0, 50)] public float floatAmount = 10.0f; // Biên độ nhấp nhô (pixel)
        [Range(0, 15)] public float tiltAmount = 5.0f;   // Độ nghiêng ngẫu nhiên

        [Header("Timing")]
        public float displayDuration = 2.0f; // Thời gian hiển thị
        public float fadeDuration = 0.5f;    // Thời gian mờ dần

        private CanvasGroup _canvasGroup;
        private Vector3 _originalPos;
        private float _animationTimer;
        private bool _isVisible = false;

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            // Lưu vị trí gốc trên UI để làm mốc nhấp nhô
            _originalPos = judgementImage.transform.localPosition;
        }

        public void ShowJudgement(HitResult result)
        {
            if (result == HitResult.None) return;

            // 1. Chọn Sprite
            Sprite targetSprite = null;
            switch (result)
            {
                case HitResult.Perfect: targetSprite = perfectSprite; break;
                case HitResult.Great: targetSprite = greatSprite; break;
                case HitResult.Miss: targetSprite = missSprite; break;
            }
            if (targetSprite == null) return;

            judgementImage.sprite = targetSprite;
            judgementImage.SetNativeSize(); // Đảm bảo ảnh sắc nét

            // 2. Reset trạng thái Animation (Tạo cú "Punch" mới)
            _canvasGroup.alpha = 1f;
            _animationTimer = 0f;
            _isVisible = true;

            // 3. Hiệu ứng xoay nhẹ ngẫu nhiên (cho đỡ nhàm chán)
            // Xoay từ -5 đến 5 độ
            float randomZ = Random.Range(-tiltAmount, tiltAmount);
            judgementImage.transform.localRotation = Quaternion.Euler(0, 0, randomZ);
        }

        void Update()
        {
            if (!_isVisible) return;

            _animationTimer += Time.deltaTime;

            // --- A. XỬ LÝ SCALE (NẢY NẢY) ---
            // Lấy giá trị từ đường cong AnimationCurve
            float scaleValue = scaleCurve.Evaluate(_animationTimer);
            judgementImage.transform.localScale = new Vector3(scaleValue, scaleValue, 1);


            // --- B. XỬ LÝ FLOAT (HÌNH SIN) ---
            // Công thức: Vị trí gốc + (Sin(thời gian * tốc độ) * biên độ)
            // Cộng thêm _animationTimer vào Time.time để mỗi lần hiện nó không bị giật
            float newY = _originalPos.y + Mathf.Sin(_animationTimer * floatSpeed) * floatAmount;

            judgementImage.transform.localPosition = new Vector3(_originalPos.x, newY, _originalPos.z);


            // --- C. XỬ LÝ FADE OUT (MỜ DẦN) ---
            if (_animationTimer > displayDuration)
            {
                // Bắt đầu tính thời gian fade
                float timeSinceFadeStart = _animationTimer - displayDuration;

                // Tính toán Alpha giảm dần về 0
                float alpha = Mathf.Lerp(1f, 0f, timeSinceFadeStart / fadeDuration);
                _canvasGroup.alpha = alpha;

                // Nếu mờ hết rồi thì tắt luôn cho nhẹ máy
                if (alpha <= 0.01f)
                {
                    _isVisible = false;
                    _canvasGroup.alpha = 0f;
                }
            }
        }
    }
}