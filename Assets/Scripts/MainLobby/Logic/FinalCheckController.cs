using MainLobby.UI; // Để thấy SongInfoDisplay
using MainLobby.Data; // Để thấy GlobalDataManager
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MainLobby.Logic
{
    public class FinalCheckController : MonoBehaviour
    {
        public static FinalCheckController Instance; // Thêm Singleton để Panel dễ gọi

        [Header("--- REFERENCES ---")]
        public SongInfoDisplay infoDisplay; // Kéo cái Prefab RightZone vào đây

        [Header("--- SETTINGS ---")]
        public string gameSceneName = "RhythmGameScene";

        private void Awake() { Instance = this; }

        // Được gọi từ FinalCheckPanel khi Show()
        public void OnPanelOpened()
        {
            RefreshData();
        }

        private void RefreshData()
        {
            // 1. Kiểm tra Vali
            if (GlobalDataManager.Instance == null) return;

            var session = GlobalDataManager.Instance.CurrentSession;
            if (session == null || session.SelectedChart == null) return;

            // 2. Đẩy data vào SongInfoDisplay
            if (infoDisplay != null)
            {
                infoDisplay.UpdateInfo(session.SelectedSong);
            }
        }

        // --- BUTTON EVENTS ---

        public void OnStartClicked()
        {
            Debug.Log("FinalCheck: >>> START GAME <<<");

            // Tắt nhạc trước khi đi
            if (GlobalAudioManager.Instance) GlobalAudioManager.Instance.StopAllMusic();

            // Chuyển Scene
            SceneManager.LoadScene(gameSceneName);
        }

        public void OnBackClicked()
        {
            Debug.Log("FinalCheck: <<< BACK");

            // Gọi UI Manager quay lại trang trước (SongSelect)
            if (LobbyUIManager.Instance) LobbyUIManager.Instance.Back();
        }
    }
}