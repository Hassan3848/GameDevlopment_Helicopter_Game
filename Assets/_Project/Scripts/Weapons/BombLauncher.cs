using HelicopterCombat.Combat;
using UnityEngine;
using System;

namespace HelicopterCombat.Weapons
{
    [DisallowMultipleComponent]
    public sealed class BombLauncher : MonoBehaviour
    {
        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private Transform dropPoint;
        [SerializeField] private GameObject ownerRoot;
        [SerializeField] private Rigidbody ownerRigidbody;
        [SerializeField, Min(0)] private int maxAmmo = 8;
        [SerializeField, Min(0f)] private float dropCooldown = 0.55f;

        private float nextDropTime;

        public event Action<BombLauncher, GameObject, Transform> Dropped;

        public int CurrentAmmo { get; private set; }
        public int MaxAmmo => maxAmmo;
        public bool IsUnlimited => maxAmmo >= int.MaxValue / 4;

        public void Configure(GameObject configuredBombPrefab, Transform configuredDropPoint, GameObject configuredOwnerRoot, Rigidbody configuredOwnerRigidbody)
        {
            bombPrefab = configuredBombPrefab;
            dropPoint = configuredDropPoint;
            ownerRoot = configuredOwnerRoot;
            ownerRigidbody = configuredOwnerRigidbody;
            CurrentAmmo = maxAmmo;
        }

        private void Awake()
        {
            CurrentAmmo = maxAmmo;
        }

        public bool TryDrop()
        {
            if (Time.time < nextDropTime ||
                CurrentAmmo <= 0 ||
                bombPrefab == null ||
                dropPoint == null)
            {
                return false;
            }

            GameObject bombObject = Instantiate(
                bombPrefab,
                dropPoint.position,
                dropPoint.rotation);

            ExplosiveProjectile projectile = bombObject.GetComponent<ExplosiveProjectile>();

            if (projectile != null)
            {
                projectile.InitializeOwner(ownerRoot != null ? ownerRoot : gameObject, ownerRigidbody);
            }

            Dropped?.Invoke(this, bombObject, dropPoint);
            CurrentAmmo--;
            nextDropTime = Time.time + dropCooldown;
            return true;
        }
    }
}
