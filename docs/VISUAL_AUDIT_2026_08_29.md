# Auditoria Visual, UI e UX — 29 ago 2026

> Conduzida em Play Mode via Unity MCP (MainMenu, SampleScene, CombatScene a 1920x1080)
> e no build WebGL #32. Medições diretas de rects, contrastes e import settings — não
> leitura de código. Relatório formatado:
> https://claude.ai/code/artifact/027f7f38-0804-4e75-9d7e-46512a40be5a

## ⚠️ Correções à auditoria (feitas ao implementar a Fase 1)

**Dez** afirmações deste relatório estavam **erradas** e são corrigidas aqui. Ficam
registradas porque o erro de método é reutilizável — e porque metade da nota que dei ao
jogo veio de defeitos que não existiam.

| Afirmação original | Realidade | Como o erro aconteceu |
|---|---|---|
| #13 "Dinheiro alto vaza 90px para fora da tela" | **Falso.** `MoneyText` tem `autoSize` e encolhe 19,7 → 12,7pt; cabe em 128px com `$999999999` | Medi `preferredWidth`, que ignora o auto-size |
| #9 "3 botões de idioma com largura 0 no mesmo pixel" | **Falso.** O `VerticalLayoutGroup` os dispõe a 372×44 quando o painel é ativado | Li o `sizeDelta` serializado de um painel **inativo**, cujo layout nunca rodou |
| #20 "Placeholder `Item Descrption` visível ao jogador" | **Falso.** `ItemTooltip` sobrescreve os 3 textos antes de exibir | Vi o valor de design-time na cena e presumi que chegava à tela |
| #15 "Retratos nulos = quadrados brancos 136×136" | **Falso.** `PortraitManager` zera o `CanvasGroup.alpha`; medido 0,00 em runtime | Li `sprite=NULL` + `color.a=1` e não conferi o CanvasGroup |
| **#7 "Inimigos são bolinhas vermelhas sem sprite"** | **Falso.** 32 PNGs existem, 32 de 34 `EnemyData` têm sprite, 25 de 25 stages têm inimigos | **Abri a `CombatScene` direto, sem WorldMap — auditei o modo de fallback achando que era o jogo** |
| "`FloatingText` a 3pt com alpha 0.30, dano invisível" | **Falso.** É `TextMeshPro` world-space (3 = unidades de mundo) e o alpha 0.30 era um frame do meio do fade | Peguei um frame no meio da coroutine de fade |
| "`fontSize=400` em rect de 1x1 é absurdo" | **Falso.** World Space Canvas com scale 0,01 — 400 × 0,0006 dá tamanho normal | Li os world corners, que já vêm escalados |
| "Loja vazia + gold dessincronizado + título em inglês" (3 itens) | **Um único bug**, no `ItemDatabase`, não três | Tratei sintomas como causas distintas |
| "`DisplayTilemap` na `MinimapTerrain` é acoplamento acidental" | **Falso.** É deliberado e documentado em `MinimapCamera.cs` — separa "chão" de "props" | Não li o comentário antes de concluir |

O bug real por trás da loja: `ItemDatabase.Instance` só chamava `Initialize()` dentro do
`if (instance == null)`, então uma primeira chamada prematura cacheava um dicionário vazio
para a sessão inteira. Corrigido em `86e4f3e`, com testes de regressão provados vermelhos
em `d5c1c35`.

**A lição de método**, em duas partes:

1. **Ler um valor em repouso não é observar o comportamento.** `preferredWidth` antes do
   auto-size, world corners já escalados, um frame no meio de um fade, o `sizeDelta` de um
   painel cujo layout nunca rodou, o texto de design-time de um label sobrescrito em
   runtime — todos "medidos" e todos falsos.

