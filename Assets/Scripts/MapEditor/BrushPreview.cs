using System.Collections.Generic;
using UnityEngine;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Mostra a FORMA que o pincel vai pintar, antes do clique.
    ///
    /// A celula sob o mouse nao e marcada aqui: quem faz isso e o indicador que o
    /// jogo ja tem (`Cursor` + CursorController), emprestado ao editor enquanto ele
    /// esta aberto. Desenhar um quadrado proprio para isso era um segundo indicador
    /// competindo com o primeiro.
    ///
    /// O BrushTool ja tinha ganchos para isto (`brushPreviewPrefab`, `linePreview`,
    /// `rectanglePreview`), mas todos guardam contra null e nunca foram ligados: o
    /// prefab nunca existiu, entao `UpdateVisualFeedback` rodava a cada frame sem
    /// desenhar nada. Este componente cria os proprios quadrados em runtime, pelo
    /// mesmo motivo que a paleta se constroi sozinha — nao depender de prefab.
    ///
    /// Desenha na sorting layer WorldUI (a mais alta do projeto), senao o preview
    /// fica ENTERRADO sob o tilemap do chao, que e o mesmo defeito que ja mordeu os
    /// sprites do jogo antes.
    /// </summary>
    [RequireComponent(typeof(RuntimeMapEditor))]
    public class BrushPreview : MonoBehaviour
    {
        [SerializeField] private Color corPintar = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] private Color corApagar = new Color(0.9f, 0.45f, 0.45f, 0.45f);
        [SerializeField] private Color corContorno = new Color(1f, 0.95f, 0.6f, 0.9f);

        private RuntimeMapEditor mapEditor;
        private BrushTool brushTool;
        private Camera cam;
        private Transform poolPai;
        private Sprite spriteQuadrado;

        // Reaproveitamos os quadrados: um pincel grande ou um retangulo grande
        // trocaria de tamanho a cada frame, e criar/destruir sprites nesse ritmo
        // enche o GC sem necessidade.
        private readonly List<SpriteRenderer> pool = new();
        private int emUso;


        private void Start()
        {
            mapEditor = GetComponent<RuntimeMapEditor>();
            brushTool = GetComponent<BrushTool>();
            cam = Camera.main;

            spriteQuadrado = CriarSpriteQuadrado();

            poolPai = new GameObject("BrushPreview").transform;
            poolPai.SetParent(transform, false);
            poolPai.gameObject.SetActive(false);

            mapEditor.OnEditorToggled += AoAlternarEditor;
        }

        private void OnDestroy()
        {
            if (mapEditor != null) mapEditor.OnEditorToggled -= AoAlternarEditor;
            if (spriteQuadrado != null) Destroy(spriteQuadrado);
        }

        private void AoAlternarEditor(bool aberto)
        {
            if (poolPai != null) poolPai.gameObject.SetActive(aberto);
        }

        private void Update()
        {
            if (mapEditor == null || !mapEditor.IsEditorActive) return;

            // Camera.main pode ser null logo apos uma troca de cena.
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null) return;
            }

            // Sobre a UI o clique nao pinta (BrushTool ignora), entao o preview
            // tambem nao deve sugerir que pintaria.
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                Esconder();
                return;
            }

            var celula = CelulaSobOCursor();
            Desenhar(CelulasAfetadas(celula));
        }

        private Vector3Int CelulaSobOCursor()
        {
            var mundo = cam.ScreenToWorldPoint(Input.mousePosition);
            var dual = mapEditor.DualGrid;
            if (dual != null && dual.placeholderTilemap != null)
            {
                var c = dual.placeholderTilemap.WorldToCell(mundo);
                c.z = 0;
                return c;
            }
            return new Vector3Int(Mathf.FloorToInt(mundo.x), Mathf.FloorToInt(mundo.y), 0);
        }

        /// <summary>
        /// As celulas que o clique de agora afetaria. Para linha e retangulo em
        /// curso, mostra a forma inteira; senao, so a area do pincel.
        /// </summary>
        private List<Vector3Int> CelulasAfetadas(Vector3Int celula)
        {
            var resultado = new List<Vector3Int>();
            if (brushTool == null) { resultado.Add(celula); return resultado; }

            var inicio = brushTool.DragStart;
            if (inicio.HasValue)
            {
                switch (mapEditor.selectedBrush)
                {
                    case BrushType.Line:
                        return CelulasDaLinha(inicio.Value, celula);
                    case BrushType.Rectangle:
                        return CelulasDoRetangulo(inicio.Value, celula);
                }
            }

            return brushTool.AreaDoPincel(celula);
        }

        private static List<Vector3Int> CelulasDaLinha(Vector3Int a, Vector3Int b)
        {
            // Bresenham, o mesmo tracado que o BrushTool usa ao confirmar.
            var pontos = new List<Vector3Int>();
            int x = a.x, y = a.y;
            int dx = Mathf.Abs(b.x - a.x), dy = Mathf.Abs(b.y - a.y);
            int sx = a.x < b.x ? 1 : -1, sy = a.y < b.y ? 1 : -1;
            int erro = dx - dy;

            while (true)
            {
                pontos.Add(new Vector3Int(x, y, 0));
                if (x == b.x && y == b.y) break;
                int e2 = 2 * erro;
                if (e2 > -dy) { erro -= dy; x += sx; }
                if (e2 < dx) { erro += dx; y += sy; }
            }
            return pontos;
        }

        private static List<Vector3Int> CelulasDoRetangulo(Vector3Int a, Vector3Int b)
        {
            var pontos = new List<Vector3Int>();
            int x0 = Mathf.Min(a.x, b.x), x1 = Mathf.Max(a.x, b.x);
            int y0 = Mathf.Min(a.y, b.y), y1 = Mathf.Max(a.y, b.y);
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    pontos.Add(new Vector3Int(x, y, 0));
            return pontos;
        }

        private void Desenhar(List<Vector3Int> celulas)
        {
            bool apagando = mapEditor.selectedBrush == BrushType.Eraser
                         || mapEditor.selectedTileType == ExtendedTileType.Grass;
            Color cor = apagando ? corApagar : corPintar;

            emUso = 0;
            foreach (var c in celulas)
            {
                var sr = Pegar();
                // +0.5 porque o tileAnchor das celulas e o centro, nao o canto.
                sr.transform.position = new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
                sr.color = cor;
                sr.gameObject.SetActive(true);
            }

            for (int i = emUso; i < pool.Count; i++)
                pool[i].gameObject.SetActive(false);
        }

        private void Esconder()
        {
            foreach (var sr in pool) sr.gameObject.SetActive(false);
            emUso = 0;
        }

        private SpriteRenderer Pegar()
        {
            if (emUso < pool.Count) return pool[emUso++];

            var go = new GameObject("PreviewCell");
            go.transform.SetParent(poolPai, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spriteQuadrado;
            // WorldUI e a sorting layer mais alta do projeto. Sem isto o preview fica
            // enterrado sob o tilemap do chao.
            sr.sortingLayerName = "WorldUI";
            sr.sortingOrder = 1000;
            pool.Add(sr);
            emUso++;
            return sr;
        }

        /// <summary>
        /// Um quadrado branco de 1x1 unidade, gerado em codigo. Evita depender de um
        /// sprite no disco so para desenhar um retangulo translucido.
        /// </summary>
        private static Sprite CriarSpriteQuadrado()
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();

            // pixelsPerUnit = 8 para a textura 8x8 cobrir exatamente uma celula.
            return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
        }
    }
}
