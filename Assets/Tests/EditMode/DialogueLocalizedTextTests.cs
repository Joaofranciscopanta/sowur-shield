using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using SowurShield.Dialogue;

namespace SowurShield.Tests
{

/// <summary>
/// O texto de dialogo nao mora no DialogueTree (2026-09-04).
///
/// O no guarda uma CHAVE ("dialogue.maren.default.start") que aponta para a colecao
/// "Dialogue", com uma tabela por idioma. Escrever uma fala e escrever nas tres.
///
/// Estes testes existem por causa de um defeito que ninguem tinha notado: o
/// inspector de DialogueTree editava a fala com
/// `dialogueTextProp.stringValue = TextArea(...)`, e um LocalizedString num
/// SerializedProperty tem propertyType Generic — stringValue le vazio e escrever
/// nele NAO GRAVA NADA, sem excecao e sem warning. O campo de texto daquele
/// inspector nunca escreveu fala nenhuma; quem digitava via o texto sumir.
/// </summary>
public class DialogueLocalizedTextTests
{
    /// <summary>
    /// A regressao central. Se este teste passar a falhar, alguem voltou a tratar
    /// LocalizedString como string — e o editor de dialogo mente de novo.
    /// </summary>
    [Test]
    public void LocalizedString_NaoEUmCampoDeTextoComum()
    {
        var arvore = ScriptableObject.CreateInstance<DialogueTree>();
        arvore.nodes = new[] { new DialogueNode { nodeId = "start" } };

        try
        {
            var so = new SerializedObject(arvore);
            var prop = so.FindProperty("nodes").GetArrayElementAtIndex(0)
                         .FindPropertyRelative("dialogueText");

            Assert.AreEqual(SerializedPropertyType.Generic, prop.propertyType,
                "LocalizedString e Generic, nao String. Quem editar por .stringValue " +
                "escreve no vazio: o texto some e nao ha erro nenhum.");

            prop.stringValue = "isto nao deveria persistir";
            so.ApplyModifiedProperties();

            Assert.IsTrue(string.IsNullOrEmpty(prop.stringValue),
                "Escrever em stringValue num LocalizedString nao grava — e por isso " +
                "que o campo do inspector precisa passar pelas tabelas de idioma.");
        }
        finally
        {
            Object.DestroyImmediate(arvore);
        }
    }

    /// <summary>
    /// O inspector nao pode voltar a usar o atalho quebrado.
    /// </summary>
    [Test]
    public void InspectorDoDialogueTree_NaoUsaStringValueNoTexto()
    {
        var fonte = System.IO.File.ReadAllText(System.IO.Path.Combine(
            Application.dataPath, "Scripts/Dialogue/Editor/DialogueTreeEditor.cs"));

        Assert.IsFalse(fonte.Contains("dialogueTextProp.stringValue"),
            "O texto do dialogo tem que ser escrito nas tabelas de idioma, nao no " +
            "SerializedProperty — la ele se perde em silencio.");

        Assert.IsTrue(fonte.Contains("DesenharTextoLocalizado"),
            "O inspector precisa desenhar os campos por idioma.");
    }

    /// <summary>
    /// Um LocalizedString com nome mas KeyId 0 resolve para nada em runtime e desenha
    /// um balao de fala VAZIO. O VillagerDialogueFactory ja documentava isso; o campo
    /// novo do inspector tem que carimbar o id tambem.
    /// </summary>
    [Test]
    public void CampoDeTexto_CarimbaOKeyIdNaoSoONome()
    {
        var fonte = System.IO.File.ReadAllText(System.IO.Path.Combine(
            Application.dataPath, "Scripts/Dialogue/Editor/LocalizedTextField.cs"));

        Assert.IsTrue(fonte.Contains("TableEntryReference = entrada.Id"),
            "Sem carimbar o Id, o no aponta para a chave so pelo nome, m_KeyId fica 0 " +
            "e o balao de fala sai vazio em runtime.");
    }

    /// <summary>
    /// Escrever so em pt deixaria en/es no fallback — um NPC que fala portugues para
    /// quem joga em ingles.
    /// </summary>
    [Test]
    public void CampoDeTexto_EscreveEmTodosOsIdiomas()
    {
        var fonte = System.IO.File.ReadAllText(System.IO.Path.Combine(
            Application.dataPath, "Scripts/Dialogue/Editor/LocalizedTextField.cs"));

        Assert.IsTrue(fonte.Contains("foreach (var tabela in colecao.StringTables)"),
            "O campo tem que percorrer todas as tabelas da colecao, nao so uma.");
    }

    /// <summary>
    /// As chaves seguem "dialogue.&lt;arvore&gt;.&lt;no&gt;" minusculo, como as 322
    /// entradas que ja existem.
    /// </summary>
    [Test]
    public void AsArvoresExistentes_UsamChavesNoPadrao()
    {
        var caminhos = AssetDatabase.FindAssets("t:DialogueTree")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.Contains("/Resources/"))
            .ToArray();

        if (caminhos.Length == 0) Assert.Ignore("Sem arvores de dialogo em Resources.");

        int comChave = 0;
        foreach (var caminho in caminhos)
        {
            var arvore = AssetDatabase.LoadAssetAtPath<DialogueTree>(caminho);
            foreach (var no in arvore.nodes)
            {
                var chave = no.dialogueText.TableEntryReference.Key;
                if (string.IsNullOrEmpty(chave)) continue;

                comChave++;
                Assert.IsTrue(chave.StartsWith("dialogue."),
                    $"'{chave}' em {arvore.name} foge do padrao 'dialogue.<arvore>.<no>'.");
                Assert.AreEqual(chave.ToLowerInvariant(), chave,
                    $"'{chave}' tem maiuscula: as chaves do projeto sao minusculas.");
            }
        }

        Assert.Greater(comChave, 0,
            "Nenhum no tem chave — ou as arvores estao vazias, ou o campo mudou de forma.");
    }

    /// <summary>
    /// A tabela "Dialogue" tem que existir com os tres idiomas: e onde as falas vivem.
    /// </summary>
    [Test]
    public void ATabelaDeDialogo_TemOsTresIdiomas()
    {
        var tabelas = AssetDatabase.FindAssets("t:StringTable")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.Contains("StringTables/Dialogue_"))
            .ToArray();

        Assert.AreEqual(3, tabelas.Length,
            "Esperado Dialogue_en, Dialogue_pt e Dialogue_es. Uma fala escrita sem " +
            "uma delas sai em branco naquele idioma.");
    }
}

}
