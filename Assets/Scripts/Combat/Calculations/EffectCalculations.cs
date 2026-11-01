using System;

namespace MutaBorn.Gameplay
{
    // Choose rounding at the call site so damage, healing, and costs
    // do not accidentally inherit different rounding rules.
    public enum CalculationRounding
    {
        Floor = 0,
        Ceiling = 1,
        NearestAwayFromZero = 2

    }

    // Pure numeric helpers. No UnitData, ScriptableObjects, or gameplay state.
    // Callers supply values from units, damage results, items, or effects.
    public static class EffectCalculations
    {
        // percent = 20 means 20%, not 0.20. Negative values are supported.
        public static double PercentageOf(double value, double percent)
        {
            ValidateFinite(value, nameof(value));
            ValidateFinite(percent, nameof(percent));

            return ValidateResult(value * (percent / 100d));

        }

        // Positive missing amount; does not modify the underlying resource.
        public static double Missing(double maximum, double current)
        {
            ValidateFinite(maximum, nameof(maximum));
            ValidateFinite(current, nameof(current));

            return ValidateResult(Math.Max(0d, maximum - current));

        }

        // Explicit checked division for configurable formulas.
        public static double Divide(double numerator, double denominator)
        {
            ValidateFinite(numerator, nameof(numerator));
            ValidateFinite(denominator, nameof(denominator));

            if (denominator == 0d)
            {
                throw new DivideByZeroException("An effect calculation cannot divide by zero.");

            }

            return ValidateResult(numerator / denominator);

        }

        // Round only once, after composing all contributions, immediately
        // before passing the result to an existing int-based gameplay operation.
        public static int ToInt(double value, CalculationRounding rounding)
        {
            ValidateFinite(value, nameof(value));

            double rounded;

            switch (rounding)
            {
                case CalculationRounding.Floor:
                {
                    rounded = Math.Floor(value);

                    break;

                }
                case CalculationRounding.Ceiling:
                {
                    rounded = Math.Ceiling(value);

                    break;

                }
                case CalculationRounding.NearestAwayFromZero:
                {
                    rounded = Math.Round(value, MidpointRounding.AwayFromZero);

                    break;

                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(rounding), rounding,
                        "Unsupported calculation rounding mode.");

                }

            }

            if (rounded > int.MaxValue || rounded < int.MinValue)
            {
                throw new OverflowException("The calculated amount is outside the range of the current int-based game resources.");

            }

            return (int)rounded;

        }

        private static void ValidateFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName,
                    "Effect calculation inputs must be finite numbers.");

            }

        }

        private static double ValidateResult(double result)
        {
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new OverflowException("An effect calculation produced a non-finite result.");

            }

            return result;

        }

    }

}
