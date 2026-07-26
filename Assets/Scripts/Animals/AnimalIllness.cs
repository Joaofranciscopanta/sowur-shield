using UnityEngine;

namespace SowurShield.Animals
{
    /// <summary>
    /// Owns an animal's neglect/illness state machine, extracted from <see cref="Animal"/>.
    ///
    /// Plain C# class (NOT a MonoBehaviour) — it has no GameObject lifecycle of its own and is
    /// held as a field by <see cref="Animal"/>. Deliberately knows nothing about inventories,
    /// items or happiness: the caller performs the medicine transaction and then calls
    /// <see cref="Cure"/>, so this class stays trivially unit-testable.
    ///
    /// Threshold is passed per call rather than stored in the constructor because
    /// <c>CombatTeamSpawner</c> assigns <c>Animal.animalData</c> via reflection AFTER
    /// <c>AddComponent&lt;Animal&gt;()</c> — a constructor-captured threshold would be read
    /// before the data exists.
    /// </summary>
    public class AnimalIllness
    {
        /// <summary>Consecutive days the animal was neither petted nor fed.</summary>
        public int NeglectDays { get; private set; }

        /// <summary>True when the animal is ill (production blocked, combat stats penalised).</summary>
        public bool IsIll { get; private set; }

        /// <summary>
        /// Advance the neglect streak by one day. Call once per day, BEFORE the daily
        /// pet/feed flags are reset.
        /// Any care action (petting OR feeding) resets the streak; illness itself is only
        /// cleared by <see cref="Cure"/>.
        /// </summary>
        /// <param name="wasCaredForToday">True if the animal was petted OR fed today.</param>
        /// <param name="thresholdDays">Consecutive neglect days required to become ill.</param>
        public void UpdateNeglect(bool wasCaredForToday, int thresholdDays)
        {
            if (wasCaredForToday)
            {
                NeglectDays = 0;
                return;
            }

            NeglectDays++;
            if (!IsIll && NeglectDays >= thresholdDays)
                IsIll = true;
        }

        /// <summary>
        /// Clear the illness and reset the neglect streak. The caller is responsible for
        /// having already consumed the cure item.
        /// </summary>
        public void Cure()
        {
            IsIll = false;
            NeglectDays = 0;
        }

        /// <summary>Restore persisted state on load (negative day counts are clamped to 0).</summary>
        public void RestoreState(int neglectDays, bool isIll)
        {
            NeglectDays = Mathf.Max(0, neglectDays);
            IsIll = isIll;
        }
    }
}
