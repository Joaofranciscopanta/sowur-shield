using System.Collections.Generic;
using SowurShield.Dialogue;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Como o painel do codex fala com os assets, sem depender do Editor.
    ///
    /// Mesmo motivo do <see cref="IDialogueBridge"/>: escrever numa StringTable exige
    /// `UnityEditor.Localization`, e o assembly de runtime nao pode ver assemblies de
    /// Editor -- referenciar isso quebraria a build de jogador.
    ///
    /// O codex tem uma diferenca em relacao ao dialogo, e e a razao de existir uma
    /// interface propria: um no de dialogo tem UM texto, e uma entrada de codex tem
    /// DOIS (titulo e corpo) mais um limiar de relacionamento, que nao e texto nenhum
    /// e por isso nao passa pela tabela.
    /// </summary>
    public interface ICodexBridge
    {
        /// <summary>Os NPCs da cena que tem codex (bio ou lore).</summary>
        List<NPCDialogueInteractable> Personagens();

        List<string> Idiomas();
        string RotuloDoIdioma(string codigo);

        /// <summary>A bio do personagem num idioma.</summary>
        string LerBio(NPCDialogueInteractable npc, string idioma);
        bool EscreverBio(NPCDialogueInteractable npc, string idioma, string texto);

        /// <summary>Titulo e corpo da entrada <paramref name="indice"/> num idioma.</summary>
        string LerTitulo(NPCDialogueInteractable npc, int indice, string idioma);
        string LerCorpo(NPCDialogueInteractable npc, int indice, string idioma);

        bool EscreverTitulo(NPCDialogueInteractable npc, int indice, string idioma, string texto);
        bool EscreverCorpo(NPCDialogueInteractable npc, int indice, string idioma, string texto);

        /// <summary>
        /// O relacionamento minimo para a entrada aparecer. Nao e texto, entao nao vive
        /// na tabela de traducao -- vive no proprio NPC, igual em todos os idiomas.
        /// </summary>
        float LerLimiar(NPCDialogueInteractable npc, int indice);
        bool EscreverLimiar(NPCDialogueInteractable npc, int indice, float valor);

        int QuantasEntradas(NPCDialogueInteractable npc);
    }

    /// <summary>Onde a implementacao de Editor se registra.</summary>
    public static class CodexBridge
    {
        public static ICodexBridge Atual { get; set; }
        public static bool Disponivel => Atual != null;
    }
}
