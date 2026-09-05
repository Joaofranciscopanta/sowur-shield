using System.Collections;
using UnityEngine;

namespace SowurShield.Combat
{
    /// <summary>
    /// Animacao de combate feita por TRANSFORMACAO, sem arte nova.
    ///
    /// Dos 289 AnimationClips do projeto, nenhum e de combate: todos sao Idle/Walk/Eat.
    /// Os triggers do Animator (Attack, Hurt, Crit, Die, Poison, Weakness) ja eram
    /// disparados pelo CombatUnit desde `7623020`, mas nao havia estado nenhum ligado a
    /// eles -- eram no-op. Golpe, dano e morte aconteciam sem que nada se mexesse.
    ///
    /// Aqui o movimento sai de codigo: avancar e voltar, tremer, encolher e desvanecer.
    /// Nao substitui arte de animacao, mas e o tipo de movimento que jogo 2D de turno
    /// costuma usar mesmo tendo arte, e funciona com QUALQUER sprite -- inclusive os
    /// placeholders e as esferas cinzas.
    ///
    /// ⚠️ A escala do CombatUnit nao e 1: o NormalizeSpriteSize ajusta cada sprite para
    /// 0,8 unidades de altura, e unidades do jogador levam X NEGATIVO para olhar para a
    /// direita. Por isso tudo aqui e relativo a escala inicial, nunca atribuido -- ver
    /// reference_unity_ppu_mixed_world_scale, que e a mesma armadilha noutro sitio.
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatMotion : MonoBehaviour
    {
        // Guardados no Awake: sao a referencia para onde tudo tem de voltar.
        private Vector3 poseLocal;
        private Vector3 escalaBase;
        private bool guardado;

        private Coroutine emCurso;
        private SpriteRenderer sr;

        /// <summary>Para que lado esta unidade avanca ao atacar.</summary>
        private float Frente => ehDoJogador ? 1f : -1f;
        private bool ehDoJogador;

        [Header("Golpe")]
        [SerializeField] private float avanco = 0.42f;
        [SerializeField] private float tempoDeIda = 0.09f;
        [SerializeField] private float tempoDeVolta = 0.16f;

        [Header("Dano")]
        [SerializeField] private float recuo = 0.16f;
        [SerializeField] private float tremor = 0.07f;

        [Header("Morte")]
        [SerializeField] private float tempoDeMorte = 0.5f;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            Guardar();
        }

        /// <summary>
        /// Fixa a pose de repouso. Chamado no Awake e de novo pelo CombatUnit depois de
        /// montar o visual: o NormalizeSpriteSize corre DEPOIS do Awake e reescreve a
        /// escala, entao o valor guardado no Awake estaria errado para todo o combate.
        /// </summary>
        public void Guardar()
        {
            poseLocal = transform.localPosition;
            escalaBase = transform.localScale;
            guardado = true;
        }

        /// <summary>De que lado esta a unidade, para saber para onde avanca.</summary>
        public void DefinirLado(bool doJogador) => ehDoJogador = doJogador;

        public void Atacar()  => Trocar(RotinaDeAtaque());
        public void Levar(bool critico) => Trocar(RotinaDeDano(critico));
        public void Morrer()  => Trocar(RotinaDeMorte());

        /// <summary>Estado alterado (veneno/fraqueza): um tremor curto, sem deslocamento.</summary>
        public void Estado(Color tom) => Trocar(RotinaDeEstado(tom));

        private void Trocar(IEnumerator rotina)
        {
            if (!guardado) Guardar();
            if (!isActiveAndEnabled) return;

            // Uma animacao de cada vez: duas a mexer no mesmo transform deixariam a
            // unidade parada fora do lugar quando a perdedora restaurasse a pose.
            if (emCurso != null) StopCoroutine(emCurso);
            emCurso = StartCoroutine(rotina);
        }

        private IEnumerator RotinaDeAtaque()
        {
            var destino = poseLocal + new Vector3(avanco * Frente, 0f, 0f);

            yield return Deslocar(poseLocal, destino, tempoDeIda, Suavizar);
            yield return Deslocar(destino, poseLocal, tempoDeVolta, SuavizarSaida);

            transform.localPosition = poseLocal;
            emCurso = null;
        }

        private IEnumerator RotinaDeDano(bool critico)
        {
            // Critico bate mais fundo: mesmo gesto, amplitude maior.
            float forca = critico ? 1.9f : 1f;
            var atras = poseLocal - new Vector3(recuo * Frente * forca, 0f, 0f);

            yield return Deslocar(poseLocal, atras, 0.06f, Suavizar);

            // Tremor: some sozinho porque a amplitude cai a cada passagem.
            int passos = critico ? 5 : 3;
            for (int i = 0; i < passos; i++)
            {
                float amplitude = tremor * forca * (1f - (float)i / passos);
                transform.localPosition = atras + new Vector3(
                    (i % 2 == 0 ? amplitude : -amplitude), amplitude * 0.35f, 0f);
                yield return new WaitForSeconds(0.035f);
            }

            yield return Deslocar(transform.localPosition, poseLocal, 0.12f, SuavizarSaida);

            transform.localPosition = poseLocal;
            emCurso = null;
        }

        private IEnumerator RotinaDeMorte()
        {
            var inicio = transform.localPosition;
            Color corInicial = sr != null ? sr.color : Color.white;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / tempoDeMorte;
                float p = Mathf.Clamp01(t);

                // Tomba para o lado de onde veio o golpe e afunda um pouco.
                transform.localRotation = Quaternion.Euler(0f, 0f, -75f * Frente * Suavizar(p));
                transform.localPosition = inicio + new Vector3(0f, -0.18f * p, 0f);

                // Encolhe pela escala BASE, que nao e 1 e pode ter X negativo.
                transform.localScale = escalaBase * Mathf.Lerp(1f, 0.82f, p);

                if (sr != null)
                    sr.color = new Color(corInicial.r, corInicial.g, corInicial.b,
                                         Mathf.Lerp(corInicial.a, 0.25f, p));

                yield return null;
            }

            emCurso = null;
        }

        private IEnumerator RotinaDeEstado(Color tom)
        {
            Color original = sr != null ? sr.color : Color.white;

            for (int i = 0; i < 3; i++)
            {
                if (sr != null) sr.color = tom;
                transform.localPosition = poseLocal + new Vector3(0f, 0.05f, 0f);
                yield return new WaitForSeconds(0.08f);

                if (sr != null) sr.color = original;
                transform.localPosition = poseLocal;
                yield return new WaitForSeconds(0.08f);
            }

            transform.localPosition = poseLocal;
            emCurso = null;
        }

        private IEnumerator Deslocar(Vector3 de, Vector3 para, float duracao,
                                     System.Func<float, float> curva)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duracao;
                transform.localPosition = Vector3.LerpUnclamped(de, para, curva(Mathf.Clamp01(t)));
                yield return null;
            }
        }

        // Acelera no inicio: o golpe parte com intencao, em vez de deslizar.
        private static float Suavizar(float t) => t * t * (3f - 2f * t);

        // Volta amortecida, sem o solavanco de parar de repente.
        private static float SuavizarSaida(float t) => 1f - (1f - t) * (1f - t);

        /// <summary>Devolve a unidade a pose de repouso. Usado ao reviver e nos testes.</summary>
        public void Repousar()
        {
            if (emCurso != null) { StopCoroutine(emCurso); emCurso = null; }
            if (!guardado) return;

            transform.localPosition = poseLocal;
            transform.localScale = escalaBase;
            transform.localRotation = Quaternion.identity;
        }
    }
}
