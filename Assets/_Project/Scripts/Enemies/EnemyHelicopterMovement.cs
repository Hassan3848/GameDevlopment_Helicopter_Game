using UnityEngine;

namespace HelicopterCombat.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class EnemyHelicopterMovement : MonoBehaviour
    {
        [SerializeField] private Rigidbody helicopterRigidbody;
        [SerializeField] private Terrain terrain;
        [SerializeField] private EnemyHelicopterSeparation separation;

        [SerializeField, Min(0f)] private float cruiseSpeed = 24f;
        [SerializeField, Min(0f)] private float returnCruiseSpeed = 22f;
        [SerializeField, Min(0f)] private float maximumSpeed = 30f;
        [SerializeField, Min(0f)] private float acceleration = 16f;
        [SerializeField, Min(0f)] private float brakingAcceleration = 20f;
        [SerializeField, Min(0f)] private float yawSpeed = 115f;
        [SerializeField, Min(0f)] private float yawAcceleration = 300f;
        [SerializeField, Min(0f)] private float combatDistance = 58f;
        [SerializeField, Min(0f)] private float minimumCombatDistance = 38f;
        [SerializeField, Min(0f)] private float combatDistanceTolerance = 8f;
        [SerializeField] private float orbitStrength = 0.55f;
        [SerializeField, Min(0f)] private float minimumTerrainClearance = 24f;
        [SerializeField, Min(0f)] private float maximumHeightAboveTerrain = 155f;
        [SerializeField] private float preferredAltitudeOffset = 34f;
        [SerializeField, Min(0f)] private float altitudeResponse = 2.4f;
        [SerializeField, Min(0f)] private float verticalSpeedLimit = 13f;
        [SerializeField] private float orbitSign = 1f;
        [SerializeField, Min(0f)] private float minimumAltitudeAboveTarget = 10f;
        [SerializeField, Min(0f)] private float maximumAltitudeAboveTarget = 20f;
        [SerializeField, Range(0f, 1f)] private float combatForwardPressure = 0.35f;
        [SerializeField] private Vector3 homePosition;
        [SerializeField, Min(0f)] private float returnToHomeDistance = 225f;
        [SerializeField, Min(0f)] private float hardCombatBoundaryDistance = 280f;
        [SerializeField, Min(0f)] private float returnArrivalDistance = 18f;

        private Vector3 desiredTargetPosition;
        private bool hasTarget;
        private bool returnToCombatZone;
        private float currentYawSpeed;
        private bool warningLogged;

        public Vector3 LocalVelocity { get; private set; }
        public Vector3 NormalizedMovement { get; private set; }
        public Vector3 HomePosition => homePosition;
        public float CombatDistance => combatDistance;
        public float MinimumCombatDistance => minimumCombatDistance;
        public float CombatDistanceTolerance => combatDistanceTolerance;
        public float DistanceFromHome => Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(homePosition.x, 0f, homePosition.z));
        public bool IsBeyondSoftBoundary => DistanceFromHome > returnToHomeDistance;
        public bool IsBeyondHardBoundary => DistanceFromHome >= hardCombatBoundaryDistance;
        public bool IsNearHome => DistanceFromHome <= returnArrivalDistance;

        public void Configure(Rigidbody configuredRigidbody, Terrain configuredTerrain, EnemyHelicopterSeparation configuredSeparation)
        {
            helicopterRigidbody = configuredRigidbody;
            terrain = configuredTerrain;
            separation = configuredSeparation;
        }

        public void ConfigureOrbit(float configuredOrbitSign)
        {
            orbitSign = Mathf.Approximately(configuredOrbitSign, 0f) ? 1f : Mathf.Sign(configuredOrbitSign);
        }

        public void ConfigureHomeAndBoundary(
            Vector3 configuredHomePosition,
            float configuredReturnToHomeDistance,
            float configuredHardCombatBoundaryDistance,
            float configuredReturnArrivalDistance,
            float configuredReturnCruiseSpeed,
            float configuredMaximumHeightAboveTerrain,
            float configuredMinimumTerrainClearance)
        {
            homePosition = configuredHomePosition;
            returnToHomeDistance = Mathf.Max(0f, configuredReturnToHomeDistance);
            hardCombatBoundaryDistance = Mathf.Max(returnToHomeDistance, configuredHardCombatBoundaryDistance);
            returnArrivalDistance = Mathf.Max(0f, configuredReturnArrivalDistance);
            returnCruiseSpeed = Mathf.Max(0f, configuredReturnCruiseSpeed);
            maximumHeightAboveTerrain = Mathf.Max(configuredMinimumTerrainClearance, configuredMaximumHeightAboveTerrain);
            minimumTerrainClearance = Mathf.Max(0f, configuredMinimumTerrainClearance);
        }

        public void SetTarget(Vector3 targetPosition, bool targetAvailable, bool shouldReturnToCombatZone)
        {
            desiredTargetPosition = targetPosition;
            hasTarget = targetAvailable;
            returnToCombatZone = shouldReturnToCombatZone;
        }

        private void Reset()
        {
            helicopterRigidbody = GetComponent<Rigidbody>();
            separation = GetComponent<EnemyHelicopterSeparation>();
        }

        private void Awake()
        {
            if (helicopterRigidbody == null)
            {
                helicopterRigidbody = GetComponent<Rigidbody>();
            }

            if (separation == null)
            {
                separation = GetComponent<EnemyHelicopterSeparation>();
            }

            if (homePosition == Vector3.zero)
            {
                homePosition = transform.position;
            }

            helicopterRigidbody.useGravity = false;
            helicopterRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            helicopterRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            helicopterRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void FixedUpdate()
        {
            if (NeedsSafetyRecovery())
            {
                RecoverToHome();
            }

            Vector3 targetVelocity = returnToCombatZone
                ? CalculateReturnVelocity()
                : (hasTarget ? CalculateCombatVelocity() : CalculateHoverVelocity());

            ApplyVelocity(targetVelocity);
            ApplyYaw(targetVelocity);
            LocalVelocity = transform.InverseTransformDirection(helicopterRigidbody.linearVelocity);
            NormalizedMovement = maximumSpeed > 0f ? Vector3.ClampMagnitude(LocalVelocity / maximumSpeed, 1f) : Vector3.zero;
        }

        private Vector3 CalculateCombatVelocity()
        {
            Vector3 toTarget = desiredTargetPosition - transform.position;
            Vector3 horizontalToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            float horizontalDistance = horizontalToTarget.magnitude;
            Vector3 targetDirection = horizontalDistance > 0.001f ? horizontalToTarget / horizontalDistance : transform.forward;
            Vector3 orbitDirection = Vector3.Cross(Vector3.up, targetDirection).normalized * orbitSign;

            Vector3 horizontalDirection;

            if (horizontalDistance > combatDistance + combatDistanceTolerance)
            {
                horizontalDirection = targetDirection;
            }
            else if (horizontalDistance < minimumCombatDistance)
            {
                horizontalDirection = (-targetDirection + orbitDirection * 0.30f).normalized;
            }
            else
            {
                horizontalDirection = Vector3.Lerp(
                    orbitDirection * orbitStrength,
                    targetDirection,
                    combatForwardPressure).normalized;
            }

            Vector3 separationDirection = separation != null ? separation.CalculateSeparation() : Vector3.zero;
            horizontalDirection = Vector3.ClampMagnitude(horizontalDirection + separationDirection, 1f);

            float desiredCombatHeight = desiredTargetPosition.y + Mathf.Clamp(
                preferredAltitudeOffset,
                minimumAltitudeAboveTarget,
                maximumAltitudeAboveTarget);

            return horizontalDirection * cruiseSpeed + Vector3.up * CalculateVerticalVelocity(desiredCombatHeight);
        }

        private Vector3 CalculateReturnVelocity()
        {
            Vector3 returnTarget = homePosition;
            float groundHeight = SampleGroundHeight(returnTarget);
            returnTarget.y = Mathf.Max(groundHeight + minimumTerrainClearance + 14f, homePosition.y);

            Vector3 toHome = returnTarget - transform.position;
            Vector3 horizontalToHome = Vector3.ProjectOnPlane(toHome, Vector3.up);
            float horizontalDistance = horizontalToHome.magnitude;

            if (horizontalDistance <= returnArrivalDistance)
            {
                return CalculateHoverVelocity(returnTarget.y);
            }

            Vector3 homeDirection = horizontalDistance > 0.001f ? horizontalToHome / horizontalDistance : transform.forward;
            Vector3 separationDirection = separation != null ? separation.CalculateSeparation() : Vector3.zero;
            Vector3 horizontalDirection = Vector3.ClampMagnitude(homeDirection + separationDirection * 0.6f, 1f);
            return horizontalDirection * returnCruiseSpeed + Vector3.up * CalculateVerticalVelocity(returnTarget.y);
        }

        private Vector3 CalculateHoverVelocity()
        {
            float desiredHeight = Mathf.Max(SampleGroundHeight(transform.position) + minimumTerrainClearance + 10f, homePosition.y);
            return CalculateHoverVelocity(desiredHeight);
        }

        private Vector3 CalculateHoverVelocity(float desiredHeight)
        {
            float verticalVelocity = CalculateVerticalVelocity(desiredHeight);
            Vector3 dampedHorizontal = -Vector3.ProjectOnPlane(helicopterRigidbody.linearVelocity, Vector3.up) * 0.65f;
            return dampedHorizontal + Vector3.up * verticalVelocity;
        }

        private float CalculateVerticalVelocity(float desiredHeight)
        {
            float groundHeight = SampleGroundHeight(transform.position);
            float minimumHeight = groundHeight + minimumTerrainClearance;
            float maximumHeight = groundHeight + maximumHeightAboveTerrain;
            float clampedHeight = Mathf.Clamp(desiredHeight, minimumHeight, maximumHeight);
            return Mathf.Clamp(
                (clampedHeight - transform.position.y) * altitudeResponse,
                -verticalSpeedLimit,
                verticalSpeedLimit);
        }

        private float SampleGroundHeight(Vector3 position)
        {
            return terrain != null ? terrain.SampleHeight(position) + terrain.transform.position.y : 0f;
        }

        private bool NeedsSafetyRecovery()
        {
            Vector3 position = helicopterRigidbody.position;

            if (!IsFinite(position) || !IsFinite(helicopterRigidbody.linearVelocity))
            {
                return true;
            }

            if (terrain == null)
            {
                return false;
            }

            float terrainHeight = SampleGroundHeight(position);
            return position.y < terrainHeight - 2f;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !(float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z) ||
                float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z));
        }

        private void RecoverToHome()
        {
            float groundHeight = SampleGroundHeight(homePosition);
            Vector3 safePosition = homePosition;
            safePosition.y = Mathf.Max(groundHeight + minimumTerrainClearance + 20f, homePosition.y);
            helicopterRigidbody.position = safePosition;
            helicopterRigidbody.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            helicopterRigidbody.linearVelocity = Vector3.zero;
            helicopterRigidbody.angularVelocity = Vector3.zero;

            if (!warningLogged)
            {
                warningLogged = true;
                Debug.LogWarning(name + " recovered to its home position after invalid physics state.", this);
            }
        }

        private void ApplyVelocity(Vector3 targetVelocity)
        {
            Vector3 currentVelocity = helicopterRigidbody.linearVelocity;
            Vector3 velocityDelta = targetVelocity - currentVelocity;
            float accelerationLimit = targetVelocity.sqrMagnitude > currentVelocity.sqrMagnitude
                ? acceleration
                : brakingAcceleration;

            Vector3 force = Vector3.ClampMagnitude(
                velocityDelta / Time.fixedDeltaTime,
                accelerationLimit);

            helicopterRigidbody.AddForce(force, ForceMode.Acceleration);

            if (helicopterRigidbody.linearVelocity.magnitude > maximumSpeed)
            {
                helicopterRigidbody.linearVelocity = helicopterRigidbody.linearVelocity.normalized * maximumSpeed;
            }
        }

        private void ApplyYaw(Vector3 targetVelocity)
        {
            Vector3 lookDirection = returnToCombatZone
                ? Vector3.ProjectOnPlane(homePosition - transform.position, Vector3.up)
                : (hasTarget
                    ? Vector3.ProjectOnPlane(desiredTargetPosition - transform.position, Vector3.up)
                    : Vector3.ProjectOnPlane(targetVelocity, Vector3.up));

            if (lookDirection.sqrMagnitude < 0.01f)
            {
                currentYawSpeed = Mathf.MoveTowards(currentYawSpeed, 0f, yawAcceleration * Time.fixedDeltaTime);
                return;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            float yawDelta = Mathf.DeltaAngle(transform.eulerAngles.y, desiredRotation.eulerAngles.y);
            float targetYawSpeed = Mathf.Clamp(yawDelta / Time.fixedDeltaTime, -yawSpeed, yawSpeed);
            currentYawSpeed = Mathf.MoveTowards(currentYawSpeed, targetYawSpeed, yawAcceleration * Time.fixedDeltaTime);
            helicopterRigidbody.MoveRotation(helicopterRigidbody.rotation * Quaternion.Euler(0f, currentYawSpeed * Time.fixedDeltaTime, 0f));
        }
    }
}
