using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MainLobby.Data;
using MainLobby.UI;

namespace MainLobby.Logic
{
    public class SongSelectController : MonoBehaviour
    {
        [Header("--- STATIC SLOTS REFERENCES ---")]
        public SongPanelItem topSlot;
        public SongPanelItem centerSlot;
        public SongPanelItem bottomSlot;

        [Header("--- MANAGERS ---")]
        public SongInfoDisplay infoDisplay; // RightZone

        // State Data
        private List<SongEntry> _fullSongList;
        private int _currentIndex = 0;
        private string _currentDiffKey = "hard";

        private bool _isLoaded = false;
        private bool _isActive = false;
        private Coroutine _previewCoroutine;

        public static SongSelectController Instance;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            StartCoroutine(InitSequence());
        }

        private IEnumerator InitSequence()
        {
            while (SongLibraryManager.Instance == null || SongLibraryManager.Instance.GetSongList().Count == 0)
            {
                yield return null;
            }

            _fullSongList = SongLibraryManager.Instance.GetSongList();
            _isLoaded = true;

            // Lần đầu load: Vẽ UI nhưng KHÔNG phát nhạc (để nhạc Lobby chạy)
            UpdateUI(playAudio: false);
        }

        // --- 1. KHI MỞ PANEL (Show) ---
        public void OnPanelOpened()
        {
            _isActive = true;

            if (!_isLoaded || _fullSongList == null) return;

            // Logic quan trọng:
            // Nếu nhạc đang chơi đúng bài này rồi (do từ FinalCheck back về) -> Không cần phát lại
            // Nhưng để đơn giản, ta cứ gọi UpdateUI, GlobalAudio sẽ tự xử lý (hoặc fade lại nhẹ nhàng)
            UpdateUI(playAudio: true);
        }

        // --- 2. KHI ĐÓNG PANEL (Hide) ---
        public void OnPanelClosed()
        {
            _isActive = false;

            // CHỈ DỪNG LỆNH CHỜ (Debounce)
            // Nếu người chơi lướt nhanh quá rồi tắt panel, ta hủy lệnh phát nhạc sắp tới
            if (_previewCoroutine != null) StopCoroutine(_previewCoroutine);

            // QUAN TRỌNG: KHÔNG GỌI StopPreview() Ở ĐÂY!
            // Để nhạc tiếp tục chạy khi qua FinalCheck
        }

        // --- 3. NÚT BACK (Gắn vào Button Back ở góc màn hình) ---
        public void OnBackToLobby()
        {
            // Gọi Manager tắt UI
            if (LobbyUIManager.Instance != null)
            {
                LobbyUIManager.Instance.Back(); // Hàm này sẽ gọi SongSelectPanel.Hide() -> gọi OnPanelClosed()
            }
        }

