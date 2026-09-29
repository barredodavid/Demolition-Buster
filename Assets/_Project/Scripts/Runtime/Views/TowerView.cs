using DemolitionBuster.Core;
using DemolitionBuster.Core.Tower;
using UnityEngine;
using UnityEngine.Pool;

namespace DemolitionBuster.Views
{
    /// <summary>
    /// Renders the <see cref="TowerModel"/>. The view never owns gameplay state: after each hit it
    /// re-reads the model, then plays a short "fall by one floor" offset animation to sell the descent.
    /// </summary>
    public sealed class TowerView : MonoBehaviour
    {
        [SerializeField] private BlockView _blockPrefab;
        [SerializeField] private DebrisView _debrisPrefab;
        [SerializeField] private CoinPopView _coinPopPrefab;

        [SerializeField] private float _blockHeight = 1f;
        [SerializeField] private float _blockHalfWidth = 1f;
        [Tooltip("Height of the hit point above the base block center.")]
        [SerializeField] private float _impactHeight = 0.1f;
        [Tooltip("Higher = snappier descent.")]
        [SerializeField] private float _descentSharpness = 28f;
        [Tooltip("Floors alternate between these tints so the descent stays readable.")]
        [SerializeField] private Color[] _floorTints = { Color.white };
        [Tooltip("Every FloorsPerTier floors the facades take the next tint: a visible reward for climbing.")]
        [SerializeField] private Color[] _tierTints =
        {
            Color.white,
            new Color(1f, 0.82f, 0.66f),  // brique
            new Color(0.7f, 0.86f, 1f),   // verre
            new Color(0.8f, 1f, 0.72f),   // mousse
            new Color(1f, 0.74f, 0.88f),  // néon
            new Color(0.82f, 0.76f, 1f),  // nuit
        };
        [Tooltip("Facade variants, picked pseudo-randomly but stable per floor.")]
        [SerializeField] private Sprite[] _floorSprites = new Sprite[0];

        private BlockView[] _blocks = new BlockView[0];
        private float _descentOffset;
        private ObjectPool<DebrisView> _debrisPool;
        private ObjectPool<CoinPopView> _coinPool;

        private void Awake()
        {
            _debrisPool = CreatePool(_debrisPrefab, 4);
            _coinPool = CreatePool(_coinPopPrefab, 2);
        }

        /// <summary>Point of the base block's facade facing <paramref name="side"/>, where strikes land.</summary>
        public Vector3 ImpactPoint(Side side) =>
            transform.position + new Vector3(side.Sign() * _blockHalfWidth, _descentOffset + _impactHeight, 0f);

        public void Build(int blockCount)
        {
            foreach (BlockView block in _blocks) Destroy(block.gameObject);

            _blocks = new BlockView[blockCount];
            for (int i = 0; i < blockCount; i++)
            {
                _blocks[i] = Instantiate(_blockPrefab, transform);
                _blocks[i].name = $"Block_{i:00}";
            }
            Layout();
        }

        /// <summary>Displays the tower instantly, e.g. after a reset.</summary>
        public void Render(TowerModel tower)
        {
            _descentOffset = 0f;
            Refresh(tower);
        }

        /// <summary>Plays the destruction of the former base block, then shows the tower one floor lower.</summary>
        public void PlayBlockDestroyed(TowerModel tower, Block destroyed, Side hitFrom)
        {
            // Absolute floor index of the destroyed block, to keep its tint consistent while it flies away.
            int destroyedFloor = tower.DestroyedCount - 1;
            Vector3 basePosition = transform.position + Vector3.up * _descentOffset;

            DebrisView debris = _debrisPool.Get();
            debris.Launch(destroyed, SpriteForFloor(destroyedFloor), TintForFloor(destroyedFloor), basePosition, -hitFrom.Sign(), _debrisPool.Release);

            if (destroyed.HasCoin)
            {
                CoinPopView coin = _coinPool.Get();
                coin.Play(basePosition, _coinPool.Release);
            }

            _descentOffset += _blockHeight;
            Refresh(tower);
        }

        private void Update()
        {
            if (_descentOffset <= 0f) return;

            // Frame-rate independent exponential decay towards 0.
            _descentOffset = Mathf.Lerp(_descentOffset, 0f, 1f - Mathf.Exp(-_descentSharpness * Time.deltaTime));
            if (_descentOffset < 0.001f) _descentOffset = 0f;
            Layout();
        }

        private void Refresh(TowerModel tower)
        {
            for (int level = 0; level < _blocks.Length && level < tower.Count; level++)
            {
                int floor = tower.DestroyedCount + level;
                _blocks[level].Show(tower[level], SpriteForFloor(floor), TintForFloor(floor));
            }
            Layout();
        }

        private void Layout()
        {
            for (int level = 0; level < _blocks.Length; level++)
            {
                _blocks[level].transform.localPosition = new Vector3(0f, level * _blockHeight + _descentOffset, 0f);
            }
        }

        public const int FloorsPerTier = 50;

        private Color TintForFloor(int floor)
        {
            Color floorTint = _floorTints.Length == 0 ? Color.white : _floorTints[floor % _floorTints.Length];
            Color tierTint = _tierTints.Length == 0 ? Color.white : _tierTints[(floor / FloorsPerTier) % _tierTints.Length];
            return floorTint * tierTint;
        }

        private Sprite SpriteForFloor(int floor)
        {
            if (_floorSprites.Length == 0) return null;
            // Knuth multiplicative hash: looks random, but a floor always gets the same facade (and so does its debris).
            uint hash = unchecked((uint)floor * 2654435761u);
            return _floorSprites[(int)((hash >> 16) % (uint)_floorSprites.Length)];
        }

        private ObjectPool<T> CreatePool<T>(T prefab, int capacity) where T : Component
        {
            return new ObjectPool<T>(
                createFunc: () => Instantiate(prefab, transform.parent),
                actionOnGet: item => item.gameObject.SetActive(true),
                actionOnRelease: item => item.gameObject.SetActive(false),
                actionOnDestroy: item => Destroy(item.gameObject),
                defaultCapacity: capacity);
        }
    }
}
