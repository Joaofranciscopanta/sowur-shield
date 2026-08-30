# Roteiro de teste — o que jogar para exercitar cada sistema

> Escrito em 2026-08-30. O objetivo é que **jogar este roteiro toque em todo sistema
> do jogo**, para você encontrar o que os testes automatizados não encontram.
> Você já achou 2 bugs em minutos que 842 testes passaram batido.

## Como funciona agora

Um jogo novo começa com a quest **Primeira Colheita** ativa e o tutorial de 6 passos
rodando. O objetivo atual aparece no **canto superior esquerdo**, abaixo da stamina.

Cada aldeão oferece uma quest ao conversar. Falar com alguém já **inicia** a quest —
não há tela de aceitar.

## O roteiro

| # | O que fazer | O que isso testa | O que deve acontecer |
|---|---|---|---|
| 1 | Seguir o tutorial (6 passos) | Tutorial, ferramentas iniciais, arar/plantar/regar | A barra embaixo avança sozinha a cada passo |
| 2 | Colher a primeira cenoura | Crescimento de plantação, `HarvestCrop` | Quest **Primeira Colheita** completa, **+50 ouro**, texto flutuante |
| 3 | Falar com a **Joana** | Diálogo, retrato, `TalkToNPC` | Inicia **Conhecer a Vila**: falar com Tomás, Isabela e Bento |
| 4 | Falar com **Tomás**, **Isabela** e **Bento** | Cadeia de 3 objetivos | Barra do tracker enche em terços; **+100 ouro** ao fim |
| 5 | Falar com a **Isabela** | `CollectItem` (2 itens diferentes) | Inicia **Despensa da Isabela**: 3 ovos + 2 leites |
| 6 | Cuidar das galinhas e vacas, pegar ovos/leite do chão | Animais, comedouro, `GroundItem`, coleta | Objetivos avançam ao **pegar**, não ao produzir |
| 7 | Falar com o **Bento** | Objetivos empilhados no mesmo item | Inicia **Coletor de Ovos** (5 ovos) — os ovos contam para as **duas** quests |
| 8 | Falar com a **Clara** | Ciclo de plantio completo | Inicia **Remédio da Clara**: 2 repolhos + 2 rabanetes |
| 9 | Falar com o **Elias** *antes* de colher | **Gating por pré-requisito** | Ele **não** oferece a quest de combate |
| 10 | Falar com o **Elias** *depois* da Primeira Colheita | Pré-requisito liberado | Inicia **Limpar Campos Ensolarados** |
| 11 | Abrir o Mapa-Múndi e entrar em Campos Ensolarados | WorldMap, transição de cena, combate | Batalha carrega; ao vencer, `CompleteBattle` fecha a quest |
| 12 | Falar com a **Maren** e escolher um tópico | Efeito em nó profundo (não na saudação) | Inicia **Conhecer a Maren** |
| 13 | Abrir o inventário e clicar em **Organizar** | Ordenação, botão novo | Itens compactam para o início, em ordem alfabética |
| 14 | Vender na caixa de venda e dormir | Economia, ciclo de dia, autosave | Ouro entra; o dia avança |
| 15 | Deixar a stamina baixar | **Cor de estado crítico** | Barra fica **âmbar abaixo de 40%**, **vermelha abaixo de 20%** |
| 16 | Apertar **M** três vezes | Estados do minimapa | Normal → semi-transparente → **tela cheia de verdade (90%)** |
| 17 | Trocar o idioma no menu | Localização | Tudo traduz, **inclusive** os botões do tutorial |

## O que olhar com atenção

Coisas que mexi e que só você julga:

- **Caixa de diálogo** — agora é 43% da tela com o retrato do NPC à esquerda. O recorte
  pega cabeça e ombros; conferir se algum aldeão fica mal enquadrado.
- **Barra do tutorial** — 5% da tela, embaixo. Conferir se dá pra ler enquanto joga.
- **Badge "E"** — aparece sobre qualquer coisa interagível.
- **Andar atrás de árvores e casas** — deve passar por trás quando você está acima delas.

## Limites conhecidos

- **8 dos 9 retratos** são silhuetas sem rosto, então o diálogo usa o **sprite do NPC**.
  Quando um retrato real for desenhado, apagar o nome de `PlaceholderPortraits` em
  `NPCDialogueInteractable.cs` e ele volta a mandar.
- A quest da Maren sai de um **tópico**, não da saudação — é assim que foi escrita.
- O terreno tem a paleta certa, mas a **estrutura** dos tiles ainda é a da demo.
