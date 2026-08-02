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

## 3. Ícones de Skills — **19 arquivos** 🔴 PRIORIDADE ALTA

**Correção:** numa versão anterior deste documento eu disse que `AnimalSkills/` estava
vazia. **Estava errado** — existem 7 skills funcionando, e 23 dos 28 animais já têm
skill ativa atribuída. Meu comando de busca filtrava por nome de arquivo contendo
"skill", e os assets não seguem esse padrão (`VenomousBite.asset`, etc.).

O que realmente falta:

- **As 7 skills existentes não têm ícone** (`skillIcon: {fileID: 0}` em todas)
- **`availablePassiveSkills` está vazia em 26 dos 28 animais** — as 2 passivas que
  existem estão ligadas só ao Sparrow e à chicken
- Nenhuma skill usa os ganchos de **felicidade** ou **estação**, que o código suporta

Especificação dos ícones:

- **Dimensões: 64×64 px**
- Pasta destino: `Assets/Art/UI/Skills/`
- Símbolo único centralizado, legível a 32px, sem texto.

### 3a. Ícones para as 7 skills que JÁ existem (obrigatório)

| Arquivo | Skill no jogo | Prompt |
|---|---|---|
| `skill_venomous_bite.png` | Venomous Bite | `game skill icon, fanged bite with green venom drip, purple and toxic green, simple bold symbol` |
| `skill_toxic_quack.png` | Toxic Quack | `game skill icon, sound wave rings with green fumes, sickly green and yellow, simple bold symbol` |
| `skill_peck_weakening.png` | Peck of Weakening | `game skill icon, sharp beak strike with downward arrow, grey and pale yellow, simple bold symbol` |
| `skill_draining_howl.png` | Draining Howl | `game skill icon, howling sound waves draining energy, dark blue and violet, simple bold symbol` |
| `skill_feather_shield.png` | Feather Shield | `game skill icon, shield made of layered feathers, cream and sky blue, simple bold symbol` |
| `skill_flock_instinct.png` | Flock Instinct (passiva) | `game skill icon, three small birds flying in formation, warm orange and brown, simple bold symbol` |
| `skill_supporters_blessing.png` | Supporter's Blessing (passiva) | `game skill icon, radiant blessing sparkle over a small heart, gold and soft white, simple bold symbol` |

### 3b. Ícones para as 12 skills novas

✅ **As 12 skills já foram criadas e ligadas aos animais** (`Assets/Resources/AnimalSkills/`).
Estão funcionando no jogo — só falta o ícone de cada uma. Ver a tabela de design abaixo
para o que cada uma faz.

| Arquivo | Skill | Prompt |
|---|---|---|
| `skill_hoof_kick.png` | Coice | `game skill icon, powerful hoof kick with impact starburst, tan and grey, simple bold symbol` |
| `skill_hide_wall.png` | Muralha de Couro | `game skill icon, thick leather barrier shield, deep brown and bronze, simple bold symbol` |
| `skill_herd_bond.png` | Vínculo do Rebanho | `game skill icon, two animal silhouettes joined by a heart, warm pink and cream, simple bold symbol` |
| `skill_loyal_companion.png` | Companheiro Leal | `game skill icon, glowing heart with a paw print inside, rose and gold, simple bold symbol` |
| `skill_rooster_fury.png` | Fúria do Galo | `game skill icon, angry rooster comb with fire streaks, red and orange, simple bold symbol` |
| `skill_large_brood.png` | Ninhada Numerosa | `game skill icon, cluster of small chicks with speed lines, yellow and warm brown, simple bold symbol` |
| `skill_winter_coat.png` | Calor de Inverno | `game skill icon, snowflake over a thick wool coat, icy blue and white, simple bold symbol` |
| `skill_spring_vigor.png` | Vigor da Primavera | `game skill icon, sprouting green shoot with speed swirls, fresh green and pink blossom, simple bold symbol` |
| `skill_precise_peck.png` | Bicada Certeira | `game skill icon, sharp beak hitting a bullseye target, yellow and red, simple bold symbol` |
| `skill_restoring_song.png` | Canto Restaurador | `game skill icon, musical note with a green healing cross, soft green and white, simple bold symbol` |
| `skill_flock_call.png` | Chamado do Bando | `game skill icon, open beak with rising sound waves and up arrows, sky blue and gold, simple bold symbol` |
| `skill_burrow.png` | Escavar | `game skill icon, dirt mound with tunnel entrance and dust, brown and dark earth, simple bold symbol` |

### Design das 12 skills novas — JÁ IMPLEMENTADAS

Tudo abaixo usa só o que o sistema já suporta (Stun, Shield, Burn, Poison, Weakness;
condições por classe, família, contagem de família, felicidade e estação).

**Estado atual no jogo:** 19 skills no total (7 antigas + 12 novas). 23 animais têm
skill ativa e passivas ligadas. Os 5 `egg_*` ficam de fora de propósito — são ovos.
Reatribuí também as ativas por espécie: vacas usam Coice, galinhas Bicada Certeira,
coelho Escavar, Sparrow Canto Restaurador. Antes disso o coelho usava "Feather Shield"
e toda vaca usava "Draining Howl".

