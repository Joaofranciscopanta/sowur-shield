# Catálogo de Sprites Animáveis — Sowur Shield

> Varredura de todos os sprites importados em `Assets/Art` e `Assets/Sprites`.
> "Animável" = sprite sheet fatiado em múltiplos frames (Sprite Mode: Multiple) que forma uma ou mais animações.
> Gerado automaticamente a partir dos `.png` + `.meta` (contagem de frames e dimensões).

---

## 1. Personagem principal (o "Bunny")

Pasta: `Assets/Art/Characters/`

| Arquivo | Dim. | Frames | Grade | O que contém | Já tem `.anim`? |
|---|---|---|---|---|---|
| **Premium Charakter Spritesheet.png** | 384×1152 | 227 | 8×24 (48px) | **Folha completa da skin premium** — idle, caminhada e TODAS as ações nas 4 direções: machado/cortar, enxada/cavar, regar (regador), lançar (vara/pesca) | Parcial |
| **Basic Charakter Spritesheet.png** | 192×192 | 16 | 4×4 (48px) | Skin básica: **idle + caminhada** nas 4 direções. **É esta que o Bunny usa hoje na cena** (clipes de walk/idle vêm daqui) | Sim |
| **Basic Charakter Actions.png** | 96×576 | 33 | 2 col | Skin básica: **ações** — machado levantado, golpe de machado/enxada, regador, cavar, nas direções | Parcial |
| **water from wateringcan frames.png** | 432×144 | 9 | 9 col | **Efeito de água** saindo do regador (splash, 9 frames) | Não |
| **Tools.png** | 96×96 | 36 | 6×6 (16px) | Ícones de ferramentas (grade fatiada, mas são ícones estáticos, não animação) | — |

### Animações do personagem já cortadas em `.anim`
Pasta `Art/Characters/Walking Animation/` e `Action Animation/`:

- **Caminhada:** `player_Walk_Down/Up/Left/Right` (+ variantes `_2`) — 4 frames cada, 12 fps
- **Parado (idle):** `player_Idle_Down/Up/Left/Right` (+ variantes `_2`)
- **Ações:** `player_axe_down` (machado p/ baixo), `player_ digging_up`, `player_ digging_down` (cavar), `player_plowing_right` (arar/enxada p/ direita)

> **Observação:** a folha **Premium** tem MUITO mais animação do que foi cortada até agora (machado/regar/pescar nas 4 direções). Só uma parte virou clipe `.anim`. Boa parte do "andar, pular, usar machado" está ali esperando ser fatiada.

---

## 2. Animais (Sprout Lands — premium pack)

Pasta: `Assets/Art/ThirdParty/Sprout Lands - Sprites - premium pack/Animals/`

### Galinha (Chicken)
| Arquivo | Dim. | Frames | Contém |
|---|---|---|---|
| `chicken default/blue/brown/green/red.png` | 128×432 | 100 cada | **idle, andar (4 direções), bicar/comer, sentar, pulinho com poeira, corações (felicidade)** — 5 cores |
| `Chicken_Baby(.../_Blue/_Brown/_Green/_Red).png` | 128×304 | 105 cada | Pintinho: idle, andar, bicar, pulinho — 5 cores |
| `Chicken_Egg/Egg_Spritesheet(_Brown/_Green/_blue/_red).png` | 160×288 | 52 cada | **Ovo balançando + chocando/rachando** — 5 cores |
| `Chicken/Free Chicken Sprites.png` | 64×32 | 6 | Versão grátis simples da galinha |

### Vaca (Cow)
| Arquivo | Dim. | Frames | Contém |
|---|---|---|---|
| `Cow/Brown/Green/Light/Pink/Purple cow animations.png` | 256×256 | 47 cada | **idle, andar (direções), comer grama, corações (felicidade)** — 5 cores |
| `Cow_Baby/baby brown/green/light/pink/purple cow.png` | 256×288 | 43 cada | Bezerro: idle, andar, comer — 5 cores |
| `Cow/Free Cow Sprites.png` | 96×64 | 5 | Versão grátis simples da vaca |

---

## 3. Inimigos

Pasta: `Assets/Art/Enemies/` (Cave, Forest, Meadow, Mountain, Volcano)

> ⚠️ A maioria dos inimigos foi importada como **imagem única (1 frame)** — ou seja, ainda **não estão fatiados/animados**. Abaixo, os que **já têm múltiplos frames** (animáveis) vs. os estáticos.

