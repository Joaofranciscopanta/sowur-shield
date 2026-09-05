using System.Linq;
using System.Text;
using UnityEngine;

namespace SowurShield.Debugging
{
    /// <summary>
    /// Verifica, DENTRO DA BUILD, as coisas que so quebram fora do Editor.
    ///
    /// Existe porque a build e o Play Mode divergiram de forma silenciosa: o
    /// `PrefabCatalog.Tudo()` estava inteiro dentro de `#if UNITY_EDITOR` e devolvia uma
    /// lista VAZIA no jogo montado -- a paleta do editor de mapa abria sem um unico item e
    /// o pincel nao pintava nada, sem nenhuma mensagem. No Editor tudo parecia bem.
    ///
    /// Roda so quando o jogo e iniciado com `-selfcheck` e escreve no log do player, entao
    /// nao custa nada a quem joga. E a unica forma de verificar estas coisas sem ser um
    /// humano a abrir a build e olhar.
    /// </summary>
    public static class BuildSelfCheck
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Instalar()
        {
            if (!System.Array.Exists(System.Environment.GetCommandLineArgs(),
                                     a => a == "-selfcheck"))
                return;

            var go = new GameObject("BuildSelfCheck");
            go.AddComponent<Executor>();
            Object.DontDestroyOnLoad(go);
        }

        private class Executor : MonoBehaviour
        {
            private void Start() => StartCoroutine(Correr());

            private System.Collections.IEnumerator Correr()
            {
                // O jogo abre no MainMenu; o que ha para verificar vive na SampleScene.
                // Carregar diretamente evita depender de clicar em "Novo Jogo".
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "SampleScene")
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
                    yield return null;
                }

                // Esperar as tabelas de localizacao e os Start() de toda a cena.
                yield return new WaitForSecondsRealtime(6f);

                var sb = new StringBuilder();
                sb.AppendLine("===== SELFCHECK DA BUILD =====");

                // O idioma ativo muda o que se espera dos nomes de item: sem isto, ver
                // "Wood" em vez de "Madeira" parece falta de traducao quando pode ser
                // apenas o jogo a correr em ingles.
                var locale = UnityEngine.Localization.Settings.LocalizationSettings
                             .SelectedLocaleAsync.IsDone
                    ? UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale
                    : null;
                sb.AppendLine($"[IDIOMA] {(locale != null ? locale.Identifier.Code : "<nao resolvido>")}");

                Catalogo(sb);
                Traducoes(sb);
                Alcances(sb);
                Dock(sb);
                Plantacoes(sb);
                Tutorial(sb);
                Assets(sb);
                Codex(sb);
                Conversa(sb);
                Dormir(sb);

                sb.AppendLine("===== FIM DO SELFCHECK =====");
                Debug.Log(sb.ToString());

