using UnityEngine;

namespace HelicopterCombat.Combat
{
    public interface IDamageable
    {
        void ApplyDamage(float damage, GameObject source);
    }
}
