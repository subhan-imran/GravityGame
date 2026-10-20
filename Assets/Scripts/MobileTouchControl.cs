using UnityEngine;
using UnityEngine.EventSystems;

namespace GravityGame
{
    public enum MobileControlKind { Joystick, Look, Jump }

    // Each control owns its initiating pointer, so jumping or looking cannot steal the movement finger.
    public sealed class MobileTouchControl : MonoBehaviour, IPointerDownHandler, IDragHandler,
        IPointerUpHandler, IEndDragHandler, ICancelHandler
    {
        public MobileControlKind kind;
        public SphericalPlayer player;
        public PlanetCamera cameraRig;
        public RectTransform knob;
        [Range(0f, 0.5f)] public float deadZone = 0.12f;
        public Vector2 Value { get; private set; }
        public bool HasPointer => owner != int.MinValue;
        private int owner = int.MinValue;

        public void OnPointerDown(PointerEventData data)
        {
            if (HasPointer || player == null || player.HasLost || player.IsPaused || data.button != PointerEventData.InputButton.Left) return;
            owner = data.pointerId;
            if (kind == MobileControlKind.Joystick) MoveStick(data);
            if (kind == MobileControlKind.Jump) player.RequestJump();
        }

        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != owner || player == null || player.HasLost || player.IsPaused) return;
            if (kind == MobileControlKind.Joystick) MoveStick(data);
            if (kind == MobileControlKind.Look && cameraRig != null)
            {
                var canvas = GetComponentInParent<Canvas>();
                cameraRig.AddTouchLook(data.delta / Mathf.Max(0.01f, canvas != null ? canvas.scaleFactor : 1f));
            }
        }

        private void MoveStick(PointerEventData data)
        {
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out Vector2 local)) return;
            float radius = Mathf.Max(1f, Mathf.Min(rect.rect.width, rect.rect.height) * 0.5f);
            Vector2 raw = Vector2.ClampMagnitude(local / radius, 1f);
            float magnitude = raw.magnitude;
            Value = magnitude <= deadZone ? Vector2.zero : raw.normalized * ((magnitude - deadZone) / (1f - deadZone));
            if (knob != null) knob.anchoredPosition = raw * radius * 0.65f;
            player.SetTouchMovement(Value);
        }

        public void OnPointerUp(PointerEventData data) { if (data.pointerId == owner) ResetInput(); }
        public void OnEndDrag(PointerEventData data) { if (data.pointerId == owner) ResetInput(); }
        public void OnCancel(BaseEventData data) => ResetInput();
        private void OnDisable() => ResetInput();

        public void ResetInput()
        {
            owner = int.MinValue;
            Value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (kind == MobileControlKind.Joystick && player != null) player.SetTouchMovement(Vector2.zero);
        }
    }
}
