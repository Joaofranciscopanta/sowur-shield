using UnityEngine.Localization;

namespace SowurShield.Core
{
    /// <summary>
    /// Safe wrapper around LocalizedString.GetLocalizedString() that never throws.
    /// A LocalizedString field with no Table/Entry assigned in the Inspector (IsEmpty == true)
    /// throws ArgumentException when resolved — this returns an empty string instead, so UI code
    /// degrades gracefully until every field has been wired up in the Unity Editor.
    /// </summary>
    public static class LocalizedStringExtensions
    {
        public static string SafeGetLocalizedString(this LocalizedString localizedString)
        {
            if (localizedString == null || localizedString.IsEmpty)
                return string.Empty;

            try
            {
                return localizedString.GetLocalizedString();
            }
            catch (System.Exception)
            {
                return string.Empty;
            }
        }

        public static string SafeGetLocalizedString(this LocalizedString localizedString, params object[] arguments)
        {
            if (localizedString == null || localizedString.IsEmpty)
                return string.Empty;

            try
            {
                localizedString.Arguments = arguments;
                return localizedString.GetLocalizedString();
            }
            catch (System.Exception)
            {
                return string.Empty;
            }
        }
    }
}
