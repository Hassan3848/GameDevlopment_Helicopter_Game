using System;
using UnityEngine;

namespace HelicopterCombat.Combat
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private bool destroyWhenDead;

        public event Action<Health, float, float> HealthChanged;
        public event Action<Health> Died;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead => CurrentHealth <= 0f;

        private bool deathRaised;

        public void Configure(float configuredMaxHealth, bool configuredDestroyWhenDead)
        {
            maxHealth = Mathf.Max(1f, configuredMaxHealth);
            destroyWhenDead = configuredDestroyWhenDead;
            CurrentHealth = maxHealth;
        }

        private void Awake()
        {
            CurrentHealth = Mathf.Clamp(maxHealth, 0f, maxHealth);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);

            if (!Application.isPlaying)
            {
                CurrentHealth = maxHealth;
            }
        }

        public void ApplyDamage(float damage, GameObject source)
        {
            if (damage <= 0f || IsDead)
            {
                return;
            }

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0f, maxHealth);
            HealthChanged?.Invoke(this, previousHealth, CurrentHealth);

            if (IsDead && !deathRaised)
            {
                deathRaised = true;
                Died?.Invoke(this);

                if (destroyWhenDead)
                {
                    Destroy(gameObject, 0.05f);
                }
            }
        }
    }
}
