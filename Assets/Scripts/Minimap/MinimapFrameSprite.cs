using UnityEngine;

namespace SowurShield.Minimap
{

/// <summary>
/// Moldura do minimapa desenhada por codigo, com 9-slice honesto.
///
/// A arte anterior (frame_decorative_border.png) tinha um ornamento de ~66px de largura,
/// mas o Image na cena estava com 9-slice de 24px. O Sliced nunca comprime a borda: ele
/// fixa os 24px dos cantos e ESTICA todo o resto — ou seja, 42px de ornamento eram
/// esticados horizontalmente na barra de cima e verticalmente nas laterais. Dai as
/// "bordas que nao batem": o desenho do canto era cortado no meio e o miolo do ornamento
/// virava um borrao alongado. Em 1720x940 (o tamanho real do fullscreen nesta cena) o
/// estiramento passava de 8x.
///
/// A saida nao e outra textura fixa — e uma que possa ser fatiada corretamente. Esta
/// moldura e construida de forma que TODO o detalhe cabe dentro da borda do 9-slice, e a
/// faixa central e deliberadamente uniforme, entao esticar a faixa central nao deforma
/// nada visivel. Assim a mesma moldura serve 200x200 no HUD e 1720x940 no fullscreen.
///
/// Desenhada em codigo pelo mesmo motivo que <see cref="MinimapIconSprites"/>: e geometria
/// simples, e o projeto nao tem geracao de imagem configurada.
/// </summary>
public static class MinimapFrameSprite
{
    // Um lado da textura. A borda do 9-slice e BorderPx; o miolo restante e a faixa
    // esticavel, mantida lisa de proposito.
    private const int Size = 96;
    private const int BorderPx = 28;
    private const float PixelsPerUnit = 100f;

    private static Sprite cached;

    /// <summary>Caminho do asset gerado a partir desta mesma receita.</summary>
    private const string AssetPath = "Sprites/UI/Frames/minimap_frame";

    /// <summary>Moldura com o furo central transparente, pronta para Image.Type.Sliced.</summary>
    public static Sprite Get()
    {
        if (cached != null) return cached;

        // Preferir o asset em Resources: um Sprite.Create em memoria nao carrega a borda de
        // 9-slice do importador, e — mais importante — nao e um asset, entao uma cena que o
        // guardasse ficaria com a referencia nula ao recarregar. O asset e gerado a partir
        // desta mesma funcao (menu Sowur Shield > Minimap), entao os dois nunca divergem.
        cached = Resources.Load<Sprite>(AssetPath);
        if (cached != null) return cached;

        // Sem o asset (projeto que ainda nao o gerou), desenhar em memoria: melhor uma
        // moldura sem 9-slice do que nenhuma moldura.
        cached = DrawFresh();
        return cached;
    }

    /// <summary>
    /// Desenha a moldura do zero, ignorando o asset e o cache.
    ///
    /// E o que a ferramenta de editor grava em disco. Tem de ignorar o asset, senao
    /// regenerar produziria uma copia de si proprio em vez de aplicar a receita atual.
    /// </summary>
    public static Sprite DrawFresh()
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.name = "MinimapFrame";

        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                tex.SetPixel(x, y, PixelAt(x, y));

        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, Size, Size),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit,
            0,
            SpriteMeshType.FullRect,
            // L, B, R, T — casa exatamente com a espessura desenhada, entao a faixa
            // esticada e so a parte lisa.
            new Vector4(BorderPx, BorderPx, BorderPx, BorderPx));
    }

    // Paleta de madeira do UITheme (woodDark / woodMid / woodLight) mais um realce
    // dourado, para a moldura pertencer ao mesmo conjunto do resto da UI.
    private static readonly Color WoodDark  = new Color(0.420f, 0.267f, 0.137f, 1f);
    private static readonly Color WoodMid   = new Color(0.545f, 0.353f, 0.169f, 1f);
    private static readonly Color WoodLight = new Color(0.651f, 0.416f, 0.247f, 1f);
    private static readonly Color Gold      = new Color(0.957f, 0.827f, 0.369f, 1f);
    private static readonly Color Clear     = new Color(0f, 0f, 0f, 0f);

    // Espessuras medidas a partir da borda externa, somando 24px < BorderPx=28, de modo
    // que o ultimo passo termina dentro da regiao de canto do 9-slice.
    private const int RimOuter    = 2;   // contorno escuro externo
    private const int Bevel       = 3;   // realce claro (luz vinda de cima-esquerda)
    private const int Body        = 13;  // corpo de madeira
    private const int GoldLine    = 2;   // fio dourado interno
    private const int RimInner    = 3;   // contorno escuro interno

    private static Color PixelAt(int x, int y)
    {
        // Distancia ate a borda mais proxima. E isto que torna a moldura fatiavel: a cor
        // depende so da profundidade a partir da borda, entao a faixa central e constante
        // ao longo de cada aresta e esticar nao muda nada.
        int left = x, right = Size - 1 - x, bottom = y, top = Size - 1 - y;
        int depth = Mathf.Min(Mathf.Min(left, right), Mathf.Min(bottom, top));

        int d0 = RimOuter;
        int d1 = d0 + Bevel;
        int d2 = d1 + Body;
        int d3 = d2 + GoldLine;
        int d4 = d3 + RimInner;

        if (depth >= d4) return Clear;          // furo central: o mapa aparece aqui
        if (depth < d0)  return WoodDark;       // contorno externo
        if (depth < d1)                          // chanfro iluminado
        {
            // Luz de cima-esquerda: as arestas superior e esquerda recebem o realce claro,
            // as opostas ficam na sombra. Sem isso a moldura fica chapada.
            bool lit = (top == depth) || (left == depth);
            return lit ? WoodLight : WoodDark;
        }
        if (depth < d2)                          // corpo, com uma veia sutil
        {
            // Uma variacao leve e deterministica evita o aspecto de plastico liso sem
            // introduzir detalhe que o esticamento denunciaria.
            bool vein = ((x * 7 + y * 13) % 23) < 3;
            return vein ? WoodDark : WoodMid;
        }
        if (depth < d3) return Gold;            // fio dourado
        return WoodDark;                         // contorno interno, contra o mapa
    }
}

}
