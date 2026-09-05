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
        }
    }
}
