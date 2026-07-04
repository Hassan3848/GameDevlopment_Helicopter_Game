using UnityEngine;

namespace HelicopterCombat.Enemies
{
    [DisallowMultipleComponent]
    public sealed class TankTurretAimer : MonoBehaviour
    {
        [SerializeField] private Transform tankRoot;
        [SerializeField] private Transform turretYawPivot;
        [SerializeField] private Transform barrelPitchPivot;
        [SerializeField] private Transform muzzle;
        [SerializeField] private EnemyTankTargeting targeting;
        [SerializeField, Min(0f)] private float yawRotationSpeed = 75f;
        [SerializeField, Min(0f)] private float pitchRotationSpeed = 55f;
        [SerializeField] private float minimumPitch = 0f;
        [SerializeField] private float maximumPitch = 68f;
        [SerializeField] private float targetAimOffset = 2.5f;
        [SerializeField, Min(0f)] private float aimAlignmentTolerance = 6f;

        public Transform Muzzle => muzzle;
        public bool IsAligned { get; private set; }

        public void Configure(
            Transform configuredTankRoot,
            Transform configuredTurretYawPivot,
            Transform configuredBarrelPitchPivot,
            Transform configuredMuzzle,
            EnemyTankTargeting configuredTargeting)
        {
            tankRoot = configuredTankRoot;
            turretYawPivot = configuredTurretYawPivot;
            barrelPitchPivot = configuredBarrelPitchPivot;
            muzzle = configuredMuzzle;
            targeting = configuredTargeting;
        }

        private void LateUpdate()
        {
            UpdateAim();
        }

        private void UpdateAim()
        {
            if (turretYawPivot == null || barrelPitchPivot == null || muzzle == null || targeting == null || targeting.AssignedTarget == null)
            {
                IsAligned = false;
                return;
            }

            Vector3 aimPoint = targeting.AssignedTarget.position + Vector3.up * targetAimOffset;
            Vector3 toTarget = aimPoint - turretYawPivot.position;
            Vector3 flatDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up);

            if (flatDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetYaw = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
                turretYawPivot.rotation = Quaternion.RotateTowards(
                    turretYawPivot.rotation,
                    targetYaw,
                    yawRotationSpeed * Time.deltaTime);
            }

            Vector3 localDirection = barrelPitchPivot.parent != null
                ? barrelPitchPivot.parent.InverseTransformDirection(aimPoint - barrelPitchPivot.position)
                : aimPoint - barrelPitchPivot.position;
            float desiredPitch = Mathf.Atan2(localDirection.y, Mathf.Max(0.001f, localDirection.z)) * Mathf.Rad2Deg;
            desiredPitch = Mathf.Clamp(desiredPitch, minimumPitch, maximumPitch);

            Quaternion targetPitch = Quaternion.Euler(-desiredPitch, 0f, 0f);
            barrelPitchPivot.localRotation = Quaternion.RotateTowards(
                barrelPitchPivot.localRotation,
                targetPitch,
                pitchRotationSpeed * Time.deltaTime);

            Vector3 muzzleToTarget = aimPoint - muzzle.position;
            IsAligned = muzzleToTarget.sqrMagnitude > 0.001f &&
                Vector3.Angle(muzzle.forward, muzzleToTarget.normalized) <= aimAlignmentTolerance;
        }
    }
}
