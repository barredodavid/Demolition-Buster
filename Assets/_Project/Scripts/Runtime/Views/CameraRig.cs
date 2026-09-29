using UnityEngine;

namespace DemolitionBuster.Views
{
    /// <summary>
    /// Keeps the gameplay framing identical on every phone: a fixed world width is always visible
    /// and the bottom of the view is anchored, so extra height on tall screens reveals more tower.
    /// Also owns the screen shake, applied to the child camera so it never fights the framing.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _visibleWorldWidth = 6f;
        [SerializeField] private float _minOrthographicSize = 5f;
        [Tooltip("World Y coordinate of the bottom edge of the screen.")]
        [SerializeField] private float _visibleBottom = -5.2f;

        private float _lastAspect = -1f;
        private float _shakeAmplitude;
        private float _shakeDuration;
        private float _shakeTimeLeft;

        public void Shake(float amplitude, float duration)
        {
            // Never let a small shake cut a bigger one short.
            if (amplitude < _shakeAmplitude * (_shakeTimeLeft / Mathf.Max(_shakeDuration, 0.0001f))) return;
            _shakeAmplitude = amplitude;
            _shakeDuration = duration;
            _shakeTimeLeft = duration;
        }

        private void LateUpdate()
        {
            if (!Mathf.Approximately(_camera.aspect, _lastAspect)) Frame();
            UpdateShake();
        }

        private void Frame()
        {
            _lastAspect = _camera.aspect;
            float size = Mathf.Max(_minOrthographicSize, _visibleWorldWidth * 0.5f / _camera.aspect);
            _camera.orthographicSize = size;
            transform.position = new Vector3(transform.position.x, _visibleBottom + size, transform.position.z);
        }

        private void UpdateShake()
        {
            Vector3 offset = Vector3.zero;
            if (_shakeTimeLeft > 0f)
            {
                _shakeTimeLeft -= Time.deltaTime;
                float strength = _shakeAmplitude * Mathf.Clamp01(_shakeTimeLeft / _shakeDuration);
                offset = (Vector3)(Random.insideUnitCircle * strength);
            }
            _camera.transform.localPosition = new Vector3(offset.x, offset.y, _camera.transform.localPosition.z);
        }
    }
}