2. **Abrir uma cena direto pula a inicialização que o jogo faz.** O erro mais caro foi o
   combate: entrei na `CombatScene` sem passar pelo WorldMap, peguei o modo de fallback e
   descrevi como se fosse o produto. Os logs do Console diziam exatamente isso
   (`No stage selected — using fallback test enemies`) e eu não os li.

   Isso também explica a observação do usuário de que "às vezes os assets não carregam no
   Play Mode mas aparecem na build": não é o Editor falhando, é a cena sendo aberta fora do
   fluxo. **Sempre conferir o Console antes de chamar algo de defeito.**

## Veredito

> ⚠️ **Nota revisada após implementar as correções.** A nota original era **3.5/10 /
> maturidade 2/5**. Com dez achados desmentidos — incluindo o combate inteiro, que eu
> auditei em modo de fallback — a avaliação honesta é **5.5/10, maturidade 3/5 (indie em
> desenvolvimento)**. O jogo estava consistentemente melhor do que eu reportei.

Nota geral **5.5/10** (era 3.5). Maturidade **3/5 (indie em desenvolvimento)**.

O problema central **não é falta de features** — é o pipeline de arte 2D e a ausência de
composição de tela. Farming, animais, diálogo com memória, missões, relacionamento, 4 slots
de save, minimapa com fog, combate por turnos e localização em 3 idiomas estão todos
implementados e funcionando. O código está em nível 3; a apresentação está em nível 1.

Existe um design system real e bem documentado (`UITheme.cs`). O problema é **adoção**,
não ausência.

## Notas

| Dimensão | Original | Revisada | Por quê |
|---|---|---|---|
| Geral | 3.5 | **5.5** | 10 dos 20 achados eram falsos |
| Visual | 3 | **5** | O combate tem cenário pintado; a arte estava lá |
| UI | 4 | **5** | Layouts que julguei quebrados funcionavam |
| UX | 4 | **5** | Fallbacks são deliberados e logados, não bugs |
| Polimento | 2 | **4** | Retratos, portraits e estados já existiam |
| Coerência | 3 | 3 | Mantida: PPU e escalas seguem inconsistentes |
| Legibilidade | 4 | 4 | Mantida: contraste do minimapa era real |
| Apresentação profissional | 2 | **4** | O fluxo real se apresenta bem melhor |

As dimensões "Visual", "Coerência" e "Apresentação" são julgamento subjetivo de direção de
arte. As afirmações factuais abaixo são todas medidas, com a evidência numérica junto.

## Os 4 achados estruturais

### 1. O mundo não tem terreno (P0)

O "chão" verde-limão **não é o terreno do jogo** — é o `MinimapTerrainPainter`. Provado
removendo a layer 8 do culling mask da Main Camera: o verde desapareceu junto com o fog.
O mundo real é branco/vazio, com objetos flutuando.

```
DisplayTilemap  -> layer = MinimapTerrain (8), não Default
tiles distintos -> TilesDemo_6 x 10.201  (1 tile único repetido)
Main Camera     -> cullingMask -65 (exclui só layer 6)
```

### 2. Fog do minimapa renderiza sobre o mundo (P0)

`MinimapFog` está na layer `MinimapTerrain (8)`, que a Main Camera renderiza. A vinheta
escura que cobre o gameplay é este bug, não iluminação.

```
MinimapFog: layer=8, sortingOrder=50, extents=(14.86, 15.69)
Main Camera renderiza layer 8? -> True
```

### 3. Pipeline de pixel art inconsistente (P0)

```
filterMode -> 59 de 70 texturas em cena = Bilinear (84%)
           -> 497 de 643 arquivos .meta no disco (77%)
PPU        -> 14 valores distintos: 16, 32, 40, 100, 128,
              749, 799, 803, 864, 890, 909, 948, 959, 994
escalas    -> de 0,30 a 5,50 compensando PPU errado à mão
```

Os PPU de 749–994 são auto-gerados no import, nunca configurados.

### 4. Uma única sorting layer no projeto inteiro (P0)

