using System.Collections.Generic;
using UnityEngine;

namespace HelicopterCombat.Combat
{
    [DisallowMultipleComponent]
    public sealed class Explosion : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damage = 60f;
        [SerializeField, Min(0.1f)] private float radius = 7f;
        [SerializeField, Min(0f)] private float force = 450f;
        [SerializeField, Min(0f)] private float upwardForce = 0.75f;
        [SerializeField, Min(0.1f)] private float lifeTime = 2.5f;
        [SerializeField] private LayerMask damageLayers = ~0;

        private static readonly Collider[] Hits = new Collider[64];
        private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        private GameObject sourceRoot;
        private TeamMember sourceTeamMember;
        private bool initialized;

        public void ConfigureDefaults(float configuredDamage, float configuredRadius, float configuredForce, float configuredUpwardForce, float configuredLifeTime)
        {
            damage = configuredDamage;
            radius = configuredRadius;
            force = configuredForce;
            upwardForce = configuredUpwardForce;
            lifeTime = configuredLifeTime;
        }

        public void Initialize(float configuredDamage, float configuredRadius, float configuredForce, GameObject source)
        {
            damage = configuredDamage;
            radius = configuredRadius;
            force = configuredForce;
            sourceRoot = source;
            sourceTeamMember = sourceRoot != null ? sourceRoot.GetComponent<TeamMember>() : null;
            ApplyExplosion();
        }

        private void Start()
        {
            if (!initialized)
            {
                ApplyExplosion();
            }
        }

        private void ApplyExplosion()
        {
            initialized = true;
            damagedTargets.Clear();

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                radius,
                Hits,
                damageLayers,
                QueryTriggerInteraction.Ignore);

            for (int index = 0; index < hitCount; index++)
            {
                Collider hit = Hits[index];

                if (hit == null || IsSourceCollider(hit))
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable != null && !IsFriendlyTarget(hit) && damagedTargets.Add(damageable))
                {
                    damageable.ApplyDamage(damage, sourceRoot);
                }

                Rigidbody attachedRigidbody = hit.attachedRigidbody;

                if (attachedRigidbody != null && !attachedRigidbody.isKinematic)
                {
                    attachedRigidbody.AddExplosionForce(
                        force,
                        transform.position,
                        radius,
                        upwardForce,
                        ForceMode.Impulse);
                }
            }

            Destroy(gameObject, lifeTime);
        }

        private bool IsSourceCollider(Collider hit)
        {
            if (sourceRoot == null)
            {
                return false;
            }

            Transform hitTransform = hit.transform;
            Transform sourceTransform = sourceRoot.transform;
            return hitTransform == sourceTransform || hitTransform.IsChildOf(sourceTransform);
        }

        private bool IsFriendlyTarget(Collider hit)
        {
            if (sourceTeamMember == null || hit == null)
            {
                return false;
            }

            TeamMember hitTeamMember = hit.GetComponentInParent<TeamMember>();

            if (hitTeamMember == null)
            {
                return false;
            }

            return sourceTeamMember.Team != CombatTeam.Neutral &&
                sourceTeamMember.Team == hitTeamMember.Team;
        }
    }
}
