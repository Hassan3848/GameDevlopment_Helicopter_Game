using HelicopterCombat.Weapons;
using UnityEngine;

namespace HelicopterCombat.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private PlayerCombatInputReader combatInputReader;
        [SerializeField] private MissileLauncher missileLauncher;
        [SerializeField] private BombLauncher bombLauncher;

        public void Configure(PlayerCombatInputReader configuredInputReader, MissileLauncher configuredMissileLauncher, BombLauncher configuredBombLauncher)
        {
            combatInputReader = configuredInputReader;
            missileLauncher = configuredMissileLauncher;
            bombLauncher = configuredBombLauncher;
        }

        private void OnEnable()
        {
            if (combatInputReader == null)
            {
                combatInputReader = GetComponent<PlayerCombatInputReader>();
            }

            if (missileLauncher == null)
            {
                missileLauncher = GetComponent<MissileLauncher>();
            }

            if (bombLauncher == null)
            {
                bombLauncher = GetComponent<BombLauncher>();
            }

            if (combatInputReader != null)
            {
                combatInputReader.FireMissilePressed += HandleFireMissilePressed;
                combatInputReader.DropBombPressed += HandleDropBombPressed;
            }
        }

        private void OnDisable()
        {
            if (combatInputReader != null)
            {
                combatInputReader.FireMissilePressed -= HandleFireMissilePressed;
                combatInputReader.DropBombPressed -= HandleDropBombPressed;
            }
        }

        private void HandleFireMissilePressed()
        {
            missileLauncher?.TryFire();
        }

        private void HandleDropBombPressed()
        {
            bombLauncher?.TryDrop();
        }
    }
}
