# Guia de Setup do Sistema de Combate — Sowur Shield
**Para uso pessoal — passo a passo completo**

---

## Visão Geral: O que você vai montar

O sistema de combate tem duas cenas separadas:

```
SampleScene (Fazenda)          CombatScene (Batalha)
─────────────────────          ─────────────────────
CombatTriggerZone              GridManager
TeamAssemblerUI                CombatTeamSpawner
StageButton (Worldmap)         TurnManager
                               BattleStatusUI
                               BattleResultsUI
```

O fluxo é: jogador aperta E na zona de combate → abre TeamAssembler → escolhe animais → aperta Start Battle → carrega CombatScene → batalha acontece → volta pra fazenda.

---

## PARTE 1 — Criar os Assets (Inimigos + Stages)

> **Faça isso primeiro, antes de qualquer setup de cena.**

### Passo 1 — Abrir o criador de assets

1. No Unity, espere compilar (barra no canto inferior direito terminar)
2. No menu superior: **Tools → Combat → Create All Combat Assets**
3. Vai abrir uma janela chamada "Combat Asset Creator"
4. Clique no botão verde **"Create ALL Assets (Enemies + Stages)"**
5. Clique **Create** no diálogo de confirmação
6. Aguarda aparecer a mensagem "Done" — isso cria todos os 34 inimigos e 25 stages

**Resultado esperado no Project:**
```
Assets/Resources/
  Enemies/
    Meadow/   → 6 assets
    Forest/   → 7 assets
    Cave/     → 7 assets
    Mountain/ → 7 assets
    Volcano/  → 7 assets
  Stages/
    Meadow/   → 5 assets (Stage_001 a Stage_005)
    Forest/   → 5 assets (Stage_006 a Stage_010)
    Cave/     → 5 assets (Stage_011 a Stage_015)
    Mountain/ → 5 assets (Stage_016 a Stage_020)
    Volcano/  → 5 assets (Stage_021 a Stage_025)
```

> Se a pasta `Enemies` ou `Stages` não apareceu, clique direito na pasta Resources → **Refresh**.

---

## PARTE 2 — Criar os Prefabs necessários

Você vai precisar criar 4 prefabs antes de montar as cenas:
1. CombatUnit Prefab
2. GridSlot Prefab (para o TeamAssembler)
3. AnimalCard Prefab
4. HealthBar Prefab

---

### Passo 2 — CombatUnit Prefab

Este é o objeto que representa cada animal/inimigo no campo de batalha.

1. **Hierarchy → clique direito → Create Empty**
2. Renomeie para `CombatUnit`
3. Com o CombatUnit selecionado, no **Inspector → Add Component**:
   - Adicione `CombatUnit` (script)
4. O script vai pedir um `healthBarPrefab` — deixe vazio por enquanto (você vai preencher depois)
5. **Salvar como prefab**: arraste o GameObject da Hierarchy para a pasta `Assets/Prefabs/`
6. Renomeie o arquivo para `CombatUnit.prefab`
7. **Delete** o GameObject da Hierarchy (você tem o prefab salvo)

---

### Passo 3 — HealthBar Prefab

A barra de vida que aparece em cima de cada unidade.

1. **Hierarchy → clique direito → UI → Canvas**
   - Renomeie para `HealthBar`
   - No Inspector do Canvas: **Render Mode → World Space**
   - **Width:** 1, **Height:** 0.2
2. Dentro do Canvas, **clique direito → UI → Slider**
   - Renomeie para `HealthSlider`
   - Selecione o Slider, no Inspector:
     - **Min Value:** 0, **Max Value:** 1, **Value:** 1
     - **Interactable:** desmarque
   - Expanda o Slider na Hierarchy: selecione **Fill** (dentro de Fill Area)
     - No Image component do Fill: mude a cor para **verde** (0, 255, 0)
3. De volta no `HealthBar` Canvas: **Add Component → `UnitHealthBar`** (script)
   - Arraste o `HealthSlider` para o campo **Health Slider**
   - Ajuste o **Offset Above Unit** para `(0, 0.6, 0)`
4. **Salvar como prefab**: arraste para `Assets/Prefabs/HealthBar.prefab`
5. **Delete** o GameObject da Hierarchy

**Agora linke no CombatUnit:**
1. Abra `Assets/Prefabs/CombatUnit.prefab` (duplo clique)
2. No Inspector do CombatUnit script, arraste `Assets/Prefabs/HealthBar.prefab` para o campo **Health Bar Prefab**
3. Salve o prefab (**Ctrl+S**)