`ProjectSettings/TagManager.asset` tem apenas `Default`. Todos os 142 sprites da cena
estão nela, diferenciados só por `sortingOrder` 0–5. Sem estrutura de profundidade.

## Top 20 problemas

| # | Pri | Problema | Local |
|---|---|---|---|
| 1 | P0 | Mundo sem terreno; verde é o painter do minimapa | SampleScene |
| 2 | P0 | Fog do minimapa renderiza sobre o mundo | Main Camera |
| 3 | P0 | 84% das texturas em Bilinear | Import settings |
| 4 | P0 | Projeto tem 1 única sorting layer | TagManager |
| 5 | P0 | Loja abre vazia, gold dessincronizado (0 vs $567) | ShopUI |
| 6 | P0 | Textos sobrepostos no combate (241px de colisão) | CombatScene |
| 7 | P0 | Inimigos são bolinhas vermelhas sem sprite | CombatScene |
| 8 | P1 | Grid do inventário estoura 59px — 4a linha cortada | StorageContainer |
| 9 | P1 | Botões de idioma com largura 0, 3 no mesmo pixel | MainMenu |
| 10 | P1 | MainMenuCanvas em 1366x768; resto em 1920x1080 | MainMenu |
| 11 | P1 | VersionText inteiramente fora da tela (Y negativo) | MainMenu |
| 12 | P1 | Texto do minimapa branco sobre branco 39% (~1:1) | MinimapUI |
| 13 | P1 | Dinheiro alto vaza 90px para fora da tela | MoneyText |
| 14 | P1 | ~20 strings em inglês num jogo em PT | Global |
| 15 | P1 | Retratos de diálogo nulos com alpha 1 | DialoguePanel |
| 16 | P1 | Minimapa "fullscreen" usa 40% da tela, 85% preto | MinimapController |
| 17 | P2 | Hotbar a 2px do fundo da tela | Inventory |
| 18 | P2 | Days e TimeText se sobrepõem 2px | HUD |
| 19 | P2 | Título do menu de pausa metade fora da moldura | GameMenuUI |
| 20 | P2 | Placeholder "Item Descrption" (com erro) ativo | Tooltip |

## Medições da HUD

| Elemento | Rect | Posição | Problema |
|---|---|---|---|
| StaminaBarBG | 190x36 | X 23–213 | Barra real é 60x20 dentro (31%) |
| MoneyText | 128x38 | X 1707–1835 | Precisa 195px — estoura 52% |
| Days | 230x40 | X 767–**997** | Sobrepõe TimeText |
| TimeText | 150x40 | X **995**–1145 | 2px de colisão |
| Inventory (hotbar) | 620x75 | Y 2–62 | 2px do fundo |
| DialogueCanvas | 1920x1058 | X 0–**1940** | 20px fora da tela |

## Inventário: a conta que não fecha

```
largura:  9 x 60 + 8 x 5 = 580  <= 604  OK
altura:   4 x 60 + 3 x 5 = 255  >  196  ESTOURO de 59px
```

## Combate

```
PlayerTeamText   X=[651..951]    "Seu Time: 1/1"
ModifierBanner   X=[710..1210]   "O dano causado e recebido é dobrado!"
                 -> 241px de sobreposição direta

NameText     fontSize=400  em rect de 1x1 px
HealthText   fontSize=400  em rect de 1x1 px
FloatingText fontSize=3    alpha=0.30   <- dano invisível
```

Único sprite da cena: `Chicken (Test)`, placeholder, com escala **negativa** (-5,33).
`ConsumableList` tem altura 0 (300x0). "Turno: 2/500" expõe limite de debug.

## Casos extremos

| Caso | Necessário | Disponível | Resultado |
|---|---|---|---|
| `$999999999` | 303px | 128px | Vaza até x=2010 numa tela de 1920 |
| Nome de item longo | 462px | 260px | Estouro 202px (mitigado por Ellipsis) |

