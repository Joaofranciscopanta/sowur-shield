using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SowurShield.Dialogue;
using SowurShield.Inventory;
using SowurShield.MapEditor;

namespace SowurShield.Tests
{

/// <summary>
/// Editar presentes pela paleta do editor (2026-09-05).
///
/// A regra que define o desenho: a reacao e casada por `item.itemName` -- o id interno --
/// em <see cref="NPCDialogueInteractable.GetReactionTo"/>, nunca pelo nome traduzido. Um
/// nome mal escrito produz uma preferencia que **nunca dispara e nao da erro nenhum**, e so
/// se descobre testando presente por presente. Por isso o painel oferece a lista de itens
/// que existem em vez de um campo de texto.
/// </summary>
public class RelationshipPaletteTests
{
    private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

    private static string[] Ler(NPCDialogueInteractable npc, string campo) =>
        (typeof(NPCDialogueInteractable).GetField(campo, Priv)?.GetValue(npc) as string[])
        ?? new string[0];

    /// <summary>
    /// O contrato central: toda preferencia gravada tem que existir no ItemDatabase.
    ///
    /// Este teste varre as personagens REAIS do jogo, entao pega tanto um erro de
    /// digitacao antigo como um que a paleta viesse a introduzir.
    /// </summary>
    [Test]
    public void TodoPresente_ReferenciaUmItemQueExiste()
    {
        var itens = new HashSet<string>(
            Resources.LoadAll<Item>("").Where(i => i != null).Select(i => i.itemName));
        if (itens.Count == 0) Assert.Ignore("Sem itens em Resources.");

        var quebrados = new List<string>();
        foreach (var pf in Resources.LoadAll<GameObject>(NPCPalettePlacer.PastaDeNPCs))
        {
            var npc = pf.GetComponent<NPCDialogueInteractable>();
            if (npc == null) continue;

            foreach (var campo in new[] { "lovedGifts", "likedGifts", "dislikedGifts" })
                foreach (var nome in Ler(npc, campo))
                    if (!itens.Contains(nome))
                        quebrados.Add($"{pf.name}.{campo}: \"{nome}\"");
        }

        Assert.IsEmpty(quebrados,
            "Estas preferencias apontam para itens que nao existem, entao nunca disparam " +
            "e nao produzem erro nenhum: " + string.Join(", ", quebrados));
    }

    /// <summary>
    /// Um item so pode estar numa das tres listas.
    ///
    /// GetReactionTo testa amados, depois gosta, depois odeia, e devolve a PRIMEIRA que
    /// bater: um item repetido em duas listas silenciosamente perde a segunda, e o autor
    /// nunca fica a saber qual das duas o jogo esta a usar.
    /// </summary>
    [Test]
    public void NenhumItem_EstaEmDuasListasAoMesmoTempo()
    {
        var conflitos = new List<string>();
        foreach (var pf in Resources.LoadAll<GameObject>(NPCPalettePlacer.PastaDeNPCs))
        {
            var npc = pf.GetComponent<NPCDialogueInteractable>();
            if (npc == null) continue;

            var todos = Ler(npc, "lovedGifts")
                .Concat(Ler(npc, "likedGifts"))
                .Concat(Ler(npc, "dislikedGifts"))
                .ToList();

            foreach (var g in todos.GroupBy(n => n).Where(g => g.Count() > 1))
                conflitos.Add($"{pf.name}: \"{g.Key}\" x{g.Count()}");
        }

        Assert.IsEmpty(conflitos,
            "GetReactionTo devolve a primeira lista que bater, entao a segunda e ignorada " +
            "em silencio: " + string.Join(", ", conflitos));
    }

    /// <summary>
    /// A ponte de Editor tem que estar registada, senao o painel abre inerte.
    ///
    /// O registo e por [InitializeOnLoad] no RelationshipRuntimeBridge -- se alguem mover
    /// a classe para fora do asmdef de Editor, ou tirar o atributo, o painel deixa de
    /// gravar sem nenhum erro visivel.
    /// </summary>
    [Test]
    public void APonteDeEditor_EstaRegistada()
    {
        Assert.IsTrue(RelationshipBridge.Disponivel,
            "Sem ponte registada o painel de presentes abre e nao grava nada.");
    }

