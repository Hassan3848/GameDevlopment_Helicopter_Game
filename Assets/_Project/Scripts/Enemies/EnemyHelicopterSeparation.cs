using UnityEngine;

namespace HelicopterCombat.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyHelicopterSeparation : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float separationRadius = 18f;
        [SerializeField, Min(0f)] private float separationStrength = 1.5f;
        [SerializeField] private LayerMask queryLayers = ~0;

        private readonly Collider[] hits = new Collider[24];

        public Vector3 CalculateSeparation()
        {
            Vector3 separation = Vector3.zero;
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                separationRadius,
                hits,
                queryLayers,
                QueryTriggerInteraction.Ignore);

            for (int index = 0; index < hitCount; index++)
            {
                Collider hit = hits[index];

                if (hit == null)
                {
                    continue;
                }

                EnemyHelicopterBrain other = hit.GetComponentInParent<EnemyHelicopterBrain>();

                if (other == null || other.transform == transform || !other.isActiveAndEnabled || other.IsDefeated)
                {
                    continue;
                }

                Vector3 away = transform.position - other.transform.position;
                float distance = away.magnitude;

                if (distance > 0.001f && distance < separationRadius)
                {
                    separation += away.normalized * ((separationRadius - distance) / separationRadius);
                }
            }

            return separation.sqrMagnitude > 0.001f
                ? separation.normalized * separationStrength
                : Vector3.zero;
        }
    }
}
