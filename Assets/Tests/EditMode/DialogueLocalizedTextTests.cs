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
/// nele NAO GRAVA NADA. O campo de texto daquele inspector nunca escreveu fala
/// nenhuma; quem digitava via o texto sumir.
///
/// O Unity ate loga "type is not a supported string value" a cada toque no campo —
/// mas sem dizer QUAL campo, uma vez por frame enquanto o inspector estava aberto.
/// No meio do ruido do console, ninguem ligou o erro ao texto que desaparecia.
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

            // O tipo E a prova: `stringValue` so existe para propriedades String, e
            // este campo e Generic. Nao tocamos nele aqui de proposito — CADA toque
            // (leitura inclusive) faz o Unity logar "type is not a supported string
            // value", e o test runner trata log de erro como falha.
            Assert.AreEqual(SerializedPropertyType.Generic, prop.propertyType,
                "LocalizedString e Generic, nao String. Editar por .stringValue escreve " +
                "no vazio: o texto some e o inspector fica cuspindo erro no console.");
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
            "SerializedProperty — la ele se perde.");

        Assert.IsTrue(fonte.Contains("DesenharTextoLocalizado"),
            "O inspector precisa desenhar os campos por idioma.");
    }

    /// <summary>
    /// O campo tem que carimbar o KeyId, nao so o nome. Medido: 25 dos 58 nos do
    /// jogo tem KeyId 0 e ainda resolvem (o Unity cai para a busca por nome), mas o
    /// id sobrevive a renomear a chave na tabela — e e o que o VillagerDialogueFactory
    /// ja fazia.
    /// </summary>
    [Test]
    public void CampoDeTexto_CarimbaOKeyIdNaoSoONome()
    {
        var fonte = System.IO.File.ReadAllText(System.IO.Path.Combine(
            Application.dataPath, "Scripts/Dialogue/Editor/LocalizedTextField.cs"));

        Assert.IsTrue(fonte.Contains("TableEntryReference = entrada.Id"),
            "Sem carimbar o Id, o no fica preso a chave pelo NOME: renomear a chave " +
            "na tabela quebra a referencia em silencio.");
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
