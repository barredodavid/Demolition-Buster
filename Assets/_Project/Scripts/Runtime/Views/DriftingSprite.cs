using UnityEngine;

namespace DemolitionBuster.Views
{
    /// <summary>
    /// Slides a backdrop element (a cloud) sideways forever, wrapping around a world-space range.
    /// Keeps the menu and the game alive without any gameplay cost.
    /// </summary>
    public sealed class DriftingSprite : MonoBehaviour
    {
        [Tooltip("World units per second. Negative drifts to the left.")]
        [SerializeField] private float _speed = 0.1f;
        [Tooltip("The element wraps around between these X positions: keep them outside the widest supported view.")]
        [SerializeField] private float _minX = -5.5f;
        [SerializeField] private float _maxX = 5.5f;

        private void Update()
        {
            Vector3 position = transform.position;
            position.x = _minX + Mathf.Repeat(position.x + _speed * Time.deltaTime - _minX, _maxX - _minX);
            transform.position = position;
        }
    }
}
