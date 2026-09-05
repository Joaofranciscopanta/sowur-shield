using UnityEngine.Localization;

namespace SowurShield.Core
{
    /// <summary>
    /// Safe wrapper around LocalizedString.GetLocalizedString() that never throws.
    /// A LocalizedString field with no Table/Entry assigned in the Inspector (IsEmpty == true)
    /// throws ArgumentException when resolved — this returns an empty string instead, so UI code
    /// degrades gracefully until every field has been wired up in the Unity Editor.
    ///
    /// GetLocalizedString() internally calls WaitForCompletion() on the underlying Addressables
    /// operation, which is unsupported on WebGL (no threads to block) and raises a native
    /// exception that a managed try/catch cannot reliably intercept there. Bail out to an empty
    /// string until LocalizationManager has preloaded every string table, instead of calling
    /// into it and triggering a load (and a WaitForCompletion) for the table.
    /// </summary>
    public static class LocalizedStringExtensions
    {
        private static bool IsLocalizationReady => LocalizationManager.AreTablesReady;

        public static string SafeGetLocalizedString(this LocalizedString localizedString)
        {
            if (localizedString == null || localizedString.IsEmpty || !IsLocalizationReady)
                return string.Empty;

            try
            {
                return SemMensagemDeErro(localizedString.GetLocalizedString());
            }
            catch (System.Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Devolve vazio quando o Unity respondeu com a sua mensagem de chave em falta.
        ///
        /// `GetLocalizedString()` NAO lanca quando a entrada nao existe -- devolve o texto
        /// "No translation found for 'Key Id 20037808398524416' in ...". O try/catch nao
        /// apanha isso, e a mensagem ia direta para o ecra: o Lucas viu-a a pairar sobre o
        /// machado e sobre a madeira, em letras brancas atravessadas no mundo.
        ///
        /// Devolver vazio faz quem chama cair no seu proprio recuo (o `itemName`, por
        /// exemplo), que e um nome legivel mesmo que nao traduzido.
        /// </summary>
        private static string SemMensagemDeErro(string valor)
        {
            if (string.IsNullOrEmpty(valor)) return string.Empty;
            return valor.StartsWith("No translation found") ? string.Empty : valor;
        }

        public static string SafeGetLocalizedString(this LocalizedString localizedString, params object[] arguments)
        {
            if (localizedString == null || localizedString.IsEmpty || !IsLocalizationReady)
                return string.Empty;

            try
            {
                localizedString.Arguments = arguments;
                return SemMensagemDeErro(localizedString.GetLocalizedString());
            }
            catch (System.Exception)
            {
                return string.Empty;
            }
        }
    }
}