### Com múltiplos frames (animáveis)
| Inimigo | Frames | Região |
|---|---|---|
| Mushroom Spore | 12 | Cave |
| Shadow Stalker | 2 | Cave |
| Vine Sprite | 3 | Forest |
| Enemy 19 — Snow Wolf | 3 | Mountain |
| Enemy 20 — Ice Elemental | 3 | Mountain |
| Enemy 24 — Mountain Titan (Mini-boss) | 2 | Mountain |
| Enemy 25 — Fire Slime | 4 | Volcano |
| Enemy 27 — Lava Knight | 7 | Volcano |
| Enemy 28 — Infernal Demon | 4 | Volcano |
| Enemy 29 — Flame Colossus | 3 | Volcano |
| Enemy 30 — Lord of Ashes (Final Boss) | 2 | Volcano |

### Estáticos (1 frame — não animados ainda)
Cave Bat, Cave Troll (Boss), Crystal Beetle, Dark Spider, Stone Golem, Ancient Wolf (Boss), Forest Boar, Great Owl, Howling Wolf, Poison Frog, Treant (Miniboss), Crow, Field Rat, Giant Slime, Honey Bee, Meadow Wolf, Slime, Enemy 21 — Mountain Bandit, Enemy 22 — Stone Yeti, Enemy 23 — Frost Golem, Enemy 26 — Magma Lizard.

---

## 4. Objetos e cenário animados

### Sprout Lands — `.../Objects/`
| Arquivo | Frames | Contém |
|---|---|---|
| `Tree animations/tree_sprites.png` | 23 | Árvore balançando |
| `.../tree_appel_sprites.png` | 46 | Macieira (balanço + queda de maçã) |
| `.../tree_orange/peach/pear_sprites.png` | 45 cada | Laranjeira / pessegueiro / pereira |
| `.../tree_fall_animation_sprite_sheet.png` | 34 | Árvore caindo (corte) |
| `.../Fruit animations without tree/no_tree_appel/orange/peach/pear.png` | 66–69 | Só a fruta caindo (sem a árvore) |
| `Water Objects.png` | 26 | Água/reflexos animados |
| `Boats.png` | 7 | Barcos |
| `Farming Plants.png` | 54 | Plantas de cultivo (estágios/balanço) |
| `Mushrooms, Flowers, Stones.png` | 43 | Cogumelos, flores, pedras |
| `Trees, stumps and bushes.png` | 31 | Árvores, tocos e arbustos |
| `signs.png` / `signs_sides.png` | 6 / 8 | Placas |

### Home pack — `.../main-characters-home-.../PNG/`
| Arquivo | Frames | Contém |
|---|---|---|
| `Trees_animation.png` | 119 | Árvores animadas (balanço) |
| `bird_fly_animation.png` | 48 | **Pássaro voando** |
| `bird_jump_animation.png` | 60 | **Pássaro pulando** |
| `cat_animation.png` | 45 | **Gato** (idle/andar) |
| `Smoke_animation.png` | 19 | Fumaça (chaminé) |
| `exterior.png` / `Interior.png` / `ground_grass_details.png` / `house_details.png` | 124 / 50 / 239 / 7 | Tilesets e detalhes (fatiados, mas em maioria peças de mapa, não animação) |

---

## 5. Plantações (crescimento) — `Art/ThirdParty/growing_plants/`

Estas **não são sprite sheets de animação**, e sim **sequências de crescimento** (um PNG por estágio), trocadas pelo `CropGrowthManager`. Tecnicamente "animáveis" como progressão de estágios:

beet (13), cabbage (20), carrot (16), corn (20), cucumber (20), eggplant (9), onion (6), peas (8), pepper (12), pumpkin (20), radish (8), salad (7), spinach (5), tomat/tomate (20), watermelon (19), wheat/trigo (7).
Também há tiras `*_sequence.png` (carrot, cabbage, tomat) com todos os estágios lado a lado.

---

## Resumo rápido

- **Personagem (Bunny):** folha **Premium** = pacote completo (andar, machado, enxada/cavar, regar, pescar em 4 direções). Folha **Basic** = só andar/idle (é a que roda hoje). Só uma fração virou clipe `.anim`.
- **Animais:** galinha, pintinho, ovo, vaca e bezerro — todos animados, em 5 cores cada, com idle/andar/comer/corações.
- **Inimigos:** 11 têm frames de animação; ~21 ainda são imagem única.
- **Cenário:** árvores, frutas caindo, água, pássaro (voar/pular), gato, fumaça, barcos.
- **Plantações:** sequências de crescimento por estágio (não frame-a-frame).
