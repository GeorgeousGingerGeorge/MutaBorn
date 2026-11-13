using System;
using UnityEngine;

namespace MutaBorn.Gameplay
{
    // Small gameplay-to-UI notification channel. It does not change any
    // combat calculations and remains safe when no indicator canvas exists.
    public static class CombatFloatingFeedback
    {
        public static event Action<UnitData, ResourceIdentifier, int> ResourceGained;
        public static event Action<UnitController, bool> StatusApplied;
        public static event Action<UnitController, int> GuardBlocked;
        public static event Action<UnitController> GuardActivated;
        public static event Action<UnitController, bool> CaptureResolved;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscribers()
        {
            ResourceGained = null;
            StatusApplied = null;
            GuardBlocked = null;
            GuardActivated = null;
            CaptureResolved = null;

        }

        public static void ReportResourceGain(UnitData data, ResourceIdentifier resource, int amount)
        {
            if (data != null && amount > 0)
            {
                ResourceGained?.Invoke(data, resource, amount);

            }

        }

        public static void ReportStatusApplied(UnitController unit, bool isDebuff)
        {
            if (unit != null)
            {
                StatusApplied?.Invoke(unit, isDebuff);

            }

        }

        public static void ReportGuardActivated(UnitController unit)
        {
            if (unit != null)
            {
                GuardActivated?.Invoke(unit);

            }

        }

        public static void ReportCaptureResolved(UnitController target, bool succeeded)
        {
            if (target != null)
            {
                CaptureResolved?.Invoke(target, succeeded);

            }

        }

        public static void ReportGuardBlocked(UnitController unit, int preventedAmount)
        {
            if (unit != null && preventedAmount > 0)
            {
                GuardBlocked?.Invoke(unit, preventedAmount);

            }

        }

    }

}
