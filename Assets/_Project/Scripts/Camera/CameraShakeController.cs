using HelicopterCombat.Combat;
using HelicopterCombat.Core;
using UnityEngine;

namespace HelicopterCombat.CameraSystem
{
    [DefaultExecutionOrder(3000)]
    [DisallowMultipleComponent]
    public sealed class CameraShakeController : MonoBehaviour
    {
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private Health playerHealth;

        private float remainingTime;
        private float positionAmplitude;
        private float rotationAmplitude;

        public static CameraShakeController Instance { get; private set; }

        public void Configure(GameFlowController configuredGameFlowController, Health configuredPlayerHealth)
        {
            gameFlowController = configuredGameFlowController;
            playerHealth = configuredPlayerHealth;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandlePlayerHealthChanged;
                playerHealth.HealthChanged += HandlePlayerHealthChanged;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandlePlayerHealthChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void LateUpdate()
        {
            if (remainingTime <= 0f || !CanShake())
            {
                remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
                return;
            }

            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
            float normalized = remainingTime > 0f ? remainingTime : 0f;
            Vector3 positionalOffset = Random.insideUnitSphere * positionAmplitude * normalized;
            Vector3 rotationalOffset = Random.insideUnitSphere * rotationAmplitude * normalized;

            transform.position += positionalOffset;
            transform.rotation = transform.rotation * Quaternion.Euler(rotationalOffset);
        }

        public void ShakeExplosion(Vector3 worldPosition, float radius, float positionStrength, float rotationStrength, float duration)
        {
            if (!CanShake())
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, worldPosition);
            if (distance > radius)
            {
                return;
            }

            float attenuation = 1f - Mathf.Clamp01(distance / radius);
            Shake(positionStrength * attenuation, rotationStrength * attenuation, duration);
        }

        public void Shake(float positionStrength, float rotationStrength, float duration)
        {
            if (!CanShake())
            {
                return;
            }

            positionAmplitude = Mathf.Max(positionAmplitude, positionStrength);
            rotationAmplitude = Mathf.Max(rotationAmplitude, rotationStrength);
            remainingTime = Mathf.Max(remainingTime, duration);
        }

        private void HandlePlayerHealthChanged(Health changedHealth, float previousHealth, float currentHealth)
        {
            if (currentHealth < previousHealth && currentHealth > 0f)
            {
                Shake(0.22f, 1.5f, 0.30f);
            }
        }

        private bool CanShake()
        {
            return gameFlowController != null && gameFlowController.State == GameFlowController.GameFlowState.Playing;
        }
    }
}
