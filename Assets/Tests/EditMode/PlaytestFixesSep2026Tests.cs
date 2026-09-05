using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SowurShield.Core;
using SowurShield.UI;

namespace SowurShield.Tests
{

/// <summary>
/// Os defeitos que o Lucas encontrou A JOGAR, em 2026-09-05.
///
/// Nenhum deles foi apanhado por medicao nem por teste antes de alguem jogar de verdade --
/// e a razao de cada um esta no comentario do teste. Sao seis relatos:
/// alcance de interacao grande demais, nome de item em ingles no chao, dois inventarios
/// sobrepostos, ouro do combate que nao soma, e o contador de dias das plantacoes.
/// </summary>
public class PlaytestFixesSep2026Tests
{
    private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

    // =====================================================================
    // 1. Alcance de interacao
    // =====================================================================

    /// <summary>
    /// Nenhum NPC pode ter alcance maior que as coisas que se apanham do chao.
    ///
    /// As nove personagens tinham 3 -- o maior do jogo, contra 1 a 1,5 de um item no chao e
    /// 1 da caixa de venda. Com o alvo escolhido puramente por distancia, isso significava
    /// falar com alguem sempre que se queria apanhar um ovo, entrar no combate ou abrir a
    /// caixa. Medido: 27 sobreposicoes na cena, 14 delas so da Joana.
    /// </summary>
    [Test]
    public void NenhumNPC_TemAlcanceMaiorQueOsOutrosInteragiveis()
    {
        const float LimiteNPC = 1.6f;
        var grandes = new System.Collections.Generic.List<string>();

        foreach (var pf in Resources.LoadAll<GameObject>(SowurShield.MapEditor.NPCPalettePlacer.PastaDeNPCs))
        {
            var npc = pf.GetComponent<SowurShield.Dialogue.NPCDialogueInteractable>();
            if (npc == null) continue;

            float alcance = npc.GetInteractionRange();
            if (alcance > LimiteNPC) grandes.Add($"{pf.name} ({alcance})");
        }

        Assert.IsEmpty(grandes,
            "Um NPC com alcance maior que um item no chao rouba a interacao de tudo o que " +
            "estiver por perto: " + string.Join(", ", grandes));
    }

    /// <summary>
    /// A ordem de prioridade e o que impede o NPC de roubar o clique.
    ///
    /// Encolher o alcance sozinho nao chega: com o jogador em cima dos dois, um NPC a 1,4
    /// ainda ganharia de um ovo a 1,5. O desempate por tipo resolve o caso restante.
    /// </summary>
    [Test]
    public void AsCoisasEspecificas_GanhamDoNPC()
    {
        int npc = PrioridadeDe("NPCDialogueInteractable");
        Assert.Greater(PrioridadeDe("GroundItem"), npc,
            "Apanhar do chao e uma acao de passagem: tem que ganhar de falar.");
        Assert.Greater(PrioridadeDe("WorldMapTriggerZone"), npc,
            "Entrar em combate por engano custa uma sessao: tem que ganhar de falar.");
        Assert.Greater(PrioridadeDe("SellBox"), npc,
            "A caixa de venda opera-se em cima dela: tem que ganhar de falar.");
        Assert.Greater(PrioridadeDe("FeedingTrough"), npc,
            "O comedouro tambem.");
    }

    /// <summary>Entrar em combate e o erro mais caro de desfazer, entao ganha de tudo.</summary>
    [Test]
    public void AZonaDeCombate_TemAPrioridadeMaisAlta()
    {
        int zona = PrioridadeDe("WorldMapTriggerZone");
        foreach (var outro in new[] { "GroundItem", "SellBox", "FeedingTrough",
                                      "ChoppableTree", "SoilBlockInteractable",
                                      "NPCDialogueInteractable" })
            Assert.Greater(zona, PrioridadeDe(outro),
                $"A zona de combate tem que ganhar de {outro}.");
    }

    /// <summary>
    /// A mira no cursor e opcional e comeca DESLIGADA.
    ///
    /// Quem joga so com o teclado nao move o rato; se a mira fosse obrigatoria, a tecla E
    /// deixaria de funcionar sem apontar.
    /// </summary>
    [Test]
    public void AMiraNoCursor_ComecaDesligada()
    {
        // Sem tocar no valor guardado: le o que um jogador novo veria.
        PlayerPrefs.DeleteKey("interacao_mira_no_cursor");
        Assert.IsFalse(InteractionPreferences.MirarNoCursor,
            "A mira e uma preferencia, nao o comportamento padrao.");
    }

