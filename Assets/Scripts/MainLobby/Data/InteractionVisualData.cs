using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MainLobby.Data
{
    [CreateAssetMenu(fileName = "Main_VisualData", menuName = "MainLobby/Interaction Data")]
    public class InteractionVisualData : ScriptableObject
    {
        [System.Serializable]
        public class InteractionClipSet
        {
            public AnimationClip introClip;
            public AnimationClip loopClip;
            public AnimationClip outroClip;
        }

        [System.Serializable]
        public class VisualPair
        {
            public string npcID;
            public string objectID;
            public InteractionClipSet clipSet;
        }

        // ĐỂ PUBLIC để Tool có thể ghi đè vào đây
        public List<VisualPair> visualPairs = new List<VisualPair>();

        // Dictionary dùng lúc chạy game (Runtime) để tra cứu cho nhanh
        private Dictionary<string, InteractionClipSet> _lookup;

        public void Initialize()
        {
            _lookup = new Dictionary<string, InteractionClipSet>();
            foreach (var pair in visualPairs)
            {
                string key = $"{pair.npcID}_{pair.objectID}";
                if (!_lookup.ContainsKey(key))
                {
                    _lookup.Add(key, pair.clipSet);
                }
            }
        }

        public InteractionClipSet GetOverrideClips(string npcID, string objectTypeID)
        {
            if (_lookup == null) Initialize();

            string key = $"{npcID}_{objectTypeID}";
            if (_lookup.TryGetValue(key, out var clips))
            {
                return clips;
            }
            return new InteractionClipSet(); // Trả về rỗng nếu không tìm thấy
        }

        public bool HasDataFor(string npcID, string objectTypeID)
        {
            if (_lookup == null) Initialize();
            return _lookup.ContainsKey($"{npcID}_{objectTypeID}");
        }
    }
}