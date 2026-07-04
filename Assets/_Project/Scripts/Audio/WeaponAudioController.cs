using HelicopterCombat.Weapons;
using UnityEngine;

namespace HelicopterCombat.Audio
{
    [DisallowMultipleComponent]
    public sealed class WeaponAudioController : MonoBehaviour
    {
        [SerializeField] private MissileLauncher missileLauncher;
        [SerializeField] private BombLauncher bombLauncher;
        [SerializeField] private AudioClip missileLaunchClip;
        [SerializeField] private AudioClip bombDropClip;

        public void Configure(
            MissileLauncher configuredMissileLauncher,
            BombLauncher configuredBombLauncher,
            AudioClip configuredMissileLaunchClip,
            AudioClip configuredBombDropClip)
        {
            missileLauncher = configuredMissileLauncher;
            bombLauncher = configuredBombLauncher;
            missileLaunchClip = configuredMissileLaunchClip;
            bombDropClip = configuredBombDropClip;
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
            if (missileLaunchClip == null || AudioOneShotService.Instance == null)
            {
                return;
            }

            Vector3 position = launchPoint != null ? launchPoint.position : transform.position;
            AudioOneShotService.Instance.Play3D(
                missileLaunchClip,
                position,
                AudioCategory.Weapons,
                0.85f,
                1f,
                1f,
                AudioRolloffMode.Logarithmic,
                6f,
                110f);
        }

        private void HandleBombDropped(BombLauncher launcher, GameObject projectile, Transform dropPoint)
        {
            if (bombDropClip == null || AudioOneShotService.Instance == null)
            {
                return;
            }

            Vector3 position = dropPoint != null ? dropPoint.position : transform.position;
            AudioOneShotService.Instance.Play3D(
                bombDropClip,
                position,
                AudioCategory.Weapons,
                0.65f,
                1f,
                1f,
                AudioRolloffMode.Logarithmic,
                5f,
                90f);
        }
    }
}