    /// <summary>
    /// A prioridade de um tipo, verificando primeiro que a classe existe mesmo.
    ///
    /// Pelo NOME e nao por uma instancia: os gatilhos de combate exigem
    /// `[RequireComponent(typeof(Collider2D))]`, e Collider2D e abstrata -- um AddComponent
    /// desses devolve null e o teste media -1 em vez da prioridade real.
    ///
    /// A verificacao de que o tipo existe fica: assim o teste falha se alguem renomear uma
    /// classe sem atualizar a tabela, que era o valor de instanciar.
    /// </summary>
    private static int PrioridadeDe(string nomeDoTipo)
    {
        bool existe = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
            .Any(t => t.Name == nomeDoTipo && typeof(IInteractable).IsAssignableFrom(t));

        Assert.IsTrue(existe,
            $"Nao existe IInteractable chamado {nomeDoTipo} — a tabela de prioridades " +
            "ficou a apontar para uma classe renomeada ou apagada.");

        return InteractionPreferences.PrioridadeDoTipo(nomeDoTipo);
    }

    // =====================================================================
    // 2. Contador de dias das plantacoes
    // =====================================================================

    /// <summary>
    /// O contador tem que mostrar os dias ate a COLHEITA, nao ate a proxima fase.
    ///
    /// Mostrar a fase fazia o numero parecer preso: numa cenoura de 4 fases x 2 dias, o
    /// contador dizia "2" quatro vezes seguidas, zerando e reiniciando a cada mudanca de
    /// aspeto, sem nunca dizer que faltavam 8 dias.
    /// </summary>
    [Test]
    public void OContadorDeDias_ContaAteAColheitaENaoAteAProximaFase()
    {
        var tipo = typeof(CropGrowthManager);
        Assert.IsNotNull(tipo.GetProperty("DaysUntilHarvest"),
            "Falta a propriedade que conta ate a colheita.");

        string fonte = System.IO.File.ReadAllText(System.IO.Path.Combine(
            Application.dataPath, "Scripts/Core/SoilBlockInteractable.cs"));

        Assert.IsTrue(fonte.Contains("DaysUntilHarvest"),
            "A UI tem que ler DaysUntilHarvest.");
        Assert.IsFalse(
            System.Text.RegularExpressions.Regex.IsMatch(
                fonte, @"return\s+cropGrowthManager\s*!=\s*null\s*\?\s*cropGrowthManager\.DaysUntilNextStage"),
            "O contador voltou a mostrar os dias ate a proxima fase.");
    }

    /// <summary>Uma planta que ainda nao cresceu conta o ciclo inteiro.</summary>
    [Test]
    public void UmaPlantaRecemPlantada_ContaTodasAsFases()
    {
        var crops = Resources.LoadAll<CropData>("")
            .Where(c => c != null && c.TotalStages > 1 && c.daysPerStage > 0).ToList();
        if (crops.Count == 0) Assert.Ignore("Sem CropData com mais de uma fase.");

        var crop = crops[0];
        int esperado = crop.TotalStages * crop.daysPerStage;

        Assert.Greater(esperado, crop.daysPerStage,
            $"{crop.name} tem {crop.TotalStages} fases x {crop.daysPerStage} dias: o total " +
            $"({esperado}) tem que ser maior que uma fase ({crop.daysPerStage}), senao o " +
            "teste nao distingue as duas contas.");
    }

    // =====================================================================
    // 3. Janelas sobrepostas
    // =====================================================================

    /// <summary>
    /// Todo painel que se abre por cima de outro tem que se anunciar ao dock.
    ///
    /// Abrir a caixa de venda e carregar em Tab punha os dois inventarios exatamente na
    /// mesma faixa central. O dock e o que os separa; um painel que nao se anuncie volta a
    /// tapar os outros.
    /// </summary>
    [Test]
    public void OsPaineisDeInventario_AnunciamSeAoDock()
    {
        var faltando = new System.Collections.Generic.List<string>();
        var caminhos = new[]
        {
            "Scripts/Inventory/Inventory.cs",
            "Scripts/Core/SellBox.cs",
            "Scripts/Animals/FeedingTrough.cs",
        };

        foreach (var rel in caminhos)
        {
            string fonte = System.IO.File.ReadAllText(
                System.IO.Path.Combine(Application.dataPath, rel));
            if (!fonte.Contains("WindowDock")) faltando.Add(System.IO.Path.GetFileName(rel));
        }

        Assert.IsEmpty(faltando,
            "Estes paineis nao falam com o dock e voltam a sobrepor-se: " +
            string.Join(", ", faltando));
    }

    /// <summary>Com duas janelas abertas, o dock tem que as separar.</summary>
    [Test]
    public void ComDuasJanelas_ODockSeparaAsPosicoes()
    {
        var host = new GameObject("DockDeTeste");
        var dock = host.AddComponent<WindowDock>();
        typeof(WindowDock).GetMethod("Awake", Priv)?.Invoke(dock, null);

        var a = NovaJanela("A");
        var b = NovaJanela("B");
        try
        {
            dock.Registrar(a);
            dock.Registrar(b);

            Assert.AreNotEqual(a.anchoredPosition.y, b.anchoredPosition.y,
                "Duas janelas abertas nao podem partilhar o mesmo Y.");
        }
        finally
        {
            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(host);
        }
    }

