using UnityEngine;

namespace HelicopterCombat.Utilities
{
    [DisallowMultipleComponent]
    public sealed class RotorSpinner : MonoBehaviour
    {
        [SerializeField] private Transform[] rotors;
        [SerializeField] private Vector3 localAxis = Vector3.up;
        [SerializeField, Min(0f)] private float speed = 1200f;

        public void Configure(Transform[] configuredRotors, Vector3 configuredLocalAxis, float configuredSpeed)
        {
            rotors = configuredRotors;
            localAxis = configuredLocalAxis.sqrMagnitude > 0.001f ? configuredLocalAxis.normalized : Vector3.up;
            speed = Mathf.Max(0f, configuredSpeed);
        }

        private void Update()
        {
            if (rotors == null || rotors.Length == 0)
            {
                return;
            }

            float delta = speed * Time.deltaTime;

            foreach (Transform rotor in rotors)
            {
                if (rotor != null)
                {
                    rotor.Rotate(localAxis, delta, Space.Self);
                }
            }
        }
    }
}
