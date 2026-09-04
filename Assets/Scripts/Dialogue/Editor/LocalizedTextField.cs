using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace SowurShield.Dialogue.Editor
{
    /// <summary>
    /// Edita o texto de um <see cref="LocalizedString"/> direto no inspector,
    /// escrevendo nas tres tabelas de idioma.
    ///
    /// Existe porque o caminho intuitivo NAO funciona: num SerializedProperty, um
    /// LocalizedString tem `propertyType == Generic`, entao `prop.stringValue` le
    /// string vazia e **escrever nele nao grava nada** — sem excecao, sem warning.
    /// O DialogueTreeEditor fazia exatamente isso em quatro lugares, o que quer
    /// dizer que o campo de texto daquele inspector nunca escreveu fala nenhuma:
    /// quem digitava via o texto sumir ao sair do campo.
    ///
    /// O texto de dialogo nao mora no DialogueTree. O no guarda uma CHAVE
    /// ("dialogue.maren.default.start") que aponta para a colecao "Dialogue", com
    /// uma tabela por idioma. Escrever uma fala e escrever nas tres.
    ///
    /// O caminho correto foi tirado de VillagerDialogueFactory.AddLocalizedLine,
    /// que e o unico lugar do projeto que ja criava dialogo que funciona.
    /// </summary>
    public static class LocalizedTextField
    {
        private const string NomeDaTabela = "Dialogue";

        /// <summary>
        /// Desenha um campo por idioma e grava as edicoes. Devolve true se algo mudou.
        ///
        /// `chaveSugerida` e usada quando o LocalizedString ainda nao aponta para
        /// chave nenhuma — um no recem-criado. Sem ela nao ha onde gravar o texto.
        /// </summary>
        public static bool Desenhar(LocalizedString alvo, string chaveSugerida, float altura = 54f)
        {
            var colecao = LocalizationEditorSettings.GetStringTableCollection(NomeDaTabela);
            if (colecao == null)
            {
                EditorGUILayout.HelpBox(
                    $"A colecao de strings '{NomeDaTabela}' nao foi encontrada. " +
                    "O texto do dialogo nao pode ser editado sem ela.",
                    MessageType.Warning);
                return false;
            }

            string chave = ChaveDe(alvo, chaveSugerida);
            if (string.IsNullOrEmpty(chave))
            {
                EditorGUILayout.HelpBox(
                    "Este no ainda nao tem chave de localizacao. Preencha o Node ID " +
                    "para que uma chave possa ser gerada.",
                    MessageType.Info);
                return false;
            }

            EditorGUILayout.LabelField("Chave", chave, EditorStyles.miniLabel);

            bool mudou = false;
            foreach (var tabela in colecao.StringTables)
            {
                string idioma = tabela.LocaleIdentifier.Code;
                var entrada = tabela.GetEntry(chave);
                string atual = entrada != null ? entrada.LocalizedValue : "";

                EditorGUILayout.LabelField(RotuloDoIdioma(idioma), EditorStyles.miniBoldLabel);
                string novo = EditorGUILayout.TextArea(atual, GUILayout.Height(altura));

                if (novo == atual) continue;

                Gravar(colecao, tabela, chave, novo);
                mudou = true;
            }

            if (mudou) CarimbarChave(alvo, colecao, chave);
            return mudou;
        }

        /// <summary>
        /// A chave que este LocalizedString usa, ou a sugerida se ele ainda nao
        /// aponta para nenhuma.
        /// </summary>
        private static string ChaveDe(LocalizedString alvo, string chaveSugerida)
        {
            if (alvo == null) return chaveSugerida;

            var referencia = alvo.TableEntryReference;
            if (!string.IsNullOrEmpty(referencia.Key)) return referencia.Key;

            // Uma referencia por id ainda nao resolvida: procuramos o nome na shared.
            if (referencia.KeyId != 0)
            {
                var colecao = LocalizationEditorSettings.GetStringTableCollection(NomeDaTabela);
                var entrada = colecao?.SharedData?.GetEntry(referencia.KeyId);
                if (entrada != null) return entrada.Key;
            }

            return chaveSugerida;
        }

        private static void Gravar(StringTableCollection colecao, StringTable tabela,
                                    string chave, string valor)
        {
            var shared = colecao.SharedData;
            // AddKey so quando a chave e nova: chamar de novo criaria uma segunda
            // entrada com o mesmo nome e outro id, e o no apontaria para a errada.
            if (shared.GetEntry(chave) == null) shared.AddKey(chave);

            Undo.RecordObject(tabela, "Editar texto do dialogo");
            tabela.AddEntry(chave, valor);
            EditorUtility.SetDirty(tabela);
            EditorUtility.SetDirty(shared);
        }

        /// <summary>
        /// Aponta o LocalizedString para a chave — por ID, nao so pelo nome.
        ///
        /// Um LocalizedString com nome mas `m_KeyId == 0` resolve para nada em
        /// runtime e desenha um balao de fala VAZIO. Este detalhe esta documentado
        /// no VillagerDialogueFactory justamente porque ja mordeu antes.
        /// </summary>
        private static void CarimbarChave(LocalizedString alvo, StringTableCollection colecao,
                                          string chave)
        {
            if (alvo == null) return;

            var entrada = colecao.SharedData.GetEntry(chave);
            if (entrada == null) return;

            alvo.TableReference = colecao.TableCollectionNameReference;
            alvo.TableEntryReference = entrada.Id;
        }

        private static string RotuloDoIdioma(string codigo)
        {
            if (codigo.StartsWith("pt")) return "Português";
            if (codigo.StartsWith("es")) return "Español";
            if (codigo.StartsWith("en")) return "English";
            return codigo;
        }

        /// <summary>
        /// A chave que um no deveria usar, no padrao do projeto:
        /// "dialogue.&lt;arvore&gt;.&lt;no&gt;", tudo minusculo.
        /// </summary>
        public static string ChavePadrao(string nomeDaArvore, string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return "";
            string arvore = (nomeDaArvore ?? "").Replace(" ", "_").ToLowerInvariant();
            return $"dialogue.{arvore}.{nodeId.ToLowerInvariant()}";
        }
    }
}
