using UnityEngine;
using UnityEngine.UI;
using System;

namespace MainLobby.UI
{
    // Class trừu tượng, các panel con sẽ kế thừa thằng này
    public class BasePanel : MonoBehaviour
    {
        [Header("Base Settings")]
        [SerializeField] protected bool isOverlay = false; // Nếu true: Không tắt panel cũ khi mở panel này (ví dụ Popup)
        [SerializeField] protected Button backButton; // Kéo nút Back vào đây (nếu có)

        // Sự kiện để báo cho UIManager biết
        public event Action<BasePanel> OnRequestClose;

        protected virtual void Awake()
        {
            // Tự động gán chức năng cho nút Back (nếu có)
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        // Mỗi Panel con sẽ tự định nghĩa cách nó hiện ra (Trượt, Fade, hay Bật cái rụp)
        public virtual void Show()
        {
            gameObject.SetActive(true);
            // Animation code mặc định (có thể override)
        }

        public virtual void Hide()
        {
            // Animation code mặc định
            gameObject.SetActive(false);
        }

        protected virtual void OnBackButtonClicked()
        {
            // Gửi tín hiệu: "Ê Manager, tao muốn đóng!"
            OnRequestClose?.Invoke(this);
        }

        // Check xem panel này có phải dạng popup đè lên không
        public bool IsOverlay => isOverlay;
    }
}