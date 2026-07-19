# Status de Animação — Sowur Shield

> Nota de continuidade para o próximo chat. O que já foi animado e o que falta.
> Os clipes/controllers gerados ficam em `Assets/Art/Characters/Generated/`.
> Padrão usado: fatiar sprite a partir do `.meta` da folha (internalID) → `.anim` (sprite-swap) → controller → Animator no prefab/AnimalData.

## ✅ Já animado (feito e na cena)

- **Bunny (jogador)** — animator completo: idle / walk / run / hoe / axe / water × 4 direções.
  `Generated/Bunny_*.anim` + `Generated/BunnyController.controller` (na cena, Bunny usa esse controller).
  Código: `PlayerMove.FaceDirection` + `TriggerActionAnimation`; run via `isRunning`; ações disparadas por `CursorController.ProcessToolUsage` e `SoilBlockInteractable.WaterSoil`.
- **Regar** — respingo (`WaterSplashEffect.cs`, sheet em `Resources/Effects/WaterCanFrames`) + frames laterais compostos (corpo+regador) em `Generated/Bunny_WaterSide.png`.
- **Galinha (Clucky)** — idle/walk/eat + `ChickenController` + Animator adicionado ao prefab `Prefabs/Clucky.prefab`. Usa `chicken blue`.
- **Vaca (Bessie)** — animal NOVO completo: `Generated/Animals/Cow_*.anim` + `CowController` + `Resources/Animals/cow.asset` + `Prefabs/Bessie.prefab` (na cena). Produz **leite** (`Resources/Items/Milk.asset` + `Resources/Prefabs/GroundItems/Milk_GroundItem.prefab`).
- **Cortar árvore** — `Scripts/Farming/ChoppableTree.cs` (IInteractable) + `Generated/Scenery/Tree_Sway`(sutil)/`Tree_Fall` + `TreeController` + `Prefabs/Tree.prefab` (na cena). Item **Wood** (`Resources/Items/Wood.asset` + ground drop). Item **Axe** (`Resources/Tools/Axe.asset` + `Prefabs/GroundAxe.prefab` na cena p/ pegar).
- **Ajuste de ranges** — `GameBalance` (defaultInteractionRange 1.2, sellBox 1.4, maxToolDistance 1.6), NPC 1.3, árvore 1.2. Objetos espalhados na cena.

## ✅ Clipes + Controllers + Prefabs gerados (NA CENA — 1 exemplar de cada)

### Variações de cor — Galinha (4 cores novas)
- `ChickenBrown/Default/Green/Red` — Idle/Walk/Eat .anim + Controller + Prefab na cena (x≈12–18, y≈-2)
- Mesma estrutura da blue (indices 0-3 idle, 11-20 walk, 47-51 eat). Parâmetros: `IsWalking`, `IsEating`.

### Variações de cor — Vaca (4 cores novas)
- `CowGreen/Light/Pink/Purple` — Idle/Walk/Eat .anim + Controller + Prefab na cena (x≈12–20, y≈-5)
- Mesma estrutura da brown (indices 0-2 idle, 3-10 walk, 32-35 eat). Parâmetros: `IsWalking`, `IsEating`.

### Pintinhos (5 cores)
- `ChickDefault/Blue/Brown/Green/Red` — Idle/Walk/Eat .anim + Controller + Prefab na cena (x≈12–18, y≈-8)
- Indices: 0-3 idle, 4-10 walk, 47-51 eat. Parâmetros: `IsWalking`, `IsEating`.

### Bezerros (5 cores)
- `CalfBrown/Green/Light/Pink/Purple` — Idle/Walk/Eat .anim + Controller + Prefab na cena (x≈12–20, y≈-11)
- Indices: 0-1 idle, 19-26 walk, 27-30 eat. Parâmetros: `IsWalking`, `IsEating`.

### Ovos (5 cores)
- `EggDefault/Brown/Green/Blue/Red` — Wobble/Hatch .anim + Controller + Prefab na cena (x≈12–18, y≈-14)
- Wobble (loop): indices 1-9. Hatch (once): indices 47-51. Parâmetro: `IsHatching`.

