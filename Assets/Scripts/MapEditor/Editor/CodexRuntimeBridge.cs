using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Localization;
using SowurShield.Dialogue;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Le e escreve o codex (bio + lore dos NPCs) a partir do jogo rodando.
    ///
    /// Vive no asmdef Editor-only pelo mesmo motivo do <see cref="DialogueRuntimeBridge"/>:
    /// escrever numa StringTable exige `UnityEditor.Localization`.
    ///
    /// **Escreve sempre na tabela, nunca em texto cru.** Os campos legados (`npcBio`,
    /// `NpcLoreEntry.title/body`) sao strings simples e nao traduzem -- foi assim que as
    /// 81 entradas do codex ficaram meses so em portugues, aparecendo em portugues para
    /// quem jogava em ingles. Um painel que gravasse texto cru reproduziria esse defeito
    /// a cada entrada nova.
    ///
    /// ⚠️ `LocalizedString` num SerializedProperty e **Generic**: `.stringValue` le vazio
    /// e escrever nele nao faz nada. Tem de se descer a `m_TableEntryReference.m_KeyId`,
    /// que e o que o CodexLocalizationTool ja fazia certo -- e o inspector de DialogueTree
    /// ainda faz errado em 4 sitios.
    /// </summary>
    [InitializeOnLoad]
    public class CodexRuntimeBridge : ICodexBridge
    {
        static CodexRuntimeBridge()
        {
            CodexBridge.Atual = new CodexRuntimeBridge();
        }

        private const string NomeDaTabela = "Dialogue";

        public List<NPCDialogueInteractable> Personagens()
        {
            return Object.FindObjectsByType<NPCDialogueInteractable>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                // Sem lore nenhuma nao ha codex para editar: generic_npc e chicken sao
                // placeholders e so poluiriam a lista.
                .Where(n => n != null && n.GetTotalLoreCount() > 0)
                .OrderBy(n => n.gameObject.name)
                .ToList();
        }

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

        public int QuantasEntradas(NPCDialogueInteractable npc)
            => npc != null ? npc.GetTotalLoreCount() : 0;

        // ------------------------------------------------------------------
        // Bio
        // ------------------------------------------------------------------

        public string LerBio(NPCDialogueInteractable npc, string idioma)
            => LerCampo(npc, so => so.FindProperty("npcBioLocalized"), idioma);

        public bool EscreverBio(NPCDialogueInteractable npc, string idioma, string texto)
            => EscreverCampo(npc, so => so.FindProperty("npcBioLocalized"),
                             ChaveDaBio(npc), idioma, texto);

        // ------------------------------------------------------------------
        // Lore
        // ------------------------------------------------------------------

        public string LerTitulo(NPCDialogueInteractable npc, int i, string idioma)
            => LerCampo(npc, so => CampoDaLore(so, i, "titleLocalized"), idioma);

        public string LerCorpo(NPCDialogueInteractable npc, int i, string idioma)
            => LerCampo(npc, so => CampoDaLore(so, i, "bodyLocalized"), idioma);

        public bool EscreverTitulo(NPCDialogueInteractable npc, int i, string idioma, string texto)
            => EscreverCampo(npc, so => CampoDaLore(so, i, "titleLocalized"),
                             ChaveDaLore(npc, i, "title"), idioma, texto);

        public bool EscreverCorpo(NPCDialogueInteractable npc, int i, string idioma, string texto)
            => EscreverCampo(npc, so => CampoDaLore(so, i, "bodyLocalized"),
                             ChaveDaLore(npc, i, "body"), idioma, texto);

        // ------------------------------------------------------------------
        // Limiar de relacionamento (nao e texto: nao vai para a tabela)
        // ------------------------------------------------------------------

        public float LerLimiar(NPCDialogueInteractable npc, int i)
        {
            if (npc == null) return 0f;
            var so = new SerializedObject(npc);
            var p = CampoDaLore(so, i, "requiredRelationship");
            return p != null ? p.floatValue : 0f;
        }

        public bool EscreverLimiar(NPCDialogueInteractable npc, int i, float valor)
        {
            if (npc == null) return false;
            var so = new SerializedObject(npc);
            var p = CampoDaLore(so, i, "requiredRelationship");
            if (p == null) return false;

            // O relacionamento do jogo vai de -100 a 100; fora disso a entrada ficaria
            // impossivel de desbloquear (ou sempre visivel) sem aviso nenhum.
            p.floatValue = Mathf.Clamp(valor, -100f, 100f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(npc);
            MarcarCenaSuja(npc);
            return true;
        }

        // ------------------------------------------------------------------
        // Mecanica comum
        // ------------------------------------------------------------------

        private static SerializedProperty CampoDaLore(SerializedObject so, int i, string campo)
        {
            var lista = so.FindProperty("loreEntries");
            if (lista == null || i < 0 || i >= lista.arraySize) return null;
            return lista.GetArrayElementAtIndex(i).FindPropertyRelative(campo);
        }

        private static string LerCampo(NPCDialogueInteractable npc,
                                       System.Func<SerializedObject, SerializedProperty> achar,
                                       string idioma)
        {
            if (npc == null) return "";

            var colecao = LocalizationEditorSettings.GetStringTableCollection(NomeDaTabela);
            if (colecao == null) return "";

            var so = new SerializedObject(npc);
            var prop = achar(so);
            if (prop == null) return "";

            var idProp = prop.FindPropertyRelative("m_TableEntryReference.m_KeyId");
            long id = idProp != null ? idProp.longValue : 0;
            if (id == 0) return "";

            var entrada = colecao.SharedData.GetEntry(id);
            if (entrada == null) return "";

            var tabela = colecao.StringTables.FirstOrDefault(
                t => t.LocaleIdentifier.Code.StartsWith(idioma));
            return tabela?.GetEntry(entrada.Key)?.LocalizedValue ?? "";
        }

        private static bool EscreverCampo(NPCDialogueInteractable npc,
                                          System.Func<SerializedObject, SerializedProperty> achar,
                                          string chavePadrao, string idioma, string texto)
        {
            if (npc == null) return false;

            var colecao = LocalizationEditorSettings.GetStringTableCollection(NomeDaTabela);
            if (colecao == null) return false;

            var so = new SerializedObject(npc);
            var prop = achar(so);
            if (prop == null) return false;

            var shared = colecao.SharedData;

            // Se o campo ja aponta para uma entrada, escrevemos NELA -- criar outra
            // chave deixaria a antiga orfa e as traducoes ja feitas para tras.
            var idProp = prop.FindPropertyRelative("m_TableEntryReference.m_KeyId");
            long id = idProp != null ? idProp.longValue : 0;

            var entrada = id != 0 ? shared.GetEntry(id) : null;
            if (entrada == null)
            {
                entrada = shared.GetEntry(chavePadrao) ?? shared.AddKey(chavePadrao);
                if (entrada == null) return false;
            }

            var tabela = colecao.StringTables.FirstOrDefault(
                t => t.LocaleIdentifier.Code.StartsWith(idioma));
            if (tabela == null) return false;

            tabela.AddEntry(entrada.Key, texto);
            EditorUtility.SetDirty(tabela);
            EditorUtility.SetDirty(shared);

            // Religar por ID, nao por nome: um LocalizedString com o nome preenchido e
            // o id a 0 ainda resolve (o Unity cai para a busca por nome), mas o id
            // sobrevive a renomear a chave. Ver LocalizedTextField.
            var tabelaRef = prop.FindPropertyRelative("m_TableReference.m_TableCollectionName");
            if (tabelaRef != null) tabelaRef.stringValue = NomeDaTabela;
            if (idProp != null) idProp.longValue = entrada.Id;
            var chaveProp = prop.FindPropertyRelative("m_TableEntryReference.m_Key");
            if (chaveProp != null) chaveProp.stringValue = string.Empty;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(npc);
            MarcarCenaSuja(npc);

            // Sem isto, sair do Play Mode restaura os assets do disco e o texto
            // digitado some -- o mesmo motivo do DialogueRuntimeBridge.
            AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>
        /// O NPC vive na CENA, nao num prefab: SetDirty nele nao basta para a cena ser
        /// gravada. (No DialogueRuntimeBridge isto nao aparece porque la o alvo e um
        /// asset de DialogueTree.)
        /// </summary>
        private static void MarcarCenaSuja(NPCDialogueInteractable npc)
        {
            if (Application.isPlaying) return;   // em Play Mode a cena nao se grava
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                npc.gameObject.scene);
        }

        // As chaves seguem EXATAMENTE o padrao do CodexLocalizationTool ("npc.<id>.bio",
        // "npc.<id>.lore<i>.title"), para o painel escrever nas mesmas entradas em vez de
        // duplicar as 81 que ja existem com outro nome.
        //
        // ⚠️ E o `GetNPCId()`, nao o nome do GameObject: um NPC sem npcId herda o id do
        // NOME, entao dois objetos com o mesmo nome partilham codex -- e usar o nome
        // diretamente aqui divergiria do tool assim que alguem preenchesse um npcId.
        private static string ChaveDaBio(NPCDialogueInteractable npc)
            => $"npc.{npc.GetNPCId()}.bio";

        private static string ChaveDaLore(NPCDialogueInteractable npc, int i, string parte)
            => $"npc.{npc.GetNPCId()}.lore{i}.{parte}";
    }
}