---

### Passo 4 — GridSlot Prefab (para o TeamAssembler)

O slot clicável onde você arrasta os animais no TeamAssembler.

1. **Hierarchy → clique direito → UI → Image**
   - Renomeie para `GridSlot`
   - **Width:** 80, **Height:** 80
   - Cor: cinza escuro semi-transparente (ex: R:50 G:50 B:50 A:150)
2. Dentro do `GridSlot`, **clique direito → UI → Image**
   - Renomeie para `AnimalIcon`
   - **Width:** 60, **Height:** 60
   - Desmarque o componente Image (vai aparecer só quando tiver animal)
3. Dentro do `GridSlot`, **clique direito → UI → Text - TextMeshPro**
   - Renomeie para `PositionText`
   - **Font Size:** 8, texto alinhado ao centro embaixo
4. No GameObject raiz `GridSlot`: **Add Component → `GridPositionSlot`** (script)
   - Arraste o `Image` do próprio GridSlot para **Slot Background**
   - Arraste `AnimalIcon` para **Animal Icon**
   - Arraste `PositionText` para **Position Text**
5. **Salvar como prefab**: arraste para `Assets/Prefabs/GridSlot.prefab`
6. **Delete** o GameObject da Hierarchy

---

### Passo 5 — AnimalCard Prefab

O cartão de cada animal na lista do TeamAssembler.

1. **Hierarchy → clique direito → UI → Image**
   - Renomeie para `AnimalCard`
   - **Width:** 380, **Height:** 120
   - Cor: branco
2. Dentro do `AnimalCard`, crie 4 filhos (**UI → Image** e **UI → Text - TextMeshPro**):

   **Filho 1 — Portrait (Image):**
   - Nome: `Portrait`
   - Tamanho: **80x80**, ancorado à esquerda
   - Posição: X=-140, Y=0

   **Filho 2 — NameText (TextMeshPro):**
   - Nome: `NameText`
   - Tamanho: **180x30**, no topo ao centro
   - Font Size: 16, **Bold**

   **Filho 3 — HappinessText (TextMeshPro):**
   - Nome: `HappinessText`
   - Tamanho: **180x25**, ao centro
   - Font Size: 12

   **Filho 4 — FoodStatusText (TextMeshPro):**
   - Nome: `FoodStatusText`
   - Tamanho: **180x25**, embaixo ao centro
   - Font Size: 11

3. No GameObject raiz `AnimalCard`: **Add Component → `AnimalSelectionCard`** (script)
   - **Animal Portrait** → arraste `Portrait`
   - **Name Text** → arraste `NameText`
   - **Happiness Text** → arraste `HappinessText`
   - **Food Status Text** → arraste `FoodStatusText`
   - **Card Background** → arraste o próprio `Image` do `AnimalCard`
4. **Salvar como prefab**: arraste para `Assets/Prefabs/AnimalCard.prefab`
5. **Delete** o GameObject da Hierarchy

---

## PARTE 3 — Montar a SampleScene (Fazenda)

> Abra a SampleScene antes de continuar.

Você vai adicionar dois sistemas à cena da fazenda:
- **CombatTriggerZone** — a área onde o jogador aperta E para entrar no combate
- **TeamAssemblerUI** — o painel de seleção de animais

---

### Passo 6 — Criar a CombatTriggerZone

Este é o "portão" do combate na fazenda.

1. **Hierarchy → clique direito → Create Empty**
   - Renomeie para `CombatTriggerZone`
2. Posicione onde quiser no mapa (ex: perto de uma porta ou portão)
3. **Add Component → Box Collider 2D**
   - Ajuste o **Size** para cobrir a área de entrada (ex: X:2, Y:2)
   - **Is Trigger: marque** ✓
4. **Add Component → `CombatTriggerZone`** (script)
   - **Zone Name:** ex. "Floresta Sombria"
   - **Zone Difficulty:** 1 (ou o que quiser)
   - **Interaction Range:** 2

**Opcional — Adicionar prompt visual:**
1. Dentro de `CombatTriggerZone`, **clique direito → UI → Canvas** (ou um GameObject com sprite de texto)
   - Renomeie para `InteractPrompt`
   - Coloque "Pressione E" como texto
   - Posicione acima da zona
2. Arraste `InteractPrompt` para o campo **Interact Prompt** no Inspector do `CombatTriggerZone`

---

### Passo 7 — Criar o TeamAssemblerUI

Este é o painel grande que abre quando o jogador ativa a zona de combate.

