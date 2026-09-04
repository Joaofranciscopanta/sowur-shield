using System.Collections.Generic;
using SowurShield.Dialogue;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Como o painel de dialogo fala com os assets, sem depender do Editor.
    ///
    /// Escrever nas tabelas de localizacao exige `UnityEditor.Localization`, que so
    /// existe no Editor. Referenciar isso a partir de SowurShield.Runtime quebraria
    /// a build de jogador — o assembly de runtime nao pode ver assemblies de Editor.
    ///
    /// Entao o painel (runtime) conhece apenas esta interface, e a implementacao
    /// vive em SowurShield.MapEditor.Editor, que se registra sozinho no arranque.
    /// Sem implementacao registrada o painel simplesmente nao aparece, em vez de
    /// quebrar a compilacao.
    /// </summary>
    public interface IDialogueBridge
    {
        List<DialogueTree> Arvores();
        List<string> Idiomas();
        string RotuloDoIdioma(string codigo);
        string LerTexto(DialogueNode no, string idioma);
        bool EscreverTexto(DialogueTree arvore, DialogueNode no, string idioma, string texto);
    }

    /// <summary>Onde a implementacao de Editor se registra.</summary>
    public static class DialogueBridge
    {
        public static IDialogueBridge Atual { get; set; }
        public static bool Disponivel => Atual != null;
    }
}