O caso do dinheiro não é hipotético — é a progressão normal de um farming sim.

## Responsividade / WebGL

```
canvas WebGL   960 x 600   -> aspect 1.600
UI projetada  1920 x 1080  -> aspect 1.778
scaleFactor efetivo ~0,55
```

O build público roda num aspect ratio para o qual a UI nunca foi projetada, então os
estouros medidos em 1080p são proporcionalmente piores lá.

| Canvas | Ref | 1080p | 4K |
|---|---|---|---|
| Maioria (20 canvases) | 1920x1080 | 1,00 | 2,00 |
| MainMenuCanvas | 1366x768 | **1,41** | 2,81 |
| MobileControlsCanvas | 1280x720 | 1,50 | 3,00 |
| Canvas (NPC x2, inativos) | 800x600 | **2,40** | 4,80 |

## Acessibilidade

- `StateText`/`ZoomText` do minimapa: branco puro sobre branco a 39% alpha (~1:1, mínimo
  WCAG é 4.5:1)
- 34 labels abaixo de 14pt
- Sem escala de UI, sem opção para daltonismo, sem remapeamento de teclas
- Minimapa depende exclusivamente de cor para distinguir marcadores

## O que está bom (não mexer)

- **Menu de pausa** — a peça de UI mais bem resolvida; usar como referência
- **Logo e identidade do MainMenu** — ativo de marca mais forte
- **`UITheme.cs`** — design system genuíno, documentado, com histórico do porquê
- **Nunito em 293/293 labels** — zero labels sem font asset
- **Localização PT/ES/EN** — infraestrutura funciona; as ~20 strings soltas são pontas
- **Landing page do WebGL** — está acima do jogo em acabamento
- **Amplitude dos sistemas** — muito além do que a aparência sugere

## Não testado nesta auditoria

- Gamepad real e navegação por teclado nos menus
- O WebGL **jogando** (o painel do navegador não compôs frames; verificada apenas a página
  e a geometria do canvas)
- Resoluções físicas diferentes de 1920x1080 (fatores de escala foram calculados, não
  observados)

## Roadmap

**Fase 1 — Crítico** — ✅ **CONCLUÍDA** (branch `fix/visual-audit-phase1`)

| Item | Estado | Commit |
|---|---|---|
| `MinimapFog`/`MinimapGround` para a layer `Minimap (6)` | ✅ feito | `85b0837` |
| Pilha de 8 sorting layers criada | ✅ feito | `85b0837` |
| Passe de import: Point + no compression + no mipmaps (530 sprites) | ✅ feito | `2d90a87` |
| Loja vazia / gold / título — bug do `ItemDatabase` | ✅ feito | `86e4f3e` |
| Sobreposição de textos do combate (banner y=-40 → -110) | ✅ feito | `86e4f3e` |
| `StorageContainer` 196 → 255px + alinhamento com a moldura | ✅ feito | `15150bf` |
| `MoneyText` 128 → 200px (folga, não overflow) | ✅ feito | `15150bf` |
| Testes de regressão, provados vermelhos primeiro | ✅ feito | `d5c1c35` |
| Separar `DisplayTilemap` do minimapa | ❌ **cancelado** — é design deliberado, ver correções acima | — |
| PPU único | ❌ **não aplicado** — o importer já usa 100 de forma consistente; os PPU estranhos eram de sprites fatiados | — |

Resultado: 834 EditMode + 73 PlayMode, zero falhas.

**O que a Fase 1 NÃO resolveu**: o terreno continua sendo 1 tile repetido 10.201 vezes. A
infraestrutura DualGrid está completa (16 tiles de regra + placeholders de grama e terra),
então isso é trabalho de *design de nível*, não correção de bug — e é o maior ganho visual
que resta.

**Fase 2 — Importante** — parcialmente concluída

