using HelicopterCombat.Combat;
using HelicopterCombat.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace HelicopterCombat.UI
{
    [DisallowMultipleComponent]
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private MissileLauncher missileLauncher;
        [SerializeField] private BombLauncher bombLauncher;
        [SerializeField] private Text healthValueText;
        [SerializeField] private Text missileCountText;
        [SerializeField] private Text bombCountText;
        [SerializeField] private Image healthBarFill;

        public void Configure(
            Health configuredPlayerHealth,
            MissileLauncher configuredMissileLauncher,
            BombLauncher configuredBombLauncher,
            Text configuredHealthValueText,
            Text configuredMissileCountText,
            Text configuredBombCountText,
            Image configuredHealthBarFill)
        {
            playerHealth = configuredPlayerHealth;
            missileLauncher = configuredMissileLauncher;
            bombLauncher = configuredBombLauncher;
            healthValueText = configuredHealthValueText;
            missileCountText = configuredMissileCountText;
            bombCountText = configuredBombCountText;
            healthBarFill = configuredHealthBarFill;
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandleHealthChanged;
                playerHealth.HealthChanged += HandleHealthChanged;
            }

            RefreshAll();
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandleHealthChanged;
            }
        }

        private void Start()
        {
            RefreshAll();
        }

        private void Update()
        {
            RefreshAmmo();
        }

        private void HandleHealthChanged(Health changedHealth, float previousHealth, float currentHealth)
        {
            RefreshHealth();
        }

        private void RefreshAll()
        {
            RefreshHealth();
            RefreshAmmo();
        }

        private void RefreshHealth()
        {
            if (playerHealth == null)
            {
                return;
            }

            if (healthValueText != null)
            {
                healthValueText.text = $"HEALTH {Mathf.RoundToInt(playerHealth.CurrentHealth)} / {Mathf.RoundToInt(playerHealth.MaxHealth)}";
            }

            if (healthBarFill != null)
            {
                float normalized = playerHealth.MaxHealth > 0f ? playerHealth.CurrentHealth / playerHealth.MaxHealth : 0f;
                healthBarFill.fillAmount = Mathf.Clamp01(normalized);
            }
        }

        private void RefreshAmmo()
        {
            if (missileCountText != null && missileLauncher != null)
            {
                missileCountText.text = missileLauncher.IsUnlimited
                    ? "MISSILES: INF"
                    : $"MISSILES: {missileLauncher.CurrentAmmo}";
            }

            if (bombCountText != null && bombLauncher != null)
            {
                bombCountText.text = bombLauncher.IsUnlimited
                    ? "BOMBS: INF"
                    : $"BOMBS: {bombLauncher.CurrentAmmo}";
            }
        }
    }
}
