using UnityEngine;
using MainLobby.Data;

namespace MainLobby.Core
{
    [DefaultExecutionOrder(-100)] 
    public class LobbyDataManager : MonoBehaviour
    {
        public static LobbyDataManager Instance { get; private set; }

        [Header("Central Database")]
        [Tooltip("Kéo file Main_VisualData (ScriptableObject) vào đây")]
        [SerializeField] private InteractionVisualData mainVisualData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); 

            if (mainVisualData != null)
            {
                mainVisualData.Initialize();
            }
        }

        public InteractionVisualData.InteractionClipSet GetClips(string npcID, string objectTypeID)
        {
            if (mainVisualData == null) return default;
            return mainVisualData.GetOverrideClips(npcID, objectTypeID);
        }

        public InteractionVisualData.InteractionClipSet GetInteractionClips(string npcID, string objID)
        {
            if (mainVisualData != null)
            {
                return mainVisualData.GetOverrideClips(npcID, objID);
            }
            return new InteractionVisualData.InteractionClipSet();
        }

        public bool HasData(string npcID, string objectTypeID)
        {
            if (mainVisualData == null) return false;
            return mainVisualData.HasDataFor(npcID, objectTypeID);
        }
    }
}