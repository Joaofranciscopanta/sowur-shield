using UnityEngine;
using UnityEngine.InputSystem;
using SowurShield.Core;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Camera livre enquanto o editor de mapa esta aberto: setas para andar pelo
    /// mundo, roda do mouse (ou +/-) para zoom.
    ///
    /// O FollowPlayer cola a camera no jogador em LateUpdate, todo frame. Nao basta
    /// mover a camera — ela voltaria imediatamente — entao o componente e desligado
    /// enquanto se constroi e religado ao fechar, devolvendo a camera ao jogador.
    /// </summary>
    [RequireComponent(typeof(RuntimeMapEditor))]
    public class MapEditorCamera : MonoBehaviour
    {
        [Header("Movimento")]
        [SerializeField] private float velocidade = 12f;
        [SerializeField] private float velocidadeRapida = 30f;

        [Header("Zoom")]
        [SerializeField] private float passoDoZoom = 1.5f;
        // O jogo roda com ortho 2.6. Menos que 1 nao mostra nada util; mais que 30
        // ja passa muito do mundo pintado (o placeholder tem 25x24 celulas).
        [SerializeField] private float zoomMinimo = 1.5f;
        [SerializeField] private float zoomMaximo = 30f;

        private RuntimeMapEditor mapEditor;
        private Camera cam;
        private Camera cameraFixada;
        private FollowPlayer follow;

        private float zoomOriginal;
        private Vector3 posicaoOriginal;
        private bool ativo;

        private void Start()
        {
            mapEditor = GetComponent<RuntimeMapEditor>();
            mapEditor.OnEditorToggled += AoAlternarEditor;
        }

        private void OnDestroy()
        {
            if (mapEditor != null) mapEditor.OnEditorToggled -= AoAlternarEditor;
        }

        private void AoAlternarEditor(bool aberto)
        {
            if (aberto) Assumir();
            else Devolver();
        }

        /// <summary>
        /// Fixa a camera a controlar, em vez de descobri-la por Camera.main. Existe
        /// para teste: em EditMode, Camera.main devolve a camera da cena aberta no
        /// Editor, nao a que o teste acabou de criar.
        /// </summary>
        public void UsarCamera(Camera alvo) => cameraFixada = alvo;

        private void Assumir()
        {
            cam = cameraFixada != null ? cameraFixada : Camera.main;
            if (cam == null) return;

            follow = cam.GetComponent<FollowPlayer>();

            // Guardamos para devolver exatamente como estava: quem constroi nao quer
            // sair do editor e encontrar a camera com outro zoom.
            zoomOriginal = cam.orthographicSize;
            posicaoOriginal = cam.transform.position;

            // Sem isto o LateUpdate do FollowPlayer desfaz cada movimento nosso.
            if (follow != null) follow.enabled = false;

            ativo = true;
        }

        private void Devolver()
        {
            if (!ativo || cam == null) return;

            cam.orthographicSize = zoomOriginal;
            cam.transform.position = posicaoOriginal;
            if (follow != null) follow.enabled = true;

            ativo = false;
        }

        private void Update()
        {
            if (!ativo || cam == null) return;

            var teclado = Keyboard.current;
            if (teclado == null) return;

            Mover(teclado);
            Zoom(teclado);
        }

        private void Mover(Keyboard teclado)
        {
            var direcao = Vector2.zero;
            if (teclado.leftArrowKey.isPressed)  direcao.x -= 1f;
            if (teclado.rightArrowKey.isPressed) direcao.x += 1f;
            if (teclado.upArrowKey.isPressed)    direcao.y += 1f;
            if (teclado.downArrowKey.isPressed)  direcao.y -= 1f;

            if (direcao == Vector2.zero) return;

            bool rapido = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;
            float v = rapido ? velocidadeRapida : velocidade;

            // Escalado pelo zoom: com a camera afastada, um passo fixo em unidades de
            // mundo parece lentissimo na tela. Dividimos pelo tamanho de referencia do
            // jogo (2.6) para a sensacao de velocidade ficar constante.
            v *= Mathf.Max(1f, cam.orthographicSize / 2.6f);

            // unscaledDeltaTime: o editor deve responder mesmo se algo pausou o jogo.
            var passo = (Vector3)(direcao.normalized * v * Time.unscaledDeltaTime);
            cam.transform.position += passo;
        }

        private void Zoom(Keyboard teclado)
        {
            float delta = 0f;

            var mouse = Mouse.current;
            // Sobre a paleta, a roda e da LISTA e nao da camera.
            //
            // Os dois liam a mesma roda: rolar a lista de 67 objetos para achar uma arvore
            // afastava o mundo ao mesmo tempo, e chegar ao fim da lista significava perder
            // o enquadramento. Relatado a jogar a build. Mesma guarda que o ObjectPlacer ja
            // usa para o clique -- sobre a UI o gesto pertence a UI.
            if (mouse != null && !SobreAUI())
            {
                float roda = mouse.scroll.ReadValue().y;
                if (!Mathf.Approximately(roda, 0f)) delta -= Mathf.Sign(roda) * passoDoZoom;
            }

            // Teclas como alternativa: nem todo mundo usa mouse com roda, e no
            // trackpad o scroll costuma ser dificil de dosar.
            if (teclado.equalsKey.wasPressedThisFrame || teclado.numpadPlusKey.wasPressedThisFrame)
                delta -= passoDoZoom;
            if (teclado.minusKey.wasPressedThisFrame || teclado.numpadMinusKey.wasPressedThisFrame)
                delta += passoDoZoom;

            if (Mathf.Approximately(delta, 0f)) return;

            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize + delta, zoomMinimo, zoomMaximo);
        }

        /// <summary>
        /// Se o cursor esta sobre um elemento de UI (uma das paletas, tipicamente).
        ///
        /// As teclas +/- do Zoom continuam a funcionar mesmo aqui: nao ha ambiguidade
        /// nenhuma nelas, e sao o recuo para quem usa trackpad.
        /// </summary>
        private static bool SobreAUI()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }

        /// <summary>Recentra a camera no jogador sem sair do editor.</summary>
        public void CentrarNoJogador()
        {
            if (!ativo || cam == null) return;
            var jogador = FindFirstObjectByType<PlayerMove>();
            if (jogador == null) return;

            var p = jogador.transform.position;
            cam.transform.position = new Vector3(p.x, p.y, cam.transform.position.z);
        }
    }
}
