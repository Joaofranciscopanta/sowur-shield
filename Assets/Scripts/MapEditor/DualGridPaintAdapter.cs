using UnityEngine;
using UnityEngine.Tilemaps;
using SowurShield.Farming;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Traduz a intencao de pintura do editor para o que o DualGridTilemap do jogo entende.
    ///
    /// O editor foi escrito contra um sistema de tiles imaginado: um enum de 15 valores
    /// (Grass, Dirt, Water, Stone, Sand...). O jogo tem outro modelo — o dual grid e
    /// BINARIO. Ele so distingue duas coisas no placeholder, e deriva qual dos 16 tiles
    /// desenhar a partir dos 4 vizinhos de cada canto.
    ///
    /// Esta classe e a ponte. Nada mais no editor precisa saber como o dual grid funciona.
    ///
    /// ⚠️ A inversao NAO e um bug — e como o jogo sempre funcionou:
    ///   `if (placeholder == grassPlaceholderTile) return Dirt; else return Grass;`
    /// Pintar `grassPlaceholderTile` marca a celula como DIRT, e celula VAZIA conta como
    /// GRASS. Por isso apagar terra e limpar a celula, nao pintar outro tile.
    /// </summary>
    public static class DualGridPaintAdapter
    {
        /// <summary>
        /// Os tipos do enum que o dual grid consegue representar de verdade.
        /// Os outros (Water, Stone, Sand, Wood, Brick, Ice, Lava, Custom1-5) nao tem
        /// placeholder no jogo — pintar com eles nao teria efeito visual nenhum, entao
        /// o editor precisa saber disso ANTES de deixar o usuario escolher.
        /// </summary>
        public static bool IsPaintable(ExtendedTileType type)
        {
            return type == ExtendedTileType.Grass
                || type == ExtendedTileType.Dirt
                || type == ExtendedTileType.None;
        }

        /// <summary>
        /// Pinta uma celula no dual grid do jogo. Devolve false quando o tipo pedido
        /// nao existe neste tileset — o chamador decide se avisa o usuario ou ignora.
        /// </summary>
        public static bool Paint(DualGridTilemap dualGrid, Vector3Int coords, ExtendedTileType type)
        {
            if (dualGrid == null) return false;
            if (!IsPaintable(type)) return false;

            // Dirt e o unico tipo que pinta um tile de verdade no placeholder.
            // Grass e None limpam a celula, porque "ausencia" ja significa grama.
            Tile placeholder = (type == ExtendedTileType.Dirt)
                ? dualGrid.grassPlaceholderTile
                : null;

            dualGrid.SetCell(coords, placeholder);
            return true;
        }

        /// <summary>
        /// Le o que ha numa celula, no vocabulario do editor.
        /// </summary>
        public static ExtendedTileType Read(DualGridTilemap dualGrid, Vector3Int coords)
        {
            if (dualGrid == null || dualGrid.placeholderTilemap == null)
                return ExtendedTileType.None;

            var tile = dualGrid.placeholderTilemap.GetTile(coords);
            return (tile == dualGrid.grassPlaceholderTile)
                ? ExtendedTileType.Dirt
                : ExtendedTileType.Grass;
        }

        /// <summary>
        /// Reconstroi o MapData a partir do que esta pintado na cena agora.
        ///
        /// Isto resolve o `// TODO: Scan current tilemap state and update MapData` do
        /// RuntimeMapEditor. Sem isto, salvar um mapa que voce nao pintou nesta sessao
        /// gravava um MapData vazio por cima do mundo existente.
        /// </summary>
        public static int CaptureInto(DualGridTilemap dualGrid, MapData mapData)
        {
            if (dualGrid == null || mapData == null) return 0;
            if (dualGrid.placeholderTilemap == null) return 0;

            mapData.tileData.Clear();

            var tilemap = dualGrid.placeholderTilemap;
            tilemap.CompressBounds();

            int captured = 0;
            foreach (var pos in tilemap.cellBounds.allPositionsWithin)
            {
                var cell = (Vector3Int)pos;
                // So gravamos as celulas de terra: grama e o estado padrao e gravar as
                // ~10.000 celulas vazias faria o asset explodir sem ganho nenhum.
                if (tilemap.GetTile(cell) == dualGrid.grassPlaceholderTile)
                {
                    mapData.SetTileAt(cell, ExtendedTileType.Dirt);
                    captured++;
                }
            }
            return captured;
        }

        /// <summary>
        /// Aplica um MapData sobre a cena, substituindo o que estiver pintado.
        /// </summary>
        public static int Apply(DualGridTilemap dualGrid, MapData mapData)
        {
            if (dualGrid == null || mapData == null) return 0;
            if (dualGrid.placeholderTilemap == null) return 0;

            // Limpa antes de aplicar, senao terra antiga sobrevive fora do novo mapa.
            dualGrid.placeholderTilemap.ClearAllTiles();

            int applied = 0;
            foreach (var entry in mapData.tileData)
            {
                if (!IsPaintable(entry.tileType)) continue;
                Tile placeholder = (entry.tileType == ExtendedTileType.Dirt)
                    ? dualGrid.grassPlaceholderTile
                    : null;
                dualGrid.placeholderTilemap.SetTile(entry.position, placeholder);
                applied++;
            }

            // Um unico refresh no fim: SetCell por celula recalcularia os 4 vizinhos
            // a cada chamada, o que e ordens de magnitude mais lento num mapa cheio.
            dualGrid.RefreshDisplayTilemap();
            return applied;
        }
    }
}
