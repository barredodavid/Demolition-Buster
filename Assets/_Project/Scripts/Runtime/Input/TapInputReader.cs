using System;
using DemolitionBuster.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace DemolitionBuster.InputHandling
{
    /// <summary>
    /// Turns raw input into "tap left / tap right".
    /// Uses EnhancedTouch so every finger counts: players alternate two thumbs very fast,
    /// and a primary-touch-only binding would drop the taps that overlap.
    /// Mouse and keyboard (arrows / A-D) are supported for the editor and desktop builds.
    /// </summary>
    public sealed class TapInputReader : MonoBehaviour
    {
        public event Action<Side> SideTapped;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            ETouch.onFingerDown += HandleFingerDown;
        }

        private void OnDisable()
        {
            // The input system may already be torn down (application quit, test fixtures): nothing left to release.
            if (!EnhancedTouchSupport.enabled) return;
            ETouch.onFingerDown -= HandleFingerDown;
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Emit(Side.Left);
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Emit(Side.Right);
            }

            // Touch devices are handled by EnhancedTouch; reading the mouse too would double count simulated clicks.
            Mouse mouse = Mouse.current;
            if (Touchscreen.current == null && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                EmitScreenPosition(mouse.position.ReadValue());
            }
        }

        private void HandleFingerDown(Finger finger) => EmitScreenPosition(finger.screenPosition);

        private void EmitScreenPosition(Vector2 screenPosition)
        {
            Emit(screenPosition.x < Screen.width * 0.5f ? Side.Left : Side.Right);
        }

        private void Emit(Side side) => SideTapped?.Invoke(side);
    }
}
