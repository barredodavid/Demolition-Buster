using DemolitionBuster.Core.Progression;
using UnityEngine;

namespace DemolitionBuster.Config
{
    /// <summary>What the strike produces at the tower.</summary>
    public enum StrikeEffect
    {
        /// <summary>Contact hit: spark, concrete dust and chips on the facade.</summary>
        Impact,
        /// <summary>Ranged hit: an energy ball flies from the hand to the facade, then bursts.</summary>
        EnergyBlast,
    }

    /// <summary>
    /// A playable character sold in the shop. Drawn with two illustrated frames (idle / strike) that face right,
    /// i.e. towards the tower when standing on its left.
    /// </summary>
    [CreateAssetMenu(menuName = "Demolition Buster/Destroyer", fileName = "Destroyer")]
    public sealed class DestroyerDefinition : ScriptableObject, IShopItem
    {
        [Tooltip("Stable identifier stored in save files. Never change it once shipped.")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField, Min(0)] private int _price;

        [Header("Frames")]
        [SerializeField] private Sprite _idleFrame;
        [SerializeField] private Sprite _strikeFrame;

        [Header("Strike")]
        [Tooltip("How far the destroyer dashes toward the tower on each hit, in world units.")]
        [SerializeField, Min(0f)] private float _strikeLunge = 0.1f;
        [SerializeField] private StrikeEffect _strikeEffect = StrikeEffect.Impact;
        [Tooltip("Energy blast only: launch point relative to the feet, facing right, in world units.")]
        [SerializeField] private Vector2 _blastOrigin = new Vector2(0.45f, 0.55f);
        [SerializeField] private Color _blastColor = new Color(0.3f, 0.75f, 1f);

        public string Id => _id;
        public int Price => _price;
        public string DisplayName => _displayName;
        public string Description => _description;

        public Sprite IdleFrame => _idleFrame;
        public Sprite StrikeFrame => _strikeFrame != null ? _strikeFrame : _idleFrame;

        public float StrikeLunge => _strikeLunge;
        public StrikeEffect StrikeEffect => _strikeEffect;
        public Vector2 BlastOrigin => _blastOrigin;
        public Color BlastColor => _blastColor;

#if UNITY_EDITOR
        /// <summary>Editor-only setters used by the project setup tool.</summary>
        public void EditorConfigure(string id, string displayName, string description, int price,
            Sprite idleFrame, Sprite strikeFrame, float strikeLunge)
        {
            _id = id;
            _displayName = displayName;
            _description = description;
            _price = price;
            _idleFrame = idleFrame;
            _strikeFrame = strikeFrame;
            _strikeLunge = strikeLunge;
            _strikeEffect = StrikeEffect.Impact;
        }

        public void EditorConfigureEnergyBlast(Vector2 origin, Color color)
        {
            _strikeEffect = StrikeEffect.EnergyBlast;
            _blastOrigin = origin;
            _blastColor = color;
        }
#endif
    }
}