#### 7.1 — Criar a estrutura do Canvas

1. **Hierarchy → clique direito → UI → Canvas**
   - Renomeie para `TeamAssemblerCanvas`
   - **Render Mode:** Screen Space — Overlay
2. Dentro do Canvas, **clique direito → UI → Panel**
   - Renomeie para `AssemblerPanel`
   - Deixe cobrir a tela toda (anchors: stretch/stretch)
   - Cor de fundo: preto semi-transparente (A: 200)
3. **Desative** o `AssemblerPanel` no Inspector (checkbox do lado do nome) — ele vai ser ativado por código

#### 7.2 — Criar o painel de seleção de animais (esquerda)

Dentro do `AssemblerPanel`:

1. **clique direito → UI → Panel**
   - Renomeie para `AnimalSelectionPanel`
   - Posicione na **metade esquerda** da tela
   - Tamanho: **Width: 420, Height: 600** (ou o que couber na sua tela)

2. Dentro do `AnimalSelectionPanel`, **clique direito → UI → Scroll View**
   - Renomeie para `AnimalScrollView`
   - Deixe cobrir o painel inteiro
   - No Inspector do **Scroll Rect**:
     - **Horizontal:** desmarque
     - **Vertical:** marque ✓
   - Expanda o `AnimalScrollView` na Hierarchy:
     - Selecione `Viewport → Content`
     - Renomeie `Content` para **`Content`** (já está assim)
     - **Add Component → Vertical Layout Group**
       - **Spacing:** 5
       - **Child Force Expand Width:** marque ✓
       - **Child Force Expand Height:** desmarque
     - **Add Component → Content Size Fitter**
       - **Vertical Fit:** Preferred Size

> **IMPORTANTE:** O campo `Animal Card Container` no script vai receber exatamente este `Content` GameObject.

#### 7.3 — Criar o painel da grade (direita)

Dentro do `AssemblerPanel`:

1. **clique direito → UI → Panel**
   - Renomeie para `GridPanel`
   - Posicione na **metade direita** da tela
   - Tamanho: **Width: 400, Height: 600**

2. Dentro do `GridPanel`, **clique direito → UI → Panel**
   - Renomeie para `GridContainer`
   - Tamanho: **Width: 360, Height: 500**
   - **Add Component → Grid Layout Group**
     - **Cell Size:** X:80, Y:80
     - **Spacing:** X:5, Y:5
     - **Start Corner:** Upper Left
     - **Constraint:** Fixed Column Count
     - **Constraint Count:** 3 (são 3 colunas de jogador: 6, 7, 8)

#### 7.4 — Criar a área de informações (abaixo da grade)

Ainda dentro do `GridPanel`:

1. **clique direito → UI → Text - TextMeshPro**
   - Renomeie para `TeamSizeText`
   - Texto inicial: "Team: 0/15"

2. **clique direito → UI → Text - TextMeshPro**
   - Renomeie para `FoodRequirementsText`
   - Texto inicial: "All animals fed!"

3. **clique direito → UI → Text - TextMeshPro**
   - Renomeie para `SynergiesText`
   - Texto inicial: "Synergies: TBD"

#### 7.5 — Criar os botões

Dentro do `AssemblerPanel`:

1. Crie 4 botões (**clique direito → UI → Button - TextMeshPro**):
   - `FeedAllButton` — texto: "Alimentar Todos"
   - `ClearGridButton` — texto: "Limpar Grade"
   - `StartBattleButton` — texto: "Iniciar Batalha"
   - `CancelButton` — texto: "Cancelar"
   - Posicione-os onde quiser (geralmente embaixo)

#### 7.6 — Adicionar o script TeamAssemblerUI

1. Selecione o `TeamAssemblerCanvas` (o Canvas raiz)
2. **Add Component → `TeamAssemblerUI`** (script)
3. Preencha **todos** os campos no Inspector:

| Campo no Inspector | O que arrastar |
|---|---|
| **Assembler Panel** | `AssemblerPanel` |
| **Animal Selection Panel** | `AnimalSelectionPanel` |
| **Animal Card Container** | `Content` (dentro do ScrollView) |
| **Grid Panel** | `GridPanel` |
| **Grid Container** | `GridContainer` |
| **Grid Slot Prefab** | `Assets/Prefabs/GridSlot.prefab` |
| **Animal Card Prefab** | `Assets/Prefabs/AnimalCard.prefab` |
| **Zone Name Text** | `TeamSizeText` (ou crie um ZoneNameText separado) |
| **Team Size Text** | `TeamSizeText` |
| **Food Requirements Text** | `FoodRequirementsText` |
| **Synergies Text** | `SynergiesText` |
| **Feed All Button** | `FeedAllButton` |
| **Clear Grid Button** | `ClearGridButton` |
| **Start Battle Button** | `StartBattleButton` |
| **Cancel Button** | `CancelButton` |
| **Combat Scene Name** | `CombatScene` (nome exato da sua cena de combate) |

