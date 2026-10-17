using DG.Tweening;
using RhythmCore.Logic;
using TMPro;
using UnityEngine;

namespace RhythmCore.UI
{
    public class GameplayUIManager : MonoBehaviour
    {
        [Header("Text References")]
        public AnimatedNumberDisplay scoreDisplay;
        public AnimatedNumberDisplay comboDisplay;

        private Vector3 _defaultComboScale;

        [Header("Judge Backgrounds")]
        public GameObject judgeBgNormal;
        //public GameObject judgeBgCloud;

        void Start()
        {
            ScoreManager.OnScoreChanged += UpdateScoreUI;
            ScoreManager.OnComboChanged += UpdateComboUI;

            _defaultComboScale = comboDisplay.transform.localScale;
        }

        void OnDestroy()
        {
            ScoreManager.OnScoreChanged -= UpdateScoreUI;
            ScoreManager.OnComboChanged -= UpdateComboUI;
        }

        void UpdateScoreUI(int newScore)
        {
            scoreDisplay.SetValue(newScore);
        }

        void UpdateComboUI(int newCombo)
        {
            if (newCombo > 0)
            {
                comboDisplay.gameObject.SetActive(true);
                comboDisplay.SetValue(newCombo);

                comboDisplay.transform.DOKill();
                comboDisplay.transform.localScale = _defaultComboScale;
                comboDisplay.transform.DOPunchScale(_defaultComboScale * 0.2f, 0.2f);
            }
            else
            {
                comboDisplay.gameObject.SetActive(false);
            }
        }
    }
}