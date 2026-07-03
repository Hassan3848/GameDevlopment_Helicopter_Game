using System;
using HelicopterCombat.Combat;
using UnityEngine;

namespace HelicopterCombat.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerDeathHandler : MonoBehaviour
    {
        [SerializeField] private HelicopterInputReader inputReader;
        [SerializeField] private HelicopterFlightController flightController;
        [SerializeField] private PlayerCombatInputReader combatInputReader;
        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private Rigidbody playerRigidbody;

        public static event Action<PlayerDeathHandler> PlayerDefeated;
        public event Action<PlayerDeathHandler> Defeated;

        public bool IsDefeated { get; private set; }

        private Health health;

        public void Configure(
            HelicopterInputReader configuredInputReader,
            HelicopterFlightController configuredFlightController,
            PlayerCombatInputReader configuredCombatInputReader,
            PlayerWeaponController configuredWeaponController,
            Rigidbody configuredRigidbody)
        {
            inputReader = configuredInputReader;
            flightController = configuredFlightController;
            combatInputReader = configuredCombatInputReader;
            weaponController = configuredWeaponController;
            playerRigidbody = configuredRigidbody;
        }

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (health != null)
            {
                health.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        private void HandleDied(Health deadHealth)
        {
            if (IsDefeated)
            {
                return;
            }

            IsDefeated = true;

            if (inputReader != null)
            {
                inputReader.enabled = false;
            }

            if (flightController != null)
            {
                flightController.enabled = false;
            }

            if (combatInputReader != null)
            {
                combatInputReader.enabled = false;
            }

            if (weaponController != null)
            {
                weaponController.enabled = false;
            }

            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            Debug.Log("GAME OVER: Player defeated. Game Over screen will be added in Milestone 6.", this);
            Defeated?.Invoke(this);
            PlayerDefeated?.Invoke(this);
        }
    }
}
