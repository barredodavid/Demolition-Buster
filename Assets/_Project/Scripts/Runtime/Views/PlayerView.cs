using DemolitionBuster.Config;
using DemolitionBuster.Core;
using UnityEngine;

namespace DemolitionBuster.Views
{
    /// <summary>
    /// The destroyer standing next to the tower. Frames are drawn facing right (towards the tower
    /// when standing on the left); the visual root is mirrored for the right side.
    /// On each hit the strike frame is held briefly while the character dashes toward the tower.
    /// </summary>
    public sealed class PlayerView : MonoBehaviour
    {
        [Header("Rig")]
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private SpriteRenderer _frame;

        [Header("Layout")]
        [Tooltip("Horizontal distance from the tower axis.")]
        [SerializeField] private float _sideOffset = 2f;

        [Header("Strike")]
        [SerializeField] private float _strikeDuration = 0.18f;
        [Tooltip("Share of the strike spent dashing in, then holding the impact pose, before recovering.")]
        [SerializeField, Range(0.05f, 0.5f)] private float _swingPortion = 0.2f;
        [SerializeField, Range(0f, 0.6f)] private float _holdPortion = 0.35f;
        [Tooltip("The strike frame stays visible while the strike curve is above this value.")]
        [SerializeField, Range(0f, 1f)] private float _strikeFrameThreshold = 0.25f;

        [Header("Death")]
        [SerializeField] private float _deathDuration = 0.45f;
        [SerializeField] private Color _deathTint = new Color(0.55f, 0.55f, 0.6f);

        private DestroyerDefinition _destroyer;
        private Side _side = Side.Left;
        private float _strikeTime = float.MaxValue;
        private float _deathTime = -1f;

        public void Apply(DestroyerDefinition destroyer)
        {
            _destroyer = destroyer;
            _frame.sprite = destroyer.IdleFrame;
            _frame.color = Color.white;
        }

        public void ResetTo(Side side)
        {
            _deathTime = -1f;
            _strikeTime = float.MaxValue;
            _visualRoot.localRotation = Quaternion.identity;
            _visualRoot.localPosition = Vector3.zero;
            _frame.color = Color.white;
            if (_destroyer != null) _frame.sprite = _destroyer.IdleFrame;
            MoveTo(side);
        }

        public void MoveTo(Side side)
        {
            if (side == Side.None) return;
            _side = side;
            transform.localPosition = new Vector3(side.Sign() * _sideOffset, transform.localPosition.y, transform.localPosition.z);
            _visualRoot.localScale = new Vector3(Facing, 1f, 1f);
        }

        /// <summary>+1 when facing right. The frames look right, so the right-side destroyer is mirrored to face the tower.</summary>
        private float Facing => _side == Side.Right ? -1f : 1f;

        public void PlayStrike() => _strikeTime = 0f;

        public void PlayDeath() => _deathTime = 0f;

        /// <summary>World position of the energy blast launch point, at the impact pose.</summary>
        public Vector3 BlastOrigin()
        {
            Vector2 origin = _destroyer != null ? _destroyer.BlastOrigin : Vector2.zero;
            float lunge = _destroyer != null ? _destroyer.StrikeLunge : 0f;
            return transform.position + new Vector3(Facing * (origin.x + lunge), origin.y, 0f);
        }

        private void Update()
        {
            if (_deathTime >= 0f)
            {
                AnimateDeath();
                return;
            }

            AnimateStrikeAndIdle();
        }

        private void AnimateStrikeAndIdle()
        {
            _strikeTime += Time.deltaTime;
            float strike = StrikeCurve(_strikeTime / _strikeDuration);
            float lunge = _destroyer != null ? _destroyer.StrikeLunge : 0f;

            // Dash toward the tower with a small squash on impact, subtle breathing when idle.
            float breathing = Mathf.Sin(Time.time * 4f) * 0.015f;
            _visualRoot.localPosition = new Vector3(Facing * lunge * strike, 0f, 0f);
            _visualRoot.localScale = new Vector3(Facing * (1f + 0.06f * strike), 1f + breathing - 0.06f * strike, 1f);

            if (_destroyer != null)
            {
                _frame.sprite = strike > _strikeFrameThreshold ? _destroyer.StrikeFrame : _destroyer.IdleFrame;
            }
        }

        /// <summary>
        /// 0 = idle pose, 1 = impact pose. Fast ease-out dash, short hold on the impact so the hit reads
        /// even at high tap rates, then a smooth recovery.
        /// </summary>
        private float StrikeCurve(float t)
        {
            if (t >= 1f) return 0f;
            if (t < _swingPortion)
            {
                float swing = t / _swingPortion;
                return 1f - (1f - swing) * (1f - swing);
            }
            if (t < _swingPortion + _holdPortion) return 1f;

            float recover = (t - _swingPortion - _holdPortion) / Mathf.Max(0.0001f, 1f - _swingPortion - _holdPortion);
            return 1f - recover * recover * (3f - 2f * recover);
        }

        private void AnimateDeath()
        {
            _deathTime += Time.deltaTime;
            float t = Mathf.Clamp01(_deathTime / _deathDuration);
            float easeOut = 1f - (1f - t) * (1f - t);

            // Knocked over, away from the tower (positive Z = counter-clockwise = falling towards -X).
            _visualRoot.localRotation = Quaternion.Euler(0f, 0f, -_side.Sign() * 85f * easeOut);
            _visualRoot.localPosition = new Vector3(-Facing * 0.35f * easeOut, 0f, 0f);
            _frame.color = Color.Lerp(Color.white, _deathTint, t);
        }
    }
}
