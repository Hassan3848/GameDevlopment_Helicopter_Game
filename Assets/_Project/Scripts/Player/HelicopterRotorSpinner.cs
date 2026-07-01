using UnityEngine;

namespace HelicopterCombat.Player
{
    /// <summary>
    /// Decorative rotor animation. It has no physics responsibility.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HelicopterRotorSpinner : MonoBehaviour
    {
        [SerializeField] private Vector3 localRotationAxis = Vector3.up;
        [SerializeField, Min(0f)] private float degreesPerSecond = 1800f;

        public void Configure(Vector3 configuredAxis, float configuredDegreesPerSecond)
        {
            localRotationAxis = configuredAxis.normalized;
            degreesPerSecond = Mathf.Max(0f, configuredDegreesPerSecond);
        }

        private void Update()
        {
            transform.Rotate(
                localRotationAxis,
                degreesPerSecond * Time.deltaTime,
                Space.Self);
        }
    }
}
