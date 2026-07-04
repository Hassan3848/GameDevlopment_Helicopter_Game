using HelicopterCombat.Weapons;
using UnityEngine;

namespace HelicopterCombat.VFX
{
    [DisallowMultipleComponent]
    public sealed class WeaponVfxController : MonoBehaviour
    {
        [SerializeField] private MissileLauncher missileLauncher;
        [SerializeField] private BombLauncher bombLauncher;
        [SerializeField] private GameObject missileMuzzleFlashPrefab;
        [SerializeField] private GameObject bombReleasePuffPrefab;

        public void Configure(
            MissileLauncher configuredMissileLauncher,
            BombLauncher configuredBombLauncher,
            GameObject configuredMissileMuzzleFlashPrefab,
            GameObject configuredBombReleasePuffPrefab)
        {
            missileLauncher = configuredMissileLauncher;
            bombLauncher = configuredBombLauncher;
            missileMuzzleFlashPrefab = configuredMissileMuzzleFlashPrefab;
            bombReleasePuffPrefab = configuredBombReleasePuffPrefab;
        }

        private void OnEnable()
        {
            if (missileLauncher != null)
            {
                missileLauncher.Fired -= HandleMissileFired;
                missileLauncher.Fired += HandleMissileFired;
            }

            if (bombLauncher != null)
            {
                bombLauncher.Dropped -= HandleBombDropped;
                bombLauncher.Dropped += HandleBombDropped;
            }
        }

        private void OnDisable()
        {
            if (missileLauncher != null)
            {
                missileLauncher.Fired -= HandleMissileFired;
            }

            if (bombLauncher != null)
            {
                bombLauncher.Dropped -= HandleBombDropped;
            }
        }

        private void HandleMissileFired(MissileLauncher launcher, GameObject projectile, Transform launchPoint)
        {
            if (missileMuzzleFlashPrefab != null && launchPoint != null)
            {
                Instantiate(missileMuzzleFlashPrefab, launchPoint.position, launchPoint.rotation);
            }
        }

        private void HandleBombDropped(BombLauncher launcher, GameObject projectile, Transform dropPoint)
        {
            if (bombReleasePuffPrefab != null && dropPoint != null)
            {
                Instantiate(bombReleasePuffPrefab, dropPoint.position, dropPoint.rotation);
            }
        }
    }
}