### ~~Inimigos~~ — REMOVIDOS
- Os 11 prefabs de inimigos e controllers foram deletados a pedido do jogador.
- Clipes `.anim` de idle permanecem em `Generated/Enemies/` como referência mas não estão na cena.

### Cenário — 5 com Prefab + Controller
- `BirdDecor` — estados Fly / Jump, parâmetro `IsJumping` (3, 8)
- `CatDecor` — estados Idle / Walk, parâmetro `IsWalking` (-3, -3.5)
- `SmokeDecor` — estado único loop (-5, -4) — perto da casa
- `WaterDecor` — estado único loop (-1, -6) — lago perto da casa
- `HomeTreeDecor` — estado único Sway (0, 5.5) — centro da vila

## ✅ Sistema de Pesca (novo)

- **Bunny** — animação de pesca: `Fish_Down/Up/Right/Left` × 4 direções no BunnyController (trigger `Fish`).
  Clipes em `Generated/Bunny_Fish_*.anim`, mesmos frames do Premium sheet (movimento de lançar semelhante ao regar).
- **FishingSpot.cs** — `Assets/Scripts/Farming/FishingSpot.cs` (IInteractable). Timer aleatório (2-5s), 75% chance de pegar peixe, 15% chance de peixe raro.
- **FishingRod** — `Resources/Tools/FishingRod.asset` (tag `FishingRod`), `Prefabs/GroundFishingRod.prefab` na cena (x=3,y=-14).
- **Fish / RareFish** — `Resources/Items/Fish.asset` (valor 15) e `RareFish.asset` (valor 50), prefabs de GroundItem em `Resources/Prefabs/GroundItems/`.
- **FishingSpot prefabs** — 2 instâncias na cena: `FishingSpot_Lake` (5,-15) e `FishingSpot_River` (8,-15).
- **CursorController** — atualizado para reconhecer tag `FishingRod` e chamar `ProcessFishing()` + trigger `Fish`.

## ✅ NPC Genérico (novo)

- **generic_npc.prefab** — sprite atualizado para Basic Character (não mais placeholder de galinha).
  - Animator com `NPCController.controller` (guid: `085b933f3b0b4d4c9ef280962c3ecf9e`).
  - Clipes: `NPC_Idle_Down/Up/Right/Left` (6fps, loop), `NPC_Walk_Down/Up/Right/Left` (10fps, loop).
  - Ações: `NPC_Hoe_Down/Up/Right/Left`, `NPC_Axe_Down/Up`, `NPC_Water_Down/Up`.
  - BlendTree 2D Freeform Cartesian: parâmetros `MoveX`, `MoveY`, `IsWalking`.
  - Sprite sheet: Basic Charakter Spritesheet (guid: `ce5acadeea6b2484692086623f08bcb8`), Actions (guid: `0cc01add09507584a9fc511b4296f763`).

## ✅ Árvores Frutíferas (4 tipos)

- **Apple / Orange / Peach / Pear** — cada uma com 3 clipes + 1 controller:
  - `FruitTree{Type}_IdleSway.anim` (6 frames, 6fps, loop) — balanço suave
  - `FruitTree{Type}_Shake.anim` (12 frames, 10fps, once) — sacudir para colher
  - `FruitTree{Type}_Grow.anim` (4 frames, 2fps, once) — estágios de crescimento
  - `FruitTree{Type}Controller.controller` — Idle→Shake via trigger `Shake`, auto-retorno
- Prefabs em `Prefabs/FruitTrees/FruitTree_{Type}.prefab` (Animator + SpriteRenderer + BoxCollider2D).
- Na cena: Apple (13,-2), Orange (15.5,-2), Peach (13,-4.5), Pear (15.5,-4.5) — pomar à direita.
- Sheets: `tree_appel/orange/peach/pear_sprites.png` do premium pack.

## ✅ Plantações (análise concluída)

