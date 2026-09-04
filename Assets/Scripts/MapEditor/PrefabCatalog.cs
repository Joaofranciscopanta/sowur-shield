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
            "Assets/Resources/Prefabs/Decorations",
            "Assets/Resources/Prefabs/FruitTrees",
            "Assets/Resources/Prefabs/Fruits",
            "Assets/Resources/Prefabs/GroundItems"
        };

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

        /// <summary>Tudo o que o editor sabe colocar, agrupado pela pasta de origem.</summary>
        public static IReadOnlyList<Entrada> Tudo()
        {
            if (cache != null) return cache;
            cache = new List<Entrada>();

#if UNITY_EDITOR
            foreach (var pasta in Pastas)
            {
                if (!UnityEditor.AssetDatabase.IsValidFolder(pasta)) continue;

                var guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { pasta });
                foreach (var guid in guids)
                {
                    var caminho = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var nome = System.IO.Path.GetFileNameWithoutExtension(caminho);
                    var categoria = System.IO.Path.GetFileName(pasta);
                    cache.Add(new Entrada(caminho, nome, categoria));
                }
            }
            cache = cache.OrderBy(e => e.Categoria).ThenBy(e => e.Nome).ToList();
#endif
            return cache;
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
