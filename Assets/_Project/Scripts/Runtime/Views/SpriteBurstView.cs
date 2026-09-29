using System;
using UnityEngine;

namespace DemolitionBuster.Views
{
    /// <summary>
    /// One short-lived effect sprite (flash, dust puff, chip...): grows, spins, drifts and fades out.
    /// Pooled and driven by <see cref="HitEffects"/>, so one prefab serves every kind of burst.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteBurstView : MonoBehaviour
    {
        [SerializeField] private float _drag = 5f;
        [SerializeField] private float _gravity = -4f;

        private SpriteRenderer _renderer;
        private HitEffects.Burst _settings;
        private Color _color;
        private Vector3 _velocity;
        private float _startAngle;
        private float _age;
        private Action<SpriteBurstView> _onFinished;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Play(HitEffects.Burst settings, Color color, Vector3 position, Vector3 velocity, int sortingOrder,
            Action<SpriteBurstView> onFinished)
        {
            _settings = settings;
            _color = color;
            _velocity = velocity;
            _startAngle = UnityEngine.Random.Range(0f, 360f);
            _age = 0f;
            _onFinished = onFinished;
            _renderer.sprite = settings.sprite;
            _renderer.sortingOrder = sortingOrder;
            transform.position = position;
            Apply(0f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            _velocity += Vector3.up * (_gravity * _settings.gravityScale * dt);
            _velocity *= Mathf.Exp(-_drag * dt);
            transform.position += _velocity * dt;

            float t = Mathf.Clamp01(_age / Mathf.Max(0.0001f, _settings.duration));
            Apply(t);
            if (t >= 1f) _onFinished?.Invoke(this);
        }

        private void Apply(float t)
        {
            float easeOut = 1f - (1f - t) * (1f - t) * (1f - t);
            transform.localScale = Vector3.one * Mathf.Lerp(_settings.startScale, _settings.endScale, easeOut);
            transform.rotation = Quaternion.Euler(0f, 0f, _startAngle + _settings.spin * t);
            Color color = _color;
            color.a *= 1f - t * t;
            _renderer.color = color;
        }
    }
}
