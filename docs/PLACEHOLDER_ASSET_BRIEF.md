# Sowur Shield — Guia de Geração de Placeholders

Documento para gerar todos os assets que faltam. Cada seção traz **nome exato do
arquivo**, **dimensões**, e um **prompt pronto para colar** em IA de imagem.

Coloque tudo numa pasta só (ex.: `C:\Users\Lucas\Sowur Shield\_Incoming\`),
mantendo os nomes exatos. Depois eu importo, configuro os import settings e ligo
cada arquivo ao asset correspondente no Unity.

> **Os nomes dos arquivos não são sugestão.** Eu ligo os assets por nome; se o nome
> mudar, a ligação falha e o item fica invisível no jogo.

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

## 1. Sprites de corpo dos NPCs — **9 arquivos** 🔴 PRIORIDADE ALTA

**Por que é o mais importante:** hoje os 9 NPCs da vila usam **o mesmo** sprite
placeholder cinza. No jogo eles são visualmente indistinguíveis — o jogador não
consegue saber quem é quem sem chegar perto e abrir o diálogo.

- **Dimensões: 32×32 px** (igual ao `npc_villager_placeholder.png` atual)
- Pasta destino no jogo: `Assets/Art/NPCs/`
- Corpo inteiro, de frente, parado (idle). Só 1 frame por enquanto.

| Arquivo | Personagem | Prompt (acrescente ao prompt base) |
|---|---|---|
| `npc_tomas.png` | Tomás, ferreiro | `village blacksmith, burly man, leather apron, soot-stained arms, short dark beard, hammer at belt, standing idle, facing viewer` |
| `npc_isabela.png` | Isabela, padeira | `village baker, woman, flour-dusted apron, hair tied in a bun, warm cream and wheat colors, holding a small loaf, standing idle, facing viewer` |
| `npc_joana.png` | Joana, pescadora | `village fisherwoman, weathered woman, rolled-up trousers, straw hat, fishing rod over shoulder, blue and tan colors, standing idle, facing viewer` |
| `npc_elias.png` | Elias, pastor | `village shepherd, calm older man, wool cloak, shepherd crook, muted green and brown, standing idle, facing viewer` |
| `npc_clara.png` | Clara, herborista | `village herbalist, woman, green robe, satchel of herbs, small flowers in hair, standing idle, facing viewer` |
| `npc_rui.png` | Rui, carpinteiro | `village carpenter, sturdy man, tool belt, rolled sleeves, wood shavings, brown and amber colors, standing idle, facing viewer` |
| `npc_nara.png` | Nara, viajante | `traveling wanderer, young woman, hooded travel cloak, backpack, dusty boots, purple and grey colors, standing idle, facing viewer` |
| `npc_bento.png` | Bento, o mais velho | `village elder, old man, walking cane, long white beard, simple tunic, seated-ready posture, standing idle, facing viewer` |
| `npc_maren.png` | Maren, vendedora de sementes | `seed merchant, kind woman, seed pouches on belt, wide-brim hat, earthy green and gold colors, standing idle, facing viewer` |

---

## 2. Retrato da Maren — **1 arquivo** 🟡

Os outros 8 villagers já têm retrato; **só a Maren não tem**.

- **Dimensões: 64×80 px** (igual aos `portrait_*.png` existentes)
- Pasta destino: `Assets/Art/NPCs/Portraits/`
- Enquadramento: cabeça e ombros, olhando para o jogador.

| Arquivo | Prompt |
|---|---|
| `portrait_maren.png` | `character portrait bust, head and shoulders, kind woman in her forties, warm smile, wide-brim straw hat, earthy green clothing, seed pouch strap visible, looking at viewer, cozy farming RPG portrait` |

---

## 3. Ícones de Skills dos Animais — **12 arquivos** 🔴 PRIORIDADE ALTA

**Por que importa:** a pasta `Assets/Resources/AnimalSkills/` está **vazia**. Os 28
animais do jogo entram em combate sem nenhuma habilidade — o sistema existe em
código mas não tem conteúdo nenhum.

- **Dimensões: 64×64 px**
- Pasta destino: `Assets/Art/UI/Skills/`
- Ícone simples, símbolo centralizado, legível em tamanho pequeno.

| Arquivo | Skill | Prompt |
|---|---|---|
| `skill_peck.png` | Bicada | `game skill icon, sharp beak strike, yellow and white, simple bold symbol on transparent background` |
| `skill_charge.png` | Investida | `game skill icon, charging horns with motion lines, brown and orange, simple bold symbol` |
| `skill_kick.png` | Coice | `game skill icon, hoof kick with impact burst, tan and grey, simple bold symbol` |
| `skill_bite.png` | Mordida | `game skill icon, fanged bite marks, red and white, simple bold symbol` |
| `skill_wool_guard.png` | Defesa de Lã | `game skill icon, fluffy wool shield, cream and soft blue, simple bold symbol` |
| `skill_milk_heal.png` | Cura do Leite | `game skill icon, milk droplet with green cross, white and green, simple bold symbol` |
| `skill_egg_toss.png` | Ovo Arremessado | `game skill icon, thrown egg with crack, white and yellow, simple bold symbol` |
| `skill_feather_flurry.png` | Rajada de Penas | `game skill icon, swirling feathers, light blue and white, simple bold symbol` |
| `skill_stampede.png` | Debandada | `game skill icon, dust cloud with many hoofprints, brown and beige, simple bold symbol` |
| `skill_rally.png` | Grito de Guerra | `game skill icon, upward arrow with sparkle, gold and yellow, simple bold symbol` |
| `skill_burrow.png` | Escavar | `game skill icon, dirt mound with tunnel hole, brown and dark brown, simple bold symbol` |
| `skill_lullaby.png` | Canção de Ninar | `game skill icon, musical notes with sleep z, soft purple and blue, simple bold symbol` |

---

## 4. Sprites de Construções — **2 arquivos** 🟡

`Barn.asset` e `Greenhouse.asset` existem mas estão **sem sprite**. `Silo` e
`Workshop` já têm.

- **Dimensões: 128×128 px**
- Pasta destino: `Assets/Art/Environment/Houses/`

| Arquivo | Prompt |
|---|---|
| `building_barn.png` | `2D game building sprite, red wooden barn with white trim, hay loft door, sloped roof, farm building, front view, transparent background, pixel art` |
| `building_greenhouse.png` | `2D game building sprite, glass greenhouse with wooden frame, green plants visible inside, glass panel roof, front view, transparent background, pixel art` |

---

## 5. Efeitos Sonoros — **14 arquivos** 🟡

O projeto tem só **7 arquivos de áudio no total**. Estes são os SFX que o código
já procura mas não encontra.

- **Formato: WAV**, 44.1 kHz, mono
- **Duração: 0.1s a 0.5s** (curtos — tocam a cada ação)
- Pasta destino: `Assets/Audio/SFX/`

Para gerar em IA de áudio (ElevenLabs SFX, Optimizer, etc.):

| Arquivo | Prompt |
|---|---|
| `sfx_pickup.wav` | `short bright pickup chime, item collected, cheerful, 0.2 seconds` |
| `sfx_harvest.wav` | `crisp plant pluck, leaves rustle, harvest crop, 0.3 seconds` |
| `sfx_plant.wav` | `soft soil pat, seed planted in dirt, 0.3 seconds` |
| `sfx_hoe.wav` | `dirt being tilled, soil scrape and crumble, 0.4 seconds` |
| `sfx_water.wav` | `watering can pour, gentle water splash on soil, 0.5 seconds` |
| `sfx_shovel.wav` | `shovel digging into earth, single scoop, 0.4 seconds` |
| `sfx_sell.wav` | `coins clinking, money received, cash register cheerful, 0.4 seconds` |
| `sfx_menu_open.wav` | `soft UI panel whoosh open, wooden drawer, 0.2 seconds` |
| `sfx_menu_close.wav` | `soft UI panel whoosh close, wooden drawer shutting, 0.2 seconds` |
| `sfx_button_click.wav` | `subtle UI button click, soft wooden tap, 0.1 seconds` |
| `sfx_confirm.wav` | `positive confirmation chime, two rising notes, 0.3 seconds` |
| `sfx_cancel.wav` | `soft negative blip, single low note, 0.2 seconds` |
| `sfx_sleep.wav` | `gentle sleep transition, soft descending chime, night, 0.8 seconds` |
| `sfx_step.wav` | `single soft footstep on grass, muted, 0.15 seconds` |

---

## O que NÃO precisa gerar

Confirmado por auditoria — já existe e está ligado:

- ✅ **34 inimigos** — todos os sprites existem (Cave/Forest/Meadow/Mountain/Volcano)
- ✅ **Todos os itens** têm ícone
- ✅ **8 dos 9 retratos** de NPC (só falta Maren)
- ✅ **Silo e Workshop** têm sprite
- ✅ **7 quests** existem
- ✅ **Música** — 3 faixas de OST no projeto

---

## ⚠️ Achado importante: Mountain e Volcano

Os 14 inimigos de Mountain e Volcano aparecem como "sem sprite" nos dados, **mas a
arte existe** — 12 PNGs em `Assets/Art/Enemies/Mountain/` e `/Volcano/`.

O problema é que os nomes não batem:

| Arte que existe | EnemyData que espera |
|---|---|
| `Enemy 19 — Snow Wolf.png` | `ArmoredBear.asset` |
| `Enemy 20 — Ice Elemental.png` | `FrostDrake.asset` |
| `Enemy 25 — Fire Slime.png` | `MagmaSlime.asset` |
| ... | ... |

São 12 arquivos de arte para 14 inimigos — e os nomes descrevem criaturas
diferentes das que os dados definem.

**Isto é decisão sua, não trabalho de gerar arte.** Duas saídas:

1. **Renomear os inimigos nos dados** para casar com a arte que existe (Snow Wolf,
   Ice Elemental, Fire Slime...). Zero arte nova; sobram 2 inimigos sem sprite.
2. **Gerar 14 sprites novos** com os nomes que os dados já usam (ArmoredBear,
   FrostDrake, MagmaSlime...). Aproveita as stats e o balanceamento que já existem.

Me diga qual prefere que eu preparo os prompts ou faço o religamento.

---

## Total a gerar

| Categoria | Qtd | Prioridade |
|---|---|---|
| Sprites de corpo dos NPCs | 9 | 🔴 Alta |
| Ícones de skills de animais | 12 | 🔴 Alta |
| Efeitos sonoros | 14 | 🟡 Média |
| Sprites de construção | 2 | 🟡 Média |
| Retrato da Maren | 1 | 🟡 Média |
| **TOTAL** | **38** | |

Mais a decisão sobre Mountain/Volcano acima.

---

## Depois que você gerar

1. Joga tudo numa pasta (ex.: `_Incoming/`) com os nomes exatos da tabela
2. Me avisa
3. Eu importo, configuro os import settings certos (PPU, filtro Point, alpha,
   compressão) e ligo cada arquivo ao asset/ScriptableObject que o usa
4. Rodo os testes e um build pra confirmar que nada quebrou

Não precisa acertar import settings nem organizar em pastas — eu faço essa parte.

---

## Nota sobre as skills (seção 3)

Os 12 nomes de skill acima são **proposta minha**, não vêm dos dados — a pasta está
vazia, então não há nada definido ainda. Os ícones servem para qualquer conjunto,
mas se você quiser outros nomes/efeitos, me fala antes que eu ajusto a lista.
Eu crio os ScriptableObjects de skill (dano, custo, alvo) junto com a importação.
