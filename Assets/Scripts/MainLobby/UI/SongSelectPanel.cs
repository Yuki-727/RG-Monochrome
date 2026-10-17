using MainLobby.Logic;
using UnityEngine;

namespace MainLobby.UI
{
    public class SongSelectPanel : BasePanel
    {
        public SongSelectController controller;

        [Header("--- FLOATING UI REFERENCE ---")]
        // Kéo cái GameObject "SongSelect_Buttons" ở Hierarchy vào đây
        [SerializeField] private GameObject floatingUIContainer;

        public override void Show()
        {
            base.Show();

            // 1. Logic Controller
            if (controller != null) controller.OnPanelOpened();

            // 2. Bật các nút bấm bay lơ lửng
            if (floatingUIContainer != null)
            {
                floatingUIContainer.SetActive(true);

                // Mẹo: Nếu muốn đẹp, ông có thể gắn CanvasGroup vào floatingUIContainer
                // và Fade In nó từ 0 -> 1 ở đây (dùng DOTween hoặc Coroutine)
            }

            Debug.Log("Song Select: SHOW (Book + Floating UI)");
        }

        public override void Hide()
        {
            base.Hide();

            // 1. Logic Controller
            if (controller != null) controller.OnPanelClosed();

            // 2. Ẩn các nút bấm
            if (floatingUIContainer != null)
            {
                floatingUIContainer.SetActive(false);
            }
        }
    }
}