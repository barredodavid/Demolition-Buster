using System;
using UnityEngine;

namespace DemolitionBuster.Views
{
    /// <summary>Short "coin collected" feedback: the coin jumps up, grows and fades out.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CoinPopView : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.55f;
        [SerializeField] private float _rise = 1.6f;
        [SerializeField] private float _endScale = 1.6f;

        private SpriteRenderer _renderer;
        private Vector3 _start;
        private float _age;
        private Action<CoinPopView> _onFinished;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Play(Vector3 position, Action<CoinPopView> onFinished)
        {
            _start = position;
            _age = 0f;
            _onFinished = onFinished;
            Apply(0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _duration);
            Apply(t);
            if (t >= 1f) _onFinished?.Invoke(this);
        }

        private void Apply(float t)
        {
            float easeOut = 1f - (1f - t) * (1f - t);
            transform.position = _start + Vector3.up * (_rise * easeOut);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, _endScale, easeOut);
            _renderer.color = new Color(1f, 1f, 1f, 1f - t * t);
        }
    }
}
