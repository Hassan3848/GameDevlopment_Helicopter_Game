using HelicopterCombat.Combat;
using UnityEngine;

namespace HelicopterCombat.Weapons
{
    [DisallowMultipleComponent]
    public sealed class MissileLauncher : MonoBehaviour
    {
        [SerializeField] private GameObject missilePrefab;
        [SerializeField] private Transform[] launchPoints;
        [SerializeField] private GameObject ownerRoot;
        [SerializeField] private Rigidbody ownerRigidbody;
        [SerializeField, Min(0)] private int maxAmmo = 20;
        [SerializeField, Min(0f)] private float fireCooldown = 0.25f;

        private float nextFireTime;
        private int nextLaunchPointIndex;

        public int CurrentAmmo { get; private set; }

        public void Configure(GameObject configuredMissilePrefab, Transform[] configuredLaunchPoints, GameObject configuredOwnerRoot, Rigidbody configuredOwnerRigidbody)
        {
            missilePrefab = configuredMissilePrefab;
            launchPoints = configuredLaunchPoints;
            ownerRoot = configuredOwnerRoot;
            ownerRigidbody = configuredOwnerRigidbody;
            CurrentAmmo = maxAmmo;
        }

        public void ConfigureAmmoAndCooldown(int configuredMaxAmmo, float configuredFireCooldown)
        {
            maxAmmo = Mathf.Max(0, configuredMaxAmmo);
            fireCooldown = Mathf.Max(0f, configuredFireCooldown);
            CurrentAmmo = maxAmmo;
        }

        private void Awake()
        {
            CurrentAmmo = maxAmmo;
        }

        public bool TryFire()
        {
            if (Time.time < nextFireTime ||
                CurrentAmmo <= 0 ||
                missilePrefab == null ||
                launchPoints == null ||
                launchPoints.Length == 0)
            {
                return false;
            }

            Transform launchPoint = GetNextValidLaunchPoint();

            if (launchPoint == null)
            {
                return false;
            }

            GameObject missileObject = Instantiate(
                missilePrefab,
                launchPoint.position,
                launchPoint.rotation);

            ExplosiveProjectile projectile = missileObject.GetComponent<ExplosiveProjectile>();

            if (projectile != null)
            {
                projectile.InitializeOwner(ownerRoot != null ? ownerRoot : gameObject, ownerRigidbody);
            }

            CurrentAmmo--;
            nextFireTime = Time.time + fireCooldown;
            return true;
        }

        private Transform GetNextValidLaunchPoint()
        {
            for (int attempt = 0; attempt < launchPoints.Length; attempt++)
            {
                int index = (nextLaunchPointIndex + attempt) % launchPoints.Length;
                Transform point = launchPoints[index];

                if (point != null)
                {
                    nextLaunchPointIndex = (index + 1) % launchPoints.Length;
                    return point;
                }
            }

            return null;
        }
    }
}
