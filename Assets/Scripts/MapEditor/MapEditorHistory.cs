using System.Collections.Generic;
using UnityEngine;
using SowurShield.Farming;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Desfazer e refazer no editor de mapa (Ctrl+Z / Ctrl+Y).
    ///
    /// Guarda apenas as celulas que mudaram, nao o mapa inteiro: um retangulo de 12
    /// celulas custa 12 entradas, e nao uma copia das ~10.000 celulas do tilemap.
    /// Isso importa porque o pincel continuo dispara um passo a cada poucos frames.
    ///
    /// Um "passo" e um gesto do usuario — um clique, um arrasto inteiro, um balde —
    /// e nao cada celula individual. Desfazer um arrasto de retangulo tem que voltar
    /// o retangulo todo de uma vez; um Ctrl+Z por celula seria inutilizavel.
    /// </summary>
    [RequireComponent(typeof(RuntimeMapEditor))]
    public class MapEditorHistory : MonoBehaviour
    {
        /// <summary>Uma celula que mudou: o que havia antes e o que passou a haver.</summary>
        private readonly struct Mudanca
        {
            public readonly Vector3Int Celula;
            public readonly ExtendedTileType Antes;
            public readonly ExtendedTileType Depois;

            public Mudanca(Vector3Int celula, ExtendedTileType antes, ExtendedTileType depois)
            {
                Celula = celula;
                Antes = antes;
                Depois = depois;
            }
        }

        [SerializeField] private int maximoDePassos = 100;

        private RuntimeMapEditor mapEditor;
        private readonly List<List<Mudanca>> desfazer = new();
        private readonly List<List<Mudanca>> refazer = new();

        private List<Mudanca> passoEmCurso;
        // Enquanto aplicamos um desfazer, as escritas que ele provoca nao podem
        // virar historico novo — senao o proximo Ctrl+Z desfaria o proprio desfazer.
        private bool aplicandoHistorico;

        public int PassosParaDesfazer => desfazer.Count;
        public int PassosParaRefazer => refazer.Count;

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
            // O historico e da sessao de edicao. Manter passos de uma sessao anterior
            // deixaria o Ctrl+Z desfazer algo que o usuario nem lembra de ter feito.
            if (!aberto) Limpar();
        }

        public void Limpar()
        {
            desfazer.Clear();
            refazer.Clear();
            passoEmCurso = null;
        }

        /// <summary>Abre um passo. Chamado quando o gesto comeca (botao pressionado).</summary>
        public void IniciarPasso()
        {
            if (aplicandoHistorico) return;
            passoEmCurso = new List<Mudanca>();
        }

        /// <summary>
        /// Registra uma celula que mudou. Ignora quando nada mudou de fato: pintar
        /// terra sobre terra nao e um passo, e sem este filtro um arrasto sobre area
        /// ja pintada encheria o historico de passos vazios.
        /// </summary>
        public void RegistrarMudanca(Vector3Int celula, ExtendedTileType antes, ExtendedTileType depois)
        {
            if (aplicandoHistorico || passoEmCurso == null) return;
            if (antes == depois) return;
            passoEmCurso.Add(new Mudanca(celula, antes, depois));
        }

        /// <summary>Fecha o passo. Chamado quando o gesto termina (botao solto).</summary>
        public void FinalizarPasso()
        {
            if (aplicandoHistorico || passoEmCurso == null) return;

            if (passoEmCurso.Count > 0)
            {
                desfazer.Add(passoEmCurso);
                // Uma acao nova invalida o caminho de refazer: e o comportamento que
                // todo editor tem, e manter o refazer daria um estado impossivel.
                refazer.Clear();

                if (desfazer.Count > maximoDePassos)
                    desfazer.RemoveAt(0);
            }
            passoEmCurso = null;
        }

        public bool Desfazer()
        {
            if (desfazer.Count == 0) return false;

            var passo = desfazer[desfazer.Count - 1];
            desfazer.RemoveAt(desfazer.Count - 1);

            Aplicar(passo, paraTras: true);
            refazer.Add(passo);
            return true;
        }

        public bool Refazer()
        {
            if (refazer.Count == 0) return false;

            var passo = refazer[refazer.Count - 1];
            refazer.RemoveAt(refazer.Count - 1);

            Aplicar(passo, paraTras: false);
            desfazer.Add(passo);
            return true;
        }

        private void Aplicar(List<Mudanca> passo, bool paraTras)
        {
            var dual = mapEditor.DualGrid;
            if (dual == null) return;

            aplicandoHistorico = true;
            try
            {
                // Ao desfazer, percorremos de tras para frente: se o mesmo gesto tocou
                // a mesma celula duas vezes, o estado correto e o da PRIMEIRA vez.
                if (paraTras)
                {
                    for (int i = passo.Count - 1; i >= 0; i--)
                        mapEditor.SetTileAtPosition(passo[i].Celula, passo[i].Antes);
                }
                else
                {
                    for (int i = 0; i < passo.Count; i++)
                        mapEditor.SetTileAtPosition(passo[i].Celula, passo[i].Depois);
                }
            }
            finally
            {
                aplicandoHistorico = false;
            }
        }
    }
}
