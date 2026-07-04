using UnityEngine;
using System.Collections.Generic;

namespace HelicopterCombat.Utilities
{
    [DisallowMultipleComponent]
    public sealed class RotorSpinner : MonoBehaviour
    {
        [SerializeField] private Transform[] rotors;
        [SerializeField] private Vector3 localAxis = Vector3.up;
        [SerializeField, Min(0f)] private float speed = 1200f;

        private static readonly string[] RotorNameHints =
        {
            "rotor",
            "propeller",
            "blade",
            "mainrotor",
            "tailrotor"
        };

        public void Configure(Transform[] configuredRotors, Vector3 configuredLocalAxis, float configuredSpeed)
        {
            rotors = configuredRotors;
            localAxis = configuredLocalAxis.sqrMagnitude > 0.001f ? configuredLocalAxis.normalized : Vector3.up;
            speed = Mathf.Max(0f, configuredSpeed);
        }

        private void Awake()
        {
            EnsureRotorReferences();
        }

        private void Update()
        {
            if (!HasUsableRotors())
            {
                EnsureRotorReferences();
            }

            if (!HasUsableRotors())
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

        private bool HasUsableRotors()
        {
            if (rotors == null || rotors.Length == 0)
            {
                return false;
            }

            foreach (Transform rotor in rotors)
            {
                if (rotor != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureRotorReferences()
        {
            List<Transform> discoveredRotors = new List<Transform>();
            Transform[] candidates = GetComponentsInChildren<Transform>(true);

            foreach (Transform candidate in candidates)
            {
                if (candidate == null || candidate == transform)
                {
                    continue;
                }

                string lowerName = candidate.name.ToLowerInvariant();
                for (int i = 0; i < RotorNameHints.Length; i++)
                {
                    if (lowerName.Contains(RotorNameHints[i]))
                    {
                        discoveredRotors.Add(candidate);
                        break;
                    }
                }
            }

            if (discoveredRotors.Count > 0)
            {
                rotors = discoveredRotors.ToArray();
            }
        }
    }
}
