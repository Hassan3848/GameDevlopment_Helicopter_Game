using HelicopterCombat.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HelicopterCombat.CameraSystem
{
    /// <summary>
    /// Smooth third-person camera with mouse orbit, gentle auto-centering,
    /// cursor management, and collision prevention against scene obstacles.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonHelicopterCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private HelicopterInputReader inputReader;

        [Header("Follow")]
        [SerializeField] private Vector3 pivotOffset = Vector3.zero;
        [SerializeField, Min(1f)] private float distance = 12f;
        [SerializeField, Min(1f)] private float minimumDistance = 3f;
        [SerializeField, Min(0f)] private float positionSmoothTime = 0.12f;

        [Header("Orbit")]
        [SerializeField] private float defaultPitch = 14f;
        [SerializeField] private float minimumPitch = -10f;
        [SerializeField] private float maximumPitch = 55f;
        [SerializeField, Min(0f)] private float lookSensitivity = 0.12f;
        [SerializeField] private bool autoCenterBehindHelicopter = true;
        [SerializeField, Min(0f)] private float autoCenterSpeed = 65f;

        [Header("Collision")]
        [SerializeField] private LayerMask obstacleLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.35f;
        [SerializeField, Min(0f)] private float collisionPadding = 0.20f;

        private readonly RaycastHit[] collisionHits = new RaycastHit[16];
        private float yaw;
        private float pitch;
        private Vector3 followVelocity;

        public void Configure(
            Transform configuredTarget,
            HelicopterInputReader configuredInputReader)
        {
            target = configuredTarget;
            inputReader = configuredInputReader;
        }

        private void Awake()
        {
            if (target != null && inputReader == null)
            {
                inputReader = target.GetComponentInParent<HelicopterInputReader>();
            }
        }

        private void Start()
        {
            if (target != null)
            {
                yaw = target.eulerAngles.y;
            }

            pitch = defaultPitch;
            LockCursor();
        }

        private void Update()
        {
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame &&
                Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            UpdateOrbitAngles();

            Vector3 pivotPosition = target.TransformPoint(pivotOffset);
            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition =
                pivotPosition + orbitRotation * Vector3.back * distance;

            Vector3 collisionAdjustedPosition =
                ResolveCameraCollision(pivotPosition, desiredPosition);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                collisionAdjustedPosition,
                ref followVelocity,
                positionSmoothTime);

            Vector3 lookDirection = pivotPosition - transform.position;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(
                    lookDirection.normalized,
                    Vector3.up);
            }
        }

        private void UpdateOrbitAngles()
        {
            if (inputReader == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 lookInput = inputReader.Look;

            yaw += lookInput.x * lookSensitivity;
            pitch -= lookInput.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);

            if (autoCenterBehindHelicopter && lookInput.sqrMagnitude < 0.01f)
            {
                yaw = Mathf.MoveTowardsAngle(
                    yaw,
                    target.eulerAngles.y,
                    autoCenterSpeed * Time.deltaTime);
            }
        }

        private Vector3 ResolveCameraCollision(
            Vector3 pivotPosition,
            Vector3 desiredPosition)
        {
            Vector3 castDirection = desiredPosition - pivotPosition;
            float castDistance = castDirection.magnitude;

            if (castDistance <= 0.01f)
            {
                return desiredPosition;
            }

            castDirection /= castDistance;

            int hitCount = Physics.SphereCastNonAlloc(
                pivotPosition,
                collisionRadius,
                castDirection,
                collisionHits,
                castDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            float closestValidHitDistance = float.PositiveInfinity;

            for (int index = 0; index < hitCount; index++)
            {
                Collider hitCollider = collisionHits[index].collider;

                if (hitCollider == null || IsPartOfTarget(hitCollider.transform))
                {
                    continue;
                }

                closestValidHitDistance = Mathf.Min(
                    closestValidHitDistance,
                    collisionHits[index].distance);
            }

            if (float.IsPositiveInfinity(closestValidHitDistance))
            {
                return desiredPosition;
            }

            float safeDistance = Mathf.Clamp(
                closestValidHitDistance - collisionPadding,
                minimumDistance,
                castDistance);

            return pivotPosition + castDirection * safeDistance;
        }

        private bool IsPartOfTarget(Transform candidate)
        {
            return candidate == target || candidate.IsChildOf(target) || target.IsChildOf(candidate);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