                yield return new WaitForSecondsRealtime(1f);
                Application.Quit();
            }

            /// <summary>O defeito principal: catalogo vazio fora do Editor.</summary>
            private static void Catalogo(StringBuilder sb)
            {
                var tudo = SowurShield.MapEditor.PrefabCatalog.Tudo();
                int pessoas = tudo.Count(SowurShield.MapEditor.PrefabCatalog.EhNPC);
                sb.AppendLine($"[CATALOGO] entradas={tudo.Count} pessoas={pessoas} " +
                              $"cenario={tudo.Count - pessoas}");

                if (tudo.Count == 0)
                {
                    sb.AppendLine("[CATALOGO] FALHA: vazio — a paleta abriria sem nada.");
                    return;
                }

                // Resolver de volta e o que o pincel precisa para instanciar.
                int resolvem = tudo.Count(e =>
                    SowurShield.MapEditor.PrefabCatalog.Resolver(e.Caminho) != null);
                sb.AppendLine($"[CATALOGO] resolvem de volta: {resolvem}/{tudo.Count}"
                              + (resolvem == tudo.Count ? "" : "  <<< FALHA"));
            }

            /// <summary>O texto "No translation found" que aparecia sobre os itens.</summary>
            private static void Traducoes(StringBuilder sb)
            {
                var itens = Resources.LoadAll<SowurShield.Inventory.Item>("");
                var maus = itens
                    .Where(i => i != null)
                    .Select(i => new { i.itemName, Nome = i.GetDisplayName() })
                    .Where(x => string.IsNullOrEmpty(x.Nome)
                             || x.Nome.StartsWith("No translation"))
                    .ToList();

                sb.AppendLine($"[TRADUCAO] itens={itens.Length} com problema={maus.Count}"
                              + (maus.Count == 0 ? "" : "  <<< FALHA"));
                foreach (var m in maus)
                    sb.AppendLine($"[TRADUCAO]   {m.itemName} -> \"{m.Nome}\"");

                // Amostra do que esta certo, para se ver que a tabela carregou mesmo.
                var ok = itens.Where(i => i != null && i.itemName == "Wood"
                                       || i != null && i.itemName == "Axe").ToList();
                foreach (var i in ok)
                    sb.AppendLine($"[TRADUCAO]   ok: {i.itemName} -> \"{i.GetDisplayName()}\"");
            }

            /// <summary>Alcance de interacao: nenhum NPC pode passar dos outros.</summary>
            private static void Alcances(StringBuilder sb)
            {
                var npcs = Object.FindObjectsByType<SowurShield.Dialogue.NPCDialogueInteractable>(
                    FindObjectsSortMode.None);
                int grandes = npcs.Count(n => n.GetInteractionRange() > 1.6f);
                sb.AppendLine($"[ALCANCE] NPCs={npcs.Length} com alcance>1.6={grandes}"
                              + (grandes == 0 ? "" : "  <<< FALHA"));
            }

            /// <summary>O dock tem de existir e SEPARAR de facto duas janelas abertas.</summary>
            private static void Dock(StringBuilder sb)
            {
                var dock = SowurShield.UI.WindowDock.Instance;
                sb.AppendLine($"[DOCK] instalado={dock != null}"
                              + (dock != null ? "" : "  <<< FALHA"));
                if (dock == null) return;

                // Abrir o comedouro e o inventario, que era o par que se sobrepunha.
                var um = Object.FindFirstObjectByType<SowurShield.Core.UIManager>();
                var trough = Object.FindFirstObjectByType<SowurShield.Animals.FeedingTrough>();
                var inv = Object.FindFirstObjectByType<SowurShield.Inventory.Inventory>();
                if (um == null || trough == null || inv == null)
                {
                    sb.AppendLine("[DOCK] sem o par para testar (comedouro/inventario)");
                    return;
                }

                um.TryOpenWindow(trough as SowurShield.Core.IUIWindow);
                if (!inv.IsInventoryOpen) inv.ToggleInventory();

                Canvas.ForceUpdateCanvases();

                var a = Caixa("TroughPanel");
                var b = Caixa("InventoryPanelBG");
                sb.AppendLine($"[DOCK] janelas={dock.Contagem}  comedouro y {a.x:0}..{a.y:0}" +
                              $"  inventario y {b.x:0}..{b.y:0}");

                float sobreposicao = Mathf.Min(a.y, b.y) - Mathf.Max(a.x, b.x);
                sb.AppendLine($"[DOCK] sobreposicao={sobreposicao:0}px"
                              + (sobreposicao > 0f ? "  <<< FALHA" : ""));

                if (inv.IsInventoryOpen) inv.ToggleInventory();
                um.ForceCloseAllWindows();
            }

            /// <summary>Extremos verticais (min, max) de um painel ativo, em pixels de ecra.</summary>
            private static Vector2 Caixa(string nome)
            {
                foreach (var rt in Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
                {
                    if (rt.gameObject.name != nome || !rt.gameObject.activeInHierarchy) continue;
                    var c = new Vector3[4];
                    rt.GetWorldCorners(c);
                    return new Vector2(c[0].y, c[2].y);
                }
                return Vector2.zero;
            }

            /// <summary>O contador tem de contar ate a colheita.</summary>
            private static void Plantacoes(StringBuilder sb)
            {
                var crops = Resources.LoadAll<SowurShield.Core.CropData>("")
                    .Where(c => c != null && c.TotalStages > 1).ToList();

                foreach (var c in crops.Take(3))
                    sb.AppendLine($"[PLANTA] {c.name}: {c.TotalStages} fases x " +
                                  $"{c.daysPerStage} dias = {c.TotalStages * c.daysPerStage} " +
                                  "ate colher");
            }

            /// <summary>
            /// O passo 1 tem de completar pelo caminho que o jogo usa mesmo.
            ///
            /// Ha DOIS caminhos para arar: o `TillSoil` (bloco que ja existia) e o
            /// `TillSoilDirectly` (chao virgem, que e o que o CursorController chama).
            /// So o primeiro notificava o tutorial, entao arar no jogo nao completava
            /// passo nenhum. Verificamos o segundo, que era o partido.
            /// </summary>
            private static void Tutorial(StringBuilder sb)
            {
                var tm = SowurShield.Core.TutorialManager.Instance;
                if (tm == null) { sb.AppendLine("[TUTORIAL] sem TutorialManager na cena"); return; }

                var campo = typeof(SowurShield.Core.TutorialManager).GetField(
                    "_currentStepIndex",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                System.Func<int> passo = () => campo == null ? -99 : (int)campo.GetValue(tm);

                // O save da build pode ter o tutorial ja concluido -- e ai o StartTutorial
                // sai logo e o indice fica em -1, o que parece "preso" sem ser. Zeramos o
                // estado para medir o comportamento, nao o save.
                var concluido = typeof(SowurShield.Core.TutorialManager).GetField(
                    "_tutorialComplete",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                sb.AppendLine($"[TUTORIAL] save trazia concluido={concluido?.GetValue(tm)}");
                concluido?.SetValue(tm, false);

                var feitos = typeof(SowurShield.Core.TutorialManager).GetField(
                    "_completedStepIds",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                (feitos?.GetValue(tm) as System.Collections.Generic.HashSet<string>)?.Clear();

                tm.StartTutorial();
                int antes = passo();

                var go = new GameObject("SelfCheckSoil");
                var solo = go.AddComponent<SowurShield.Core.SoilBlockInteractable>();
                solo.TillSoilDirectly();
                int depois = passo();
                Object.Destroy(go);

                sb.AppendLine($"[TUTORIAL] arar chao virgem: passo {antes} -> {depois} " +
                              $"({(depois > antes ? "avanca" : "PRESO")})");
            }

            /// <summary>
            /// As lacunas de asset fechadas a 2026-09-05, medidas DENTRO da build.
            ///
            /// Os testes de EditMode ja cobrem isto, mas leem pelo AssetDatabase, que nao
            /// existe no jogo montado. Aqui a leitura e por Resources, que e o caminho
            /// real -- um asset fora de `Assets/Resources/` existe no Editor e SOME na
            /// build, que foi exatamente o que aconteceu com a paleta do editor de mapa.
            /// </summary>
            private static void Assets(StringBuilder sb)
            {
                int inimigos = 0, semSprite = 0;
                foreach (var e in Resources.LoadAll<SowurShield.Combat.EnemyData>("Enemies"))
                {
                    if (e == null) continue;
                    inimigos++;
                    if (e.sprite == null) { semSprite++; sb.AppendLine($"[ASSETS]   inimigo sem sprite: {e.name}"); }
                }
                sb.AppendLine($"[ASSETS] inimigos={inimigos} sem sprite={semSprite}");

                int animais = 0, semArte = 0, semAnim = 0;
                foreach (var a in Resources.LoadAll<SowurShield.Animals.AnimalData>("Animals"))
                {
                    if (a == null) continue;
                    animais++;
                    if (a.idleSprite == null) { semArte++; sb.AppendLine($"[ASSETS]   animal sem sprite: {a.name}"); }
                    if (a.animatorController == null) { semAnim++; sb.AppendLine($"[ASSETS]   animal sem animator: {a.name}"); }
                }
                sb.AppendLine($"[ASSETS] animais={animais} sem sprite={semArte} sem animator={semAnim}");

                int skills = 0, semIcone = 0;
                foreach (var s in Resources.LoadAll<SowurShield.Animals.AnimalSkill>("AnimalSkills"))
                {
                    if (s == null) continue;
                    skills++;
                    if (s.skillIcon == null) { semIcone++; sb.AppendLine($"[ASSETS]   skill sem icone: {s.name}"); }
                }
                sb.AppendLine($"[ASSETS] skills={skills} sem icone={semIcone}");
            }

            /// <summary>
            /// O codex tem de traduzir. Durante meses nao traduziu: as 81 entradas
            /// estavam em portugues cru com keyId 0, e como a build abre em `en`, quem
            /// jogava em ingles via o jogo todo traduzido e o codex em portugues.
            ///
            /// ⚠️ Verificado AQUI e nao so por teste porque um LocalizedString mal ligado
            /// devolve a string "No translation found..." em vez de lancar -- e essa
            /// string passaria por um teste que so verificasse "nao esta vazio".
            /// </summary>
            private static void Codex(StringBuilder sb)
            {
                int npcs = 0, bios = 0, entradas = 0, quebrados = 0;

                foreach (var npc in Object.FindObjectsByType<SowurShield.Dialogue.NPCDialogueInteractable>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (npc == null || npc.GetTotalLoreCount() == 0) continue;
                    npcs++;

                    string bio = npc.GetBio();
                    if (string.IsNullOrEmpty(bio) || bio.Contains("No translation found"))
                    {
                        quebrados++;
                        sb.AppendLine($"[CODEX]   {npc.gameObject.name}: bio nao resolve");
                    }
                    else bios++;

                    foreach (var e in npc.GetUnlockedLore())
                    {
                        entradas++;
                        if (string.IsNullOrEmpty(e.GetTitle()) || e.GetTitle().Contains("No translation found") ||
                            string.IsNullOrEmpty(e.GetBody())  || e.GetBody().Contains("No translation found"))
                        {
                            quebrados++;
                            sb.AppendLine($"[CODEX]   {npc.gameObject.name}: lore nao resolve");
                        }
                    }
                }

                sb.AppendLine($"[CODEX] npcs={npcs} bios ok={bios} lore visivel={entradas} quebrados={quebrados}");

                // Uma amostra do texto real: se o idioma for `en` e isto sair em
                // portugues, a ligacao caiu para o campo cru sem ninguem dar por isso.
                foreach (var npc in Object.FindObjectsByType<SowurShield.Dialogue.NPCDialogueInteractable>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (npc == null || npc.GetTotalLoreCount() == 0) continue;
                    var b = npc.GetBio();
                    sb.AppendLine($"[CODEX]   amostra {npc.gameObject.name}: \"" +
                                  (b.Length > 50 ? b.Substring(0, 50) + "..." : b) + "\"");
                    break;
                }
            }

            /// <summary>
            /// Falar com um NPC tem de abrir a conversa, nao travar o jogo.
            ///
            /// Relatado a jogar a build: interagir fazia "um barulho tipo de erro" e
            /// travava. O `StartDialogue` poe `isDialogueActive = true` e chama
            /// `DisableMovement()` ANTES de `dialogueUI.StartDialogue(...)` -- se algo
            /// depois disso rebentar, o jogador fica congelado sem conversa nenhuma, que
            /// e exatamente o sintoma.
            ///
            /// Aqui chamamos o `Interact()` de cada NPC dentro de try/catch e reportamos
            /// o estado em que o jogo ficou.
            /// </summary>
            private static void Conversa(StringBuilder sb)
            {
                var ui = Object.FindFirstObjectByType<SowurShield.Dialogue.DialogueTreeUI>(
                    FindObjectsInactive.Include);
                sb.AppendLine($"[CONVERSA] DialogueTreeUI na cena: {(ui != null ? "sim" : "NAO")}");

                var jogador = Object.FindFirstObjectByType<SowurShield.Core.PlayerMove>(
                    FindObjectsInactive.Include);

                int total = 0, abriu = 0, semFala = 0, rebentou = 0;

                foreach (var npc in Object.FindObjectsByType<SowurShield.Dialogue.NPCDialogueInteractable>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    // Inativos na cena sao placeholders (generic_npc, chicken): o jogador
                    // nunca os ve, entao "nao abriram" neles nao e defeito nenhum.
                    if (npc == null || !npc.gameObject.activeInHierarchy) continue;
                    total++;

                    bool podia = npc.CanInteract();
                    try
                    {
                        npc.Interact();

                        // O que interessa nao e "nao lancou" -- e se a conversa ABRIU.
                        // Um NPC que poe isDialogueActive e nao mostra UI e o caso que
                        // trava o jogo.
                        bool ativo = npc.IsDialogueActive();
                        if (ativo) abriu++;
                        else if (!podia) semFala++;
                        else
                        {
                            rebentou++;
                            sb.AppendLine($"[CONVERSA]   {npc.gameObject.name}: podia falar e nao abriu");
                        }

                        // Fechar pelo mesmo caminho que o jogo usa, senao o proximo NPC
                        // media o estado deixado por este.
                        if (ativo && ui != null) ui.EndDialogue();
                    }
                    catch (System.Exception ex)
                    {
                        rebentou++;
                        sb.AppendLine($"[CONVERSA]   {npc.gameObject.name} LANCOU: " +
                                      ex.GetType().Name + " - " + ex.Message);
                    }
                }

                sb.AppendLine($"[CONVERSA] npcs={total} abriram={abriu} sem fala={semFala} problema={rebentou}");

                // Se o jogador ficou sem movimento depois disto, o jogo esta travado.
                if (jogador != null)
                    sb.AppendLine($"[CONVERSA] jogador pode mover no fim: {jogador.IsMovementEnabled()}");
            }

            /// <summary>
            /// Dormir tem de deixar a pilha de janelas LIMPA.
            ///
            /// Relatado a jogar a build: "coloquei as sementes no lugar para os animais
            /// comerem, quando fui dormir no outro dia tudo estava dando Denied em todos
            /// os botoes". Uma janela esquecida na pilha faz o TryOpenWindow recusar
            /// TODAS as outras -- e durante o fade de sono o painel some de vista, entao
            /// parecia fechado.
            /// </summary>
            private static void Dormir(StringBuilder sb)
            {
                var um = SowurShield.Core.UIManager.Instance;
                if (um == null) { sb.AppendLine("[DORMIR] sem UIManager"); return; }

                var cama = Object.FindFirstObjectByType<SowurShield.Core.BedInteractable>(
                    FindObjectsInactive.Include);
                if (cama == null) { sb.AppendLine("[DORMIR] sem cama na cena"); return; }

                // Uma janela qualquer, aberta e esquecida.
                SowurShield.Core.IUIWindow vitima = null;
                foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (mb is SowurShield.Core.IUIWindow j && !(mb is SowurShield.Core.BedInteractable))
                    { vitima = j; break; }

                if (vitima == null) { sb.AppendLine("[DORMIR] sem janela para testar"); return; }

                um.ForceCloseAllWindows();
                bool abriu = um.TryOpenWindow(vitima);
                sb.AppendLine($"[DORMIR] janela '{vitima.WindowName}' aberta antes de dormir: {abriu}");

                // Correr so o inicio da SleepSequence: a limpeza acontece antes do fade,
                // e nao queremos esperar o dia inteiro avancar dentro do selfcheck.
                var mi = typeof(SowurShield.Core.BedInteractable).GetMethod("SleepSequence",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (mi != null)
                {
                    var rotina = mi.Invoke(cama, null) as System.Collections.IEnumerator;
                    if (rotina != null) rotina.MoveNext();
                }

                // O teste real: depois de dormir, outra janela abre?
                bool depois = um.TryOpenWindow(vitima);
                sb.AppendLine($"[DORMIR] abrir uma janela depois de dormir: " +
                              (depois ? "OK" : "RECUSADO -> Denied em todos os botoes"));
                um.ForceCloseAllWindows();
            }
        }
    }
}
