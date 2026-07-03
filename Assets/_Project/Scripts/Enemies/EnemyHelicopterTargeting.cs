using UnityEngine;

namespace HelicopterCombat.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyHelicopterTargeting : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float detectionRange = 220f;
        [SerializeField, Min(0f)] private float awarenessRange = 340f;
        [SerializeField, Min(0f)] private float loseTargetRange = 340f;
        [SerializeField] private float targetHeightOffset = 6f;

        public Transform AssignedTarget => target;
        public Transform CurrentTarget => HasCombatTarget ? target : null;
        public bool HasAssignedTarget => target != null;
        public bool HasCombatTarget { get; private set; }
        public bool IsWithinAwarenessRange { get; private set; }
        public float DistanceToTarget { get; private set; }
        public Vector3 TargetPosition { get; private set; }
        public Vector3 DirectionToTarget { get; private set; }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            UpdateMeasurements();
        }

        public void ConfigureRanges(float configuredDetectionRange, float configuredAwarenessRange, float configuredLoseTargetRange)
        {
            detectionRange = Mathf.Max(0f, configuredDetectionRange);
            awarenessRange = Mathf.Max(detectionRange, configuredAwarenessRange);
            loseTargetRange = Mathf.Max(awarenessRange, configuredLoseTargetRange);
        }

        private void Update()
        {
            UpdateMeasurements();
        }

        private void UpdateMeasurements()
        {
            if (target == null)
            {
                HasCombatTarget = false;
                IsWithinAwarenessRange = false;
                DistanceToTarget = float.PositiveInfinity;
                TargetPosition = transform.position;
                DirectionToTarget = transform.forward;
                return;
            }

            TargetPosition = target.position + Vector3.up * targetHeightOffset;
            Vector3 offset = TargetPosition - transform.position;
            DistanceToTarget = offset.magnitude;
            DirectionToTarget = DistanceToTarget > 0.001f ? offset / DistanceToTarget : transform.forward;
            IsWithinAwarenessRange = DistanceToTarget <= awarenessRange;

            if (HasCombatTarget)
            {
                HasCombatTarget = DistanceToTarget <= loseTargetRange;
            }
            else
            {
                HasCombatTarget = DistanceToTarget <= detectionRange;
            }
        }
    }
}