        // --- 4. CORE UPDATE UI ---
        private void UpdateUI(bool playAudio = true)
        {
            if (!_isLoaded || _fullSongList == null || _fullSongList.Count == 0) return;

            int totalSongs = _fullSongList.Count;

            // --- LOGIC MỚI: TÍNH TOÁN VỊ TRÍ 3 SLOT ---
            int indexTop, indexCenter, indexBottom;
            bool selectTop = false, selectCenter = false, selectBottom = false;

            if (_currentIndex == 0)
            {
                // TRƯỜNG HỢP 1: ĐẦU DANH SÁCH
                // Top là bài đang chọn (0), Center là (1), Bottom là (2)
                indexTop = 0;
                indexCenter = 1;
                indexBottom = 2;
                selectTop = true; // Highlight Top
            }
            else if (_currentIndex == totalSongs - 1 && totalSongs > 2)
            {
                // TRƯỜNG HỢP 2: CUỐI DANH SÁCH
                // Bottom là bài đang chọn (Max), Center là (Max-1), Top là (Max-2)
                indexTop = totalSongs - 3;
                indexCenter = totalSongs - 2;
                indexBottom = totalSongs - 1;
                selectBottom = true; // Highlight Bottom
            }
            else
            {
                // TRƯỜNG HỢP 3: Ở GIỮA (Bình thường)
                // Center là bài đang chọn, Top là (i-1), Bottom là (i+1)
                indexTop = _currentIndex - 1;
                indexCenter = _currentIndex;
                indexBottom = _currentIndex + 1;
                selectCenter = true; // Highlight Center
            }

            // Lấy data an toàn (GetSongSafe sẽ trả về null nếu index âm hoặc lố)
            SongEntry songTop = GetSongSafe(indexTop);
            SongEntry songCenter = GetSongSafe(indexCenter);
            SongEntry songBottom = GetSongSafe(indexBottom);

            // --- SETUP 3 SLOT ---
            // Lưu ý: Logic Click phải thay đổi một chút để phù hợp với vị trí

            // Slot Top
            if (topSlot) topSlot.Setup(songTop, _currentDiffKey, selectTop, () =>
            {
                // Nếu đang ở trường hợp 1 (Top được chọn) thì click vào không làm gì
                if (!selectTop) ChangeSongSelection(indexTop - _currentIndex); // Nhảy tới index của Top
            });

            // Slot Center
            if (centerSlot) centerSlot.Setup(songCenter, _currentDiffKey, selectCenter, () =>
            {
                if (selectCenter) OnSelectedSlotClicked(); // Nếu Center đang được chọn -> Confirm
                else ChangeSongSelection(indexCenter - _currentIndex);
            });

            // Slot Bottom
            if (bottomSlot) bottomSlot.Setup(songBottom, _currentDiffKey, selectBottom, () =>
            {
                if (selectBottom) OnSelectedSlotClicked(); // Nếu Bottom đang được chọn -> Confirm (cho trường hợp cuối list)
                else ChangeSongSelection(indexBottom - _currentIndex);
            });

            // --- CÁC LOGIC KHÁC GIỮ NGUYÊN ---
            SongEntry currentSongEntry = _fullSongList[_currentIndex];

            if (currentSongEntry != null)
            {
                if (GlobalDataManager.Instance)
                    GlobalDataManager.Instance.CurrentSession.SetData(currentSongEntry, _currentDiffKey);

                if (infoDisplay)
                    infoDisplay.UpdateInfo(currentSongEntry);

                if (_isActive && playAudio)
                {
                    HandleAudioPreview(currentSongEntry);
                }
            }
        }

        // --- LOGIC CHUYỂN BÀI ---
        public void ChangeSongSelection(int direction)
        {
            if (!_isActive) return;

            int newIndex = _currentIndex + direction;
            if (newIndex >= 0 && newIndex < _fullSongList.Count)
            {
                _currentIndex = newIndex;
                UpdateUI(playAudio: true);
            }
        }

        private SongEntry GetSongSafe(int index)
        {
            if (index < 0 || index >= _fullSongList.Count) return null;
            return _fullSongList[index];
        }

        public void OnSelectedSlotClicked()
        {
            if (!_isActive) return;
            Debug.Log("Click logic: Requesting Final Check...");
            // Logic vào Final Check
            if (LobbyUIManager.Instance != null)
            {
                LobbyUIManager.Instance.OpenFinalCheck();
            }
            else
            {
                HandleAudioPreview(_fullSongList[_currentIndex], forcePlay: true);
            }
        }

        // --- AUDIO HELPERS ---
        private void HandleAudioPreview(SongEntry song, bool forcePlay = false)
        {
            if (_previewCoroutine != null) StopCoroutine(_previewCoroutine);

            if (forcePlay)
            {
                PlayAudioNow(song);
            }
            else
            {
                _previewCoroutine = StartCoroutine(WaitAndPlayAudio(song));
            }
        }

        private IEnumerator WaitAndPlayAudio(SongEntry song)
        {
            yield return new WaitForSeconds(0.25f);
            if (_isActive)
            {
                PlayAudioNow(song);
            }
        }

        private void PlayAudioNow(SongEntry song)
        {
            if (GlobalAudioManager.Instance != null)
            {
                GlobalAudioManager.Instance.PlayPreview(song);
            }
        }
    }
}