using System.Collections.Generic;
using UnityEngine;

namespace HelicopterCombat.Combat
{
    [DisallowMultipleComponent]
    public sealed class TeamMember : MonoBehaviour
    {
        private static readonly HashSet<TeamMember> ActiveMembers = new HashSet<TeamMember>();

        [SerializeField] private CombatTeam team = CombatTeam.Neutral;

        public CombatTeam Team => team;
        public static IEnumerable<TeamMember> RegisteredMembers => ActiveMembers;

        public void Configure(CombatTeam configuredTeam)
        {
            team = configuredTeam;
        }

        private void OnEnable()
        {
            ActiveMembers.Add(this);
        }

        private void OnDisable()
        {
            ActiveMembers.Remove(this);
        }
    }
}
