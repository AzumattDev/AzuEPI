/*using UnityEngine;
using UnityEngine.EventSystems;

namespace AzuEPI.PlayerPreview
{
    public class RealPlayerRotationController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public static RealPlayerRotationController instance;

        [Header("Target Orbit (user-controlled)")]
        public float yawDelta = 0f; // delta from player facing at open

        public float pitch = 10f; // degrees
        public float minPitch = -80f;
        public float maxPitch = 80f;

        [Header("Zoom")] public float distance = 3.0f;
        public float minDistance = 2f;
        public float maxDistance = 15.0f; // << more zoom-out headroom

        [Header("Vertical Pan (pivot offset from chest)")]
        public float pivotYOffset = 0f; // meters relative to ~chest height

        public float minPivotY = -0.8f; // look lower
        public float maxPivotY = +1.0f; // look higher (face/head)

        [Header("Sensitivity")] public float yawPerPixel = 0.25f;
        public float pitchPerPixel = 0.25f;
        public float zoomPerScroll = 2.5f; // smoother than 12f
        public float panPerPixel = 0.01f; // vertical pan speed (meters per pixel)

        // Internal cached on open
        private float _baseYaw; // player's facing yaw at inventory open
        private bool _dragging;

        private void Awake() => instance = this;

        /// Call this on inventory open so the camera starts in FRONT of the player.
        public void InitializeFromPlayer()
        {
            var p = Player.m_localPlayer;
            if (!p) return;

            // Extract the yaw from player rotation (ignore pitch/roll)
            Vector3 fwd = p.transform.forward;
            _baseYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg; // 0deg = +Z world, increases to the right
            yawDelta = 0f; // face the player
            pitch = Mathf.Clamp(10f, minPitch, maxPitch);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            pivotYOffset = Mathf.Clamp(pivotYOffset, minPivotY, maxPivotY);

            ApplyToCamera();
        }

        private static Vector3 GetBasePivot()
        {
            var p = Player.m_localPlayer;
            if (!p) return Vector3.zero;
            return p.transform.position + Vector3.up * 1.25f; // chest/shoulder height
        }

        /// Apply current yaw/pitch/distance/pan to the preview camera.
        public void ApplyToCamera()
        {
            var panel = AzuEPICharacterPanel.instance;
            var cam = panel ? panel.cam : null;
            var p = Player.m_localPlayer;
            if (!cam || !p) return;

            // Build orbit around player's facing (base yaw) plus user delta
            float yaw = _baseYaw + yawDelta;

            Quaternion qYaw = Quaternion.AngleAxis(yaw, Vector3.up);
            Quaternion qPitch = Quaternion.AngleAxis(Mathf.Clamp(pitch, minPitch, maxPitch), Vector3.right);
            Quaternion rot = qYaw * qPitch;

            Vector3 pivot = GetBasePivot() + Vector3.up * Mathf.Clamp(pivotYOffset, minPivotY, maxPivotY);

            float d = Mathf.Clamp(distance, minDistance, maxDistance);
            Vector3 camOffset = rot * (Vector3.forward * d);
            Vector3 camPos = pivot + camOffset;

            cam.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(pivot - camPos, Vector3.up));
        }

        private bool PointerOverRender()
        {
            var panel = AzuEPICharacterPanel.instance;
            if (!panel || !panel.render) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(panel.render, ZInput.mousePosition, null);
        }

        // --- UI input ---

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (PointerOverRender()) _dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || !PointerOverRender()) return;

            // MMB (or Shift+LMB) = vertical pan; otherwise orbit
            bool panMode = Input.GetMouseButton(2) || (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0));

            if (panMode)
            {
                // Up drag -> raise camera pivot; Down drag -> lower pivot
                pivotYOffset += eventData.delta.y * panPerPixel;
                pivotYOffset = Mathf.Clamp(pivotYOffset, minPivotY, maxPivotY);
            }
            else
            {
                // Orbit
                yawDelta += eventData.delta.x * yawPerPixel;
                pitch += eventData.delta.y * pitchPerPixel;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            ApplyToCamera();
            ActualPlayerPreview.RenderOnce();
        }

        public void OnEndDrag(PointerEventData eventData) => _dragging = false;

        public void OnScroll(PointerEventData eventData)
        {
            if (!PointerOverRender()) return;

            float k = 1f + (distance * 0.25f); // zoom speed scaling factor;
            distance -= eventData.scrollDelta.y * zoomPerScroll * k;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            ApplyToCamera();
            ActualPlayerPreview.RenderOnce();
        }

        public void ResetView(float newYawDelta = 0f, float newPitch = 10f, float newDistance = 3.0f, float newPivotY = 0f)
        {
            yawDelta = newYawDelta;
            pitch = Mathf.Clamp(newPitch, minPitch, maxPitch);
            distance = Mathf.Clamp(newDistance, minDistance, maxDistance);
            pivotYOffset = Mathf.Clamp(newPivotY, minPivotY, maxPivotY);
            InitializeFromPlayer(); // recompute base yaw & apply
        }
    }
}*/