| Skill | Tipo | Efeito | Desbloqueio | Razão |
|---|---|---|---|---|
| Coice | Ativa | 1.4× dano + **Stun** 1 turno | Sempre | Stun é o efeito mais forte e **nenhuma skill usa** hoje |
| Muralha de Couro | Ativa | **Shield** 40%, 3 turnos | Classe Tank | São 10 Tanks sem nenhuma skill de tanque |
| Vínculo do Rebanho | Passiva | +15% ATK e DEF | **Felicidade ≥ 75** | Recompensa direta por cuidar bem — gancho nunca usado |
| Companheiro Leal | Passiva | +20% HP | **Felicidade ≥ 90** | Segundo tier do mesmo gancho |
| Fúria do Galo | Ativa | 1.6× dano, CD 3 | 3+ Galliformes | Premia time temático (há 15 Galliformes) |
| Ninhada Numerosa | Passiva | +10% velocidade | 5+ Galliformes | Escala com o roster grande de galinhas |
| Calor de Inverno | Passiva | +25% DEF | **Estação: Winter** | Estreia o gancho sazonal, que existe e está parado |
| Vigor da Primavera | Passiva | +20% velocidade | **Estação: Spring** | Idem, e casa com o tema de fazenda |
| Bicada Certeira | Ativa | 1.3× dano, CD 1 | Sempre | Ataque barato; hoje todos custam CD 2–3 |
| Canto Restaurador | Ativa | Cura 25, aliado | Classe Support | **Não existe cura no jogo inteiro** |
| Chamado do Bando | Ativa | Aliados +20% ATK, 2 turnos | Família Passeridae | Buff de time, categoria inexistente |
| Escavar | Ativa | **Shield** 50%, 1 turno | Família Leporidae | Dá identidade ao coelho, hoje um DPS genérico |

> ✅ **Verificado no código:** cura e buff em aliado **funcionam** de verdade —
> `TurnManager.cs:467` chama `ally.Heal(...)` e `:517` aplica `ApplyStatBuff` em todos
> os aliados vivos. Então *Canto Restaurador* e *Chamado do Bando* são só asset,
> nenhuma das 12 skills precisa de código novo.

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
- ✅ **7 skills de animal** já existem e funcionam (só falta ícone) — 23 dos 28
  animais têm skill ativa atribuída; os 5 sem são os `egg_*`, que são ovos

---

## ✅ RESOLVIDO: Mountain e Volcano

Você escolheu renomear os dados para bater com a arte. **Feito** — os 12 inimigos foram
renomeados e cada um recebeu seu sprite. Nada de arte nova foi necessário.

| Asset (arquivo mantido) | Novo nome | Arte |
|---|---|---|
| `Harpy` | Snow Wolf / Lobo da Neve | Enemy 19 |
| `ThunderEagle` | Ice Elemental / Elemental de Gelo | Enemy 20 |
| `RockHound` | Mountain Bandit / Bandido da Montanha | Enemy 21 |
| `ArmoredBear` | Stone Yeti / Yeti de Pedra | Enemy 22 |
| `FrostDrake` | Frost Golem / Golem de Gelo | Enemy 23 |
| `MountainKing` | Mountain Titan / Titã da Montanha | Enemy 24 |
| `MagmaSlime` | Fire Slime / Gosma de Fogo | Enemy 25 |
| `LavaSalamander` | Magma Lizard / Lagarto de Magma | Enemy 26 |
| `Hellhound` | Lava Knight / Cavaleiro de Lava | Enemy 27 |
| `FireImp` | Infernal Demon / Demônio Infernal | Enemy 28 |
| `VolcanicDrake` | Flame Colossus / Colosso de Chamas | Enemy 29 |
| `InfernoDragon` | Lord of Ashes / Senhor das Cinzas | Enemy 30 |

Casei arte com stats, não por ordem alfabética: o mais rápido e frágil virou o lobo, o
bruto lento virou o yeti, e o inimigo de maior HP de cada bioma ficou com a arte de boss.

Os nomes dos **arquivos** `.asset` não mudaram de propósito — cada um é referenciado por
uma StageData, e o nome do arquivo não é visto pelo jogador.

**Sobram 2 inimigos sem arte:** `IronGolem` e `ObsidianGolem`. São 6 artes por bioma para
7 inimigos. Se quiser cobrir esses dois, precisaria de 2 sprites novos (1024×1024, mesmo
estilo dos outros) — ou dá para deletar os dois assets, já que cada bioma continua com 6
inimigos.

<details>
<summary>Contexto original do problema (resolvido)</summary>

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

**Escolhida: opção 1.**

</details>

---

## Total a gerar

| Categoria | Qtd | Prioridade |
|---|---|---|
| Sprites de corpo dos NPCs | 9 | 🔴 Alta |
| Ícones das 7 skills existentes | 7 | 🔴 Alta |
| Ícones das 12 skills novas | 12 | 🟡 Média |
| Efeitos sonoros | 14 | 🟡 Média |
| Sprites de construção | 2 | 🟡 Média |
| Retrato da Maren | 1 | 🟡 Média |
| **TOTAL** | **45** | |

Mais a decisão sobre Mountain/Volcano acima.

**Se quiser cortar escopo:** os 9 NPCs + os 7 ícones de skill existente (**16 arquivos**)
resolvem as duas lacunas mais visíveis — NPCs indistinguíveis e skills sem ícone na UI.
O resto é melhoria.

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

Os 7 nomes da seção 3a **vêm dos assets reais** do projeto — esses ícones são
necessários de qualquer forma, independente de qualquer decisão de design.

Os 12 da seção 3b são **proposta minha**. Todos usam apenas efeitos e condições que o
código já implementa, mas os nomes, números e desbloqueios são discutíveis. Se quiser
outro conjunto, me fala antes de gerar — os ícones são a parte cara.

Duas ideias por trás da proposta, caso queira ajustar o rumo:

1. **Ligar a fazenda ao combate.** O código suporta desbloqueio por felicidade, e nada
   usa isso. *Vínculo do Rebanho* e *Companheiro Leal* fazem cuidar dos animais valer
   em batalha — que é o que amarra os dois lados do jogo.
2. **Preencher buracos de papel.** Hoje não existe cura, nem buff de aliado, nem Stun.
   São 10 Tanks sem skill de tanque e 1 único Support. As skills novas cobrem isso.
