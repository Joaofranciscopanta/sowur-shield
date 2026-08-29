# O que precisa de você — Sowur Shield

> Lista viva do que **eu não posso resolver sozinho** e por quê. Atualizada em 2026-08-29.
> Companion da [auditoria visual](VISUAL_AUDIT_2026_08_29.md).
> Branch atual: `fix/visual-audit-phase1` (não pusheada — nenhum Cloud Build disparado).

Ordenado por impacto. Cada item diz **por que parei** e **o que exatamente decidir/fazer**.

---

## 🔴 1. Trocar a arte do tileset do terreno — PRECISA DE ARTISTA

**Estado**: a estrutura do terreno está pronta e funcionando (pátio, curral, caminhos,
bordas resolvidas pelo dual-grid — 14 tiles distintos onde antes havia 1). O que está
errado é a **paleta**.

**O problema**: `Assets/DualGridSystem/Tiles/Grass/TilesDemo.png` é a **arte de
demonstração do plugin dual-grid** — verde-limão contra roxo-vinho. Não é arte de fazenda.

**Por que não fiz**: existe tileset real em
`Assets/Art/ThirdParty/Sprout Lands - Sprites - premium pack/Tilesets/ground tiles/New tiles/`
(`Grass_tiles_v2.png` e `Darker_Soil_Ground_Tiles.png`), mas eles são **176x112 com layout
misto** (grama + bordas + encostas juntas), enquanto o dual-grid exige uma **grade 4x4 de
16 tiles** numa ordem específica. Refatiar exige julgamento visual de qual recorte vira
qual das 16 regras — trabalho de Sprite Editor, não de script. Improvisar isso entregaria
algo pior que o placeholder atual.

**O que fazer** (você ou a Isabella):
1. Abrir `Grass_tiles_v2.png` e `Darker_Soil_Ground_Tiles.png` no Sprite Editor
2. Montar uma textura 64x64 no mesmo layout de `TilesDemo.png` (4x4, células de 16px)
3. A ordem das 16 regras está em `Assets/Scripts/DualGridTilemap/DualGridTilemap.cs:35-52`
   — os índices `tiles[0]` a `tiles[15]` mapeiam cada combinação de 4 vizinhos
4. Criar os 16 assets `Tile` e arrastar para o array `tiles` do `DualGridTilemap` na cena

**Alternativa mais barata**: se quiser só matar o roxo agora, dá para recolorir
`TilesDemo.png` num editor de imagem mantendo a estrutura — 10 minutos, resolve 80% do
problema visual.

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
- **20 dos 25 stages não têm `backgroundSprite`** — só 5 têm. Sem fundo, a batalha roda
  sobre cinza liso. *Decisão sua*: reaproveito os 5 fundos existentes por bioma (imediato,
  sem arte nova) ou fica marcado para a Isabella fazer fundos próprios?
- `"Turno: 2/500"` expõe um limite de debug ao jogador.

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

**O risco que permanece**: mesmo saudável, o build está apertado.

| Arquivo | Tamanho | Limite |
|---|---|---|
| `Default WebGL.data` | ~71 MB | 100 MB (hard) |
| `Default WebGL.wasm` | ~54,5 MB | 50 MB (warning) — **já passa do aviso** |

**O que fazer**: nada urgente, mas **qualquer adição grande de arte ou áudio pode estourar
de novo** — e o build pago roda antes do push ser recusado, então o dinheiro é gasto à toa.
Quando for adicionar muitos assets, me peça para medir antes. Se quiser resolver de vez, as
saídas são compressão de áudio, Addressables para conteúdo pesado, ou hospedar o build fora
do GitHub Pages.

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
