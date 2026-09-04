using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEditor.Localization;
using SowurShield.Dialogue;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Le e escreve dialogo a partir do jogo rodando.
    ///
    /// Vive em SowurShield.MapEditor.Editor (asmdef Editor-only) porque escrever nas
    /// tabelas exige UnityEditor.Localization. O painel, que e runtime, fala com ela
    /// pela interface IDialogueBridge — um assembly de runtime nao pode referenciar
    /// um de Editor sem quebrar a build de jogador.
    ///
    /// **Por que isto funciona em Play Mode** (nao era obvio): as StringTables sao
    /// assets, e o Unity restaura assets ao sair do Play Mode — uma fala digitada e
    /// testada se perderia. `AssetDatabase.SaveAssets()` logo apos cada escrita
    /// grava no disco antes disso acontecer. Medido: o texto sobreviveu ao stop e
    /// esta no arquivo .asset.
    ///
    /// A escrita em si e a mesma do LocalizedTextField (o campo do Inspector), que
    /// por sua vez veio do VillagerDialogueFactory — o unico caminho do projeto que
    /// ja criava dialogo funcional.
    /// </summary>
    [UnityEditor.InitializeOnLoad]
    public class DialogueRuntimeBridge : IDialogueBridge
    {
        // InitializeOnLoad roda no arranque do Editor e apos cada recompilacao, que
        // e exatamente quando o registro se perderia.
        static DialogueRuntimeBridge()
        {
            DialogueBridge.Atual = new DialogueRuntimeBridge();
        }

        private const string NomeDaTabela = "Dialogue";

        /// <summary>As arvores de dialogo que o jogo carrega (as de Resources).</summary>
        public List<DialogueTree> Arvores()
        {
            return UnityEditor.AssetDatabase.FindAssets("t:DialogueTree")
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
                .Where(p => p.Contains("/Resources/"))
                .Select(UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueTree>)
                .Where(a => a != null)
                .OrderBy(a => a.name)
                .ToList();
        }

        /// <summary>O texto de um no num idioma, ou "" se ainda nao ha entrada.</summary>
        public string LerTexto(DialogueNode no, string idioma)
        {
            if (no == null) return "";

            var colecao = LocalizationEditorSettings.GetStringTableCollection(NomeDaTabela);
            if (colecao == null) return "";

            string chave = ChaveDe(no, colecao);
            if (string.IsNullOrEmpty(chave)) return "";

            var tabela = colecao.StringTables.FirstOrDefault(
                t => t.LocaleIdentifier.Code.StartsWith(idioma));
            return tabela?.GetEntry(chave)?.LocalizedValue ?? "";
        }

        /// <summary>
        /// Grava o texto de um no num idioma e salva em disco na hora.
        /// Devolve false quando nao ha onde gravar (no sem id, tabela ausente).
        /// </summary>
        public bool EscreverTexto(DialogueTree arvore, DialogueNode no,
                                          string idioma, string texto)
        {
            if (arvore == null || no == null) return false;

            var colecao = LocalizationEditorSettings.GetStringTableCollection(NomeDaTabela);
            if (colecao == null) return false;

            string chave = ChaveDe(no, colecao);
            if (string.IsNullOrEmpty(chave))
            {
                // Sem nodeId nao ha chave possivel — e sem chave o texto nao tem onde morar.
                if (string.IsNullOrEmpty(no.nodeId)) return false;
                chave = ChavePadrao(arvore.name, no.nodeId);
            }

            var shared = colecao.SharedData;
            // AddKey so quando e nova: repetir criaria uma segunda entrada com o
            // mesmo nome e outro id, e o no apontaria para a errada.
            var entrada = shared.GetEntry(chave) ?? shared.AddKey(chave);

            var tabela = colecao.StringTables.FirstOrDefault(
                t => t.LocaleIdentifier.Code.StartsWith(idioma));
            if (tabela == null) return false;

            tabela.AddEntry(chave, texto);
            UnityEditor.EditorUtility.SetDirty(tabela);
            UnityEditor.EditorUtility.SetDirty(shared);

            // Carimbamos o KeyId alem do nome. Medido: 25 dos 58 nos do jogo tem
            // m_KeyId == 0 e AINDA resolvem — o Unity cai para a busca por nome. O
            // id e mais robusto (sobrevive a renomear a chave na tabela), e e o que
            // o VillagerDialogueFactory ja fazia; o caso fatal que ele documenta e
            // quando NEM o nome esta preenchido.
            no.dialogueText.TableReference = colecao.TableCollectionNameReference;
            no.dialogueText.TableEntryReference = entrada.Id;
            UnityEditor.EditorUtility.SetDirty(arvore);

            // O ponto que faz a edicao em Play Mode valer: sem isto, sair do Play
            // Mode restaura os assets do disco e a fala digitada some.
            UnityEditor.AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>Os idiomas da tabela, na ordem em que aparecem.</summary>
        public List<string> Idiomas()
        {
            var colecao = LocalizationEditorSettings.GetStringTableCollection(NomeDaTabela);
            if (colecao == null) return new List<string>();

            return colecao.StringTables
                .Select(t => t.LocaleIdentifier.Code)
                .OrderBy(c => c == "pt" ? 0 : c == "en" ? 1 : 2)
                .ToList();
        }

        public string RotuloDoIdioma(string codigo)
        {
            if (codigo.StartsWith("pt")) return "Português";
            if (codigo.StartsWith("es")) return "Español";
            if (codigo.StartsWith("en")) return "English";
            return codigo;
        }

        private static string ChaveDe(DialogueNode no, StringTableCollection colecao)
        {
            var referencia = no.dialogueText.TableEntryReference;
            if (!string.IsNullOrEmpty(referencia.Key)) return referencia.Key;

            if (referencia.KeyId != 0)
            {
                var entrada = colecao.SharedData.GetEntry(referencia.KeyId);
                if (entrada != null) return entrada.Key;
            }
            return "";
        }

        /// <summary>"dialogue.&lt;arvore&gt;.&lt;no&gt;", como as 322 chaves que ja existem.</summary>
        public static string ChavePadrao(string nomeDaArvore, string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return "";
            string arvore = (nomeDaArvore ?? "").Replace(" ", "_").ToLowerInvariant();
            return $"dialogue.{arvore}.{nodeId.ToLowerInvariant()}";
        }
    }
}
