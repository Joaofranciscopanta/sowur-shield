# Minimap — Asset Brief (o que gerar com IA e onde deixar)

> Estado em 2026-08-23, branch `feature/minimap-overhaul`.
> O minimapa **já funciona sem nenhum destes assets** — o terreno é gerado
> proceduralmente a partir do mundo. Tudo aqui é upgrade visual, não desbloqueio.

---

## ⚠️ Antes de mais nada: a IA não está configurada

Verificado direto no Unity nesta sessão:

```
fal:        configured: false
openrouter: configured: false
```

**Não dá para gerar nada hoje.** Você precisa pôr uma API key no secure store do
editor (MCP for Unity → configurações de provider). `fal.ai` é o mais barato para
pixel art. Enquanto isso não acontecer, o jogo continua funcionando com o terreno
procedural.

---

## ⛔ O que NÃO gerar

**Os marcadores de tipo** (player, NPC, animal, prédio…). Já estão prontos em
`MinimapIconSprites.cs`, desenhados em código, e são **melhores** que arte gerada:

- Um marcador tem 3–7px no HUD. Não há espaço para detalhe — só silhueta.
- Código escala para qualquer resolução sem reimportar.
- Muda de cor por tint, sem gerar 10 variações.

Já foi medido: usar arte do jogo encolhida virou confete ilegível.

---

## ✅ O que vale gerar (em ordem de impacto)

### 1. Tileset de terreno do minimapa ⭐ maior impacto

Substituiria as manchas procedurais por um mapa de verdade.

| | |
|---|---|
| **Pasta** | `Assets/Resources/Sprites/Minimap/Terrain/` |
| **Tamanho** | 32×32 px cada, PNG, sem transparência |
| **Quantidade** | 8 tiles |

Arquivos (nomes exatos):

```
tile_grass.png       verde médio, textura sutil
tile_grass_dark.png  variação mais escura (mosqueado)
tile_dirt.png        terra batida / caminho
tile_soil.png        solo arado (marrom escuro, sulcos)
tile_water.png       água azul
tile_stone.png       pedra / rocha cinza
tile_sand.png        areia clara (margem)
tile_forest.png      copa de árvore vista de cima
```

**Prompt sugerido:** `top-down 32x32 pixel art terrain tile, seamless tileable,
[grass/dirt/water/…], flat colors, no outline, muted farming game palette, no text`

**Importante:** vistos de **cima** (top-down), não em perspectiva. E precisam ser
*tileable* (repetir sem emenda visível).

> Ao adicionar isto, ligue `deferToAuthoredTerrain` no componente
> `MinimapTerrainPainter` (no GameObject `MinimapController`) para o painter
> procedural sair do caminho.

---

### 2. Moldura decorativa do minimapa

| | |
|---|---|
| **Pasta** | `Assets/Resources/Sprites/UI/Frames/` |
| **Arquivo** | `frame_minimap.png` |
| **Tamanho** | 256×256 px, PNG **com** transparência no miolo |
| **Import** | `Sprite`, 9-slice com bordas de ~32px |

Deve combinar com `panel_wood_generic.png` que já existe.

**Prompt:** `square wooden frame border for a game minimap, top-down farming game UI,
carved wood, hollow transparent center, 9-slice friendly, pixel art, no text`

> ⚠️ **Armadilha documentada no CLAUDE.md:** uma sprite `Sliced` cujas bordas somam
> mais que o rect **não desenha nada** — sem erro, sem aviso. Bordas de 32px exigem
> um rect de no mínimo 64×64.

---

### 3. Ícones de zoom (só se quiser botões clicáveis)

| | |
|---|---|
| **Pasta** | `Assets/Resources/Sprites/UI/Icons/` |
| **Arquivos** | `icon_zoom_in.png`, `icon_zoom_out.png`, `icon_minimap_expand.png` |
| **Tamanho** | 96×96 px, PNG com transparência |

> ⚠️ **Não** monte estes botões com o kit atual: `button_danger` é 5:1 (600×120) com
> borda de 16px. Botão quadrado com essa arte é impossível — isso custou meses no
> botão de deletar save. Se quiser botões, gere a arte do botão junto, na proporção
> certa, ou use rótulo de texto.

---

### 4. Pins colocáveis pelo jogador (feature futura, estilo Valheim)

| | |
|---|---|
| **Pasta** | `Assets/Resources/Sprites/Minimap/Pins/` |
| **Tamanho** | 32×32 px, PNG com transparência, contorno escuro |

```
pin_house.png    pin_chest.png    pin_danger.png
pin_star.png     pin_question.png pin_resource.png
```

**Prompt:** `simple 32x32 pixel art map pin icon, [house/chest/skull/star/…],
bold silhouette, thick dark outline, flat single color fill, readable at 8 pixels,
transparent background, no text`

Ainda **não há código** para pins — só gere se quiser que eu implemente depois.

---

## Regras que valem para tudo

1. **Contorno escuro obrigatório** em qualquer coisa sobre o terreno. A fazenda é
   quase toda de um tom de verde; sem contorno o marcador some justamente onde
   importa.
2. **Legível a 8px.** Se você encolher a imagem para 8×8 e não reconhecer, não serve.
3. **PNG**, nunca JPG (precisa de alpha e de bordas duras).
4. **Sem texto** dentro da arte — o jogo é localizado em EN/PT/ES.
5. Depois de colocar os arquivos, me avise: eu configuro o import (Sprite Mode,
   Pixels Per Unit, Filter Mode = Point, Compression = None) e ligo no sistema.

> ⚠️ Lembre que arte importa com `spriteMode: Multiple` às vezes, e aí
> `LoadAssetAtPath<Sprite>` devolve null. Eu trato isso na hora de ligar.

---

## Resumo: pastas a criar

```
Assets/Resources/Sprites/Minimap/Terrain/    ← 8 tiles 32x32   (prioridade 1)
Assets/Resources/Sprites/UI/Frames/          ← já existe; add frame_minimap.png
Assets/Resources/Sprites/UI/Icons/           ← já existe; add 3 ícones de zoom
Assets/Resources/Sprites/Minimap/Pins/       ← 6 pins 32x32    (só se quiser a feature)
```

Se você só fizer **uma** coisa: o tileset de terreno (item 1).
