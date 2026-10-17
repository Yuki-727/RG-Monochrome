using UnityEngine;
using UnityEngine.Events;

namespace MainLobby.World
{
    [RequireComponent(typeof(BoxCollider2D))] // Tự động thêm Collider nếu quên
    public class LobbyInteractable : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Color hoverColor = new Color(0.8f, 0.8f, 0.8f); // Màu khi di chuột vào
        private Color _originalColor;
        private SpriteRenderer _renderer;

        [Header("Events")]
        public UnityEvent OnInteract; // Kéo thả chức năng vào đây y chang cái nút Button

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _originalColor = _renderer.color;

            // Đảm bảo Collider là trigger để chuột bắt được
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        // Khi di chuột vào vật thể
        private void OnMouseEnter()
        {
            _renderer.color = hoverColor; // Sáng lên hoặc đổi màu tí cho biết là bấm được
            // Có thể thêm sound "tít" nhẹ ở đây
        }

        // Khi di chuột ra
        private void OnMouseExit()
        {
            _renderer.color = _originalColor;
        }

        // Khi Click chuột trái
        private void OnMouseDown()
        {
            // Kiểm tra xem UI có đang mở không? Nếu đang mở UI thì không cho bấm vật thể
            // (Ông có thể thêm check CanvasGroup.blocksRaycasts ở đây nếu cần)

            Debug.Log("Đã bấm vào: " + gameObject.name);
            OnInteract?.Invoke();
        }
    }
}