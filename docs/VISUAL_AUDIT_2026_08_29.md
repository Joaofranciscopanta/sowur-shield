# Auditoria Visual, UI e UX — 29 ago 2026

> Conduzida em Play Mode via Unity MCP (MainMenu, SampleScene, CombatScene a 1920x1080)
> e no build WebGL #32. Medições diretas de rects, contrastes e import settings — não
> leitura de código. Relatório formatado:
> https://claude.ai/code/artifact/027f7f38-0804-4e75-9d7e-46512a40be5a

## Veredito

Nota geral **3.5/10**. Maturidade **2/5 (vertical slice inicial)**.

O problema central **não é falta de features** — é o pipeline de arte 2D e a ausência de
composição de tela. Farming, animais, diálogo com memória, missões, relacionamento, 4 slots
de save, minimapa com fog, combate por turnos e localização em 3 idiomas estão todos
implementados e funcionando. O código está em nível 3; a apresentação está em nível 1.

Existe um design system real e bem documentado (`UITheme.cs`). O problema é **adoção**,
não ausência.

## Notas

| Dimensão | Nota |
|---|---|
| Geral | 3.5 |
| Visual | 3 |
| UI | 4 |
| UX | 4 |
| Polimento | 2 |
| Coerência | 3 |
| Legibilidade | 4 |
| Apresentação profissional | 2 |

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

**Fase 1 — Crítico** (quase tudo é configuração, não código; melhor relação esforço/impacto)
- Mover `MinimapFog`/`MinimapGround` para a layer `Minimap (6)`
- Separar `DisplayTilemap` do subsistema do minimapa
- Passe de import: Point filter + PPU único + Compression None
- Criar pilha de sorting layers e reatribuir renderers
- Corrigir sobreposição de textos do combate e os `fontSize=400`
- Preencher catálogo da loja e sincronizar gold
- `StorageContainer` 196 -> 255px
- Auto-size no `MoneyText`

**Fase 2 — Importante**: tilemap real com variação, padronizar canvases em 1920x1080,
botões de idioma, retratos de diálogo, traduzir strings, contraste do minimapa,
consolidar HUD, estados visuais de slot.

**Fase 3 — Polimento**: floating text, microanimações, ícones no minimapa, ordenação por Y,
sorting no inventário, indicador de seleção na hotbar.

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
