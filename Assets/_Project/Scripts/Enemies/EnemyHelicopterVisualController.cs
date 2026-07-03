using UnityEngine;

namespace HelicopterCombat.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyHelicopterVisualController : MonoBehaviour
    {
        [SerializeField] private EnemyHelicopterMovement movement;
        [SerializeField] private Transform visualPivot;
        [SerializeField] private Transform[] rotorTransforms;
        [SerializeField] private float forwardTiltAngle = 10f;
        [SerializeField] private float sideTiltAngle = 14f;
        [SerializeField, Min(0f)] private float tiltSmoothness = 5f;
        [SerializeField, Min(0f)] private float rotorSpeed = 1200f;
        [SerializeField] private Vector3 rotorLocalAxis = Vector3.up;

        public void Configure(EnemyHelicopterMovement configuredMovement, Transform configuredVisualPivot, Transform[] configuredRotors)
        {
            movement = configuredMovement;
            visualPivot = configuredVisualPivot;
            rotorTransforms = configuredRotors;
        }

        private void LateUpdate()
        {
            if (visualPivot != null && movement != null)
            {
                Vector3 localMotion = movement.NormalizedMovement;
                Quaternion targetRotation = Quaternion.Euler(
                    Mathf.Clamp(localMotion.z, -1f, 1f) * forwardTiltAngle,
                    0f,
                    -Mathf.Clamp(localMotion.x, -1f, 1f) * sideTiltAngle);

                visualPivot.localRotation = Quaternion.Slerp(
                    visualPivot.localRotation,
                    targetRotation,
                    tiltSmoothness * Time.deltaTime);
            }

            SpinRotors();
        }

        private void SpinRotors()
        {
            if (rotorTransforms == null || rotorTransforms.Length == 0)
            {
                return;
            }

            Vector3 axis = rotorLocalAxis.sqrMagnitude > 0.001f ? rotorLocalAxis.normalized : Vector3.up;
            float delta = rotorSpeed * Time.deltaTime;

            foreach (Transform rotor in rotorTransforms)
            {
                if (rotor != null)
                {
                    rotor.Rotate(axis, delta, Space.Self);
                }
            }
        }
    }
}
