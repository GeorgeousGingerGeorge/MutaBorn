using System.Collections;
using MutaBorn.Gameplay;
using UnityEngine;

[RequireComponent(typeof(UnitController))]
public sealed class MissionBossStatBonus : MonoBehaviour
{
    [SerializeField, Range(0, 300)] private int healthBonusPercent = 75;
    [SerializeField, Range(0, 300)] private int damageBonusPercent = 35;
    [SerializeField, Range(0, 300)] private int armorBonusPercent = 25;

    private IEnumerator Start()
    {
        UnitController unit = GetComponent<UnitController>();
        while (unit.data == null)
        {
            yield return null;

        }

        unit.data.TryReplaceStatModifiers(this, new[]
        {
            new UnitData.StatModifierEntry(StatIdentifier.MaxHealth, StatModifierMode.Percent, healthBonusPercent),
            new UnitData.StatModifierEntry(StatIdentifier.MeleeDamage, StatModifierMode.Percent, damageBonusPercent),
            new UnitData.StatModifierEntry(StatIdentifier.RangedDamage, StatModifierMode.Percent, damageBonusPercent),
            new UnitData.StatModifierEntry(StatIdentifier.MagicDamage, StatModifierMode.Percent, damageBonusPercent),
            new UnitData.StatModifierEntry(StatIdentifier.PhysicalDefense, StatModifierMode.Percent, armorBonusPercent)
        });
        unit.data.RefillResourcesToMax();

    }
}
