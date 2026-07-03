using System;
using UnityEngine;

namespace HelicopterCombat.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class EnemyUnit : MonoBehaviour
    {
        public enum EnemyUnitType
        {
            Helicopter,
            Tank
        }

        [SerializeField] private string displayName = "Enemy Unit";
        [SerializeField] private EnemyUnitType unitType = EnemyUnitType.Helicopter;

        public static event Action<EnemyUnit> AnyEnemyDestroyed;
        public event Action<EnemyUnit> Destroyed;

        public string DisplayName => displayName;
        public EnemyUnitType UnitType => unitType;
        public bool IsDestroyed { get; private set; }

        private Health health;

        public void Configure(string configuredDisplayName, EnemyUnitType configuredUnitType)
        {
            displayName = configuredDisplayName;
            unitType = configuredUnitType;
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
            if (IsDestroyed)
            {
                return;
            }

            IsDestroyed = true;
            Destroyed?.Invoke(this);
            AnyEnemyDestroyed?.Invoke(this);
        }
    }
}
