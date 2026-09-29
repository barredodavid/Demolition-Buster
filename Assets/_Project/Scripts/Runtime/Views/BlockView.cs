using DemolitionBuster.Core;
using DemolitionBuster.Core.Tower;
using UnityEngine;

namespace DemolitionBuster.Views
{
    /// <summary>Visual of a single floor: the concrete block, its optional hazards and coin.</summary>
    public sealed class BlockView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _floor;
        [SerializeField] private SpriteRenderer _hazardLeft;
        [SerializeField] private SpriteRenderer _hazardRight;
        [SerializeField] private SpriteRenderer _coin;

        private Color _floorTint = Color.white;

        public bool HasCoinVisible => _coin.enabled;
        public Vector3 CoinPosition => _coin.transform.position;

        /// <param name="floorSprite">Facade variant; null keeps the current one.</param>
        public void Show(Block block, Sprite floorSprite, Color floorTint)
        {
            if (floorSprite != null) _floor.sprite = floorSprite;
            _floorTint = floorTint;
            _hazardLeft.enabled = block.Hazard == Side.Left;
            _hazardRight.enabled = block.Hazard == Side.Right;
            _coin.enabled = block.HasCoin;
            SetAlpha(1f);
        }

        public void HideCoin() => _coin.enabled = false;

        public void SetAlpha(float alpha)
        {
            _floor.color = WithAlpha(_floorTint, alpha);
            _hazardLeft.color = WithAlpha(Color.white, alpha);
            _hazardRight.color = WithAlpha(Color.white, alpha);
            _coin.color = WithAlpha(Color.white, alpha);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
