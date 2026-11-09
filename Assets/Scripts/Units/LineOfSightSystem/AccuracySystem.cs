using UnityEngine;

namespace MutaBorn.Gameplay
{
    public sealed class AccuracySystem : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        [Header("Base Accuracy")]
        [SerializeField, Range(0f, 100f)]
        private float baseAccuracy = 75f;

        [SerializeField, Range(0f, 100f)]
        private float minimumHitChance = 5f;

        [SerializeField, Range(0f, 100f)]
        private float maximumHitChance = 95f;

        [Header("Attribute Scaling")]
        [Tooltip("Attribue value at which half of its maximum accuracy contribution is reached")]
        [SerializeField, Min(1f)]
        private float attributeHalfSaturation = 50f;

        [SerializeField, Min(0f)]
        private float maximumAtributeContribution = 10f;

        [Header("Cover Penalties")]
        [SerializeField, Min(0f)]
        private float lowCoverPenalty = 15f;

        [SerializeField, Min(0f)]
        private float highCoverPenalty = 30f;

        [Header("Stance Modifiers")]
        [SerializeField] private float crouchingAttackerPenalty = 5f;
        [SerializeField] private float crouchingDefenderPenalty = 10f;
        [Header("Dependencies")]
        [SerializeField]
        private CoverSystem coverSystem;


        public float CalculateHitChance(UnitController attacker, UnitController defender, ActionDefinition action) 
        {
            if (attacker == null || defender == null || action == null || attacker.data == null || defender.data == null) 
            {
                return 0f;
            }

            float accuracy = baseAccuracy;

            switch (action.DamageStat) 
            {
                case CombatDamageStat.Melee:
                case CombatDamageStat.Ranged:
                    {
                        accuracy += GetAttributeContribution(attacker.data.totalDexterity);
                        accuracy += GetAttributeContribution(attacker.data.totalPerception);
                     }
                    break;
                case CombatDamageStat.Magic:
                   { 
                        accuracy += GetAttributeContribution(attacker.data.totalIntelligence);
                        accuracy += GetAttributeContribution(attacker.data.totalPerception);
                    }
                    break;
                default:
                    break;
            }
            
            //accuracy -= CalculateDefenderEvasion(defender);

            accuracy += attacker.data.accuracyBonus;

            //StanceModifier Penalties
            accuracy += GetStanceModifier(attacker, defender);

            if (coverSystem != null) 
            {
                CoverResult cover = coverSystem.CalculateCover(attacker, defender);

                accuracy -= GetCoverPenalty(cover);
            }

            

            return Mathf.Clamp(accuracy, minimumHitChance, maximumHitChance);
        }

        private float GetAttributeContribution(float attributeValue) 
        {
            float attribute = Mathf.Max(0f, attributeValue);

            float contribution = attribute / (attribute + attributeHalfSaturation);

            return contribution * maximumAtributeContribution;
        }

        private float GetCoverPenalty(CoverResult cover) 
        {
            float penalty = cover.coverState switch
            {
                CoverState.LOW => lowCoverPenalty,
                CoverState.HIGH => highCoverPenalty,
                _=>0f
            };

            return penalty * Mathf.Clamp01(cover.verticalCoverage);
        }
        private float GetStanceModifier(UnitController attacker, UnitController defender) 
        {
            float modifier = 0f;
            if (attacker.currentStance == UnitStance.Crouching) 
            {
                modifier -= crouchingAttackerPenalty;
            }

            if(defender.currentStance == UnitStance.Crouching) 
            {
                modifier -= crouchingDefenderPenalty;
            }
           

            return modifier;
        }
        //private float CalculateDefenderEvasion(UnitController defender) 
        //{
        //    float dexterity = GetAttributeContribution(defender.data.totalDexterity);
        //    float perception = GetAttributeContribution(defender.data.totalPerception);

        //    float evasion = dexterity + perception;

        //    UnitStatusController statuse = defender.GetComponent<UnitStatusController>();
        //    if (statuse != null) 
        //    {
        //        evasion += statuse.DodgeChancePercent;
        //    }

        //    if (defender.currentStance == UnitStance.Crouching)
        //    {
        //        evasion += crouchingDefenderPenalty;
        //    }
            
        //    return evasion;


        //}
        public bool RollToHit(float hitChance) 
        {
            return Random.Range(0f, 100f) < hitChance;
        }
    }
}