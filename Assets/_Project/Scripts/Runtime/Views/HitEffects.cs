using System;
using DemolitionBuster.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace DemolitionBuster.Views
{
    /// <summary>
    /// Plays the feedback of a hit on the facade: a flash, concrete dust and chips for contact hits,
    /// or a traveling energy ball and a colored burst for ranged hits. Every effect is data (see
    /// <see cref="Burst"/>), tuned in the Inspector and rendered by pooled <see cref="SpriteBurstView"/>s.
    /// </summary>
    public sealed class HitEffects : MonoBehaviour
    {
        [Serializable]
        public struct Burst
        {
            public Sprite sprite;
            public Color color;
            [Min(0)] public int count;
            public float startScale;
            public float endScale;
            public float duration;
            [Tooltip("Degrees over the lifetime.")] public float spin;
            [Tooltip("Initial speed, world units per second.")] public float speed;
            [Tooltip("Emission cone around the outward direction, in degrees.")] public float spread;
            public float gravityScale;
        }

        [SerializeField] private SpriteBurstView _burstPrefab;
        [SerializeField] private EnergyBlastView _blastPrefab;
        [SerializeField] private int _sortingOrder = 25;

        [Header("Contact hit")]
        [SerializeField] private Burst _impactFlash;
        [SerializeField] private Burst _impactDust;
        [SerializeField] private Burst _impactChips;

        [Header("Energy blast hit (tinted with the destroyer's blast color)")]
        [SerializeField] private Burst _blastFlash;
        [SerializeField] private Burst _blastGlow;
        [SerializeField] private Burst _blastSparks;

        private ObjectPool<SpriteBurstView> _bursts;
        private ObjectPool<EnergyBlastView> _blasts;

        private void Awake()
        {
            _bursts = new ObjectPool<SpriteBurstView>(
                () => Instantiate(_burstPrefab, transform),
                burst => burst.gameObject.SetActive(true),
                burst => burst.gameObject.SetActive(false),
                burst => Destroy(burst.gameObject),
                defaultCapacity: 16);
            _blasts = new ObjectPool<EnergyBlastView>(
                () => Instantiate(_blastPrefab, transform),
                blast => blast.gameObject.SetActive(true),
                blast => blast.gameObject.SetActive(false),
                blast => Destroy(blast.gameObject),
                defaultCapacity: 3);
        }

        /// <summary>Contact hit on the facade. Debris flies back toward the side the hit came from.</summary>
        public void PlayImpact(Vector3 point, Side hitFrom)
        {
            var outward = new Vector2(hitFrom.Sign(), 0.35f).normalized;
            Emit(_impactDust, _impactDust.color, point, outward, _sortingOrder);
            Emit(_impactChips, _impactChips.color, point, outward, _sortingOrder + 1);
            Emit(_impactFlash, _impactFlash.color, point, outward, _sortingOrder + 2);
        }

        public void PlayEnergyBlast(Vector3 from, Vector3 to, Color color)
        {
            EnergyBlastView blast = _blasts.Get();
            blast.Launch(from, to, color, arrived =>
            {
                var outward = new Vector2(Mathf.Sign(from.x - to.x), 0.2f).normalized;
                Emit(_blastGlow, Tint(_blastGlow.color, color), arrived.Target, outward, _sortingOrder);
                Emit(_blastSparks, Tint(_blastSparks.color, color), arrived.Target, outward, _sortingOrder + 1);
                Emit(_blastFlash, Tint(_blastFlash.color, color), arrived.Target, outward, _sortingOrder + 2);
                _blasts.Release(arrived);
            });
        }

        private void Emit(Burst burst, Color color, Vector3 point, Vector2 outward, int sortingOrder)
        {
            if (burst.sprite == null) return;
            for (int i = 0; i < burst.count; i++)
            {
                float angle = UnityEngine.Random.Range(-burst.spread, burst.spread) * 0.5f;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * outward;
                Vector3 velocity = direction * (burst.speed * UnityEngine.Random.Range(0.5f, 1f));
                _bursts.Get().Play(burst, color, point, velocity, sortingOrder, _bursts.Release);
            }
        }

        /// <summary>Keeps the burst's own alpha and brightness, takes the hue from the blast color.</summary>
        private static Color Tint(Color burstColor, Color blastColor) =>
            new Color(burstColor.r * blastColor.r, burstColor.g * blastColor.g, burstColor.b * blastColor.b, burstColor.a);
    }
}
