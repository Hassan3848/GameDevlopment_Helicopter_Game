using UnityEngine;

namespace HelicopterCombat.Player
{
    /// <summary>
    /// Tilts the visual child only. The physics root stays stable and predictable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HelicopterVisualTilt : MonoBehaviour
    {
        [SerializeField] private HelicopterInputReader inputReader;

        [Header("Visual Tilt")]
        [SerializeField, Min(0f)] private float forwardTiltAngle = 15f;
        [SerializeField, Min(0f)] private float sidewaysTiltAngle = 20f;
        [SerializeField, Min(0f)] private float tiltSmoothness = 7f;

        private Quaternion restingRotation;

        public void Configure(HelicopterInputReader configuredInputReader)
        {
            inputReader = configuredInputReader;
        }

        private void Awake()
        {
            restingRotation = transform.localRotation;

            if (inputReader == null)
            {
                inputReader = GetComponentInParent<HelicopterInputReader>();
            }
        }

        private void LateUpdate()
        {
            if (inputReader == null)
            {
                return;
            }

            Quaternion targetTilt = restingRotation * Quaternion.Euler(
                -inputReader.Move.y * forwardTiltAngle,
                0f,
                -inputReader.Move.x * sidewaysTiltAngle);

            float blend = 1f - Mathf.Exp(-tiltSmoothness * Time.deltaTime);

            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetTilt,
                blend);
        }
    }
}
