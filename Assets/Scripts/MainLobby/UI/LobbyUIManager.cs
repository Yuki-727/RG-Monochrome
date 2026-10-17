using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using MainLobby.Logic; // Uncomment khi ông có AudioManager

namespace MainLobby.UI
{
    public class LobbyUIManager : MonoBehaviour
    {
        public static LobbyUIManager Instance;

        [Header("Global Elements")]
        [SerializeField] private GameObject globalDimmer;
        [SerializeField] private RectTransform whiteCurtain;

        [Header("Registered Panels")]
        [SerializeField] private BasePanel lobbyRootPanel;
        [SerializeField] private BasePanel tocPanel;
        [SerializeField] private BasePanel songSelectPanel;
        [SerializeField] private BasePanel settingsPanel;
        [SerializeField] private BasePanel finalCheckPanel;

        private Stack<BasePanel> _panelHistory = new Stack<BasePanel>();
        private bool _isTransitioning = false;

        // Logic check người chơi đã từng chọn bài chưa
        private bool _hasSelectedSongFromTOC = false;

        private void Awake()
        {
            Instance = this;

            if (globalDimmer != null) globalDimmer.SetActive(false);
            if (whiteCurtain != null) whiteCurtain.gameObject.SetActive(false);

            RegisterPanel(lobbyRootPanel);
            RegisterPanel(tocPanel);
            RegisterPanel(songSelectPanel);
            RegisterPanel(settingsPanel);
            RegisterPanel(finalCheckPanel);

            // Khởi đầu vào Lobby
            lobbyRootPanel.gameObject.SetActive(true);
            _panelHistory.Push(lobbyRootPanel);
        }

        private void Start()
        {
            // Gọi nhạc Lobby ngay khi game start
            UpdateAudioState(lobbyRootPanel);
        }

        private void RegisterPanel(BasePanel panel)
        {
            if (panel == null) return;
            panel.OnRequestClose += HandleCloseRequest;
            if (panel != lobbyRootPanel) panel.Hide();
        }

        // --- PUBLIC FLOW ---

        // Gán vào nút Start ở Lobby
        public void OpenBookSequence()
        {
            if (!_hasSelectedSongFromTOC)
                OpenPanel(tocPanel);
            else
                OpenPanel(songSelectPanel);
        }

        // --- MỚI: Nút chuyển từ SongSelect -> TOC ---
        public void GoToTOCFromSongSelect()
        {
            // Nếu TOC đã có trong lịch sử (người dùng đi từ Lobby -> TOC -> SongSelect)
            // Thì hành động này thực chất là Back
            if (_panelHistory.Contains(tocPanel))
            {
                Back();
            }
            else
            {
                // Nếu người dùng đi thẳng từ Lobby -> SongSelect (do đã chọn bài trước đó)
                // Thì giờ muốn mở TOC -> Open Panel mới
                OpenPanel(tocPanel);
            }
        }

        public void OpenSongSelect() => OpenPanel(songSelectPanel);
        public void OpenSettings() => OpenPanel(settingsPanel);
        public void OpenFinalCheck() => OpenPanel(finalCheckPanel);

        public void MarkSongSelectedFromTOC()
        {
            _hasSelectedSongFromTOC = true;
        }

        // --- CORE UI LOGIC ---

        public void OpenPanel(BasePanel nextPanel)
        {
            if (nextPanel == null || _isTransitioning) return;
            // Tránh mở trùng panel (trừ khi cần thiết)
            if (_panelHistory.Count > 0 && _panelHistory.Peek() == nextPanel) return;

            _isTransitioning = true;
            StartCoroutine(RoutineOpenPanel(nextPanel));
        }

