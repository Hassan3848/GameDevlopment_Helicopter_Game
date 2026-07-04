using UnityEngine;

namespace HelicopterCombat.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EnemyTankMovement : MonoBehaviour
    {
        public enum MovementMode
        {
            Idle,
            Pursue,
            Hold,
            ReturnHome
        }

        [SerializeField] private Rigidbody tankRigidbody;
        [SerializeField] private BoxCollider tankCollider;
        [SerializeField] private Terrain terrain;
        [SerializeField] private EnemyTankTargeting targeting;
        [SerializeField, Min(0f)] private float moveSpeed = 8f;
        [SerializeField, Min(0f)] private float rotationSpeed = 80f;
        [SerializeField, Min(0f)] private float acceleration = 9f;
        [SerializeField, Min(0f)] private float braking = 12f;
        [SerializeField, Min(0f)] private float slopeLimit = 30f;
        [SerializeField, Min(0f)] private float groundClearance = 0.08f;
        [SerializeField, Min(0f)] private float chaseStartDistance = 90f;
        [SerializeField, Min(0f)] private float preferredCombatDistance = 60f;
        [SerializeField, Min(0f)] private float minimumCombatDistance = 40f;
        [SerializeField, Min(0f)] private float homeReturnDistance = 260f;
        [SerializeField, Min(0f)] private float stoppingTolerance = 6f;
        [SerializeField] private Vector3 homePosition;
        [SerializeField, Range(0f, 1f)] private float terrainTiltStrength = 0.4f;

        private MovementMode movementMode;
        private Vector3 currentPlanarVelocity;

        public MovementMode Mode => movementMode;
        public Vector3 HomePosition => homePosition;
        public float PreferredCombatDistance => preferredCombatDistance;
        public float MinimumCombatDistance => minimumCombatDistance;
        public float ChaseStartDistance => chaseStartDistance;
        public float HomeReturnDistance => homeReturnDistance;
        public float StoppingTolerance => stoppingTolerance;
        public float GroundProjectedDistanceToTarget { get; private set; }
        public float DistanceToHome => PlanarDistanceTo(homePosition);
        public float CurrentSpeed => currentPlanarVelocity.magnitude;

        public void Configure(
            Rigidbody configuredRigidbody,
            BoxCollider configuredCollider,
            Terrain configuredTerrain,
            EnemyTankTargeting configuredTargeting)
        {
            tankRigidbody = configuredRigidbody;
            tankCollider = configuredCollider;
            terrain = configuredTerrain;
            targeting = configuredTargeting;
        }

        public void ConfigureMovement(
            Vector3 configuredHomePosition,
            float configuredMoveSpeed,
            float configuredRotationSpeed,
            float configuredAcceleration,
            float configuredBraking,
            float configuredSlopeLimit,
            float configuredGroundClearance,
            float configuredChaseStartDistance,
            float configuredPreferredCombatDistance,
            float configuredMinimumCombatDistance,
            float configuredHomeReturnDistance,
            float configuredStoppingTolerance)
        {
            homePosition = configuredHomePosition;
            moveSpeed = Mathf.Max(0f, configuredMoveSpeed);
            rotationSpeed = Mathf.Max(0f, configuredRotationSpeed);
            acceleration = Mathf.Max(0f, configuredAcceleration);
            braking = Mathf.Max(0f, configuredBraking);
            slopeLimit = Mathf.Max(0f, configuredSlopeLimit);
            groundClearance = Mathf.Max(0f, configuredGroundClearance);
            chaseStartDistance = Mathf.Max(0f, configuredChaseStartDistance);
            preferredCombatDistance = Mathf.Max(0f, configuredPreferredCombatDistance);
            minimumCombatDistance = Mathf.Max(0f, configuredMinimumCombatDistance);
            homeReturnDistance = Mathf.Max(0f, configuredHomeReturnDistance);
            stoppingTolerance = Mathf.Max(0f, configuredStoppingTolerance);
        }

        public void SetMovementMode(MovementMode configuredMode)
        {
            movementMode = configuredMode;
        }

        private void Reset()
        {
            tankRigidbody = GetComponent<Rigidbody>();
            tankCollider = GetComponent<BoxCollider>();
            targeting = GetComponent<EnemyTankTargeting>();
        }

        private void Awake()
        {
            if (tankRigidbody == null)
            {
                tankRigidbody = GetComponent<Rigidbody>();
            }

            if (tankCollider == null)
            {
                tankCollider = GetComponent<BoxCollider>();
            }

            if (targeting == null)
            {
                targeting = GetComponent<EnemyTankTargeting>();
            }

            if (homePosition == Vector3.zero)
            {
                homePosition = transform.position;
            }
        }

        private void FixedUpdate()
        {
            UpdateGroundDistance();
            Vector3 desiredPlanarVelocity = CalculateDesiredPlanarVelocity();
            float accel = desiredPlanarVelocity.sqrMagnitude > currentPlanarVelocity.sqrMagnitude ? acceleration : braking;
            currentPlanarVelocity = Vector3.MoveTowards(currentPlanarVelocity, desiredPlanarVelocity, accel * Time.fixedDeltaTime);

            Vector3 position = tankRigidbody.position;
            Vector3 nextPosition = position + currentPlanarVelocity * Time.fixedDeltaTime;
            nextPosition = GroundPosition(nextPosition);

            Quaternion nextRotation = CalculateHullRotation(position, nextPosition, currentPlanarVelocity);
            tankRigidbody.MovePosition(nextPosition);
            tankRigidbody.MoveRotation(nextRotation);
        }

        private void UpdateGroundDistance()
        {
            if (targeting == null || targeting.AssignedTarget == null)
            {
                GroundProjectedDistanceToTarget = float.PositiveInfinity;
                return;
            }

            Vector3 targetGround = GetGroundProjectedTarget();
            Vector3 from = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 to = new Vector3(targetGround.x, 0f, targetGround.z);
            GroundProjectedDistanceToTarget = Vector3.Distance(from, to);
        }

        private Vector3 CalculateDesiredPlanarVelocity()
        {
            Vector3 direction = Vector3.zero;
            float targetSpeed = 0f;

            switch (movementMode)
            {
                case MovementMode.Pursue:
                {
                    Vector3 targetGround = GetGroundProjectedTarget();
                    direction = PlanarDirectionTo(targetGround);

                    if (GroundProjectedDistanceToTarget > preferredCombatDistance + stoppingTolerance)
                    {
                        targetSpeed = moveSpeed;
                    }
                    else if (GroundProjectedDistanceToTarget < minimumCombatDistance)
                    {
                        direction = -direction;
                        targetSpeed = moveSpeed * 0.45f;
                    }

                    break;
                }
                case MovementMode.ReturnHome:
                {
                    direction = PlanarDirectionTo(homePosition);
                    float distanceToHome = PlanarDistanceTo(homePosition);

                    if (distanceToHome > stoppingTolerance)
                    {
                        targetSpeed = moveSpeed * 0.9f;
                    }

                    break;
                }
                case MovementMode.Hold:
                case MovementMode.Idle:
                default:
                    targetSpeed = 0f;
                    break;
            }

            if (direction.sqrMagnitude < 0.001f || targetSpeed <= 0.001f)
            {
                return Vector3.zero;
            }

            float slopeFactor = CalculateSlopeFactor(direction);
            return direction * (targetSpeed * slopeFactor);
        }

        private Quaternion CalculateHullRotation(Vector3 currentPosition, Vector3 nextPosition, Vector3 planarVelocity)
        {
            Vector3 facingDirection = planarVelocity.sqrMagnitude > 0.04f
                ? planarVelocity.normalized
                : GetIdleFacingDirection();

            if (facingDirection.sqrMagnitude < 0.001f)
            {
                facingDirection = transform.forward;
            }

            Vector3 terrainNormal = terrain != null
                ? terrain.terrainData.GetInterpolatedNormal(
                    Mathf.InverseLerp(terrain.transform.position.x, terrain.transform.position.x + terrain.terrainData.size.x, nextPosition.x),
                    Mathf.InverseLerp(terrain.transform.position.z, terrain.transform.position.z + terrain.terrainData.size.z, nextPosition.z))
                : Vector3.up;

            terrainNormal = Vector3.Slerp(Vector3.up, terrainNormal.normalized, terrainTiltStrength);
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(facingDirection, terrainNormal).normalized, terrainNormal);
            return Quaternion.RotateTowards(tankRigidbody.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        private Vector3 GetIdleFacingDirection()
        {
            if (movementMode == MovementMode.ReturnHome)
            {
                return PlanarDirectionTo(homePosition);
            }

            if (targeting != null && targeting.AssignedTarget != null)
            {
                return PlanarDirectionTo(GetGroundProjectedTarget());
            }

            return transform.forward;
        }

        private Vector3 GetGroundProjectedTarget()
        {
            if (targeting == null || targeting.AssignedTarget == null)
            {
                return homePosition;
            }

            Vector3 position = targeting.AssignedTarget.position;
            position.y = terrain != null ? terrain.SampleHeight(position) + terrain.transform.position.y : transform.position.y;
            return position;
        }

        private float PlanarDistanceTo(Vector3 worldPosition)
        {
            Vector3 from = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 to = new Vector3(worldPosition.x, 0f, worldPosition.z);
            return Vector3.Distance(from, to);
        }

        private Vector3 PlanarDirectionTo(Vector3 worldPosition)
        {
            Vector3 offset = new Vector3(worldPosition.x - transform.position.x, 0f, worldPosition.z - transform.position.z);
            return offset.sqrMagnitude > 0.001f ? offset.normalized : Vector3.zero;
        }

        private float CalculateSlopeFactor(Vector3 direction)
        {
            if (terrain == null || direction.sqrMagnitude < 0.001f)
            {
                return 1f;
            }

            Vector3 samplePosition = tankRigidbody.position + direction * 2.5f;
            Vector3 normal = terrain.terrainData.GetInterpolatedNormal(
                Mathf.InverseLerp(terrain.transform.position.x, terrain.transform.position.x + terrain.terrainData.size.x, samplePosition.x),
                Mathf.InverseLerp(terrain.transform.position.z, terrain.transform.position.z + terrain.terrainData.size.z, samplePosition.z)).normalized;
            float slope = Vector3.Angle(normal, Vector3.up);

            if (slope <= slopeLimit * 0.7f)
            {
                return 1f;
            }

            if (slope >= slopeLimit)
            {
                return 0.15f;
            }

            return Mathf.Lerp(1f, 0.15f, Mathf.InverseLerp(slopeLimit * 0.7f, slopeLimit, slope));
        }

        private Vector3 GroundPosition(Vector3 desiredPosition)
        {
            if (terrain == null)
            {
                return desiredPosition;
            }

            desiredPosition.x = Mathf.Clamp(
                desiredPosition.x,
                terrain.transform.position.x,
                terrain.transform.position.x + terrain.terrainData.size.x);
            desiredPosition.z = Mathf.Clamp(
                desiredPosition.z,
                terrain.transform.position.z,
                terrain.transform.position.z + terrain.terrainData.size.z);

            float groundHeight = terrain.SampleHeight(desiredPosition) + terrain.transform.position.y;
            float rideHeight = (tankCollider.size.y * 0.5f) - tankCollider.center.y + groundClearance;
            desiredPosition.y = groundHeight + rideHeight;
            return desiredPosition;
        }
    }
}
