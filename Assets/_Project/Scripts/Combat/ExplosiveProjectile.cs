using UnityEngine;

namespace HelicopterCombat.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public abstract class ExplosiveProjectile : MonoBehaviour
    {
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField, Min(0f)] private float damage = 60f;
        [SerializeField, Min(0.1f)] private float explosionRadius = 7f;
        [SerializeField, Min(0f)] private float explosionForce = 450f;
        [SerializeField, Min(0.1f)] private float lifeTime = 5f;
        [SerializeField, Min(0f)] private float armingDelay = 0.08f;

        private Rigidbody projectileRigidbody;
        private Collider[] projectileColliders;
        private GameObject ownerRoot;
        private Rigidbody ownerRigidbody;
        private TeamMember ownerTeamMember;
        private float spawnTime;
        private bool exploded;

        protected Rigidbody ProjectileRigidbody => projectileRigidbody;
        protected Rigidbody OwnerRigidbody => ownerRigidbody;
        protected float SpawnTime => spawnTime;

        public void ConfigureExplosion(GameObject configuredExplosionPrefab, float configuredDamage, float configuredRadius, float configuredForce, float configuredLifeTime, float configuredArmingDelay)
        {
            explosionPrefab = configuredExplosionPrefab;
            damage = configuredDamage;
            explosionRadius = configuredRadius;
            explosionForce = configuredForce;
            lifeTime = configuredLifeTime;
            armingDelay = configuredArmingDelay;
        }

        public void InitializeOwner(GameObject configuredOwnerRoot, Rigidbody configuredOwnerRigidbody)
        {
            ownerRoot = configuredOwnerRoot;
            ownerRigidbody = configuredOwnerRigidbody;
            ownerTeamMember = ownerRoot != null ? ownerRoot.GetComponent<TeamMember>() : null;
            IgnoreOwnerCollisions();
            IgnoreFriendlyCollisions();
            OnOwnerInitialized();
        }

        protected virtual void Awake()
        {
            projectileRigidbody = GetComponent<Rigidbody>();
            projectileColliders = GetComponentsInChildren<Collider>();
            projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            projectileRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        protected virtual void OnEnable()
        {
            spawnTime = Time.time;
            exploded = false;
            Invoke(nameof(ExplodeFromTimeout), lifeTime);
        }

        protected virtual void OnDisable()
        {
            CancelInvoke(nameof(ExplodeFromTimeout));
        }

        protected virtual void OnOwnerInitialized()
        {
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (exploded ||
                Time.time - spawnTime < armingDelay ||
                IsOwnerCollision(collision.collider) ||
                IsFriendlyCollision(collision.collider))
            {
                return;
            }

            Explode();
        }

        private void ExplodeFromTimeout()
        {
            Explode();
        }

        protected void Explode()
        {
            if (exploded)
            {
                return;
            }

            exploded = true;

            if (explosionPrefab != null)
            {
                GameObject explosionObject = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                Explosion explosion = explosionObject.GetComponent<Explosion>();

                if (explosion != null)
                {
                    explosion.Initialize(damage, explosionRadius, explosionForce, ownerRoot);
                }
            }

            Destroy(gameObject);
        }

        private void IgnoreOwnerCollisions()
        {
            if (ownerRoot == null || projectileColliders == null)
            {
                return;
            }

            Collider[] ownerColliders = ownerRoot.GetComponentsInChildren<Collider>();

            foreach (Collider projectileCollider in projectileColliders)
            {
                foreach (Collider ownerCollider in ownerColliders)
                {
                    if (projectileCollider != null && ownerCollider != null)
                    {
                        Physics.IgnoreCollision(projectileCollider, ownerCollider, true);
                    }
                }
            }
        }

        private bool IsOwnerCollision(Collider hit)
        {
            if (ownerRoot == null || hit == null)
            {
                return false;
            }

            Transform hitTransform = hit.transform;
            Transform ownerTransform = ownerRoot.transform;
            return hitTransform == ownerTransform || hitTransform.IsChildOf(ownerTransform);
        }

        private void IgnoreFriendlyCollisions()
        {
            if (ownerTeamMember == null || ownerTeamMember.Team == CombatTeam.Neutral || projectileColliders == null)
            {
                return;
            }

            foreach (TeamMember teamMember in TeamMember.RegisteredMembers)
            {
                if (teamMember == null ||
                    teamMember == ownerTeamMember ||
                    teamMember.Team != ownerTeamMember.Team)
                {
                    continue;
                }

                Collider[] friendlyColliders = teamMember.GetComponentsInChildren<Collider>();

                foreach (Collider projectileCollider in projectileColliders)
                {
                    foreach (Collider friendlyCollider in friendlyColliders)
                    {
                        if (projectileCollider != null && friendlyCollider != null)
                        {
                            Physics.IgnoreCollision(projectileCollider, friendlyCollider, true);
                        }
                    }
                }
            }
        }

        private bool IsFriendlyCollision(Collider hit)
        {
            if (ownerTeamMember == null || ownerTeamMember.Team == CombatTeam.Neutral || hit == null)
            {
                return false;
            }

            TeamMember hitTeamMember = hit.GetComponentInParent<TeamMember>();
            return hitTeamMember != null && hitTeamMember.Team == ownerTeamMember.Team;
        }
    }
}