| Item | Estado | Commit |
|---|---|---|
| Terreno pintado (14 tiles distintos, era 1) | ✅ feito | `6e0933f` |
| Contraste do minimapa (branco/branco → creme/madeira) | ✅ feito | `7cd6329` |
| `VersionText` trazido para dentro da tela + contraste | ✅ feito | `7cd6329` |
| Placeholders do tooltip traduzidos, typo corrigido | ✅ feito | `69b9981` |
| Botões de idioma | ❌ **não era bug** — ver correções acima | — |
| **Trocar a arte do tileset** | ✅ **recolorido** (retiling ainda é de artista) | `0ce489c` |
| Padronizar canvases em 1920x1080 | ❌ **não era bug** — ver abaixo | — |
| Retratos de diálogo (sprite nulo com alpha 1) | ❌ **falso positivo** — ver abaixo | — |
| Consolidar a HUD em 2 grupos | ✅ feito — 19 filhos → 12, grupo `HUD` | `9bb5aaa` |
| Estados visuais de slot (hover/selected/disabled) | ✅ feito — faltava o sprite, não o código | `e956085` |

### ⚠️ Reverificação em Play Mode (2026-08-29, sessão 2)

Rodei o jogo e conferi os 5 pendentes um a um. **Dois não eram bugs** — ambos pelo mesmo
motivo que já custou caro antes: medir um valor em repouso não é observar o comportamento.

**"Canvases fora do padrão 1920x1080" — não era bug.** Os dois canvases em `ConstantPixelSize`
800x600 são `NPCs/chicken/Canvas` e `NPCs/generic_npc/Canvas`: **World Space e inativos**, com
um único `Text UI` de balão de fala. `referenceResolution` **não faz nada** em World Space.
Todos os 20 canvases de tela já estão em 1920x1080.

**"Retratos nulos com alpha 1" — falso positivo.** Abri um diálogo real (Joana): o
`LeftPortrait` carrega `portrait_joana` corretamente, e o `RightPortrait` de fato tem sprite
nulo com `Image.color.a = 1`. **Mas ele não desenha nada** — tem um `CanvasGroup` com
`alpha = 0`, então o alpha efetivo é 0. A auditoria leu o alpha da `Image` isolado e ignorou o
`CanvasGroup` acima dela. Confirmado por screenshot: não há caixa branca no diálogo.

**Os outros dois são reais**, medidos com o jogo rodando:
- **HUD**: o canvas `UI` tem **15 filhos diretos** (StaminaBarBG, StaminaIcon, StaminaSlider,
  MoneyPanelBG, MoneyText, TopCenterPanelBG, Days, TimeText, Inventory, …) em vez de 2 grupos.
- **Slots**: `InventorySlot` **não tem componente `Button`**, então não existe transição de
  hover/selected/disabled para configurar — precisa ser implementado, não ajustado.

**O bloqueio do terreno**: a estrutura agora está certa (pátio, curral, caminhos, bordas
resolvidas pelo dual-grid), mas `TilesDemo.png` é a **arte de demonstração do plugin** —
verde-limão contra roxo-vinho. Existe tileset de fazenda real em
`Assets/Art/ThirdParty/Sprout Lands .../ground tiles/New tiles/` (`Grass_tiles_v2.png` e
`Darker_Soil_Ground_Tiles.png`), mas são 176x112 com layout misto, não a grade 4x4 de 16
tiles que o dual-grid espera. **Refatiar isso é trabalho de artista no Sprite Editor**, não
algo a improvisar por script.

> **✅ Resolvido depois desta auditoria (`0ce489c`)**: o roxo foi morto por **recoloração** —
> swap de 4 cores amostradas de `Grass_tiles_v2.png` e `Darker_Soil_Ground_Tiles.png`,
> mantendo a estrutura e os 16 recortes. O retiling completo continua sendo trabalho de
> artista, mas deixou de ser bloqueio visual. Ver `HANDOFF_LUCAS.md` item 1.

**Fase 3 — Polimento** — parcialmente concluída