        private IEnumerator RoutineOpenPanel(BasePanel nextPanel)
        {
            // Dimmer logic
            if (_panelHistory.Count > 0 && _panelHistory.Peek() == lobbyRootPanel)
            {
                if (globalDimmer != null) globalDimmer.SetActive(true);
            }

            // Curtain đóng vào
            if (whiteCurtain != null) whiteCurtain.gameObject.SetActive(true);

            // --- XỬ LÝ AUDIO: Dừng nhạc cũ ---
            // (Ví dụ: Đang chuyển cảnh thì fade out nhạc cũ đi cho mượt nếu muốn)

            yield return null; // Hoặc chờ animation curtain đóng hết

            // Ẩn panel cũ
            if (_panelHistory.Count > 0)
            {
                BasePanel currentPanel = _panelHistory.Peek();
                if (!nextPanel.IsOverlay) currentPanel.Hide();
            }

            // Hiện panel mới
            nextPanel.Show();
            _panelHistory.Push(nextPanel);

            // Curtain mở ra
            // (Nếu có animation mở màn thì chờ ở đây)

            _isTransitioning = false;

            // --- XỬ LÝ AUDIO: Bật nhạc mới ---
            UpdateAudioState(nextPanel);
        }

        public void Back()
        {
            if (_panelHistory.Count <= 1 || _isTransitioning) return;

            BasePanel current = _panelHistory.Peek();

            // Nếu đang ở TOC -> Về Lobby
            if (current == tocPanel)
            {
                ReturnToLobby();
                return;
            }

            _isTransitioning = true;
            StartCoroutine(RoutineBack());
        }

        public void ReturnToLobby()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            StartCoroutine(RoutineReturnToRoot());
        }

        private IEnumerator RoutineBack()
        {
            BasePanel currentPanel = _panelHistory.Pop();
            currentPanel.Hide();

            BasePanel previousPanel = _panelHistory.Peek();

            if (whiteCurtain != null)
            {
                // Chớp trắng (tuỳ logic animation của ông)
                yield return null;

                // Nếu về tới Lobby thì tắt Dimmer
                if (previousPanel == lobbyRootPanel)
                {
                    whiteCurtain.gameObject.SetActive(false);
                    if (globalDimmer != null) globalDimmer.SetActive(false);
                }
            }
            else yield return null;

            previousPanel.Show();
            _isTransitioning = false;

            // --- AUDIO UPDATE ---
            UpdateAudioState(previousPanel);
        }

        private IEnumerator RoutineReturnToRoot()
        {
            _panelHistory.Peek().Hide();

            // Xả hết stack chỉ giữ lại cái gốc
            while (_panelHistory.Count > 1)
            {
                _panelHistory.Pop();
            }

            BasePanel root = _panelHistory.Peek(); // Lobby

            if (whiteCurtain != null)
            {
                yield return null;
                whiteCurtain.gameObject.SetActive(false);
                if (globalDimmer != null) globalDimmer.SetActive(false);
            }

            root.Show();
            _isTransitioning = false;

            // --- AUDIO UPDATE ---
            UpdateAudioState(root);
        }

        private void HandleCloseRequest(BasePanel panel)
        {
            if (_panelHistory.Count > 0 && _panelHistory.Peek() == panel)
            {
                Back();
            }
        }

        // --- AUDIO STATE MACHINE ---
        // Hàm này chỉ gọi khi đã chuyển cảnh xong -> Hiệu năng cực tốt
        private void UpdateAudioState(BasePanel currentPanel)
        {
            // GIẢ ĐỊNH: Ông có một script GlobalAudioManager.Instance để điều phối

            if (currentPanel == lobbyRootPanel)
            {
                Debug.Log(">> Đang ở LOBBY -> Play Lobby BGM");
                // GlobalAudioManager.Instance.PlayLobbyBGM();
            }
            else if (currentPanel == tocPanel)
            {
                Debug.Log(">> Đang ở TOC -> Play TOC BGM");
                // GlobalAudioManager.Instance.PlayTOCBGM();
            }
            else if (currentPanel == songSelectPanel)
            {
                Debug.Log(">> Đang ở SongSelect -> Stop BGM (để bài hát preview tự chạy)");
                // GlobalAudioManager.Instance.StopBGM(); 
                // (SongSelectController sẽ tự lo việc phát preview nhạc)
            }
            else if (currentPanel == finalCheckPanel)
            {
                // Giữ nguyên nhạc của SongSelect hoặc làm gì đó
            }
        }
    }
}