    /// <summary>A ponte oferece as personagens e os itens que o painel precisa listar.</summary>
    [Test]
    public void APonte_OferecePersonagensEItens()
    {
        if (!RelationshipBridge.Disponivel) Assert.Ignore("Ponte nao registada.");

        Assert.IsNotEmpty(RelationshipBridge.Atual.Personagens(),
            "Sem personagens o painel nao tem o que editar.");
        Assert.IsNotEmpty(RelationshipBridge.Atual.Itens(),
            "Sem itens nao ha o que oferecer como presente.");
    }

    /// <summary>
    /// Cada personagem aparece uma vez so na lista.
    ///
    /// Duas entradas com o mesmo npcId seriam dois botoes editando o mesmo alvo, e o
    /// segundo sobrescreveria o primeiro sem aviso.
    /// </summary>
    [Test]
    public void CadaPersonagem_ApareceUmaVezSo()
    {
        if (!RelationshipBridge.Disponivel) Assert.Ignore("Ponte nao registada.");

        var ids = RelationshipBridge.Atual.Personagens().Select(n => n.GetNPCId()).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count(),
            "npcId repetido na lista: " + string.Join(", ", ids));
    }

    /// <summary>
    /// A lista de itens nao repete nomes.
    ///
    /// O ItemDatabase pode ter dois assets com o mesmo itemName (um duplicado esquecido
    /// numa pasta); dois botoes com o mesmo nome alternariam a mesma preferencia e o
    /// segundo pareceria nao responder.
    /// </summary>
    [Test]
    public void AListaDeItens_NaoRepeteNomes()
    {
        if (!RelationshipBridge.Disponivel) Assert.Ignore("Ponte nao registada.");

        var nomes = RelationshipBridge.Atual.Itens().Select(i => i.itemName).ToList();
        Assert.AreEqual(nomes.Count, nomes.Distinct().Count(),
            "itemName repetido: " + string.Join(", ",
                nomes.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key)));
    }

    /// <summary>
    /// Gravar escreve no prefab E na instancia da cena.
    ///
    /// Gravar so num deles faria a Joana que o jogador ve divergir da Joana que a paleta
    /// coloca -- e a diferenca so apareceria muito depois.
    /// </summary>
    [Test]
    public void Gravar_AtualizaOPrefabEAInstanciaDaCena()
    {
        if (!RelationshipBridge.Disponivel) Assert.Ignore("Ponte nao registada.");

        var personagens = RelationshipBridge.Atual.Personagens();
        if (personagens.Count == 0) Assert.Ignore("Sem personagens.");

        var prefab = personagens[0];
        string id = prefab.GetNPCId();

        // Guardar o estado real para restaurar: este teste escreve em assets do jogo.
        string[] amadosAntes = Ler(prefab, "lovedGifts");
        string[] gostaAntes = Ler(prefab, "likedGifts");
        string[] odeiaAntes = Ler(prefab, "dislikedGifts");

        var itens = RelationshipBridge.Atual.Itens();
        if (itens.Count == 0) Assert.Ignore("Sem itens.");
        string alvo = itens[0].itemName;

        var naCena = Object.Instantiate(prefab.gameObject)
                           .GetComponent<NPCDialogueInteractable>();
        try
        {
            RelationshipBridge.Atual.GravarPresentes(
                prefab, new[] { alvo }, new string[0], new string[0]);

            Assert.Contains(alvo, Ler(prefab, "lovedGifts"),
                "O prefab tem que receber a preferencia.");
            Assert.Contains(alvo, Ler(naCena, "lovedGifts"),
                "A instancia da cena com o mesmo npcId tambem: gravar so no prefab " +
                "deixaria a personagem que o jogador ve com os presentes antigos.");
        }
        finally
        {
            if (naCena != null) Object.DestroyImmediate(naCena.gameObject);
            RelationshipBridge.Atual.GravarPresentes(
                prefab, amadosAntes, gostaAntes, odeiaAntes);
        }
    }
}

}
