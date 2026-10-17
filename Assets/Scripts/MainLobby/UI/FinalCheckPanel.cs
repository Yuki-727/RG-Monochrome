using UnityEngine;
using MainLobby.Logic; // Namespace chứa Controller

namespace MainLobby.UI
{
    public class FinalCheckPanel : BasePanel
    {
        // Kéo FinalCheckController vào đây
        public FinalCheckController controller;

        [Header("--- FLOATING UI REFERENCE ---")]
        // Kéo cái GameObject "SongSelect_Buttons" ở Hierarchy vào đây
        [SerializeField] private GameObject floatingUIContainer;

        public override void Show()
        {
            base.Show();
            Debug.Log("UI: FinalCheck Panel -> SHOW");

            // Bảo controller: "Ê tao hiện rồi, mày update dữ liệu đi"
            if (controller != null) controller.OnPanelOpened();

            if (floatingUIContainer != null)
            {
                floatingUIContainer.SetActive(true);

                // Mẹo: Nếu muốn đẹp, ông có thể gắn CanvasGroup vào floatingUIContainer
                // và Fade In nó từ 0 -> 1 ở đây (dùng DOTween hoặc Coroutine)
            }

        }

        public override void Hide()
        {
            base.Hide();
            // Nếu cần dọn dẹp gì thì gọi controller.OnPanelClosed();


            // 2. Ẩn các nút bấm
            if (floatingUIContainer != null)
            {
                floatingUIContainer.SetActive(false);
            }
        }
    }
}