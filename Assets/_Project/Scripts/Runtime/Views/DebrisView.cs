using System;
using DemolitionBuster.Core.Tower;
using UnityEngine;

namespace DemolitionBuster.Views
{
    /// <summary>
    /// A destroyed block flying off screen. Reuses <see cref="BlockView"/> for its visuals and runs a
    /// tiny hand-rolled ballistic simulation: cheaper and more controllable than Rigidbody2D for pure juice.
    /// </summary>
    [RequireComponent(typeof(BlockView))]
    public sealed class DebrisView : MonoBehaviour
    {
        [SerializeField] private float _lifetime = 0.7f;
        [SerializeField] private float _gravity = -30f;
        [SerializeField] private Vector2 _horizontalSpeed = new Vector2(7f, 10f);
        [SerializeField] private Vector2 _verticalSpeed = new Vector2(3f, 6f);
        [SerializeField] private Vector2 _angularSpeed = new Vector2(250f, 500f);

        private BlockView _blockView;
        private Vector3 _velocity;
        private float _angularVelocity;
        private float _age;
        private Action<DebrisView> _onFinished;

        private void Awake()
        {
            _blockView = GetComponent<BlockView>();
        }

        /// <param name="direction">-1 flies left, +1 flies right.</param>
        public void Launch(Block block, Sprite floorSprite, Color floorTint, Vector3 position, int direction, Action<DebrisView> onFinished)
        {
            _blockView.Show(block, floorSprite, floorTint);
            _blockView.HideCoin(); // The coin is collected separately, it does not fly away with the block.

            transform.SetPositionAndRotation(position, Quaternion.identity);
            _velocity = new Vector3(direction * Random(_horizontalSpeed), Random(_verticalSpeed), 0f);
            _angularVelocity = -direction * Random(_angularSpeed);
            _age = 0f;
            _onFinished = onFinished;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            _velocity.y += _gravity * dt;
            transform.position += _velocity * dt;
            transform.Rotate(0f, 0f, _angularVelocity * dt);
            _blockView.SetAlpha(1f - Mathf.Clamp01(_age / _lifetime));

            if (_age >= _lifetime) _onFinished?.Invoke(this);
        }

        private static float Random(Vector2 range) => UnityEngine.Random.Range(range.x, range.y);
    }
}
