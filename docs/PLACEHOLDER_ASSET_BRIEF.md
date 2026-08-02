# Sowur Shield — Guia de Geração de Placeholders

**Auditado em 2026-08-02** — lendo cada `.asset` no disco, não confiando em notas antigas.

---

## ✅ LOTE 1 IMPORTADO (2026-08-02) — falta o lote 2

46 arquivos entregues e importados. **17 dos 19 ícones de skill estão ligados e funcionando**,
os 9 NPCs e os 2 prédios também, e as variações de som já tocam.

**Ainda falta gerar (13 arquivos):**

| Arquivo | Spec | Prompt |
|---|---|---|
| `skill_feather_shield.png` | 64×64 | `game skill icon, overlapping puffed feathers forming a rounded shield, soft cream and gold` |
| `skill_herd_bond.png` | 64×64 | `game skill icon, two animal silhouettes touching heads with a warm heart glow between them, soft passive aura glow` |
| `sfx_plant_seed.wav` | 0.2s | `tiny soft press into dirt, small papery seed rustle` |
| `sfx_shovel.wav` | 0.35s | `heavier dig and scoop, coarser than tilling` |
| `sfx_item_pickup.wav` | 0.15s | `bright short blip, cheerful and light` |
| `sfx_item_drop.wav` | 0.15s | `soft muted thud, low and quiet` |
| `sfx_sell.wav` | 0.4s | `light coin chime, two or three ascending notes` |
| `sfx_menu_open.wav` | 0.2s | `soft woody click with a gentle upward swell` |
| `sfx_menu_close.wav` | 0.2s | `the same woody click, downward` |
| `sfx_sleep.wav` | 0.5s | `slow soft exhale into a low warm hum` |

> **Variações são bem-vindas em qualquer SFX.** Basta numerar: `sfx_sell1.wav`, `sfx_sell2.wav`,
> `sfx_sell3.wav`. O `SFXManager` detecta sozinho e sorteia entre elas, sem repetir a anterior.
> Um arquivo sem número também funciona.

### ⚠️ Duas coisas a corrigir na próxima geração

**1. Fundo xadrez.** 13 dos 29 PNGs vieram com o padrão xadrez de transparência **desenhado como
pixels de verdade**, em vez de alpha. Eu recortei todos por script, mas dá trabalho e no
`skill_spring_vigor` a aura amarela tinha sido pintada por cima do xadrez — para tirar o fundo
tive que sacrificar a aura. Se a ferramenta tiver opção de "transparent background" / "PNG with
alpha", ligue.

**2. Chaves de som ≠ nome de arquivo.** O código chama `CombatHit`, os arquivos são
`sfx_combat_hit`. Já resolvi no `SFXManager` (converte PascalCase → snake_case automaticamente),
então **pode continuar nomeando como está no brief** — só não renomeie os já entregues.

Dimensões grandes (1024–1254px) **não são problema** — o Unity escala na importação.

Documento para gerar os assets que faltam. Cada seção traz **nome exato do arquivo**,
**dimensões** e um **prompt pronto para colar** em IA de imagem/áudio.

