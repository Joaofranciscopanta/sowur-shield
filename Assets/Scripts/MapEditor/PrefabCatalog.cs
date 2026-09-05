using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// O que o editor pode colocar no mundo, e como reencontrar isso depois.
    ///
    /// `ObjectSpawnData` e `NPCSpawnData` guardam um `prefabPath` desde sempre, mas
    /// nada resolvia esse caminho de volta para um prefab — por isso os objetos eram
    /// gravados no MapData e nunca reapareciam ao carregar. Este catalogo e a peca
    /// que faltava.
    ///
    /// O caminho e a identidade: um `objectId` numerico quebraria assim que alguem
    /// reordenasse a lista, e o mapa passaria a colocar arvores onde havia cercas.
    /// </summary>
    public static class PrefabCatalog
    {
        /// <summary>
        /// Pastas varridas, na ordem em que aparecem na paleta.
        ///
        /// TODAS sob Resources/ desde 2026-09-04. O catalogo em si continua sendo
        /// so-Editor (usa AssetDatabase), mas um mapa salvo agora e CARREGADO PELO
        /// JOGO, e o MapRuntimeLoader resolve os prefabs por Resources.Load. Um
        /// prefab fora de Resources/ simplesmente nao existe no build: 40 dos 57
        /// estavam assim, e todo mapa com arvore ou decoracao perderia esses objetos
        /// ao ser carregado no jogo.
        /// </summary>
        private static readonly string[] Pastas =
        {
            // NPCs em primeiro: sao o que se procura primeiro ao montar um mapa, e a
            // lista de cenario tem 57 entradas para percorrer.
            "Assets/Resources/Prefabs/NPCs",
            "Assets/Resources/Prefabs/Decorations",
            "Assets/Resources/Prefabs/FruitTrees",
            "Assets/Resources/Prefabs/Fruits",
            "Assets/Resources/Prefabs/GroundItems"
        };

        /// <summary>
        /// A pasta cujos prefabs sao pessoas, e nao cenario.
        ///
        /// A paleta precisa de distinguir os dois porque o clique vai para placers
        /// diferentes: um NPC nao se duplica nem se escala, ao contrario de uma arvore.
        /// </summary>
        public const string CategoriaNPCs = "NPCs";

        /// <summary>Se esta entrada e uma pessoa.</summary>
        public static bool EhNPC(Entrada entrada) => entrada.Categoria == CategoriaNPCs;

        public readonly struct Entrada
        {
            public readonly string Caminho;
            public readonly string Nome;
            public readonly string Categoria;

            public Entrada(string caminho, string nome, string categoria)
            {
                Caminho = caminho;
                Nome = nome;
                Categoria = categoria;
            }
        }

        private static List<Entrada> cache;

        /// <summary>
        /// Tudo o que o editor sabe colocar, agrupado pela pasta de origem.
        ///
        /// ⚠️ Varre por <c>Resources.LoadAll</c>, NAO por AssetDatabase.
        ///
        /// Ate 2026-09-05 o corpo inteiro deste metodo estava dentro de
        /// <c>#if UNITY_EDITOR</c>, e sem AssetDatabase numa build ele devolvia uma lista
        /// VAZIA. No Editor tudo funcionava; no jogo montado a paleta abria sem um unico
        /// item e o pincel nao pintava nada -- e nada no ecra dizia porque. O Lucas
        /// encontrou isto a jogar a build, depois de a mesma coisa funcionar em Play Mode.
        ///
        /// Resources.LoadAll funciona nos dois lados, e todos estes prefabs ja vivem sob
        /// Assets/Resources/ desde a opcao B (o jogo carrega mapas por Resources.Load),
        /// entao nao ha nada a mover.
        /// </summary>
        public static IReadOnlyList<Entrada> Tudo()
        {
            if (cache != null) return cache;
            cache = new List<Entrada>();

            foreach (var pasta in Pastas)
            {
                // "Assets/Resources/Prefabs/NPCs" -> "Prefabs/NPCs", que e o que
                // Resources.LoadAll entende.
                string chave = ChaveDeResources(pasta);
                if (string.IsNullOrEmpty(chave)) continue;

                var categoria = System.IO.Path.GetFileName(pasta);
                foreach (var prefab in Resources.LoadAll<GameObject>(chave))
                {
                    if (prefab == null) continue;
                    // O caminho gravado continua a ser o de projeto: e o que os mapas ja
                    // salvos guardam, e o Resolver abaixo sabe converter os dois sentidos.
                    cache.Add(new Entrada($"{pasta}/{prefab.name}.prefab", prefab.name, categoria));
                }
            }

            // Pela ORDEM DAS PASTAS, nao pelo nome da categoria: ordenar alfabeticamente
            // poria "Decorations" antes de "NPCs" e desfazia a escolha feita na lista
            // acima, onde as pessoas vem primeiro de proposito.
            cache = cache
                .OrderBy(e => System.Array.FindIndex(Pastas, p => p.EndsWith("/" + e.Categoria)))
                .ThenBy(e => e.Nome)
                .ToList();

            return cache;
        }

        /// <summary>"Assets/Resources/Prefabs/NPCs" -> "Prefabs/NPCs".</summary>
        private static string ChaveDeResources(string pastaDeProjeto)
        {
            const string marcador = "/Resources/";
            int i = pastaDeProjeto.IndexOf(marcador, System.StringComparison.Ordinal);
            return i < 0 ? null : pastaDeProjeto.Substring(i + marcador.Length);
        }

        /// <summary>Esquece a varredura — util depois de importar prefabs novos.</summary>
        public static void Invalidar() => cache = null;

        /// <summary>
        /// Resolve um caminho gravado num mapa de volta para o prefab.
        /// Devolve null quando o prefab foi movido ou apagado desde que o mapa foi
        /// salvo; quem chama decide se avisa ou ignora, mas nunca instancia null.
        /// </summary>
        public static GameObject Resolver(string caminho)
        {
            if (string.IsNullOrEmpty(caminho)) return null;

#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            if (prefab != null) return prefab;
#endif
            // Fallback para prefabs sob Resources/, que funcionam tambem em build.
            const string marcador = "/Resources/";
            int i = caminho.IndexOf(marcador, System.StringComparison.Ordinal);
            if (i < 0) return null;

            var relativo = caminho.Substring(i + marcador.Length);
            relativo = System.IO.Path.ChangeExtension(relativo, null);
            return Resources.Load<GameObject>(relativo);
        }
    }
}
