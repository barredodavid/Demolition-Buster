using System;
using UnityEngine;

namespace DemolitionBuster.Views
{
    /// <summary>
    /// Energy ball thrown by ranged destroyers: a glowing core inside a spinning swirl with flickering
    /// lightning. Travels in a straight line to the target, then reports its arrival so the burst can play.
    /// </summary>
    public sealed class EnergyBlastView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _glow;
        [SerializeField] private SpriteRenderer _core;
        [SerializeField] private SpriteRenderer _swirl;
        [SerializeField] private SpriteRenderer _sparks;

        [Tooltip("World units per second. Fast enough to keep up with rapid taps.")]
        [SerializeField] private float _speed = 12f;
        [SerializeField] private float _swirlSpeed = 900f;
        [Tooltip("Scale reached at launch, as a fraction of the full size: the ball swells as it leaves the hand.")]
        [SerializeField] private float _launchScale = 0.45f;

        private Vector3 _from;
        private Vector3 _to;
        private float _duration;
        private float _age;
        private Action<EnergyBlastView> _onArrived;

        public Vector3 Target => _to;

        public void Launch(Vector3 from, Vector3 to, Color color, Action<EnergyBlastView> onArrived)
        {
            _from = from;
            _to = to;
            _duration = Mathf.Max(0.04f, Vector3.Distance(from, to) / _speed);
            _age = 0f;
            _onArrived = onArrived;

            _glow.color = new Color(color.r, color.g, color.b, 0.85f);
            _swirl.color = color;
            _core.color = Color.Lerp(Color.white, color, 0.25f);
            _sparks.color = Color.Lerp(Color.white, color, 0.5f);

            // The ball faces its direction of travel (swirl trail behind it).
            _swirl.flipX = to.x < from.x;
            Apply(0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _duration);
            Apply(t);
            if (t >= 1f) _onArrived?.Invoke(this);
        }

        private void Apply(float t)
        {
            transform.position = Vector3.Lerp(_from, _to, t);
            transform.localScale = Vector3.one * Mathf.Lerp(_launchScale, 1f, Mathf.Sqrt(t));

            _swirl.transform.localRotation = Quaternion.Euler(0f, 0f, -_swirlSpeed * _age);
            // Lightning flickers: new random orientation every frame.
            _sparks.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            _sparks.flipY = UnityEngine.Random.value > 0.5f;
        }
    }
}
