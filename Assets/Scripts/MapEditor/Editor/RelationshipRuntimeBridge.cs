using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SowurShield.Dialogue;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Le e escreve as preferencias de presente a partir do jogo rodando.
    ///
    /// Vive em SowurShield.MapEditor.Editor (asmdef Editor-only) porque gravar num prefab
    /// exige `UnityEditor.PrefabUtility`. O painel, que e runtime, fala com ela pela
    /// interface IRelationshipBridge -- ver o mesmo desenho no DialogueRuntimeBridge.
    ///
    /// **Por que isto funciona em Play Mode**: prefabs sao assets, e o Unity restaura
    /// assets ao sair do Play Mode. `AssetDatabase.SaveAssets()` logo apos cada escrita
    /// grava no disco antes disso, exatamente como a ponte de dialogo faz com as tabelas.
    /// </summary>
    [UnityEditor.InitializeOnLoad]
    public class RelationshipRuntimeBridge : IRelationshipBridge
    {
        // InitializeOnLoad roda no arranque e apos cada recompilacao, que e quando o
        // registro se perderia.
        static RelationshipRuntimeBridge()
        {
            RelationshipBridge.Atual = new RelationshipRuntimeBridge();
        }

        private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>As personagens editaveis: os prefabs que a paleta coloca.</summary>
        public List<NPCDialogueInteractable> Personagens()
        {
            return Resources.LoadAll<GameObject>(NPCPalettePlacer.PastaDeNPCs)
                .Select(go => go.GetComponent<NPCDialogueInteractable>())
                .Where(n => n != null && !string.IsNullOrEmpty(n.GetNPCId()))
                .OrderBy(n => n.GetNPCDisplayName())
                .ToList();
        }

        /// <summary>
        /// Todo item do jogo, ordenado por nome.
        ///
        /// A lista vem do ItemDatabase e nao de texto livre porque a reacao e casada por
        /// `item.itemName` (GetReactionTo): um nome que nao existe nunca dispara, e nao
        /// produz erro nenhum -- a preferencia fica morta e so se descobre testando
        /// presente por presente.
        /// </summary>
        public List<SowurShield.Inventory.Item> Itens()
        {
            return Resources.LoadAll<SowurShield.Inventory.Item>("")
                .Where(i => i != null && !string.IsNullOrEmpty(i.itemName))
                .GroupBy(i => i.itemName)
                .Select(g => g.First())
                .OrderBy(i => i.itemName)
                .ToList();
        }

        public bool GravarPresentes(NPCDialogueInteractable alvo,
                                    IList<string> amados, IList<string> gosta, IList<string> odeia)
        {
            if (alvo == null) return false;

            string id = alvo.GetNPCId();
            if (string.IsNullOrEmpty(id)) return false;

            // O prefab primeiro: e a fonte de que a paleta instancia.
            bool algum = Aplicar(alvo, amados, gosta, odeia);

            // E a instancia da cena, se houver. Gravar so no prefab deixaria a Joana que
            // o jogador ve com os presentes antigos ate alguem a recriar.
            foreach (var naCena in Object.FindObjectsByType<NPCDialogueInteractable>(
                         FindObjectsSortMode.None))
            {
                if (naCena == alvo) continue;
                if (naCena.GetNPCId() != id) continue;
                Aplicar(naCena, amados, gosta, odeia);
                algum = true;
            }

            // Sem isto, sair do Play Mode restaura os assets do disco e a edicao some.
            UnityEditor.AssetDatabase.SaveAssets();
            return algum;
        }

        private static bool Aplicar(NPCDialogueInteractable alvo,
                                    IList<string> amados, IList<string> gosta, IList<string> odeia)
        {
            var t = typeof(NPCDialogueInteractable);
            var fAmados = t.GetField("lovedGifts", Priv);
            var fGosta = t.GetField("likedGifts", Priv);
            var fOdeia = t.GetField("dislikedGifts", Priv);
            if (fAmados == null || fGosta == null || fOdeia == null) return false;

            fAmados.SetValue(alvo, amados.ToArray());
            fGosta.SetValue(alvo, gosta.ToArray());
            fOdeia.SetValue(alvo, odeia.ToArray());

            UnityEditor.EditorUtility.SetDirty(alvo);

            // Num prefab, SetDirty no componente nao chega: e preciso marcar o asset raiz
            // para o Unity gravar o .prefab.
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(alvo))
                UnityEditor.EditorUtility.SetDirty(alvo.gameObject);

            return true;
        }

        // ------------------------------------------------------------------
        // Lore -- ainda nao editavel pelo painel; ver o comentario no
        // RelationshipPalette. Os metodos existem para o contrato ficar completo.
        // ------------------------------------------------------------------

        public void LerLore(NPCDialogueInteractable alvo, int indice, string idioma,
                            out string titulo, out string corpo)
        {
            titulo = "";
            corpo = "";
            if (alvo == null) return;

            var entradas = typeof(NPCDialogueInteractable)
                .GetField("loreEntries", Priv)?.GetValue(alvo) as NpcLoreEntry[];
            if (entradas == null || indice < 0 || indice >= entradas.Length) return;

            titulo = entradas[indice].title ?? "";
            corpo = entradas[indice].body ?? "";
        }

        public bool GravarLore(NPCDialogueInteractable alvo, int indice,
                               string idioma, string titulo, string corpo)
        {
            // O lore usa LocalizedString, entao gravar exige a mesma mecanica de
            // StringTable que o DialogueRuntimeBridge faz -- e um trabalho a parte.
            return false;
        }

        public List<string> Idiomas()
        {
            return DialogueBridge.Disponivel
                ? DialogueBridge.Atual.Idiomas()
                : new List<string>();
        }

        public string RotuloDoIdioma(string codigo)
        {
            return DialogueBridge.Disponivel
                ? DialogueBridge.Atual.RotuloDoIdioma(codigo)
                : codigo;
        }
    }
}