Coloque tudo em `C:\Users\Lucas\Sowur Shield\_Incoming\`, mantendo os nomes exatos. Depois eu
importo, configuro os import settings e ligo cada arquivo ao asset correspondente no Unity.

> **Os nomes dos arquivos não são sugestão.** Eu ligo os assets por nome; se o nome mudar, a
> ligação falha e o item fica invisível no jogo.

> ⚠️ **Esta versão substitui o brief anterior (38 arquivos).** Boa parte daquela lista já foi
> resolvida desde então, e vários itens dela já existiam quando foi escrita. A lista das skills
> mudou por completo: as 12 skills eram **proposta**; hoje existem **19 skills reais** no disco,
> com nome, tipo e descrição definidos. Os prompts abaixo vêm desses dados.

---

## Resumo

| Prioridade | Categoria | Qtd | Por quê |
|---|---|---|---|
| 🔴 1 | Ícones de skill | 19 | Todas as 19 skills do jogo estão sem ícone |
| 🔴 2 | Efeitos sonoros | 14 | O jogo é quase totalmente mudo |
| 🟡 3 | Corpos de NPC | 9 | Os 9 aldeões dividem o mesmo sprite |
| 🟡 4 | Retrato + ícones de construção | 3 | Lacunas pequenas e isoladas |
| **TOTAL** | | **45** | |

**Mínimo útil:** os 19 ícones de skill. Só isso já fecha a maior lacuna de arte que sobrou.

---

## Regras que valem para TUDO

Estas medidas vieram dos assets que já estão no projeto, não são arbitrárias.

| Regra | Valor | Porquê |
|---|---|---|
| Formato | **PNG com transparência** | Sem fundo branco/xadrez. O canal alpha é o recorte |
| Fundo | **100% transparente** | Um fundo sólido vira um retângulo opaco no jogo |
| Estilo | Pixel art 2D, cores saturadas, contorno escuro | Combina com a arte atual (farming cozy) |
| Perspectiva | Vista lateral / 3-4, chão na base da imagem | Igual aos inimigos que já existem |
| Margem | ~10% de folga nas bordas | Evita corte ao escalar |

**Prompt base** — cole no início de todo prompt de sprite:

```
2D game sprite, pixel art style, cozy farming RPG aesthetic,
fully transparent background (PNG alpha), no background scenery,
dark outline, saturated colors, side view, single centered subject,
clean silhouette readable at small size
```

---

## 1. Ícones de Skills dos Animais — **19 arquivos** 🔴 PRIORIDADE ALTA

**Por que importa:** as 19 skills existem em `Assets/Resources/AnimalSkills/` com nome, tipo,
cooldown e descrição — mas o campo `skillIcon` está **vazio (`{fileID: 0}`) nas 19**. Verifiquei
uma por uma. É a lacuna de arte mais visível que restou.

- **Dimensões: 64×64 px**
- Referência de peso e contorno: `Resources/Sprites/UI/Icons/icon_stamina_bolt.png` (o único
  ícone 64×64 que já existe)
- Símbolo único centralizado, ~4px de margem. Renderiza pequeno na UI de combate — **sem
  detalhe fino, sem texto, sem rostos**
- **Convenção:** skill ativa lê como **ação**; skill passiva lê como **estado/aura**. As duas
  precisam ser distinguíveis de relance

### 1a. Skills ativas — 12 arquivos

| Arquivo | Skill | Prompt (acrescente ao prompt base) |
|---|---|---|
| `skill_precise_peck.png` | Precise Peck | `game skill icon, sharp beak striking a small target ring, motion line behind it, yellow and white, quick and accurate` |
| `skill_peck_of_weakening.png` | Peck of Weakening | `game skill icon, beak strike with two small downward arrows beside it, dull grey-purple, weakening debuff` |
| `skill_rooster_fury.png` | Rooster Fury | `game skill icon, rooster comb silhouette wreathed in red rage marks, reckless forward motion` |
| `skill_feather_shield.png` | Feather Shield | `game skill icon, overlapping puffed feathers forming a rounded shield, soft cream and gold` |
| `skill_flock_call.png` | Flock Call | `game skill icon, open beak with three expanding sound arcs, tiny bird silhouettes riding the arcs, rallying and upbeat` |
| `skill_restoring_song.png` | Restoring Song | `game skill icon, musical notes rising from a soft glow, warm green healing tones, gentle leaf motif` |
| `skill_toxic_quack.png` | Toxic Quack | `game skill icon, duck bill emitting a sickly green cloud with bubbles, foul and poisonous` |
| `skill_venomous_bite.png` | Venomous Bite | `game skill icon, two curved fangs dripping a violet-green droplet, poison over time` |
| `skill_hoof_kick.png` | Hoof Kick | `game skill icon, hoof striking with a heavy impact burst, dust puff below, weighty and blunt` |
| `skill_hide_wall.png` | Hide Wall | `game skill icon, thick layered hide braced like a wall, heavy brown, sturdy horizontal banding` |
| `skill_draining_howl.png` | Draining Howl | `game skill icon, howling muzzle with dark spiral arcs pulling inward, desaturated blue-grey, unsettling` |
| `skill_burrow.png` | Burrow | `game skill icon, mound of earth with a tunnel opening and a tail vanishing into it, dust motes above` |

### 1b. Skills passivas — 7 arquivos

Estas precisam de um **brilho de aura suave** em volta do símbolo, para diferenciar das ativas.

| Arquivo | Skill | Prompt (acrescente ao prompt base) |
|---|---|---|
| `skill_flock_instinct.png` | Flock Instinct | `game skill icon, three small bird silhouettes in formation with a speed arc beneath, soft passive aura glow` |
| `skill_large_brood.png` | Large Brood | `game skill icon, cluster of small chicks around a central egg, restless energy sparks, soft passive aura glow` |
| `skill_herd_bond.png` | Herd Bond | `game skill icon, two animal silhouettes touching heads with a warm heart glow between them, soft passive aura glow` |
| `skill_loyal_companion.png` | Loyal Companion | `game skill icon, steadfast animal silhouette behind a protective halo, deep warm gold, soft passive aura glow` |
| `skill_supporters_blessing.png` | Supporter's Blessing | `game skill icon, upward-cupped wing under a small radiant star, soft blue-white, soft passive aura glow` |
| `skill_spring_vigor.png` | Spring Vigor | `game skill icon, fresh green sprout with an upward bounce arc, bright spring palette, soft passive aura glow` |
| `skill_winter_coat.png` | Winter Coat | `game skill icon, thick fur ruff with a snowflake resting on it, cool blue-white over warm brown, soft passive aura glow` |

> **Herd Bond e Loyal Companion** são as duas que ligam felicidade do bicho a poder de combate
> (≥75 e ≥90 de happiness). Vale que leiam como "vínculo/confiança", não como buff genérico.

---

## 2. Efeitos Sonoros — **14 arquivos** 🔴

O projeto tem só **7 arquivos de áudio no total** (3 músicas + 4 sons), contra **~40 campos
`AudioClip`** no código. É a maior lacuna do projeto — lavrar, regar, plantar, colher, vender,
pegar item, abrir menu, dormir e todo o combate estão mudos.

- **Formato: WAV**, 44.1 kHz, mono
- **Duração: 0.1s a 0.5s** (curtos — tocam a cada ação)
- Estilo cozy/suave, sem ataque agressivo. Pense *Stardew Valley*, não jogo de ação

### 2a. Chaves nomeadas — maior valor (4)

Estas são chamadas por string via `SFXManager.Play("<chave>")`. O call site **já existe no
código**: o som funciona no instante em que o clipe for atribuído.

| Arquivo | Chave | Prompt |
|---|---|---|
| `sfx_combat_hit.wav` | `CombatHit` | `soft padded thump with a light snap, impactful but not violent, 0.2 seconds` |
| `sfx_combat_death.wav` | `CombatDeath` | `gentle descending three-note fall, soft and non-gory, 0.5 seconds` |
| `sfx_harvest_crop.wav` | `HarvestCrop` | `light leafy rustle plus a satisfying soft pop, harvest crop, 0.3 seconds` |
| `sfx_pet_animal.wav` | `PetAnimal` | `warm short fur-ruffle with a tiny content chirp, 0.3 seconds` |

### 2b. Fazenda e mundo (10)

| Arquivo | Campo que usa | Prompt |
|---|---|---|
| `sfx_till_soil.wav` | `SoilBlockInteractable.tillSound` | `hoe biting into earth, crumbly dirt, 0.3 seconds` |
| `sfx_water_soil.wav` | `SoilBlockInteractable.waterSound` | `short gentle water pour onto soil, 0.4 seconds` |
| `sfx_plant_seed.wav` | `SoilBlockInteractable.plantSound` | `tiny soft press into dirt, small papery seed rustle, 0.2 seconds` |
| `sfx_shovel.wav` | `SoilBlockInteractable.shovelSound` | `heavier dig and scoop, coarser than tilling, 0.35 seconds` |
| `sfx_item_pickup.wav` | `Inventory.pickupSound` | `bright short blip, cheerful and light, 0.15 seconds` |
| `sfx_item_drop.wav` | `Inventory.dropSound` | `soft muted thud, low and quiet, 0.15 seconds` |
| `sfx_sell.wav` | `SellBox.sellSound` | `light coin chime, two or three ascending notes, 0.4 seconds` |
| `sfx_menu_open.wav` | `GameMenuManager.menuOpenSound` | `soft woody click with a gentle upward swell, 0.2 seconds` |
| `sfx_menu_close.wav` | `GameMenuManager.menuCloseSound` | `the same woody click, downward, 0.2 seconds` |
| `sfx_sleep.wav` | `BedInteractable.sleepSound` | `slow soft exhale into a low warm hum, 0.5 seconds` |

---

## 3. Sprites de corpo dos NPCs — **9 arquivos** 🟡

Existe **exatamente 1 arquivo**: `Assets/Art/NPCs/npc_villager_placeholder.png`. Os 9 aldeões
estão todos na SampleScene compartilhando ele — o jogador não consegue saber quem é quem sem
chegar perto e abrir o diálogo.

- **Dimensões: 32×32 px** (igual ao placeholder atual)
- Pasta destino no jogo: `Assets/Art/NPCs/`
- Corpo inteiro, de frente, parado (idle). Só 1 frame por enquanto
- **Consistência:** os 9 precisam ler como o mesmo estilo e proporção — só paleta, cabelo e
  silhueta de roupa mudam
- **8 destes já têm retrato** em `Art/NPCs/Portraits/`. Combine cor de cabelo e roupa com o
  retrato existente, para corpo e rosto concordarem

| Arquivo | Personagem | Prompt (acrescente ao prompt base) |
|---|---|---|
| `npc_tomas.png` | Tomás, ferreiro | `village blacksmith, burly man, leather apron, soot-stained arms, short dark beard, hammer at belt, standing idle, facing viewer` |
| `npc_isabela.png` | Isabela, padeira | `village baker, woman, flour-dusted apron, hair tied in a bun, warm cream and wheat colors, holding a small loaf, standing idle, facing viewer` |
| `npc_joana.png` | Joana, pescadora | `village fisherwoman, weathered woman, rolled-up trousers, straw hat, fishing rod over shoulder, blue and tan colors, standing idle, facing viewer` |
| `npc_elias.png` | Elias, pastor | `village shepherd, calm older man, wool cloak, shepherd crook, muted green and brown, standing idle, facing viewer` |
| `npc_clara.png` | Clara, herborista | `village herbalist, woman, green robe, satchel of herbs, small flowers in hair, standing idle, facing viewer` |
| `npc_rui.png` | Rui, carpinteiro | `village carpenter, sturdy man, tool belt, rolled sleeves, wood shavings, brown and amber colors, standing idle, facing viewer` |
| `npc_nara.png` | Nara, viajante | `traveling wanderer, young woman, hooded travel cloak, backpack, dusty boots, purple and grey colors, standing idle, facing viewer` |
| `npc_bento.png` | Bento, o mais velho | `village elder, old man, walking cane, long white beard, simple tunic, standing idle, facing viewer` |
| `npc_maren.png` | Maren, vendedora de sementes | `seed merchant, kind woman, seed pouches on belt, wide-brim hat, earthy green and gold colors, standing idle, facing viewer` |

> **Maren não tem retrato** (é a única). Desenhe corpo e retrato juntos para ficarem coerentes.

---

## 4. Retrato da Maren + ícones de construção — **3 arquivos** 🟡

| Arquivo | Dimensões | Prompt |
|---|---|---|
| `portrait_maren.png` | **64×80** | `character portrait bust, head and shoulders, kind woman in her forties, warm smile, wide-brim straw hat, earthy green clothing, seed pouch strap visible, looking at viewer, cozy farming RPG portrait` |
| `building_barn.png` | **128×128** | `2D game building sprite, red wooden barn with white trim, hay loft door, sloped roof, farm building, front view, transparent background, pixel art` |
| `building_greenhouse.png` | **128×128** | `2D game building sprite, glass greenhouse with wooden frame, green plants visible inside, glass panel roof, front view, transparent background, pixel art` |

**Campos a preencher:** `icon` em `Assets/Resources/Buildings/Barn.asset` e `Greenhouse.asset`
(ambos `{fileID: 0}` hoje). `Silo` e `Workshop` já estão preenchidos — use-os como referência
de estilo. Os retratos existentes ficam em `Assets/Art/NPCs/Portraits/`.

---

## O que NÃO precisa gerar

Confirmado por auditoria de arquivo, 2026-08-02. Registrado aqui para ninguém regerar trabalho
que já existe — **o brief anterior errava em vários destes**:

- ✅ **Animações — 280 clips `.anim` e 39 controllers.** Só 3 dos 28 animais estão sem
  `animatorController` (`Rabbit`, `Sparrow`, `duck`). **Não existe lacuna geral de animação de
  combate**, ao contrário do que as notas antigas diziam
- ✅ **Sprites e ícones de animal — 28/28 preenchidos.** Zero lacunas
- ✅ **Inimigos — 33 dos 35 com sprite** (Cave/Forest/Meadow/Mountain/Volcano)
- ✅ **Itens, ferramentas e crops — todos os ícones preenchidos.** Zero lacunas
- ✅ **UI — 27 sprites** no projeto (painéis, botões, slots, barras, frames)
- ✅ **8 dos 9 retratos** de NPC (só falta Maren)
- ✅ **Silo e Workshop** já têm ícone
- ✅ **7 quests** existem
- ✅ **Música** — 3 faixas de OST

### Fora de escopo: IronGolem e ObsidianGolem

`Mountain/IronGolem` e `Volcano/ObsidianGolem` estão sem sprite. **Não gere arte para estes.**
É o resto conhecido da decisão do PR #33 (6 arquivos de arte por bioma contra 7 inimigos). Os
outros 12 foram resolvidos renomeando os dados para casar com a arte existente — o certo aqui é
fazer o mesmo ou cortar os dois, não gerar arte nova.

---

## Total a gerar

| Categoria | Qtd | Prioridade |
|---|---|---|
| Ícones de skills (12 ativas + 7 passivas) | 19 | 🔴 Alta |
| Efeitos sonoros (4 com chave + 10 de mundo) | 14 | 🔴 Alta |
| Sprites de corpo dos NPCs | 9 | 🟡 Média |
| Ícones de construção | 2 | 🟡 Média |
| Retrato da Maren | 1 | 🟡 Média |
| **TOTAL** | **45** | |

---

## Depois que você gerar

1. Joga tudo em `_Incoming/` com os nomes exatos da tabela
2. Me avisa
3. Eu importo, configuro os import settings certos (PPU, filtro Point, alpha, compressão) e ligo
   cada arquivo ao asset/ScriptableObject que o usa — inclusive os 19 campos `skillIcon`
4. Rodo os testes e um build pra confirmar que nada quebrou

Não precisa acertar import settings nem organizar em pastas — eu faço essa parte.
