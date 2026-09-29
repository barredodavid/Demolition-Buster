using System.Collections.Generic;
using UnityEngine;

namespace DemolitionBuster.Config
{
    /// <summary>Ordered list of destroyers shown in the shop. The first entry is the default character.</summary>
    [CreateAssetMenu(menuName = "Demolition Buster/Destroyer Catalog", fileName = "DestroyerCatalog")]
    public sealed class DestroyerCatalog : ScriptableObject
    {
        [SerializeField] private List<DestroyerDefinition> _destroyers = new List<DestroyerDefinition>();

        public IReadOnlyList<DestroyerDefinition> Destroyers => _destroyers;

        public DestroyerDefinition Find(string id) => _destroyers.Find(d => d != null && d.Id == id);

#if UNITY_EDITOR
        public void EditorSetDestroyers(IEnumerable<DestroyerDefinition> destroyers)
        {
            _destroyers = new List<DestroyerDefinition>(destroyers);
        }

        private void OnValidate()
        {
            var seen = new HashSet<string>();
            foreach (DestroyerDefinition destroyer in _destroyers)
            {
                if (destroyer == null) continue;
                if (string.IsNullOrEmpty(destroyer.Id))
                    Debug.LogError($"Destroyer '{destroyer.name}' has no id.", destroyer);
                else if (!seen.Add(destroyer.Id))
                    Debug.LogError($"Duplicate destroyer id '{destroyer.Id}' in {name}.", this);
            }
        }
#endif
    }
}