**Verifique as configurações de debug:**
- **Disable Viewport Mask:** deixe **desmarcado**
- **Auto Expand Viewport:** deixe **marcado** ✓

---

### Passo 8 — Verificar o EventSystem

Para drag-and-drop funcionar, precisa ter um EventSystem na cena.

1. Na Hierarchy, procure por `EventSystem`
2. Se não existir: **Hierarchy → clique direito → UI → Event System**

---

## PARTE 4 — Criar a CombatScene

> **File → New Scene**, salve como `CombatScene`.
> Adicione a CombatScene no Build Settings: **File → Build Settings → Add Open Scenes**

A CombatScene precisa ter estes GameObjects:

```
CombatScene Hierarchy:
├── Main Camera
├── GridManager
├── TurnManager
├── CombatTeamSpawner
├── BattleStatusCanvas
│   └── BattleStatusUI
└── BattleResultsCanvas
    └── BattleResultsUI
```

---

### Passo 9 — GridManager

1. **Hierarchy → clique direito → Create Empty**
   - Renomeie para `GridManager`
2. **Add Component → `GridManager`** (script)
3. Configurações no Inspector:
   - **Grid Width:** 9
   - **Grid Height:** 5
   - **Cell Size:** 1
   - **Player Columns:** 3
   - **Grid Parent:** deixe vazio (o script cria automaticamente)
   - **Health Bar Prefab:** arraste `Assets/Prefabs/HealthBar.prefab`

---

### Passo 10 — TurnManager

1. **Hierarchy → clique direito → Create Empty**
   - Renomeie para `TurnManager`
2. **Add Component → `TurnManager`** (script)
3. Configurações no Inspector:
   - **Gauge Fill Rate:** 1
   - **Max Actions:** 500
   - **Action Micro Delay:** 0.05
   - **Verbose Logging:** pode deixar marcado enquanto testa, depois desmarque

---

### Passo 11 — CombatTeamSpawner

1. **Hierarchy → clique direito → Create Empty**
   - Renomeie para `CombatTeamSpawner`
2. **Add Component → `CombatTeamSpawner`** (script)
3. Configurações no Inspector:
   - **Combat Unit Prefab:** arraste `Assets/Prefabs/CombatUnit.prefab`
   - **Spawn Player Team:** marque ✓
   - **Spawn Enemy Team:** deixe **desmarcado** por enquanto (inimigos via CombatTestSpawner)
   - **Show Debug Logs:** marque ✓ (enquanto testa)

---

### Passo 12 — BattleStatusUI

1. **Hierarchy → clique direito → UI → Canvas**
   - Renomeie para `BattleStatusCanvas`
   - **Render Mode:** Screen Space — Overlay

2. Dentro do Canvas, crie os textos:
   - **UI → Text - TextMeshPro** → renomeie `TurnCounterText`, posicione no **topo centro**
   - **UI → Text - TextMeshPro** → renomeie `PlayerTeamText`, posicione no **topo esquerda**
   - **UI → Text - TextMeshPro** → renomeie `EnemyTeamText`, posicione no **topo direita**

3. Criar o painel de ordem de turnos:
   - **UI → Panel** → renomeie `TurnOrderPanel`, posicione no **topo** embaixo dos textos
   - **Add Component → Horizontal Layout Group**
     - **Spacing:** 5
     - **Child Force Expand:** desmarque tudo

4. Criar o ícone de turno (prefab):
   - **UI → Image** → renomeie `TurnOrderIcon`
   - **Width:** 30, **Height:** 30
   - Salve como prefab: `Assets/Prefabs/TurnOrderIcon.prefab`
   - Delete da Hierarchy

5. No `BattleStatusCanvas`: **Add Component → `BattleStatusUI`** (script)
   - **Turn Counter Text** → `TurnCounterText`
   - **Player Team Text** → `PlayerTeamText`
   - **Enemy Team Text** → `EnemyTeamText`
   - **Turn Order Panel** → `TurnOrderPanel`
   - **Turn Order Icon Prefab** → `Assets/Prefabs/TurnOrderIcon.prefab`
   - **Max Turn Order Display:** 10

