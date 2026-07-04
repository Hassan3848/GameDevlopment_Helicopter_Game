using UnityEngine;

namespace HelicopterCombat.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyTankTargeting : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Transform aimOrigin;
        [SerializeField, Min(0f)] private float detectionRange = 180f;
        [SerializeField, Min(0f)] private float awarenessRange = 240f;
        [SerializeField, Min(0f)] private float loseTargetRange = 300f;
        [SerializeField, Min(0f)] private float minimumFiringDistance = 25f;
        [SerializeField, Min(0f)] private float maximumFiringDistance = 165f;
        [SerializeField, Min(0f)] private float minimumElevationAngle = 8f;
        [SerializeField, Min(0f)] private float maximumElevationAngle = 65f;
        [SerializeField] private float aimHeightOffset = 2.5f;

        public Transform AssignedTarget => target;
        public Transform CurrentTarget => HasCombatTarget ? target : null;
        public bool HasCombatTarget { get; private set; }
        public bool IsWithinDetectionRange { get; private set; }
        public bool IsWithinAwarenessRange { get; private set; }
        public bool IsBeyondReturnThreshold { get; private set; }
        public bool IsWithinFiringRange { get; private set; }
        public bool HasValidFiringAngle { get; private set; }
        public float DistanceToTarget { get; private set; }
        public float HorizontalDistanceToTarget { get; private set; }
        public Vector3 AimPoint { get; private set; }
        public Vector3 DirectionToTarget { get; private set; }

        public void SetTarget(Transform configuredTarget)
        {
            target = configuredTarget;
            UpdateMeasurements();
        }

        public void SetAimOrigin(Transform configuredAimOrigin)
        {
            aimOrigin = configuredAimOrigin;
            UpdateMeasurements();
        }

        public void ConfigureRanges(
            float configuredDetectionRange,
            float configuredAwarenessRange,
            float configuredLoseTargetRange,
            float configuredMinimumFiringDistance,
            float configuredMaximumFiringDistance,
            float configuredMinimumElevationAngle,
            float configuredMaximumElevationAngle,
            float configuredAimHeightOffset)
        {
            detectionRange = Mathf.Max(0f, configuredDetectionRange);
            awarenessRange = Mathf.Max(detectionRange, configuredAwarenessRange);
            loseTargetRange = Mathf.Max(awarenessRange, configuredLoseTargetRange);
            minimumFiringDistance = Mathf.Max(0f, configuredMinimumFiringDistance);
            maximumFiringDistance = Mathf.Max(minimumFiringDistance, configuredMaximumFiringDistance);
            minimumElevationAngle = Mathf.Max(0f, configuredMinimumElevationAngle);
            maximumElevationAngle = Mathf.Max(minimumElevationAngle, configuredMaximumElevationAngle);
            aimHeightOffset = configuredAimHeightOffset;
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
                IsWithinDetectionRange = false;
                IsWithinAwarenessRange = false;
                IsBeyondReturnThreshold = true;
                IsWithinFiringRange = false;
                HasValidFiringAngle = false;
                DistanceToTarget = float.PositiveInfinity;
                HorizontalDistanceToTarget = float.PositiveInfinity;
                AimPoint = transform.position;
                DirectionToTarget = transform.forward;
                return;
            }

            Vector3 origin = aimOrigin != null ? aimOrigin.position : transform.position;
            AimPoint = target.position + Vector3.up * aimHeightOffset;
            Vector3 offset = AimPoint - origin;
            DistanceToTarget = offset.magnitude;
            HorizontalDistanceToTarget = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up).magnitude;
            DirectionToTarget = DistanceToTarget > 0.001f ? offset / DistanceToTarget : transform.forward;

            if (HasCombatTarget)
            {
                HasCombatTarget = HorizontalDistanceToTarget <= loseTargetRange;
            }
            else
            {
                HasCombatTarget = HorizontalDistanceToTarget <= detectionRange;
            }

            IsWithinDetectionRange = HorizontalDistanceToTarget <= detectionRange;
            IsWithinAwarenessRange = HorizontalDistanceToTarget <= awarenessRange;
            IsBeyondReturnThreshold = HorizontalDistanceToTarget > loseTargetRange;

            IsWithinFiringRange = DistanceToTarget >= minimumFiringDistance && DistanceToTarget <= maximumFiringDistance;

            float horizontalDistance = Vector3.ProjectOnPlane(offset, Vector3.up).magnitude;
            float elevationAngle = Mathf.Atan2(offset.y, Mathf.Max(0.001f, horizontalDistance)) * Mathf.Rad2Deg;
            HasValidFiringAngle = elevationAngle >= minimumElevationAngle && elevationAngle <= maximumElevationAngle;
        }
    }
}
