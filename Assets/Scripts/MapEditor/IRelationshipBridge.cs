using System.Collections.Generic;
using SowurShield.Dialogue;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Como o painel de relacionamento fala com os assets, sem depender do Editor.
    ///
    /// Mesmo desenho do <see cref="IDialogueBridge"/> e pelo mesmo motivo: gravar num
    /// prefab exige `UnityEditor.PrefabUtility`, e um assembly de runtime nao pode
    /// referenciar um de Editor sem quebrar a build de jogador. O painel conhece apenas
    /// esta interface; a implementacao vive em SowurShield.MapEditor.Editor e regista-se
    /// sozinha no arranque. Sem implementacao o painel nao aparece, em vez de quebrar a
    /// compilacao.
    /// </summary>
    public interface IRelationshipBridge
    {
        /// <summary>As personagens editaveis: os prefabs sob Resources/Prefabs/NPCs.</summary>
        List<NPCDialogueInteractable> Personagens();

        /// <summary>Todo item que pode ser presente, pelo itemName -- o id interno.</summary>
        List<SowurShield.Inventory.Item> Itens();

        /// <summary>
        /// Grava as tres listas de presentes de uma personagem.
        ///
        /// Escreve no PREFAB e na INSTANCIA da cena: hoje os dois estao identicos, e
        /// gravar so num deles faria a Joana que o jogador ve divergir da Joana que a
        /// paleta coloca -- uma diferenca que so apareceria muito depois.
        /// </summary>
        bool GravarPresentes(NPCDialogueInteractable alvo,
                             IList<string> amados, IList<string> gosta, IList<string> odeia);

        /// <summary>Grava o texto de uma entrada de lore (titulo e corpo) num idioma.</summary>
        bool GravarLore(NPCDialogueInteractable alvo, int indice,
                        string idioma, string titulo, string corpo);

        /// <summary>Le o texto de uma entrada de lore num idioma.</summary>
        void LerLore(NPCDialogueInteractable alvo, int indice, string idioma,
                     out string titulo, out string corpo);

        /// <summary>Os idiomas disponiveis, na mesma ordem do painel de dialogo.</summary>
        List<string> Idiomas();

        string RotuloDoIdioma(string codigo);
    }

    /// <summary>Onde a implementacao de Editor se regista.</summary>
    public static class RelationshipBridge
    {
        public static IRelationshipBridge Atual { get; set; }
        public static bool Disponivel => Atual != null;
    }
}
