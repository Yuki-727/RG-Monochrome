using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MainLobby.UI
{
    public class TOCItem : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI songNameText;
        [SerializeField] private TextMeshProUGUI pageNumText;
        [SerializeField] private Button btnSelect;

        private int _songIndex;
        private TOCPanel _parentPanel;

        public void Setup(string name, int index, TOCPanel parent)
        {
            songNameText.text = name;
            // +1 để hiển thị trang bắt đầu từ 1 thay vì 0
            pageNumText.text = (index + 1).ToString();

            _songIndex = index;
            _parentPanel = parent;

            btnSelect.onClick.RemoveAllListeners();
            btnSelect.onClick.AddListener(() => {
                _parentPanel.OnSongSelected(_songIndex);
            });
        }
    }
}