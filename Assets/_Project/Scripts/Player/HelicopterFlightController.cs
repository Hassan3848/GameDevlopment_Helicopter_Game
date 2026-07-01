using UnityEngine;

namespace HelicopterCombat.Player
{
    /// <summary>
    /// Applies smooth Rigidbody-based helicopter movement and yaw rotation.
    /// It does not read controls directly, keeping it independent of the input setup.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(HelicopterInputReader))]
    public sealed class HelicopterFlightController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HelicopterInputReader inputReader;
        [SerializeField] private Rigidbody helicopterRigidbody;

        [Header("Movement Speeds")]
        [SerializeField, Min(0f)] private float forwardSpeed = 22f;
        [SerializeField, Min(0f)] private float reverseSpeed = 10f;
        [SerializeField, Min(0f)] private float strafeSpeed = 12f;
        [SerializeField, Min(0f)] private float verticalSpeed = 14f;

        [Header("Acceleration")]
        [SerializeField, Min(0f)] private float horizontalAcceleration = 18f;
        [SerializeField, Min(0f)] private float verticalAcceleration = 20f;
        [SerializeField, Min(0f)] private float forwardAcceleration = 22f;

        [Header("Rotation")]
        [SerializeField, Min(0f)] private float yawSpeed = 100f;
        [SerializeField, Min(0f)] private float yawAcceleration = 360f;

        private float currentYawSpeed;

        public void Configure(
            HelicopterInputReader configuredInputReader,
            Rigidbody configuredRigidbody)
        {
            inputReader = configuredInputReader;
            helicopterRigidbody = configuredRigidbody;
        }

        private void Reset()
        {
            inputReader = GetComponent<HelicopterInputReader>();
            helicopterRigidbody = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<HelicopterInputReader>();
            }

            if (helicopterRigidbody == null)
            {
                helicopterRigidbody = GetComponent<Rigidbody>();
            }

            helicopterRigidbody.useGravity = false;
            helicopterRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            helicopterRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            helicopterRigidbody.constraints |=
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;
        }

        private void FixedUpdate()
        {
            if (inputReader == null || helicopterRigidbody == null)
            {
                return;
            }

            ApplyMovement();
            ApplyRotation();
        }

        private void ApplyMovement()
        {
            Vector2 moveInput = inputReader.Move;

            float targetForwardSpeed = moveInput.y >= 0f
                ? moveInput.y * forwardSpeed
                : moveInput.y * reverseSpeed;

            Vector3 targetLocalVelocity = new Vector3(
                moveInput.x * strafeSpeed,
                inputReader.Altitude * verticalSpeed,
                targetForwardSpeed);

            Vector3 currentLocalVelocity =
                transform.InverseTransformDirection(helicopterRigidbody.linearVelocity);

            Vector3 localAcceleration = new Vector3(
                CalculateAcceleration(
                    currentLocalVelocity.x,
                    targetLocalVelocity.x,
                    horizontalAcceleration),
                CalculateAcceleration(
                    currentLocalVelocity.y,
                    targetLocalVelocity.y,
                    verticalAcceleration),
                CalculateAcceleration(
                    currentLocalVelocity.z,
                    targetLocalVelocity.z,
                    forwardAcceleration));

            helicopterRigidbody.AddForce(
                transform.TransformDirection(localAcceleration),
                ForceMode.Acceleration);
        }

        private void ApplyRotation()
        {
            float targetYawSpeed = inputReader.Yaw * yawSpeed;

            currentYawSpeed = Mathf.MoveTowards(
                currentYawSpeed,
                targetYawSpeed,
                yawAcceleration * Time.fixedDeltaTime);

            Quaternion targetRotation = helicopterRigidbody.rotation *
                                        Quaternion.Euler(
                                            0f,
                                            currentYawSpeed * Time.fixedDeltaTime,
                                            0f);

            helicopterRigidbody.MoveRotation(targetRotation);
        }

        private static float CalculateAcceleration(
            float currentVelocity,
            float targetVelocity,
            float maximumAcceleration)
        {
            float requiredAcceleration =
                (targetVelocity - currentVelocity) / Time.fixedDeltaTime;

            return Mathf.Clamp(
                requiredAcceleration,
                -maximumAcceleration,
                maximumAcceleration);
        }
    }
}
