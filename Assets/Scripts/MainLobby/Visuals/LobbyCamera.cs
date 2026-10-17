using UnityEngine;
using UnityEngine.InputSystem;

namespace MainLobby.Visuals
{
    public class LobbyCamera : MonoBehaviour
    {
        [Header("Parallax Settings")]
        [SerializeField] private float movementStrength = 0.5f;
        [SerializeField] private float smoothTime = 0.5f;

        [Header("Focus Settings")]
        [SerializeField] private bool isFocused = false;
        [SerializeField] private Vector3 focusTarget;
        [SerializeField] private float focusZoomSize = 3.5f;

        [Header("Breathing Settings")]
        [SerializeField] private float breathSpeed = 0.3f;
        [SerializeField] private float breathAmount = 0.1f;

        private Vector3 _defaultPosition;
        private float _defaultSize;
        private Camera _cam;
        private Vector3 _velocity = Vector3.zero; // Biến này đẻ ra NaN nếu không quản lý kỹ

        Vector3 mouseViewport = Vector3.zero;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            _defaultPosition = transform.position;
            _defaultSize = _cam.orthographicSize;
        }

        private void Update()
        {
            // 1. Chặn lỗi nếu smoothTime bị set = 0 hoặc frame bị đứng
            if (Time.deltaTime <= 0.0001f || smoothTime <= 0f) return;

            // 2. Kiểm tra velocity có bị hỏng (NaN) không. Nếu có, reset về 0 ngay.
            if (float.IsNaN(_velocity.x) || float.IsNaN(_velocity.y) || float.IsNaN(_velocity.z))
            {
                _velocity = Vector3.zero;
            }

            if (isFocused)
            {
                HandleFocusMode();
            }
            else
            {
                HandleParallaxMode();
            }
        }

        private void HandleParallaxMode()
        {
            Vector3 targetPos = _defaultPosition;

            // Chỉ tính parallax khi màn hình hợp lệ
            if (Screen.width > 0 && Screen.height > 0)
            {
                if (Pointer.current != null)
                {
                    Vector2 screenPosition = Pointer.current.position.ReadValue();
                    mouseViewport = _cam.ScreenToViewportPoint(screenPosition);
                }

                // Kẹp giá trị chuột lại cho chắc ăn (tránh alt-tab chuột văng ra ngoài)
                float clampedX = Mathf.Clamp01(mouseViewport.x);
                float clampedY = Mathf.Clamp01(mouseViewport.y);

                float offsetX = (clampedX - 0.5f) * movementStrength;
                float offsetY = (clampedY - 0.5f) * movementStrength;
                float breathY = Mathf.Sin(Time.time * breathSpeed) * breathAmount;

                targetPos = new Vector3(
                    _defaultPosition.x + offsetX,
                    _defaultPosition.y + offsetY + breathY,
                    _defaultPosition.z
                );
            }

            if (!IsValidVector(targetPos))
            {
                // Nếu đích đến bị lỗi, giữ nguyên vị trí cũ, đừng di chuyển
                return;
            }

            // Di chuyển
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);

            // Zoom
            if (Mathf.Abs(_cam.orthographicSize - _defaultSize) > 0.01f)
                _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _defaultSize, Time.deltaTime * 5f);
        }

        private void HandleFocusMode()
        {
            Vector3 targetPos = new Vector3(focusTarget.x, focusTarget.y, _defaultPosition.z);

            if (IsValidVector(targetPos))
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime * 2);
            }

            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, focusZoomSize, Time.deltaTime * 3f);
        }

        private bool IsValidVector(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
                   !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
        }

        public void FocusOn(Vector3 target) { isFocused = true; focusTarget = target; }
        public void ResetCamera() { isFocused = false; }
    }
}