---

### Passo 13 — BattleResultsUI

1. **Hierarchy → clique direito → UI → Canvas**
   - Renomeie para `BattleResultsCanvas`
   - **Render Mode:** Screen Space — Overlay

2. Criar o **VictoryPanel**:
   - **UI → Panel** → renomeie `VictoryPanel`
   - Cor de fundo: verde escuro semi-transparente
   - Dentro: crie 3 TextMeshPro:
     - `VictoryTitleText` — grande, centralizado no topo
     - `VictoryStatsText` — médio, centro
     - `VictoryRewardsText` — médio, abaixo do stats
   - Crie 2 botões:
     - `VictoryReturnButton` — texto: "Voltar à Fazenda"
     - `VictoryRetryButton` — texto: "Tentar Novamente"
   - **Desative** o VictoryPanel (checkbox off)

3. Criar o **DefeatPanel** (mesma estrutura):
   - **UI → Panel** → renomeie `DefeatPanel`
   - Cor de fundo: vermelho escuro semi-transparente
   - Dentro: crie 2 TextMeshPro:
     - `DefeatTitleText`
     - `DefeatStatsText`
   - Crie 2 botões:
     - `DefeatReturnButton` — texto: "Voltar à Fazenda"
     - `DefeatRetryButton` — texto: "Tentar Novamente"
   - **Desative** o DefeatPanel

4. No `BattleResultsCanvas`: **Add Component → `BattleResultsUI`** (script)
   - **Victory Panel** → `VictoryPanel`
   - **Defeat Panel** → `DefeatPanel`
   - **Victory Title Text** → `VictoryTitleText`
   - **Victory Stats Text** → `VictoryStatsText`
   - **Victory Rewards Text** → `VictoryRewardsText`
   - **Victory Return Button** → `VictoryReturnButton`
   - **Victory Retry Button** → `VictoryRetryButton`
   - **Defeat Title Text** → `DefeatTitleText`
   - **Defeat Stats Text** → `DefeatStatsText`
   - **Defeat Return Button** → `DefeatReturnButton`
   - **Defeat Retry Button** → `DefeatRetryButton`
   - **Farm Scene Name:** `SampleScene` (nome exato da sua cena de fazenda)
   - **Combat Scene Name:** `CombatScene`

---

## PARTE 5 — Adicionar inimigos de teste na CombatScene

Por enquanto o `CombatTeamSpawner` ainda não spawna inimigos automaticamente pelo StageData. Para testar a batalha, use o `CombatTestSpawner`.

### Passo 14 — CombatTestSpawner

1. Na CombatScene, **Hierarchy → clique direito → Create Empty**
   - Renomeie para `CombatTestSpawner`
2. **Add Component → `CombatTestSpawner`** (script)
3. No Inspector:
   - **Spawn Test Enemies:** marque ✓
   - **Spawn Test Players:** deixe **desmarcado** (quem spawna jogadores é o CombatTeamSpawner)
   - **Combat Unit Prefab:** arraste `Assets/Prefabs/CombatUnit.prefab`

---

## PARTE 6 — Configurar o Worldmap (opcional por enquanto)

Se você tem uma cena de mapa-múndi com botões para escolher stages:

### Passo 15 — StageButton

Para cada botão de stage no Worldmap:

1. Selecione o botão
2. **Add Component → `StageButton`** (script)
3. Preencha:
   - **Stage Name:** deve ser **exatamente** igual ao `stageName` no asset
     - Ex: `"Sunny Fields"`, `"Whispering Woods"`, etc.
   - **World Map:** arraste o GameObject que representa o mapa (para sumir quando abrir o assembler)
4. No componente **Button** nativo:
   - Em **On Click ()** → **+** → arraste o GameObject → selecione `StageButton.OnClick`

---

## PARTE 7 — Checklist final antes de testar

Antes de apertar Play, confira cada item:

### Na SampleScene:
- [ ] `InteractionManager` existe na cena (com script `InteractionManager`)
- [ ] `UIManager` existe na cena (com script `UIManager`)
- [ ] Jogador tem tag `"Player"` e componente `PlayerMove`
- [ ] `CombatTriggerZone` tem Collider2D com **IsTrigger = true**
- [ ] `TeamAssemblerUI` tem **todos** os campos preenchidos no Inspector
- [ ] `AssemblerPanel` está **desativado** (checkbox off)
- [ ] `EventSystem` existe na cena

