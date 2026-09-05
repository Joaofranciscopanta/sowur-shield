using UnityEngine;

namespace SowurShield.Core
{
    /// <summary>
    /// Como o jogo escolhe COM O QUE voce interage quando ha varias coisas por perto.
    ///
    /// Isto nasceu de um relato de jogo: querer pegar um ovo e falar com um NPC, querer
    /// entrar no combate e falar com um NPC, querer falar com um NPC e falar com outro.
    /// Medindo, a causa era dupla -- os NPCs tinham alcance 3, o maior do jogo (item no chao
    /// tem 1 a 1,5; a caixa de venda tem 1), e o InteractionManager escolhia puramente o
    /// MAIS PROXIMO, sem nenhum desempate. Deu 27 sobreposicoes na cena, com a Joana
    /// sozinha a engolir 14 alvos e a Isabela a cobrir a entrada do combate a 0,7 unidade.
    /// </summary>
    public static class InteractionPreferences
    {
        private const string ChaveMira = "interacao_mira_no_cursor";

        /// <summary>
        /// Quando ligado, o alvo sob o CURSOR ganha de quem esta apenas por perto.
        ///
        /// Pedido pelo jogador: "colocar prioridade mais no clique do mouse mesmo, onde a
        /// seta do mouse esta". Desligado por padrao porque a tecla E deve continuar a
        /// funcionar sem exigir mira -- quem joga so com o teclado nao move o cursor.
        /// </summary>
        public static bool MirarNoCursor
        {
            get => PlayerPrefs.GetInt(ChaveMira, 0) == 1;
            set { PlayerPrefs.SetInt(ChaveMira, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>
        /// Desempate por tipo, do mais especifico para o mais generico.
        ///
        /// Numero MAIOR ganha. A ordem vem da intencao: quando se esta em cima de um ovo e
        /// de um NPC ao mesmo tempo, quase sempre se quer o ovo -- falar e uma acao que se
        /// procura, apanhar e uma acao que se faz de passagem. A zona de combate ganha de
        /// tudo porque entrar numa batalha por engano custa uma sessao inteira.
        ///
        /// So desempata entre coisas que JA estao ao alcance; nao estende alcance nenhum.
        /// </summary>
        public static int Prioridade(IInteractable alvo)
        {
            return alvo == null ? 0 : PrioridadeDoTipo(alvo.GetType().Name);
        }

        /// <summary>
        /// A mesma tabela, pelo NOME do tipo.
        ///
        /// Separado para os testes poderem verificar a ordem sem instanciar nada: os
        /// gatilhos de combate exigem `[RequireComponent(typeof(Collider2D))]`, e Collider2D
        /// e abstrata -- um AddComponent desses falha e devolve null.
        /// </summary>
        public static int PrioridadeDoTipo(string nomeDoTipo)
        {
            switch (nomeDoTipo)
            {
                // Entrar em combate por engano e o erro mais caro de desfazer.
                case "WorldMapTriggerZone":
                case "CombatTriggerZone":   return 50;

                // Apanhar do chao: acao de passagem, feita sem parar.
                case "GroundItem":          return 40;

                // Estruturas que se opera de proposito, em cima delas.
                case "SellBox":
                case "FeedingTrough":
                case "BedInteractable":     return 30;

                // Arvore e solo: ficam sob os pes o tempo todo.
                case "ChoppableTree":
                case "FishingSpot":         return 20;
                case "SoilBlockInteractable": return 10;

                // Falar e o padrao: nunca deve roubar o clique de algo mais especifico.
                default:                    return 0;
            }
        }
    }
}