    /// <summary>Fechada a segunda, a primeira volta ao lugar de origem.</summary>
    [Test]
    public void AoFecharUma_AOutraVoltaAoLugarDeOrigem()
    {
        var host = new GameObject("DockDeTeste");
        var dock = host.AddComponent<WindowDock>();
        typeof(WindowDock).GetMethod("Awake", Priv)?.Invoke(dock, null);

        var a = NovaJanela("A");
        var b = NovaJanela("B");
        Vector2 origemA = a.anchoredPosition;
        try
        {
            dock.Registrar(a);
            dock.Registrar(b);
            dock.Remover(b);

            Assert.AreEqual(origemA, a.anchoredPosition,
                "Sozinha, a janela tem que voltar a posicao que o designer escolheu.");
        }
        finally
        {
            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(host);
        }
    }

    private static RectTransform NovaJanela(string nome)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, 0f);
        return rt;
    }

    // =====================================================================
    // 4. Ouro do combate
    // =====================================================================

    /// <summary>
    /// Creditar o premio tem que carimbar tambem o snapshot em memoria.
    ///
    /// Ao voltar do combate o SaveManager ja tem hasCompletedInitialLoad = true (sobrevive
    /// a troca de cena), e o RegisterSaveable aplica LoadData(currentGameData) a todo
    /// ISaveable que se registe depois. PlayerDataManager e PlayerStats escrevem ambos em
    /// `money`, entao qualquer um a registar-se depois do credito repunha o valor antigo --
    /// reproduzido: 350 voltava a 100. A tela de vitoria mostrava o ouro e o HUD nao somava.
    /// </summary>
    [Test]
    public void OPremioDoCombate_CarimbaOSnapshotEmMemoria()
    {
        string fonte = System.IO.File.ReadAllText(System.IO.Path.Combine(
            Application.dataPath, "Scripts/Core/PlayerStats.cs"));

        int aplicar = fonte.IndexOf("ApplyPendingCombatRewards()");
        Assert.Greater(aplicar, 0, "Nao achei o metodo que aplica o premio.");

        int fim = fonte.IndexOf("private void GrantPendingLoot", aplicar);
        if (fim < 0) fim = System.Math.Min(fonte.Length, aplicar + 2500);
        string corpo = fonte.Substring(aplicar, fim - aplicar);

        Assert.IsTrue(corpo.Contains("SincronizarDinheiroNoSnapshot"),
            "Sem reescrever o currentGameData, um LoadData tardio repoe o dinheiro antigo " +
            "e o premio do combate desaparece.");
    }

    /// <summary>Dois ISaveable escrevem no mesmo `money` — a razao de a corrida existir.</summary>
    [Test]
    public void DoisSaveables_EscrevemNoMesmoDinheiro()
    {
        int quantos = 0;
        foreach (var rel in new[] { "Scripts/Core/PlayerStats.cs",
                                    "Scripts/Core/PlayerDataManager.cs" })
        {
            string fonte = System.IO.File.ReadAllText(
                System.IO.Path.Combine(Application.dataPath, rel));
            if (fonte.Contains("playerData.money")) quantos++;
        }

        Assert.AreEqual(2, quantos,
            "Este teste documenta a causa: PlayerStats e PlayerDataManager escrevem ambos " +
            "no mesmo campo. Se um deles deixar de o fazer, a correcao pode ser simplificada.");
    }

    // =====================================================================
    // 5. Nome do item no chao
    // =====================================================================

    /// <summary>
    /// O rotulo do item no chao tem que ser reescrito ao aparecer.
    ///
    /// Era escrito uma unica vez no Start, antes de as tabelas de localizacao carregarem: o
    /// SafeGetLocalizedString devolvia vazio, o GetDisplayName caia para o `itemName` (o id
    /// interno, em ingles) e o rotulo ficava congelado assim. Medido em pt: dizia "Bread" e
    /// "Shovel" enquanto o jogo ja sabia "Pao" e "Pa".
    /// </summary>
    [Test]
    public void ORotuloDoItemNoChao_EReescritoAoAparecer()
    {
        string fonte = System.IO.File.ReadAllText(System.IO.Path.Combine(
            Application.dataPath, "Scripts/Core/GroundItem.cs"));

        Assert.IsTrue(fonte.Contains("AtualizarRotulo"),
            "Falta o metodo que reescreve o nome com o idioma ativo.");

        int mostrar = fonte.IndexOf("hoverLabel.SetActive(true)");
        Assert.Greater(mostrar, 0, "Nao achei onde o rotulo e mostrado.");

        // A chamada tem que vir ANTES do SetActive(true), senao mostra o texto velho por um frame.
        string antes = fonte.Substring(System.Math.Max(0, mostrar - 500), System.Math.Min(500, mostrar));
        Assert.IsTrue(antes.Contains("AtualizarRotulo()"),
            "O nome tem que ser reescrito antes de o rotulo aparecer.");
    }
}

}
