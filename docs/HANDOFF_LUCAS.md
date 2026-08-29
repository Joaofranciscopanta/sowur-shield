# O que precisa de você — Sowur Shield

> Lista viva do que **eu não posso resolver sozinho** e por quê. Atualizada em 2026-08-29.
> Companion da [auditoria visual](VISUAL_AUDIT_2026_08_29.md).
> As correções estão em `main` (PR #40 mergeado).

Ordenado por impacto. Cada item diz **por que parei** e **o que exatamente decidir/fazer**.

---

## 🟡 1. Tileset do terreno — ✅ roxo morto, retiling real ainda é de artista

**Feito (`0ce489c`)**: `TilesDemo.png` foi **recolorido** com a paleta do próprio jogo. O
verde-limão contra roxo-vinho da arte de demo do plugin acabou — agora é grama contra terra,
e o chão parou de brigar com os sprites da Sprout Lands que ficam em cima dele.

Era um swap de 4 cores. As substituições foram **amostradas dos tilesets que o jogo já tem**,
não inventadas: a grama pegou os dois tons dominantes de `Grass_tiles_v2.png`, a terra os de
`Darker_Soil_Ground_Tiles.png`. Descoberta útil: a grama da demo era `#C0DB67` contra a real
`#C0D470` — o autor do plugin amostrou o mesmo pack. **O roxo era o problema inteiro.**

Um julgamento que precisei fazer: o pixel de borda foi para o tom médio-escuro da terra
(`#9D866F`), não para a segunda cor dela. Mapeando ao pé da letra, grama e terra ficavam
quase do mesmo valor e o contorno que separa as duas sumia.

Estrutura intacta (64x64, 4 cores, mesma contagem de pixels), então os 16 recortes e Tiles
continuam válidos. Verificado em Play Mode: 14 tiles distintos em 10.201 células.
Antes/depois em `Assets/Screenshots/terrain_recolor_clean.png`.

**O que ainda é trabalho de artista** (opcional, não bloqueia nada): a *estrutura* dos tiles
continua sendo a da demo — formas orgânicas arredondadas, sem textura, sem variação. Um
tileset de verdade teria pedrinhas, tufos e transições próprias. Se quiser isso:

1. Abrir `Grass_tiles_v2.png` e `Darker_Soil_Ground_Tiles.png` no Sprite Editor
2. Montar uma textura 64x64 no layout de `TilesDemo.png` (4x4, células de 16px)
3. A ordem das 16 regras está em `Assets/Scripts/DualGridTilemap/DualGridTilemap.cs:35-52`
   — `tiles[0]` a `tiles[15]` mapeiam cada combinação de 4 vizinhos (`tiles[6]` = tudo grama,
   `tiles[12]` = tudo terra)
4. Os 16 assets `Tile` já existem e estão ligados na cena — **basta substituir a textura**,
   não precisa recriar nada

O backup da arte original está fora do repo, mas `git show 0ce489c^:Assets/DualGridSystem/Tiles/Grass/TilesDemo.png`
recupera se precisar comparar.

---

## 🟠 2. Combate — ~~bloqueado por arte~~ **ERRO MEU, está muito melhor**

**Correção importante**: a auditoria dizia que os inimigos não tinham sprite e que o
combate era "protótipo de debug". **Isso estava errado.** Eu abri a `CombatScene`
diretamente pelo MCP, sem passar pelo WorldMap, e auditei o **modo de fallback** achando
que era o jogo.

Rodando pelo fluxo real (time montado + stage selecionado), o combate tem cenário pintado
completo, inimigos com sprite (`Slime`), o time com arte real, e o painel de ação em
português. **Zero esferas placeholder.** Ver `Assets/Screenshots/combat_real_flow.png`.

Os logs diziam isso o tempo todo e eu não li:
```
[EnemySpawner] No stage selected — using fallback test enemies.
[CombatUnit] 'Enemy_1' has no sprite; falling back to placeholder sphere.
```

**Números reais**: 32 PNGs de inimigos em `Assets/Art/Enemies/`, 32 dos 34 `EnemyData` com
sprite atribuído (só `IronGolem` e `ObsidianGolem` faltam), 25 de 25 stages com inimigos
configurados.

**O que sobra de verdade**:
- ~~20 dos 25 stages sem fundo~~ → **RESOLVIDO** (`329394c`). Não precisava de artista: os
  26 fundos já estavam desenhados em `Assets/Art/Maps`, só nunca foram atribuídos. Agora
  são **25 de 25**. Uma conferida por olho: `Stage_013_EchoChamber` pegou "Mine Tunnels 2"
  porque dois arquivos compartilham o número — troque por "Mine Tunnels" se preferir.
- **`"Turno: 2/500"`** — *decisão sua, não mexi*. O `maxActions = 500` em `TurnManager.cs:32`
  não é debug: é o limite anti-loop que encerra a batalha em empate. O incômodo é **exibir**
  isso. A string é `combat.status.turn_counter` = `"Turno: {0}/{1}"` em
  `Assets/Localization/translations.csv:150`, nos 3 idiomas. Opções:
  **(a)** `"Turno: {0}"` — some com o `/500`, mas o jogador perde a noção de que há limite;
  **(b)** manter, mas baixar `maxActions` para algo plausível como 50, e aí `2/50` lê como
  informação de ritmo em vez de número mágico;
  **(c)** deixar como está.
  Eu não escolho por você porque muda a UI nos 3 idiomas e afeta o balanceamento.

**Lição de método** (vale para você também): se abrir uma cena direto no Editor e algo
parecer vazio, **olhe o Console antes de concluir**. Este projeto loga o fallback toda vez.
É a mesma razão pela qual "às vezes os assets não carregam no Play Mode mas aparecem na
build" — entrar direto numa cena pula a inicialização que o fluxo do jogo faz.

---

## 🟠 2b. ⚠️ O build está perto do teto de 100 MB do GitHub

**Aconteceu em 2026-08-29**: o deploy do build #33 foi **rejeitado**:

```
File docs/Build/Default WebGL.data is 144.65 MB; this exceeds GitHub's file size
limit of 100.00 MB
! [remote rejected] main -> main (pre-receive hook declined)
```

Causa: eu havia setado `textureCompression = Uncompressed` em 530 texturas no passe de
pixel art. Já revertido (`86bdbf1`) — o Point filter, que é o que preserva a arte, foi
mantido.

**O risco que permanece**: mesmo saudável, o build está apertado. Medido em 2026-08-29
(sessão 2) direto nos arquivos de `docs/Build/`:

| Arquivo | Tamanho | Limite |
|---|---|---|
| `Default WebGL.data` | **79,07 MB** | 100 MB (hard) — **79% do teto** |
| `Default WebGL.wasm` | 54,53 MB | 50 MB (warning) — **já passa do aviso** |

⚠️ **A nota anterior dizia ~71 MB para o `.data`. Estava desatualizada** — são 79,07 MB. A
margem real é de ~21 MB, não ~29 MB.

### Onde estão os bytes (medido, não estimado)

O build puxa **746 assets, 62,7 MB de fonte** — resolvido por `GetDependencies` das 3 cenas
do Build Settings mais tudo em `Resources/`. Em memória de runtime as texturas somam
**76,2 MB**.

**Falso alarme que investiguei e descartei**: `Assets/Screenshots/` tem 66 MB dos meus
screenshots de debug. Como está dentro de `Assets/`, parecia estar indo para o build (o
`.gitignore` não exclui nada de build). **Não vai**: verifiquei os 87 arquivos e **zero** são
alcançáveis pelas cenas do build, e nenhum está sob `Resources/`. Unity só empacota o que é
referenciado. Custam disco e tempo de upload para o Cloud Build, não bytes do `.data`.

### O desperdício real: 8 texturas acima de 1024px

Todas ainda no default `maxTextureSize = 2048` do Unity, sem override de WebGL:

| Textura | Runtime | Importada | Realmente desenhada como |
|---|---|---|---|
| `Art/ThirdParty/heart_particle.png` | 3,00 MB | 1536x1024 | sprite de **190x190** (1,9 tiles) |
| `Art/Portraits/Wolf.png` | 3,00 MB | 1024x1536 | retrato, exibido grande |
| `Art/Portraits/Brandi.png` | 3,00 MB | 1024x1536 | retrato, exibido grande |
| `Art/ThirdParty/feeding-trough-for-farm-2.png` | 2,86 MB | 2048x696 | **fonte é 5120x1740** |
| `Texture/Dialogue Box 2.png` | 2,65 MB | 1500x800 | moldura de UI |
| `.../Trees_animation.png` | 1,32 MB | 576x1040 | — |
| `Art/Characters/Premium Charakter Spritesheet.png` | 0,99 MB | 384x1152 | — |

O caso mais claro é o `heart_particle`: paga 3 MB para desenhar um coração fatiado em
190x190. O comedouro tem arte-fonte de **5120x1740** num jogo de pixel art a 16 PPU.

**Economia estimada**: capar os não-retratos em 1024 tira **~13 MB** de memória de textura.
É só `maxTextureSize` no importer — **nenhuma arte é alterada e é reversível**.

### 🚫 Decisão do Lucas (2026-08-29): NÃO fazer agora

Levantei isso e ele decidiu **documentar e não mexer**. Não é bloqueio: o build passa hoje.
**Não executar sem ele pedir.**

Os retratos eu deixaria em 2048 de qualquer forma — são exibidos grandes no diálogo, e 8 dos
9 vão ser substituídos por arte real (item 3).

**O que continua valendo**: **qualquer adição grande de arte ou áudio pode estourar o teto** —
e o build pago roda **antes** do push ser recusado, então o dinheiro é gasto à toa. Quando for
adicionar muitos assets, me peça para medir antes. Saídas definitivas, se um dia estourar:
cortar as texturas acima, compressão de áudio, Addressables, ou hospedar fora do GitHub Pages.

---

## 🔴 3. Os retratos dos NPCs são placeholders — PRECISA DE ARTISTA

**Estado**: eu **liguei** os retratos ao diálogo (antes não apareciam nunca — 88% dos nós
não tinham retrato e a arte ficava carregada e sem uso). Agora aparecem. O problema é que
8 dos 9 **são placeholders genéricos**.

**O que existe** em `Assets/Resources/Portraits/`:

| Retrato | Estado |
|---|---|
| `portrait_maren.png` | ✅ **arte real** (10.819 bytes) — bonita, personagem com cara |
| `portrait_joana.png` | ⬜ placeholder genérico (~836 bytes) |
| `portrait_clara.png` | ⬜ placeholder genérico |
| `portrait_rui.png` | ⬜ placeholder genérico |
| `portrait_bento.png` | ⬜ placeholder genérico |
| `portrait_elias.png` | ⬜ placeholder genérico |
| `portrait_isabela.png` | ⬜ placeholder genérico |
| `portrait_nara.png` | ⬜ placeholder genérico |
| `portrait_tomas.png` | ⬜ placeholder genérico |

Os 8 placeholders são o mesmo bonequinho sem rosto, com variações mínimas de cor.

**O que fazer**: 8 retratos no estilo do `portrait_maren.png`, mesmas dimensões. A ligação
já está pronta — é só substituir os arquivos e eles aparecem automaticamente.

**Ganho**: agora que a ligação funciona, cada retrato novo aparece imediatamente em todas
as conversas daquele NPC. É provavelmente o melhor retorno visual por hora de arte no
projeto inteiro.

---

## 🟠 4. Confirmar o layout do terreno que pintei

**Estado**: pintei baseado nas posições reais dos objetos da cena, mas **é um chute de
design meu**, não uma decisão sua.

**O que pintei** (`Assets/Scripts/Editor/PaintFarmTerrain.cs`):
- Pátio da casa: x -5..2, y 0..7 (em volta da cama e da caixa de venda)
- Curral: x 5..11, y -7..5 (em volta do cocho, onde os animais já estão)
- Caminhos ligando os dois + ramais para os NPCs do sul e do oeste
- 3 clareiras para quebrar a monotonia

**O que fazer**: rodar `Sowur Shield → Terrain → Paint Farm Terrain`, olhar, e me dizer o
que mudar — ou editar à mão pelo Tile Palette, que é o fluxo normal. Re-rodar a ferramenta
repinta do zero, então **edições manuais se perdem se você rodar de novo**.

---

## 🟠 5. `Nunito SDF.asset` regenera sozinho — CAUSA NÃO INVESTIGADA

**Estado**: aconteceu **de novo** nesta sessão. Descartei 2 vezes.

**O problema**: o Editor regenera o atlas da fonte com menos glifos, às vezes **sem os
acentos** (á é í ó ç ñ). Se isso subir, **quebra PT e ES**.

**Por que não resolvi**: a causa raiz não foi investigada — pode ser configuração de
atlas dinâmico, pode ser o TMP Settings. Investigar isso é uma sessão dedicada.

**O que fazer agora**: **sempre conferir `git status` antes de commitar**. Se
`Assets/Fonts/Nunito SDF.asset` aparecer modificado e você não mexeu na fonte de
propósito, descarte:
```
git checkout -- "Assets/Fonts/Nunito SDF.asset"
```

---

## 🟡 6. `Assets/_Recovery/` — 3 cenas de crash não rastreadas

**Estado**: pendente desde antes desta sessão.

`Assets/_Recovery/0 (2).unity`, `0 (3).unity`, `0 (4).unity` são cenas de recuperação de
crash do Unity. **Não sei se contêm trabalho perdido** — só você sabe.

**O que fazer**: abrir cada uma, ver se tem algo que valha resgatar, e apagar a pasta.

---

## 🟡 7. Decisões de design que evitei tomar por você

Coisas que a auditoria apontou mas que são **escolha sua**, não defeito objetivo:

- **Caixa de diálogo**: hoje é 1872px de largura com fonte 18 — linha de ~180 caracteres.
  Stardew usa caixa estreita com retrato à esquerda. Reduzir para ~900px é uma mudança de
  identidade visual, não um bug.
- **HUD espalhada em 4 cantos**: consolidar em 2 grupos melhoraria a leitura, mas muda o
  layout que você já conhece.
- **Minimapa "fullscreen"** ocupa 40% da tela. Fazer virar tela cheia de verdade é
  simples, mas talvez você goste assim.
- **Stamina sem número**: barra de 60px sem valor numérico e sem cor de estado crítico.

Me diga quais desses você quer e eu faço.

---

## 🔴 8. O tutorial existe, está traduzido, e NUNCA rodou

**Estado**: `TutorialManager.cs` (288 linhas) implementa um tutorial de **6 passos** — arar,
plantar, regar, acariciar animal, dormir, colher — com `LocalizedString` para cada título e
descrição. As **13 entradas estão traduzidas em EN/PT/ES** em `translations.csv`, com
formatação rica (negrito nos nomes das ferramentas).

**O problema**: `TutorialManager.Instance` é **NULL**. Não existe nenhum GameObject nem
prefab com esse componente — só o script. O `SaveManager` chama
`TutorialManager.Instance?.StartTutorial()` num jogo novo, e o `?.` engole a chamada em
silêncio.

**Por que não liguei**: instanciar exige criar o painel de UI (`tutorialPanel`, `stepText`,
`stepCountText`, `skipButton`, `nextButton` são todos `[SerializeField]` sem referência).
Montar isso é decisão de design — onde o painel aparece, que tamanho tem, se bloqueia o
jogo. Fazer às cegas provavelmente ficaria pior que não ter.

**Vale muito a pena**: o primeiro passo já diz *"Equipe a **Enxada** da sua barra de itens"*
— e a enxada agora está lá desde o início (corrigido nesta sessão). O tutorial e o kit
inicial foram feitos para funcionar juntos.

**Decisão sua**: quer que eu monte o painel seguindo o estilo do menu de pausa (a peça de UI
mais bem resolvida do jogo)?

---

## ✅ 9. WorldMap — RESOLVIDO (`9bacd34`)

Os 25 botões usavam `UISprite`, o retângulo cinza padrão do Unity, cobrindo **40% da tela**
sobre o mapa pintado. Agora usam a placa de madeira do jogo: **dourada** para stage
desbloqueado, **creme** para bloqueado.

Números medidos, em duas passadas:
- **350x70** mantém a proporção 5:1 da arte (o kit é 600x120). O antigo 300x110 era 2.7:1 e
  esticava os cantos do 9-slice.
- **Margem de 45px** por lado, amostrada do sprite: o centro creme começa a 88px de 600px,
  ou seja a placa é **71% do rect**, não os ~86% que parece. Na primeira tentativa usei 26px
  e três nomes longos ainda vazavam para a madeira.
- **Origem horizontal centralizada** — a vertical já era. Com botões mais largos, o início
  fixo em 70px jogava a quinta coluna 34px para fora da tela.

Resultado: 0 de 25 labels vazando, 0 botões fora da tela, área caiu de 40% para 30%.

**Título e botão de voltar: ✅ aplicados na cena** (`5ae149d`). O título "Mapa-Múndi" e a
placa "Fechar" ligada ao `CloseMap` estão salvos na SampleScene, ambos com
`LocalizeStringEvent` nos 3 idiomas. Verificado em Play Mode: o mapa abre com título no topo,
botão de sair embaixo e as 25 placas.

Rodar o tool de novo é seguro — é idempotente: destrói e recria o chrome a cada execução.

⚠️ **Por que a tentativa anterior parou no meio.** Um `TextMeshProUGUI` criado por script de
editor não tem font asset, então o material dele é null; setar `outlineWidth` chama
`SetOutlineThickness`, que clona esse material e lança. O título ficava criado sem outline e
o botão de voltar **nunca era alcançado** — sem erro visível, o menu só "não fazia nada".
Agora a fonte (Nunito, a única com acentos) é atribuída antes, e o outline degrada para um
warning em vez de derrubar o resto. O título ficou sem outline: creme sobre folhagem escura
segura sozinho.

**Gosto, não defeito:** o botão "Fechar" tem 240x48px em 1920x1080 — funciona, mas é
discreto. Se quiser mais presença, é só aumentar `backRect.sizeDelta` em
`RestyleWorldMapButtons.cs` mantendo a proporção 5:1 da arte.

⚠️ **Não rode o importador de CSV de localização sem conferir.** Rodei para adicionar
`ui_common.world_map_title` e ele **reverteu duas traduções do MainMenu** — PT "Espaço "
voltou para "Slot ", ES "Ranura " perdeu o espaço final. O CSV está atrás das tabelas nessas
chaves. Descartei as reversões, mas o problema segue lá.

---

## ✅ Verificado no fluxo real e SEM defeitos

Percorri MainMenu → Novo Jogo → slot → fazenda → SellBox → dormir → WorldMap:

- **SellBox**: "Caixa de Venda" em PT, grid 4x3, "Valor Total: 0 moedas". Painel bem
  proporcionado. *(Cheguei a reportar que não abria — era distância: 5,66 unidades contra
  alcance de 1,4.)*
- **Dormir**: painel informativo com dia/hora e resumo da Caixa de Venda, avança o dia
  corretamente (1 → 2), HUD atualiza. Uma correção aplicada: a linha do autosave era ciano
  sobre creme, 1,12:1 (`167c360`).
- **Áudio**: 17 SFX cobrindo arar, regar, plantar, colher, cavar, petting, hit e morte; 3
  faixas de música com troca por cena. *(A auditoria dizia "feedback praticamente ausente" —
  estava errada.)*

---

## ✅ O que já está resolvido (não precisa de você)

Branch `fix/visual-audit-phase1`, 907 testes verdes:

- Fog do minimapa cobrindo o mundo → corrigido
- 530 sprites com filtro que borrava pixel art → Point filter
- Projeto sem sorting layers → pilha de 8 criada
- **Loja abrindo vazia** (bug do `ItemDatabase`) → corrigido, com testes de regressão
- Textos sobrepostos no combate → corrigido
- Inventário com linha cortada → corrigido
- Terreno chapado → estrutura pintada (falta só a paleta, item 1)
- Minimapa ilegível (branco/branco) → corrigido
- `VersionText` fora da tela → corrigido
- **Retratos nunca apareciam no diálogo** → ligados (falta só a arte, item 3)
- Menu principal 40% maior que o jogo → referência padronizada sem mudar o visual
- Placeholders em inglês + typo "Item Descrption" → traduzidos
- **20 stages de combate sem fundo** → ligados aos 26 que já existiam no disco
- Sobreposição de texto no combate → 241px → 0px, medido
- Compressão de textura que estourou o deploy → revertida, Point filter mantido