- Crescimento por estágio é tratado pelo `CropGrowthManager` (troca de sprite estático por fase, não é animação frame-a-frame).
- 6 CropData existentes (Cabbage, Carrot, Mystery, Pumpkin, Radish, Tomato) usam PNGs individuais de `growing_plants/`.
- Sheet `Farming Plants.png` (54 sprites, 80×240) do premium pack está disponível para futuros crops mas não é referenciado por nenhum CropData.
- **Não necessita de .anim clips** — sistema funciona corretamente com sprite swap.

## ✅ Decorações (34 prefabs estáticos)

- Prefabs em `Prefabs/Decorations/` — sprites do premium pack, sem animação (estáticos).
- **Utilidades**: WorkStation (-3, 3), WaterWell (-1.5, 4.5)
- **Flores**: Sunflower (1, 6), Pink (2.5, 6.5), Purple (4, 6), Blue (5.5, 6.5), Red (3, 7) — jardim acima do spawn
- **Cogumelos**: Pink (-9, 3), Brown (-8, 2), Purple (-10, 4), Group (-7.5, 3.5) — bosque à esquerda
- **Pedras**: Small1 (-4, 1), Small2 (5, 2), Large1 (-8, -2), Large2 (13, 3)
- **Arbustos verdes**: Green1 (-7, -3), Green2 (-5.5, -2.5), Clover (-6, -1.5)
- **Arbustos frutíferos**: Berry1 (-8, -6), Berry2 (-6.5, -6.5), Berry3 (-5, -6)
- **Árvores decorativas**: HeartFruit (-11, 6), OrangeFruit (-10, 3), PearFruit (12, 7), Big (14, 5)
- **Tocos**: Small (-9, 0), Large (11, 4)
- **Barcos**: Small (4, -13), Large (6, -12) — área do lago
- **Piknik**: Blanket (6, -9), Basket (6.3, -8.5)
- **Placas**: Crops (-2, 0.5), Fruit (-1, 0.5), Fish (0, 0.5) — perto do SellBox

## 🗺️ Layout da Cena (reorganizado)

```
  y=8  ── Floresta (cogumelos, árvores, Bird) ──── Jardim de flores ──
  y=5  ── HomeTree  WaterWell  Tree ──────────── Árvores decorativas ──
  y=3  ── WorkStation  NPC  Player(2,3.5) ────── Pedras / Stumps ─────
  y=1  ── Stones  Signs ──────────────────────── GroundAxe ────────────
  y=0  ──────────── SellBox(0,0) ─────────────── FeedingTrough(6,0.5) ─
  y=-2 ── Bushes ────────────────────────────── Galinhas(8-11) + Pomar(13-16)
  y=-5 ── Bed(-6,-5) Casa/Smoke ─────────────── Vacas(8-11) + Pomar ──
  y=-6 ── Berry bushes ──────────────────────────────────────────────── 
  y=-8 ──── Piknik(6,-9) ───────────────────── Pintinhos(8-11) ───────
  y=-11 ─────────────────────────────────────── Bezerros(8-11) ───────
  y=-13 ── Boats(4-6) ── FishingRod(3.5) ──── Ovos(8-11) ────────────
  y=-15 ── FishingSpots(5-8) ─────────────────────────────────────────
```

## ⬜ Pendente

### Sem spritesheet (não animável por enquanto)
- **Pato (duck)** e **Pardal (Sparrow)** — têm `Resources/Animals/*.asset` mas NÃO têm spritesheet dedicado no pack. Precisam de arte nova.
- **~21 inimigos estáticos** — imagem única, sem frames para animar. Precisam de spritesheet real.

## Notas técnicas / pendências
- **Push do git** — commit local `7909286` + novos arquivos de animação precisam de commit e push em `main`. ⚠️ Push em `main` tocando `Assets/**` dispara o build/deploy WebGL.
- Working tree tem ~2800 arquivos com diff só de fim-de-linha (CRLF) pré-existente.
- Quirk conhecido (não mexido): `PlayerMove.Update` gira o `transform` (RotateTowards) — resíduo pra top-down, pode limpar.
- Catálogo completo de sprites animáveis: `CATALOGO_SPRITES_ANIMAVEIS.md`.
- Guia de integração dos clipes do Bunny: `Assets/Art/Characters/Generated/_LEIA-ME_ANIMACOES.md`.