| Item | Estado | Commit |
|---|---|---|
| **Ordenação por Y** | ✅ feito — dá para andar atrás de árvore | `dfc5de6` |
| Indicador de seleção na hotbar | ✅ feito (mesmo commit dos slots) | `e956085` |
| Sorting no inventário | ✅ feito — a lógica existia, faltava o botão | `22a161e` |
| Ícones no minimapa | ❌ **não era pendência** — 33 ícones ativos na cena | — |
| Floating text de dano/ganho | ✅ feito na fazenda (já existia no combate) | `ebe34a4` |
| Microanimações | ⏳ pendente — único item aberto da Fase 3 | — |

**Sorting**: `Inventory.SortInventory()` já estava pronto e correto — agrupa por tipo, depois
por nome, compacta para o início do armazém e não toca no hotbar. **Nada conseguia chamá-lo.**
O `InventoryUIManager` declara quatro botões de sort e os liga, mas esse componente **não está
na cena**, então os 36 slots foram publicados sem nenhum controle de ordenação.

⚠️ **Descoberta ao posicionar o botão: a grade de 36 slots transbordava o interior pintado do
painel em todos os lados** — 46px acima, 112px abaixo, 199px à direita.

✅ **Corrigido (`bd6dca1`).** A causa era um `localScale` de **1,74** no `StorageContainer`
enquanto o painel atrás estava em 1: o rect de 604x255 desenhava a 1052x444. O mesmo scale
fazia o inventário discordar de si mesmo — um slot do armazém renderizava a ~104px contra os
48px do hotbar, mais que o dobro, para os mesmos 45 slots.

Em scale 1 a grade mede 580x271 e o interior pintado é 678x285, então cabe com folga. O
container também foi recentralizado: `panel_wood_generic` é **assimétrico** (~82/113/86/150 de
512px), então o centro da *área pintada* fica ~33px à esquerda e ~33px acima do centro do rect.
Resultado: margens de 49/49/15/15 e slots de 60px contra os 48px do hotbar.

**Ícones do minimapa**: a auditoria listou como pendente, mas há **5 scripts dedicados**
(`MinimapIcon`, `MinimapIconClusterer`, `MinimapIconSprites`, `MinimapPinManager`,
`MinimapTerrainPainter`) e **33 ícones ativos** rodando na cena. Item obsoleto.

**A ordenação por Y era o maior buraco restante.** Todos os 144 sprites estavam na layer
`Default` com ordem fixa de 0 a 5 — o jogador em 3 era desenhado *debaixo* de toda árvore em
5, independente de quem estivesse na frente. As 8 sorting layers criadas na Fase 1 nunca
tinham sido atribuídas a nada.

`YSortSprite` converte posição em `sortingOrder`, por objeto e não pela câmera — trocar a
câmera para `CustomAxis` reordenaria também os ícones do minimapa (100-130) e o fog (50), que
dependem da pilha fixa. Ordena por `bounds.min.y`, não por `transform.position.y`: a cena
mistura pivôs (NPCs nos pés, jogador/árvores/animais no centro), então ordenar pelo transform
faria uma árvore de 2 unidades ser ranqueada por um ponto a um metro do chão.

**Fase 4 — Premium** (bloqueado por arte): sprites de inimigos, cenário de combate,
iluminação, tutorial, acessibilidade, gamepad.

## Conclusão

O que impede o jogo de parecer profissional, em ordem de impacto:

1. **O mundo não existe visualmente** — sem terreno, sem profundidade, sem iluminação,
   parcialmente coberto por um bug de fog.
2. **O pipeline de arte contradiz a estética** — a arte comprada é boa; o import a destrói.
3. **Os sistemas não se apresentam** — cada um funciona, mas nenhum *se mostra* funcionando.

O que **não** é o problema: features, design system, localização ou arte de UI. O menu de
pausa e a landing page provam que a equipe entrega acabamento quando foca nele.