### Na CombatScene:
- [ ] `GridManager` existe com **Health Bar Prefab** atribuído
- [ ] `TurnManager` existe
- [ ] `CombatTeamSpawner` tem **Combat Unit Prefab** atribuído
- [ ] `BattleStatusUI` tem todos os textos atribuídos
- [ ] `BattleResultsUI` tem os painéis e botões atribuídos
- [ ] `VictoryPanel` e `DefeatPanel` estão **desativados**
- [ ] A cena `CombatScene` está no **Build Settings**

### Assets:
- [ ] Pasta `Assets/Resources/Enemies/` tem os 34 assets
- [ ] Pasta `Assets/Resources/Stages/` tem os 25 assets
- [ ] `Assets/Prefabs/CombatUnit.prefab` existe com `CombatUnit` script
- [ ] `Assets/Prefabs/HealthBar.prefab` existe com `UnitHealthBar` script
- [ ] `Assets/Prefabs/GridSlot.prefab` existe com `GridPositionSlot` script
- [ ] `Assets/Prefabs/AnimalCard.prefab` existe com `AnimalSelectionCard` script

---

## PARTE 8 — Como testar

### Teste 1 — TeamAssembler

1. Play na SampleScene
2. Ande até a `CombatTriggerZone`
3. Aperte **E**
4. O painel do TeamAssembler deve aparecer
5. Os animais da cena devem aparecer como cartões na esquerda
6. Arraste um cartão para um slot da grade
7. Alimente o animal clicando em "Alimentar Todos" (precisa ter comida no inventário)
8. O botão "Iniciar Batalha" deve ficar clicável
9. Clique → deve carregar a CombatScene

### Teste 2 — Batalha

1. Na CombatScene, os animais do jogador devem aparecer nos slots da direita (colunas 6-8)
2. Os inimigos de teste devem aparecer na esquerda (colunas 0-5)
3. A batalha deve começar automaticamente após 1 segundo
4. As barras de vida devem aparecer e diminuir
5. Ao terminar, deve aparecer o painel de Vitória ou Derrota
6. Botão "Voltar à Fazenda" deve carregar a SampleScene

---

## Problemas comuns

| Problema | Causa provável | Solução |
|---|---|---|
| TeamAssembler não abre | InteractionManager não existe na cena | Adicione o GameObject com o script InteractionManager |
| Cartões de animais não aparecem | Não tem animais com componente `Animal` na cena | Coloque pelo menos um Animal na SampleScene |
| Botão Start Battle sempre desabilitado | Animais não estão marcados como alimentados | Clique em "Alimentar Todos" (precisa ter itens de comida no inventário) |
| Unidades não aparecem na CombatScene | CombatTeamSpawner sem prefab atribuído | Atribua `CombatUnit.prefab` no campo Combat Unit Prefab |
| Batalha não começa | TurnManager não encontrou units (GridManager vazio) | Verifique se CombatTestSpawner está na cena com Spawn Test Enemies marcado |
| Stage não encontrado no StageButton | stageName não bate com o asset | O campo Stage Name deve ser exato: ex. `"Sunny Fields"` não `"sunny fields"` |
| Assets de inimigos/stages não criados | Script editor não compilou ainda | Espere a barra de compilação do Unity terminar antes de usar Tools > Combat |
| HealthBar não aparece | Prefab não atribuído no GridManager | Atribua `HealthBar.prefab` no campo Health Bar Prefab do GridManager |

---

## Resumo rápido dos scripts por cena

### SampleScene
| Script | GameObject | Função |
|---|---|---|
| `CombatTriggerZone` | Zona de entrada | Detecta jogador e abre TeamAssembler |
| `TeamAssemblerUI` | Canvas | Painel de seleção e posicionamento dos animais |
| `GridPositionSlot` | (gerado por código) | Slot da grade no assembler |
| `AnimalSelectionCard` | (gerado por código) | Cartão de animal na lista |

### CombatScene
| Script | GameObject | Função |
|---|---|---|
| `GridManager` | GridManager | Cria e gerencia a grade 9x5 |
| `CombatTeamSpawner` | CombatTeamSpawner | Spawna os animais do jogador na grade |
| `TurnManager` | TurnManager | Controla os turnos da batalha (ATB) |
| `CombatUnit` | (gerado por código) | Cada unidade na batalha |
| `UnitHealthBar` | (gerado por código) | Barra de vida de cada unidade |
| `BattleStatusUI` | Canvas | Mostra turnos, contagem de times |
| `BattleResultsUI` | Canvas | Tela de vitória/derrota |
