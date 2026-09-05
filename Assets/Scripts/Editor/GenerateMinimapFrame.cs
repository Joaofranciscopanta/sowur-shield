using UnityEditor;
using UnityEngine;
using SowurShield.Minimap;

namespace SowurShield.Editor
{

/// <summary>
/// Regenera os sprites do minimapa (moldura e marcador do jogador) como ASSETS.
///
/// Por que existe: <see cref="MinimapFrameSprite"/> sabe desenhar a moldura em memoria,
/// mas um Sprite.Create nao e um asset — uma cena que o guardasse ficaria com referencia
/// nula ao recarregar, e a moldura simplesmente sumiria sem erro nenhum. Alem disso, so o
/// importador de textura consegue gravar a borda de 9-slice, que e justamente o que a
/// moldura antiga tinha errado (24px de borda para 66px de ornamento).
///
/// Entao a receita vive no codigo e este menu materializa o resultado em disco.
/// </summary>
public static class GenerateMinimapFrame
{
    private const string FrameDir = "Assets/Resources/Sprites/UI/Frames";
    private const string FramePath = FrameDir + "/minimap_frame.png";
    private const string MarkerPath = FrameDir + "/minimap_player_marker.png";

    // Tem de casar com MinimapFrameSprite.BorderPx.
    private const int FrameBorder = 28;

    [MenuItem("Sowur Shield/Minimap/Regenerar moldura do minimapa")]
    public static void Generate()
    {
        if (!System.IO.Directory.Exists(FrameDir))
            System.IO.Directory.CreateDirectory(FrameDir);

        // A moldura em memoria: forcar o desenho, ignorando o asset que talvez ja exista,
        // senao este menu regeneraria o asset a partir de si proprio.
        WritePng(MinimapFrameSprite.DrawFresh(), FramePath, 100f, FrameBorder);
        WritePng(MinimapIconSprites.ForType(MinimapIconType.Player), MarkerPath, 32f, 0);

        AssetDatabase.Refresh();
        Debug.Log("[Minimap] Moldura e marcador regenerados em " + FrameDir);
    }

    private static void WritePng(Sprite source, string path, float ppu, int border)
    {
        if (source == null)
        {
            Debug.LogError("[Minimap] Sprite de origem nulo para " + path);
            return;
        }

        var src = source.texture;
        var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        copy.SetPixels(src.GetPixels());
        copy.Apply();

        System.IO.File.WriteAllBytes(path, copy.EncodeToPNG());
        Object.DestroyImmediate(copy);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Bilinear;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        // Sem compressao: a moldura tem linhas de 2px (o fio dourado, os contornos) que a
        // compressao com perdas transforma em franjas coloridas.
        imp.textureCompression = TextureImporterCompression.Uncompressed;

        if (border > 0)
            imp.spriteBorder = new Vector4(border, border, border, border);

        imp.SaveAndReimport();
    }
}

}
