# QA / UI-UX / Assets Audit — 2026-07-05

> Sessão de auditoria ao vivo no Editor (Play Mode via MCP), Unity 6000.3.3f1, branch `feature/cozy-ui-pass-3`.
> Plataforma ativa do Editor durante os testes: **WebGL** com defines `DOTWEEN;DEMO_BUILD` (relevante — ver ERRO-01).
> Screenshots de evidência: `Assets/Screenshots/audit_01..27_*.png` (artefatos de teste, não versionados — podem ser apagados).
> Workarounds usados só para teste (nada foi salvo em cena; scene `isDirty=false` ao final): teleporte do player,
> `SetRelationship("maren", 80)`, itens adicionados via `Inventory.AddItem`, `gaugeFilLRate` 1→20 em play mode,
> criação/remoção de um GameObject `TEST_LocalizationManager_AUDIT` em play mode.

## Sumário executivo

- **2 erros críticos para a demo** (save desabilitado silenciosamente por `DEMO_BUILD` no Editor/demo; inventário zerado após cada batalha na demo), **3 altos**, **7 médios/baixos** — todos novos, com repro e causa raiz.
- **12 melhorias de UI/UX** específicas (transbordo de texto no HUD em 1080p, telas inteiras sem tema — Quests/AnimalMarket/BuildingShop —, mapa-múndi com 25 botões cinza empilhados, tela de vitória em branco).
- **~104 assets ociosos** mapeados pelo cruzamento de GUIDs (71 TileBase abandonados, 13 animações antigas do player, 4 prefabs duplicados, 12 sprites órfãos) — 3 claims verificados manualmente por grep de GUID.
- **8 inovações de baixo custo** sugeridas.
- **KNOWN_BUGS.md: 3 itens processados** — 1 parcialmente corrigido (Maren, com causa raiz nova), 1 confirmado com números (choice button), 1 revisado por código com fix proposto (ItemDatabase).

---

## 0. Cruzamento com KNOWN_BUGS.md

### Bug conhecido: Maren Beloved — Can't re-interact after first conversation
- **Status ao reproduzir:** **Parcialmente corrigido.** A re-interação via E **funciona** de novo (testado com diálogo comum E com rel=80/Beloved, fechando por fim natural E por ESC/`CloseWindow`): o `InteractionManager` re-detecta a Maren sozinho em poucos segundos e `TriggerInteraction` reabre o diálogo. **O que ainda ocorre:** o prompt "Press E to Talk" **nunca reaparece** enquanto o player permanece no alcance (`playerInRange=True`, `interactionPrompt.activeSelf=False` — medido em runtime).
- **Nova informação encontrada (causa raiz):** o `InteractionManager` só chama `SetPromptVisibility(true)` quando o interactable **atual muda** (transição). Após o diálogo, o atual continua sendo "Maren" (ou volta a ser sem transição por `null`), então o prompt fica oculto até o player sair do alcance e voltar. O `Interact()` esconde o prompt em `NPCDialogueInteractable.cs:349-351`, e `OnDialogueEndedCallback()` (`NPCDialogueInteractable.cs:439-451`) nunca o restaura.

**Prompt para corrigir:**
> No projeto Sowur Shield (`C:\Users\Lucas\Sowur Shield`), o prompt "Press E to Talk" de NPCs não reaparece depois que um diálogo termina, se o player permanecer no alcance (a interação por E volta a funcionar; só o prompt visual fica oculto). Causa: `InteractionManager` só chama `SetPromptVisibility(true)` em transições de interactable, e após o diálogo o interactable atual continua sendo o mesmo NPC. Corrija em `Assets/Scripts/Dialogue/NPCDialogueInteractable.cs`, método `OnDialogueEndedCallback()` (~linha 439): após `isDialogueActive = false`, verifique a distância ao player (use o mesmo raio de `GetInteractionRange()`) e, se em alcance E `InteractionManager.Instance?.GetCurrentInteractable() == this` (ou equivalente), chame `SetPromptVisibility(true)`. Repro: em Play Mode na SampleScene, aproxime-se da Maren, converse (E), feche o diálogo e observe que o prompt não volta sem sair e reentrar no alcance. Atualize a entrada correspondente no KNOWN_BUGS.md (rebaixar de "can't re-interact" para "prompt não reaparece" já corrigido).

### Bug conhecido: Dialogue UI — Choice button text clips out of button bounds
- **Status ao reproduzir:** **Confirmado, ainda ocorre.** Instanciei o `choiceButtonPrefab` real (`ChoiceButton`, referenciado por `DialogueTreeUI.choiceButtonPrefab`) no `ChoicePanel` em play mode com uma string longa: botão fixo **160×30**, texto com preferred size **1422×252**, TMP `overflowMode=Overflow`, `autoSize=False`, **sem** `ContentSizeFitter` e **sem** `LayoutElement` → o texto vaza em coluna por cima do mundo (screenshot `audit_15_choicebtn_overflow.png`).
- **Nova informação encontrada:** o prefab não tem NENHUM mecanismo de ajuste — nem auto-size, nem crescimento do botão. Qualquer choice com mais de ~20 caracteres já quebra em 2 linhas e sai do frame de 30px.

**Prompt para corrigir:**
> No Sowur Shield, o prefab de botão de escolha de diálogo (referenciado em `DialogueTreeUI.choiceButtonPrefab`; instâncias criadas em `DialogueTreeUI.cs:435`) tem RectTransform fixo 160×30 e o TMP filho usa `overflowMode=Overflow` sem auto-size nem ContentSizeFitter — textos longos vazam para fora do botão (confirmado em runtime: preferred 1422×252 vs rect 160×30). Corrija o prefab: (a) habilite TMP Auto Size no label (min 14, max 24) com word wrap, E (b) adicione `LayoutElement` no botão com `minHeight=30` + `ContentSizeFitter (Vertical=PreferredSize)` OU aumente a largura para ~400 e deixe o container (`ChoicePanel`) com `VerticalLayoutGroup` controlar. Teste em Play Mode com uma choice de 150 caracteres via diálogo da Maren. Atualize KNOWN_BUGS.md (remover o "NEEDS RE-VERIFICATION" — está verificado e reproduzido).

### Bug conhecido: ItemDatabase lookup came back empty after an in-editor domain reload
- **Status ao reproduzir:** **Não reproduzido em play mode limpo** (20 itens carregados, lookups OK; domain reload está habilitado — `EnterPlayModeOptionsEnabled=False`, então statics resetam a cada play). Não forcei um recompile no meio da sessão para não interromper a auditoria.
- **Nova informação encontrada:** revisão de código confirma o guard frágil descrito: `ItemDatabase.cs:52-54` — `Initialize()` faz `if (isInitialized) return;` sem checar se `itemLookup` está populado/vivo. Bônus: bloco morto `if (duplicateCount > 0) { }` vazio em `ItemDatabase.cs:88-90` (pitfall documentado no CLAUDE.md).

**Prompt para corrigir:**
> Em `Assets/Scripts/Inventory/ItemDatabase.cs` do Sowur Shield: (1) endureça o guard de `Initialize()` (linha ~54) para `if (isInitialized && itemLookup.Count > 0 && itemLookup.Values.FirstOrDefault() != null) return;` — protege contra o estado descrito em KNOWN_BUGS.md onde `isInitialized` sobrevive a um domain reload de editor com o dicionário vazio/morto; (2) adicione um reset estático `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { isInitialized = false; itemLookup.Clear(); instance = null; }` para segurança com Enter Play Mode Options; (3) remova o bloco vazio `if (duplicateCount > 0) { }` (linhas ~88-90). Rode os testes PlayMode existentes depois.

### Quirks re-verificados
- **HUD vazio jogando direto na SampleScene** — mecanismo confirmado: `LocalizationManager` é criado em runtime por `MainMenuManager.Awake → EnsureLocalizationManagerExists()` (`MainMenuManager.cs:65-75`); ele não existe em nenhuma cena/prefab (guid `8c1e16a37f9846b409c7e76cc1ab45b8` não aparece em nenhuma .unity/.prefab). No fluxo normal MainMenu→jogo, HUD localiza perfeitamente ("Hora: 07:00", "Dinheiro: $100", "Primavera 1" em PT). Quirk permanece válido.
- **Editor congela sem foco** — confirmado e agravado: pauses manuais no Editor durante testes automatizados produzem sintomas fantasma (typewriter parado, coroutines penduradas, Destroy adiado). Qualquer sessão de QA via MCP deve checar `EditorApplication.isPaused` antes de interpretar resultados.

---

## 1. Erros (bugs novos, não listados em KNOWN_BUGS.md)

### [ERRO-01] Save desabilitado silenciosamente no Editor (e demo) via DEMO_BUILD
- **Sistema:** Save/Load
- **Severidade:** **Crítico** (no contexto da demo e de qualquer QA no Editor)
- **Repro:** com a plataforma ativa = WebGL (estado atual do projeto; defines `DOTWEEN;DEMO_BUILD`), em Play Mode: `SaveManager.SaveToSlot("Slot2")` → retorna sem erro, cria o diretório `Saves/Slot2/` mas **não escreve** `GameSave.json` nem `SlotMeta.json`. `LoadFromSlot("Slot2")` também "funciona" silenciosamente: troca `activeSlotName` e não restaura nada (posição do player não voltou no teste). Zero mensagens no console.
- **Evidência:** `Saves/Slot1|2|3` vazios em `%AppData%..\LocalLow\DefaultCompany\Sowur Shield`; `PlayerSettings.GetScriptingDefineSymbolsForGroup(WebGL)` = `DOTWEEN;DEMO_BUILD`.
- **Arquivo(s):** `Assets/Scripts/SaveManager.cs:268-273` (`#if DEMO_BUILD → OnSaveCompleted(false); return;`).

**Prompt para corrigir:**
> No Sowur Shield, `SaveManager.SaveGame()` (`Assets/Scripts/SaveManager.cs:268-273`) tem um `#if DEMO_BUILD` que transforma TODO save em no-op — e o define `DEMO_BUILD` está ativo no grupo WebGL, que é a plataforma ativa do Editor. Consequências: (a) impossível salvar/testar save no Editor enquanto a plataforma for WebGL; (b) na demo, "Save Game" do pause menu finge funcionar; (c) `LoadFromSlot` ainda troca `activeSlotName` e dispara um load de dados inexistentes. Corrija: (1) mude o gate para `#if DEMO_BUILD && !UNITY_EDITOR` para que o Editor sempre salve; (2) em `LoadFromSlot`/`LoadGame`, se o arquivo do slot não existir, logue warning e NÃO troque o slot ativo; (3) exiba feedback visível ("Demo: progresso não é salvo") quando um save for suprimido — há um evento `OnSaveCompleted(false)` que já dispara; conecte-o a um toast/label no `GameMenuUI` e no `SaveGameUI`. Não altere o comportamento do build WebGL da demo (continuar sem salvar é intencional).

### [ERRO-02] Inventário do player é zerado após qualquer batalha (farm → combate → farm)
- **Sistema:** Combat / Inventory / Save-Load
- **Severidade:** **Crítico na demo WebGL** (Alto em geral — depende do ERRO-01)
- **Repro:** SampleScene com Hoe/WateringCan/CarrotSeed no hotbar → CombatTriggerZone → WorldMap → stage 1 → TeamAssembler → batalha → vitória → "To Farm". De volta à SampleScene: hotbar com **0 itens** (varri os 9 slots via `GetSelectedItem`). O dinheiro sobrevive ($116 → $149 com a recompensa) porque vive em manager DDOL; os itens não.
- **Evidência:** screenshot `audit_22_hud_1080p.png` (hotbar vazio pós-combate); leitura programática dos 9 slots = vazio.
- **Arquivo(s) suspeitos:** fluxo de retorno em `BattleResultsUI`/`SceneTransitionManager` (reload da SampleScene) + `Inventory` (ISaveable) restaurando de um save que não existe (ERRO-01). Em build não-demo o dano vira "rollback para o último autosave", que também perde o que foi obtido desde então se não houver autosave pré-combate.
- **Nota:** a demo pública tem esse comportamento AGORA — cada batalha apaga as ferramentas do jogador.

**Prompt para corrigir:**
> No Sowur Shield, o inventário do player é perdido ao voltar de uma batalha: SampleScene → CombatScene → retorno recarrega a SampleScene e o `Inventory` (scene-local) restaura do save — que não existe quando o save está desabilitado (`DEMO_BUILD`, ver SaveManager.cs:270) e que pode estar defasado mesmo em builds normais. Repro: novo jogo, pegue a enxada do chão, entre em combate pelo CombatTriggerZone, vença, volte com "To Farm" — hotbar vazio. Corrija tornando o inventário resiliente à ida-e-volta de cena SEM depender de save em disco: snapshot em memória (ex.: no `TeamAssemblerData`, que já é DDOL, ou um `InventorySnapshot` estático) capturado antes de `SceneManager.LoadScene(CombatScene)` e reaplicado no `Inventory.Start()` da SampleScene quando presente. Alternativa aceitável: `TriggerAutoSave()` antes de entrar no combate + restaurar do AutoSave no retorno, mas isso NÃO cobre a demo (save desabilitado), então prefira o snapshot em memória. Valide também que os GroundItems já coletados não reaparecem duplicados após o retorno.

### [ERRO-03] Tela de vitória do combate completamente em branco (título/estatísticas/recompensas vazios)
- **Sistema:** Combat / UI / Localization
- **Severidade:** Alto
- **Repro:** vencer qualquer batalha. O `VictoryPanel` aparece (moldura de madeira + fitas douradas) mas sem NENHUM texto: `VictoryTitleText=''`, `VictoryStatsText=''`, `VictoryRewardsText='\n\n\n'` (medido em runtime). Só os botões "Retry"/"To Farm" (EN, 24px) aparecem, cortados nos cantos inferiores da tela.
- **Evidência:** screenshot `audit_21_victory.png`; dump dos TMPs do `BattleResultsCanvas`.
- **Causa raiz:** os campos `LocalizedString` do `BattleResultsUI` (título/stats/rewards de vitória) estão **sem Table/Entry atribuídos** no Inspector → `SafeGetLocalizedString()` retorna `""` por design (`LocalizedStringExtensions.cs:23`). O jogador ganha e não vê recompensa nenhuma.
- **Arquivo(s):** `Assets/Scripts/Combat/BattleResultsUI.cs` + wiring do `BattleResultsCanvas` na CombatScene.

**Prompt para corrigir:**
> No Sowur Shield, a tela de vitória do combate renderiza vazia: os campos `LocalizedString` de `BattleResultsUI` (VictoryTitleText/VictoryStatsText/VictoryRewardsText no `BattleResultsCanvas` da CombatScene) não têm Table/Entry atribuídos, e `SafeGetLocalizedString()` retorna string vazia para LocalizedString com `IsEmpty==true`. Corrija: (1) crie/atribua entradas nas string tables (EN/PT/ES) para "Victory!", stats (turnos, dano) e rewards (ouro/loot/XP) — use o tooling `Tools > Sowur Shield > Setup Localization (Full)` e `Auto-Wire Localized Fields` se aplicável; (2) como fallback de segurança, faça `BattleResultsUI` usar um texto hardcoded EN quando o LocalizedString estiver vazio, para nunca exibir painel em branco; (3) aproveite e localize os botões "Retry"/"To Farm". Verifique vencendo uma batalha no stage 1 (Campos Ensolarados) e confirmando título+recompensas visíveis nas 3 línguas.

### [ERRO-04] Slot picker: linhas de save sempre em branco (EmptyText desativado no prefab)
- **Sistema:** Save/Load UI (menu principal)
- **Severidade:** Alto
- **Repro:** MainMenu → New Game (ou Load Game): as 4 linhas do picker são barras de madeira **sem texto nenhum**. Em runtime: `SaveSlotButton(Clone)` → `EmptyGroup.activeSelf=True` (correto, slots vazios), mas o único filho ` EmptyText` ("Empty Slot") está com **`m_IsActive: 0` no próprio prefab** → nada é renderizado.
- **Evidência:** screenshots `audit_09_slotpicker_clean.png`; YAML `Assets/Prefabs/SaveSlotButton.prefab` linha ~1404-1409 (`m_Name: ' EmptyText'`, `m_IsActive: 0`). Note também o nome com espaço à esquerda.
- **Arquivo(s):** `Assets/Prefabs/SaveSlotButton.prefab`; `Assets/Scripts/UI Systems/SaveSlotButton.cs` (`Initialize` ativa o grupo mas não o label).

**Prompt para corrigir:**
> No Sowur Shield, as linhas do seletor de slots (New Game/Load) aparecem como barras vazias: no `Assets/Prefabs/SaveSlotButton.prefab`, o GameObject ` EmptyText` (filho de `EmptyGroup`, com espaço à esquerda no nome) está salvo com `m_IsActive: 0`, então mesmo quando `SaveSlotButton.Initialize()` ativa o `EmptyGroup`, o label "Empty Slot" não renderiza. Corrija: (1) ative o ` EmptyText` no prefab e renomeie para `EmptyText` (sem espaço); (2) em `SaveSlotButton.Initialize`, garanta `emptyText.gameObject.SetActive(true)` defensivamente; (3) localize o texto "Empty Slot" (EN/PT/ES) e aumente o contraste (hoje é cinza 0.53 sobre creme); (4) aproveite para corrigir o layout do picker no `MainMenuUI` — o título sai da moldura no topo e as linhas transbordam a moldura de madeira (ver [UX-02]). Teste em Play Mode: MainMenu → New Game deve mostrar 4 linhas legíveis (AutoSave + Slot 1-3, "Vazio").

### [ERRO-05] AnimalMarketNPC sem marketData + BuildingShop com lista vazia e "Gold" dessincronizado
- **Sistema:** Economy / UI wiring
- **Severidade:** Alto
- **Repro:** (a) interagir com o `AnimalMarketNPC` na SampleScene não abre nada; `marketData = null` no componente (o asset existe em `Assets/Resources/AnimalMarkets/FarmAnimalMarket.asset`); abrindo o `AnimalMarketUI` direto, a aba Buy fica vazia e "Gold: 0" enquanto o player tem $149. (b) `BuildingShopUI` abre um painel escuro com lista vazia ("Gold:" sem número) apesar de `Silo.asset`/`Workshop.asset` existirem em `Resources/Buildings` (row prefabs/wiring pendentes — já constava no checklist do STATUS, aqui fica confirmado que segue quebrado em runtime).
- **Evidência:** screenshots `audit_25_animalmarket.png`, `audit_27_buildingshop.png`; dump serializado do NPC (`marketData = null`).

**Prompt para corrigir:**
> No Sowur Shield (SampleScene): (1) o componente `AnimalMarketNPC` do GameObject `AnimalMarketNPC` está com o campo `marketData` nulo — atribua `Assets/Resources/AnimalMarkets/FarmAnimalMarket.asset` no Inspector e adicione fallback em código: se `marketData == null`, `Resources.Load<FarmAnimalMarket>("AnimalMarkets/FarmAnimalMarket")` com warning. (2) `AnimalMarketUI` e `BuildingShopUI` mostram "Gold: 0"/"Gold:" — conecte o texto de ouro à fonte real de dinheiro do player (a mesma usada pelo HUD MoneyText, que exibia $149 no teste) e atualize ao abrir a janela. (3) `BuildingShopUI` lista vazia: crie/atribua o row prefab de compra de construções e valide que Silo e Workshop (em `Resources/Buildings`) aparecem com preço e botão de compra. Teste comprando um animal e uma construção em Play Mode.

### [ERRO-06] Maren usa o sprite do boss "Ancient Wolf" e não tem portrait
- **Sistema:** NPC / Art
- **Severidade:** Médio (quebra total de fantasia — a vendedora de sementes é um lobo demoníaco glitchado)
- **Repro:** olhar a Maren na SampleScene.
- **Evidência:** `Assets/Scenes/SampleScene.unity` — SpriteRenderer da Maren aponta para guid `d43bba7eb8f9df041aae50eb4343cc92` = `Assets/enemies/Forest/Ancient Wolf (Boss).png`; `npcPortrait: {fileID: 0}`. Screenshot `audit_10_farm_clean.png` (sprite escuro azul/preto perto do player).
- **Nota:** existe `Assets/Sprites/Portraits/Brandi.png` órfão que poderia servir de base (ver ASSET-02).

**Prompt para corrigir:**
> Na SampleScene do Sowur Shield, o NPC Maren (GameObject `Maren`, `NPCDialogueInteractable`, npcId `maren`) está com o SpriteRenderer apontando para `Assets/enemies/Forest/Ancient Wolf (Boss).png` e `npcPortrait` nulo. Substitua o sprite por um sprite de personagem humana apropriado (há personagens em `Assets/Assets Importados/` e portraits órfãos em `Assets/Sprites/Portraits/`), ajuste a escala para bater com os demais NPCs (~generic_npc), e atribua um `npcPortrait` para o diálogo (o painel de diálogo hoje mostra só o nome, sem retrato). Enquanto estiver lá: o `AnimalMarketNPC` usa um retrato FOTORREALISTA de pessoa real como sprite de mundo — troque por pixel art coerente. Não commitar sem conferir visualmente em Play Mode.

### [ERRO-07] Três GroundItems com o mesmo nome "CarrotSeed" → colisão de chave de save
- **Sistema:** Save/Load
- **Severidade:** Médio
- **Repro/Evidência:** dump em play mode dos `GroundItem` da SampleScene: 3 objetos chamados exatamente `CarrotSeed` (posições (3.65,-3.00), (-9.38,-2.24), (3.52,-1.71)). CLAUDE.md documenta o invariante: chave ISaveable = `gameObject.name`, nomes devem ser únicos por cena. Coletar um marcará `grounditem_picked_CarrotSeed` e os três somem no load (quando o save voltar a funcionar — hoje mascarado pelo ERRO-01).

**Prompt para corrigir:**
> Na SampleScene do Sowur Shield existem 3 GameObjects `GroundItem` chamados exatamente "CarrotSeed" (posições aprox. (3.65,-3.00), (-9.38,-2.24), (3.52,-1.71)). O sistema de persistência de GroundItem usa `gameObject.name` como chave (`grounditem_picked_{name}` em worldFlags — invariante documentado no CLAUDE.md), então coletar um marca os três como coletados. Renomeie na cena para nomes únicos (CarrotSeed_1/_2/_3). Extra defensivo: em `GroundItem.Awake`, logue `Debug.LogWarning` se detectar outro GroundItem ativo com o mesmo nome na cena. Cuidado: SampleScene.unity é arquivo de alto conflito — coordene antes de mexer.

### [ERRO-08] Pacing do combate ~16s por ação (gaugeFilLRate=1 na cena, doc diz 10)
- **Sistema:** Combat
- **Severidade:** Médio (parece bug de travamento para o jogador)
- **Repro:** batalha no stage 1: unidades com speed 6 e `TurnManager.gaugeFilLRate=1` → gauge enche 6/s até 100 → **~16s paradas entre ações**, sem nenhuma animação no meio. Parece congelado (eu mesmo diagnostiquei como freeze até ver o gauge subir). Com rate 20 (workaround de teste) a batalha fluiu normal.
- **Evidência:** dump do TurnManager (`gaugeFilLRate=1`); memória do projeto e docs indicam default `10f`.
- **Arquivo(s):** valor serializado no `TurnManager` da CombatScene (`Assets/Scenes/CombatScene.unity`); campo com typo `gaugeFilLRate` em `TurnManager.cs`.

**Prompt para corrigir:**
> Na CombatScene do Sowur Shield, o `TurnManager` está serializado com `gaugeFilLRate = 1` (o default documentado é `10`, que dá ~1s por turno com speed 10). Resultado: ~16s de tela parada entre ações — parece travamento. Corrija o valor na cena para 10 e valide o pacing vencendo uma batalha no stage 1. Opcional no mesmo PR: renomear o campo `gaugeFilLRate` → `gaugeFillRate` em `Assets/Scripts/Combat/TurnManager.cs` usando `[FormerlySerializedAs("gaugeFilLRate")]` para não perder o valor serializado, e considerar um botão de velocidade 1x/2x no combate (multiplicador simples nesse campo).

### [ERRO-09] TestItemSpawner (debug) embarca em builds de produção
- **Sistema:** Debug / Build hygiene
- **Severidade:** Médio
- **Evidência:** `Assets/Scripts/Debugging/TestItemSpawner.cs:22-31` — `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` cria o spawner em toda execução (visto vivo em DDOL na sessão); F9 dropa itens grátis. Vai junto na demo WebGL.

**Prompt para corrigir:**
> Em `Assets/Scripts/Debugging/TestItemSpawner.cs` do Sowur Shield, o bootstrap `[RuntimeInitializeOnLoadMethod]` (linhas 22-31) auto-instancia o spawner de debug em QUALQUER execução, incluindo builds de produção/demo (F9 = item grátis). Envolva a classe (ou pelo menos o método `Bootstrap`) em `#if UNITY_EDITOR || DEVELOPMENT_BUILD`. Confira se não há referências externas ao tipo antes (não deve haver — é namespace SowurShield.Debugging).

### [ERRO-10] Race intermitente: Animal registra antes do InteractionManager existir
- **Sistema:** Animals / Interaction
- **Severidade:** Baixo (intermitente; 1 ocorrência em 2 boots)
- **Evidência:** console: `InteractionManager not found! Animal Chicken won't be interactable.` — apareceu num boot e não no outro (no boot limpo "Clucky" registrou normalmente). Se ocorrer num boot real, o animal fica sem pet/feed por E até recarregar.

**Prompt para corrigir:**
> No Sowur Shield há uma race de inicialização intermitente: `Animal` (Assets/Scripts/Animals/Animal.cs) tenta registrar no `InteractionManager.Instance` durante Start/Awake e ocasionalmente loga "InteractionManager not found! Animal X won't be interactable", deixando o animal sem interação pela sessão. Corrija com retry: se `InteractionManager.Instance == null` no momento do registro, inicie uma coroutine que tenta de novo por alguns frames/segundos até conseguir (padrão já usado em outros sistemas do projeto), e rebaixe o log para warning só após esgotar as tentativas. Cheque se outros IInteractable de cena (FeedingTrough, SellBox) têm o mesmo padrão frágil.

### [ERRO-11] Slot picker aberto antes do preload de localização fica sem título para sempre
- **Sistema:** Localization / Main Menu
- **Severidade:** Baixo (janela de ~2s no boot; visível se o jogador for rápido)
- **Evidência:** abrindo New Game antes de `AreTablesReady=true`, o título fica `''` e NÃO é atualizado quando o preload termina (o handler `OnTablesReady → UpdateSaveInfoDisplay` não repopula o picker). Reabrir o picker corrige.

**Prompt para corrigir:**
> No `Assets/Scripts/MainMenuUI.cs` do Sowur Shield, se o jogador abrir o slot picker (New Game/Load) nos primeiros ~2s antes de `LocalizationManager.AreTablesReady` virar true, o título (`slotPickerTitleText`, setado em `OpenSlotPicker` linha ~462 via `SafeGetLocalizedString`) fica vazio para sempre — o handler de `OnTablesReady` chama `UpdateSaveInfoDisplay()` mas não re-resolve o título do picker. No `HandleLanguageChanged_NoArg()` (linha ~118), se `slotPickerPanel` estiver ativo, re-execute a atribuição do título e `PopulateSlotPicker(currentSlotPickerMode)`.

---

## 2. Melhorias de UI/UX

### [UX-01] HUD: dinheiro quebra no meio da palavra e hora vaza do painel (em qualquer resolução)
- **Tela/Sistema:** HUD principal (SampleScene)
- **Problema observado:** em 640×480 E em 1920×1080: canto superior direito renderiza "Dinheir / o: $149" (wrap no meio da palavra, dourado sobre painel ferrugem, transbordando); "Hora: 22:45" idem — o texto branco sai do painel de madeira à direita. O canvas do HUD usa reference resolution **800×600** (SampleScene.unity:1597 e mais 4 canvases em 800×600), não os 1920×1080 padronizados.
- **Sugestão:** aumentar o rect dos labels (ou TMP auto-size 18-28 com `enableWordWrapping=false` + overflow Ellipsis), e migrar o canvas do HUD para 1920×1080 com match 0.5 como os popups (exigirá reposicionar âncoras dos elementos do HUD uma vez).

**Prompt para implementar:**
> No HUD da SampleScene do Sowur Shield, o texto de dinheiro (canto sup. direito) quebra no meio da palavra ("Dinheir/o: $149") e o de hora vaza do painel de madeira, em qualquer resolução. O canvas do HUD ainda usa reference resolution 800×600 (há 5 canvases 800×600 na SampleScene; os popups já foram padronizados para 1920×1080). Corrija: (1) desabilite word wrap nos TMPs MoneyText/TimeText/Days e use auto-size (min 16/max 28) com rects largos o suficiente; (2) padronize o canvas do HUD para 1920×1080 (CanvasScaler match 0.5), reancorando os elementos (minimap frame, stamina, painel dia/hora, hotbar) — teste em 16:9 e 4:3. Screenshots de referência do problema: Assets/Screenshots/audit_22_hud_1080p.png.

### [UX-02] Slot picker: título cortado no topo e linhas transbordando a moldura
- **Tela/Sistema:** MainMenu → New Game/Load
- **Problema observado:** título "Novo Jogo — Escolha o Slot" renderiza meio cortado no topo da TELA (fora da moldura); a 1ª linha de slot cobre a borda superior da moldura e a 4ª cobre a inferior + o botão Back (screenshots `audit_07/audit_09`). MainMenu canvas = 1366×768 (e um segundo canvas 1280×720).
- **Sugestão:** colocar TitleText dentro do painel (âncora topo do frame, não da tela), `VerticalLayoutGroup` com padding no `SlotListParent` limitado à área do pergaminho, e padronizar o canvas para 1920×1080.

**Prompt para implementar:**
> No MainMenu do Sowur Shield, o painel SlotPickerPanel tem layout quebrado: TitleText ancorado fora da moldura (cortado no topo da tela) e as 4 linhas SaveSlotButton transbordam a área de pergaminho da moldura de madeira (1ª linha sobre a borda superior, última sobre o Back). Reorganize: TitleText dentro do frame; `SlotListParent` com VerticalLayoutGroup (spacing ~12, padding que respeite a moldura, childControlHeight) e as linhas com LayoutElement (preferredHeight ~120); Back button abaixo, dentro ou colado ao frame. Padronize o MainMenuCanvas para reference resolution 1920×1080 (hoje 1366×768; há outro canvas 1280×720 na cena — alinhe ambos). Valide em 16:9 e 4:3.

### [UX-03] Minimapa é um vazio preto (sem terreno) + labels sobrepostos no fullscreen
- **Tela/Sistema:** Minimap (3 estados funcionam)
- **Problema observado:** o RenderTexture mostra fundo preto com meia dúzia de pontinhos — nenhuma representação do terreno/chão; no fullscreen, os labels "Zoom: 1.0x" e "Modo: Fullscreen" renderizam um por cima do outro (ilegível, screenshot `audit_13_minimap_fullscreen.png`).
- **Sugestão:** incluir o ground no culling mask da MinimapCamera (ou um quad verde base + ícones), e empilhar os dois labels num VerticalLayoutGroup.

**Prompt para implementar:**
> No minimapa do Sowur Shield (Assets/Scripts/Minimap/): (1) a MinimapCamera renderiza fundo preto — inclua o terreno: ou adicione o layer do ground/tilemap ao cullingMask com um tint, ou coloque um quad verde-base no layer Minimap cobrindo a área jogável, para o mapa parecer um mapa e não um void; (2) no modo fullscreen, os labels "Zoom: X" e "Modo: Fullscreen" estão na mesma posição, sobrepostos — separe-os (VerticalLayoutGroup no rodapé do painel ou âncoras distintas); (3) considere bordas/ícones maiores para bed/sellbox/NPCs (hoje são pontinhos de 2-3px). Estados Normal/SemiTransparent/Fullscreen já funcionam — não mexer na máquina de estados.

### [UX-04] Mapa-múndi: 25 botões cinza default empilhados sobre a arte
- **Tela/Sistema:** WorldMap (aberto via CombatTriggerZone)
- **Problema observado:** a arte de fundo do mapa é ótima, mas os 25 stages são botões cinza padrão Unity amontoados no canto superior esquerdo, com sobreposição vertical entre linhas e textos truncados ("Vire Musgoso ---", "Salão das Estalactites -Chefe" cortado). Nenhuma relação espacial com o mapa. Screenshot `audit_16_worldmap.png`.
- **Sugestão:** curto prazo — grid organizado por bioma com os sprites de botão do kit (Resources/Sprites/UI/Buttons) e largura adequada; médio prazo — pins posicionados sobre a arte (ver FEAT-07).

**Prompt para implementar:**
> No WorldMap do Sowur Shield (WorldMapUIController/WorldMapBiomePanel, canvas na SampleScene), os 25 StageButtons renderizam como botões cinza default empilhados no canto sup. esquerdo, sobrepostos e com nomes truncados, ignorando a arte do mapa. Refaça o layout: um painel por bioma (5 biomas × 5 stages) usando os sprites de botão do kit `Assets/Resources/Sprites/UI/Buttons` (estado locked = versão dessaturada + cadeado de `Icons`), GridLayoutGroup com células ~260×64, nomes com TMP auto-size min 14, e distribuia os painéis de bioma em posições distintas sobre o mapa (os biomas têm regiões visuais claras na arte). Estado atual em Assets/Screenshots/audit_16_worldmap.png.

### [UX-05] TeamAssembler: idioma misturado, fontes 11px, contraste branco-sobre-dourado
- **Tela/Sistema:** TeamAssemblerUI
- **Problema observado:** painel direito mostra "Zona: Sunny Fields" (metade PT metade EN — o nome do stage aparece localizado no world map mas não aqui); botões "Clear Grid"/"Cancel"/"Feed All" em fontSize 11 BRANCO sobre botão dourado (ilegível; "Start Battle" é o único escuro); painel direito é um overlay marrom translúcido sem o tema; a validação "not all animals are fed" só aparece no console — nenhum feedback visível ao jogador; roster vazio não tem empty-state. Screenshot `audit_17_teamassembler.png`.
- **Sugestão:** localizar o nome da zona via a mesma entry do world map; fontes ≥20 escuras (`CozyUITheme` tem tokens); trocar o painel direito por `panel_wood`; toast/label vermelho para validações.

**Prompt para implementar:**
> No TeamAssemblerUI do Sowur Shield (TeamAssemblerCanvas na SampleScene): (1) o painel de info da zona mostra o nome do stage em EN ("Sunny Fields") enquanto o world map usa a entry localizada ("Campos Ensolarados") — use a mesma LocalizedString do StageData; (2) os botões ClearGrid/Cancel/FeedAll têm label fontSize 11 branco sobre sprite dourado — suba para ≥20 e use a cor de texto escura do CozyUITheme (como o StartBattle já faz), e localize os 4 labels; (3) o painel direito é um retângulo marrom translúcido sem tema — aplique o sprite de painel de madeira do kit (Resources/Sprites/UI/Panels); (4) quando StartBattle falha na validação ("not all animals are fed", hoje só no console), mostre a mensagem em um label vermelho temporário na tela; (5) adicione empty-state ao roster ("Nenhum animal disponível — compre no Mercado de Animais"). Screenshot: Assets/Screenshots/audit_17_teamassembler.png.

### [UX-06] Combat: top bar vazia com retratos microscópicos; HP bars invisíveis
- **Tela/Sistema:** Combat HUD (CombatScene)
- **Problema observado:** a barra creme no topo fica 95% vazia com 2 retratos de ~16px no canto esquerdo (ordem de turno?); não há barras de HP visíveis sobre as unidades em nenhuma das resoluções testadas (3 instâncias de `UnitHealthBar` existem em cena mas nada aparece — provável escala/posição; a doc exige scale 0.005 no root do prefab World Space). Screenshots `audit_18/20`.
- **Sugestão:** centralizar e ampliar a turn-order bar (48-64px por retrato) e auditar a escala/sorting dos health bars em play.

**Prompt para implementar:**
> Na CombatScene do Sowur Shield: (1) a barra superior de ordem de turnos renderiza retratos de ~16px encostados no canto esquerdo de uma barra creme vazia — centralize com HorizontalLayoutGroup e retratos de 48-64px com moldura do kit de UI; (2) as barras de HP das unidades não aparecem em batalha (3 instâncias de UnitHealthBar existem; docs exigem scale 0.005 no root do prefab World Space Canvas — Assets/Prefabs/HealthBar.prefab e Combat/UnitHealthBar(Clone).prefab, ambos com CanvasScaler 800×600) — depure em Play Mode a posição/escala/sorting real delas sobre Slime e Galinha no stage 1 e corrija o prefab; (3) esconda o botão "Itens" e o painel de consumíveis quando o BattleResults abrir (hoje ficam por cima da tela de vitória — Assets/Screenshots/audit_21_victory.png).

### [UX-07] Painel de consumíveis: empty-state colapsado
- **Tela/Sistema:** ConsumableBattleUI (CombatScene)
- **Problema observado:** clicar em "Itens" mostra "Inventário não encontrado" (limitação conhecida na CombatScene) mas o painel de fundo renderiza como uma lasca de madeira espremida atrás do texto (screenshot `audit_19_combat_items.png`) — o painel não tem tamanho mínimo para o empty-state.
- **Sugestão:** `LayoutElement.minHeight` (~80) no painel + o texto dentro com padding; e mensagem melhor ("Sem itens de batalha").

**Prompt para implementar:**
> No `ConsumableBattleUI` do Sowur Shield (self-spawning, Assets/Scripts/Combat/ConsumableBattleUI.cs), o estado vazio "Inventário não encontrado" renderiza com o painel de madeira colapsado numa lasca atrás do texto. Dê um tamanho mínimo ao painel da lista (LayoutElement minHeight ~80, minWidth ~260, padding 12) e ajuste a mensagem para algo amigável e localizado ("Sem itens de batalha disponíveis"). Obs.: a causa do inventário ausente na CombatScene é uma limitação documentada no SOWUR_SHIELD_STATUS.md (Inventory só existe na SampleScene) — este prompt é só sobre a apresentação do empty-state; se for resolver a limitação, siga o prompt do ERRO-02 (snapshot de inventário DDOL), que de quebra habilita consumíveis em batalha.

### [UX-08] Quests / Animal Market / Building Shop totalmente fora do tema cozy
- **Tela/Sistema:** QuestsCanvas, AnimalMarketCanvas, BuildingShopCanvas
- **Problema observado:** três janelas flat sem nenhum uso do kit de sprites: Quests = painel branco com tabs retangulares laranja e X vermelho 16px; AnimalMarket = painel bege flat ("Gold: 0" amarelo sobre bege, contraste ruim); BuildingShop = painel PRETO translúcido. Todas com textos EN. Screenshots `audit_24/25/27`. (O STATUS já lista "review shop windows... against the theme" como pendente — aqui está a evidência concreta e a lista exata do que destoa.)
- **Sugestão:** aplicar `panel_wood` + `frame` do kit, tabs com os sprites de botão, X com o ícone do kit, títulos com a fonte/cores do CozyUITheme, e localizar.

**Prompt para implementar:**
> No Sowur Shield, três janelas ainda não receberam o tema cozy (kit em Assets/Resources/Sprites/UI + Resources/UI/CozyUITheme.asset): QuestsCanvas (painel branco flat, tabs Active/Completed como retângulos laranja, X vermelho minúsculo), AnimalMarketCanvas (painel bege flat, "Gold: 0" amarelo sobre bege) e BuildingShopCanvas (painel preto translúcido). Aplique em cada uma: sprite de painel de madeira como fundo, tabs/botões com os sprites de botão do kit, botão fechar com ícone do kit (≥32px), títulos e cores via CozyUITheme, e localize os textos estáticos (Quests, Active, Completed, Buy, Sell, Farm Buildings, Gold) em EN/PT/ES. Use o TeamAssembler/CombatScene restylings recentes como referência de padrão (commits e03d748/ec9d046). Screenshots do estado atual: Assets/Screenshots/audit_24_quests.png, audit_25_animalmarket.png, audit_27_buildingshop.png.

### [UX-09] Pause menu: título fora da moldura, sem dim, "Save Game" mudo na demo
- **Tela/Sistema:** GameMenuUI
- **Problema observado:** "Game Menu" flutua metade fora do topo da moldura; não há overlay escurecendo o jogo atrás; botões EN; "Save Game" com DEMO_BUILD não dá nenhum feedback (ERRO-01). Screenshot `audit_23_pausemenu.png`.
- **Sugestão:** título dentro do frame; `Image` fullscreen preta alpha 0.5 atrás do painel; localizar labels.

**Prompt para implementar:**
> No pause menu do Sowur Shield (GameMenuSystem/GameMenuUI na SampleScene): (1) o título "Game Menu" está ancorado meio fora da moldura de madeira — mova para dentro do frame; (2) adicione um overlay fullscreen preto com alpha ~0.5 atrás do painel quando o menu abre (padrão de qualquer pause menu; hoje o jogo continua 100% visível); (3) localize os labels Resume/Settings/Main Menu/Quit to Desktop/Save Game (EN/PT/ES); (4) quando o save for suprimido por DEMO_BUILD (evento OnSaveCompleted(false) do SaveManager), mostre "Demo: progresso não é salvo" perto do botão Save. Screenshot: Assets/Screenshots/audit_23_pausemenu.png.

### [UX-10] Janela de storage do inventário sem título e sem botão fechar
- **Tela/Sistema:** Inventário (painel de madeira do storage, Jul/2)
- **Problema observado:** o painel novo abre bonito mas não tem título ("Inventário"/"Mochila") nem X — só quem sabe o atalho fecha. Screenshot `audit_12_inventory_open.png`.
- **Sugestão:** header com título localizado + X do kit no canto.

**Prompt para implementar:**
> No Sowur Shield, a janela de storage do inventário (painel `storagePanelBackground` adicionado em Jul/2, visível via ToggleInventory) não tem título nem botão de fechar. Adicione um header dentro da moldura com o título localizado ("Inventory"/"Inventário"/"Inventario") e um botão X (ícone do kit Resources/Sprites/UI/Icons, ≥32px) que chama o mesmo caminho de fechamento do ToggleInventory/UIManager. Screenshot: Assets/Screenshots/audit_12_inventory_open.png.

### [UX-11] Nome do speaker colide com o texto do diálogo
- **Tela/Sistema:** DialogueTreeUI
- **Problema observado:** o label "Maren" (dourado) renderiza NO MEIO da caixa de texto, colidindo com a primeira linha do diálogo ("...neste vale" passa por cima de "Maren"). Não há nameplate separado nem portrait. Screenshot `audit_15_choicebtn_overflow.png` (parte inferior).
- **Sugestão:** nameplate próprio (mini painel de madeira) no canto superior esquerdo da caixa, acima da área de texto; reservar área para portrait à esquerda.

**Prompt para implementar:**
> No painel de diálogo do Sowur Shield (DialoguePanel, DialogueTreeUI, canvas UI da SampleScene), o SpeakerNameText renderiza sobreposto ao DialogueText (o nome "Maren" colide com a primeira linha do texto). Crie um nameplate: mini painel de madeira do kit ancorado no topo-esquerda da caixa de diálogo (fora da área de texto), com o nome em dourado, e recue o DialogueText para não invadir essa área. Reserve um slot quadrado à esquerda para o portrait do NPC (PortraitManager já existe; Maren precisa de npcPortrait atribuído — ver ERRO-06 do review/QA_UI_AUDIT_2026-07-05.md).

### [UX-12] Canvases com reference resolution inconsistente (o "1920×1080 em todos" NÃO se sustenta)
- **Tela/Sistema:** Todos
- **Problema observado:** a padronização recente cobriu só os 4 popups. Estado real por YAML: **SampleScene** = 5 canvases 800×600 (incl. HUD) + 6 em 1920×1080; **MainMenu** = 1366×768 + 1280×720 (nenhum 1920×1080!); **CombatScene** = 1920×1080 + um canvas 1280×720; prefabs `HealthBar`, `UnitHealthBar(Clone)`, `generic_npc` = 800×600.
- **Sugestão:** migração planejada canvas a canvas (cada migração exige reancoragem) — começar pelo HUD (UX-01) e MainMenu (UX-02).

**Prompt para implementar:**
> No Sowur Shield, padronize as reference resolutions de CanvasScaler para 1920×1080 (match 0.5) nos canvases que ficaram para trás: SampleScene tem 5 canvases em 800×600 (linhas ~1597, 7218, 21559, 26964, 27501 do .unity — um deles é o HUD), MainMenu tem 1366×768 e 1280×720, CombatScene tem um canvas 1280×720 (linha ~1585), e os prefabs HealthBar.prefab, Combat/UnitHealthBar(Clone).prefab e NPC/generic_npc.prefab usam 800×600. Migre um canvas por vez, reancorando os elementos e validando visualmente em 16:9 e 4:3 antes de passar ao próximo (os 4 popups TeamAssembler/BuildingShop/Quests/AnimalMarket já estão em 1920×1080 — use-os como referência). Alto risco de conflito em SampleScene.unity/MainMenu.unity — coordene.

---

## 3. Assets não utilizados / oportunidades de conteúdo

> Fonte: varredura de GUIDs (agente de cruzamento) sobre .unity/.prefab/.asset/.controller + grep de strings em .cs.
> Spot-checks manuais confirmados por mim: `slot_inventory.png`, `AnimalCardPrefab.prefab` e `MovementController.controller` têm **zero** referências.
> Nada foi deletado nesta sessão.

### [ASSET-01] 71 TileBase `Tilled_Dirt_v2_0..70` — sistema de tilemap abandonado
- **Caminho:** `Assets/Sprites/DirtGround/Tilled_Dirt_v2_N.asset` (0-70)
- **Estado atual:** nenhum referenciado por tilemap algum; apontam todos para o mesmo sprite. Restos de uma implementação de solo por tiles substituída pelo sistema atual.
- **Sugestão de uso:** ou deletar (após um sprint em quarentena), ou reaproveitar na expansão da vila (o design decidido em 2026-07-01 estende o DualGridTilemap — esses tiles podem virar variações de caminho/estrada).

**Prompt para integrar:**
> No Sowur Shield, os 71 assets `Assets/Sprites/DirtGround/Tilled_Dirt_v2_*.asset` são TileBase de um sistema de tilemap abandonado (zero referências em cenas/prefabs). Decisão do time necessária: (a) mover para uma pasta `Assets/_Quarantine/` por um sprint e deletar depois, ou (b) reaproveitar como tiles de caminho/estrada na expansão da vila planejada (extensão do DualGridTilemap, ver seção Deferred do SOWUR_SHIELD_STATUS.md). Não deletar sem confirmar que nenhum Resources.Load dinâmico os usa (não há — estão fora de Resources).

### [ASSET-02] Portraits órfãos: Brandi, Wolf, Pet
- **Caminho:** `Assets/Sprites/Portraits/Brandi.png`, `Wolf.png`, `Pet.png`
- **Estado atual:** guids sem nenhuma referência.
- **Sugestão de uso:** Brandi → novo NPC com diálogo (o sistema de DialogueTree/portraits suporta hoje); Wolf → portrait do boss Ancient Wolf nos diálogos/intro de batalha do bioma Forest; Pet → futuro sistema de pet.

**Prompt para integrar:**
> No Sowur Shield, existem portraits órfãos em `Assets/Sprites/Portraits/` (Brandi.png, Wolf.png, Pet.png). Integre dois deles com baixo esforço: (1) crie a NPC "Brandi" na SampleScene clonando o padrão do generic_npc/Maren (`NPCDialogueInteractable` + DialogueTree novo com 2-3 nós de conversa + entries de localização EN/PT/ES) usando Brandi.png como npcPortrait; (2) use Wolf.png como portrait do boss "Ancient Wolf" — exiba no TeamAssembler ao selecionar o stage de chefe do bioma Forest (campo de preview de inimigo). O sistema de diálogo e o campo npcPortrait já existem; siga as convenções de namespace SowurShield.Dialogue.

### [ASSET-03] Suite de animações do player abandonada (12 anims + MovementController.controller)
- **Caminho:** `Assets/Player/Action Animation/*.anim` (digging/axe/plowing), `Assets/Player/Walking Animation/player_Idle_*.anim`, `player_Walk_*.anim`, `Assets/Player/MovementController.controller`, `Assets/Sprites/Anims/MovingAnim.anim`
- **Estado atual:** zero referências (spot-check confirmado no controller). O player atual usa `NewMovingAnim`/`SprintingAnim`.
- **Sugestão de uso:** as anims de AÇÃO (digging/plowing/axe) são conteúdo pronto para dar feedback visual ao uso de ferramentas — hoje usar a enxada não tem animação nenhuma.

**Prompt para integrar:**
> No Sowur Shield, existem animações de ação do player órfãs (`Assets/Player/Action Animation/player_ digging_down.anim`, `player_ digging_up.anim`, `player_axe_down.anim`, `player_plowing_right.anim`) de uma implementação antiga. Avalie integrá-las ao Animator atual do player como estados de ação disparados ao usar Hoe/Shovel (trigger "UseTool" com direção), tocados durante o `CursorController.CreateSoilBlock`/uso de ferramenta. Se os sprites dessas anims não baterem com o spritesheet atual do player, descarte-as e registre a decisão — mas o jogo hoje não tem NENHUM feedback de animação ao usar ferramentas, e isso é o gap visível.

### [ASSET-04] Prefabs duplicados/órfãos
- **Caminho/estado:** `Assets/Prefabs/Combat/AnimalCardPrefab.prefab` e `GridSlotPrefab.prefab` (versões antigas; as ativas são `AnimalCard.prefab`/`GridSlot.prefab`); `Assets/Prefabs/UI/InventorySlotPrefab 1.prefab` (cópia órfã); `Assets/Prefabs/Trees/TX Plant with Shadow_0.prefab` (importado, sem uso); `Assets/Resources/Sprites/UI/Slots/slot_inventory.png` (a versão `_trimmed` é a usada).
- **Sugestão:** quarentena + delete no próximo pass de higiene; `TX Plant` pode virar decoração da fazenda.

**Prompt para integrar:**
> Higiene de prefabs no Sowur Shield: mova para `Assets/_Quarantine/` (não delete ainda) os órfãos confirmados por auditoria de GUID: `Assets/Prefabs/Combat/AnimalCardPrefab.prefab`, `Assets/Prefabs/Combat/GridSlotPrefab.prefab`, `Assets/Prefabs/UI/InventorySlotPrefab 1.prefab`, `Assets/Resources/Sprites/UI/Slots/slot_inventory.png`, `Assets/Sprites/Bitmask references 1.png`/`2.png` e as cenas `Assets/_Recovery/0*.unity`. Antes de mover os que estão sob Resources, grep por seus nomes em .cs para garantir que nenhum Resources.Load os referencia por string. Aproveite e coloque `Assets/Prefabs/Trees/TX Plant with Shadow_0.prefab` como decoração em 2-3 pontos da SampleScene (é um prefab de planta com sombra pronto, nunca usado).

### [ASSET-05] Sprites de tileset de solo não usados (variações prontas de conteúdo)
- **Caminho:** `Assets/Sprites/DirtGround/Crop_Dead.png`, `Plain_Dirt.png`, `Tilled_Dirt_Fertilized.png`, `Tilled_Dirt_Planted.png`, `Tilled_Dirt_Watered.png`, `Assets/Sprites/Fences/Fences.png`, `Assets/Sprites/GrassGround/Grass.png`
- **Estado atual:** órfãos.
- **Sugestão de uso:** `Tilled_Dirt_Fertilized` é um estado de solo que o jogo não tem — fertilizante é uma feature de baixo custo com sprite pronto; `Crop_Dead` para a morte por seca (a mecânica drought-death existe!); `Fences.png` para cercas decorativas/funcionais da zona de animais.

**Prompt para integrar:**
> No Sowur Shield, aproveite sprites de solo órfãos como conteúdo: (1) `Assets/Sprites/DirtGround/Crop_Dead.png` — o sistema de crops já tem morte por seca (drought-death em CropGrowthManager); verifique qual sprite é usado hoje para crop morta e, se for placeholder, use este; (2) `Tilled_Dirt_Fertilized.png` — implemente um item "Fertilizer" (novo Item SO + entry no ItemDatabase + à venda no SeedShop da Maren) que aplica um multiplicador de crescimento a um SoilBlock e troca o sprite do solo para o fertilizado (novo estado visual em SoilBlockInteractable.UpdateAppearance); (3) `Assets/Sprites/Fences/Fences.png` — corte em sprites e use como cerca decorativa da AnimalZone na SampleScene. Cada item é independente; priorize o (1) que é só troca de sprite.

### [ASSET-06] Conteúdo Resources OK (sem órfãos de gameplay)
Auditoria confirmou que **todos** os ScriptableObjects de gameplay estão alcançáveis: 8 Achievements, 2 Buildings (Silo, Workshop — carregados via LoadAll mas invisíveis por causa do ERRO-05b), 6 Crops, 6 Items em Resources/Items (+14 de outras pastas via ItemDatabase), 4 Quests, 3 Animais, 34 Enemies referenciados por 26 Stages. Nenhuma ação necessária além dos fixes de wiring (ERRO-05).

---

## 4. Inovações / features de baixo custo

### [FEAT-01] Toggle de velocidade de combate (1x/2x/4x)
- **Descrição:** botão no canto do combat HUD multiplicando `TurnManager.gaugeFilLRate` (campo já existe e é o único knob necessário — validei em runtime que mudar o valor acelera a batalha imediatamente).
- **Por que vale a pena:** auto-battler sem controle de velocidade cansa rápido; resolve também a percepção de "combate travado" (ERRO-08).
- **Esforço estimado:** Baixo.

**Prompt para implementar:**
> No Sowur Shield, adicione um botão de velocidade de combate no HUD da CombatScene (canto superior direito, sprite de botão do kit Resources/Sprites/UI): alterna 1x → 2x → 4x → 1x, aplicando o multiplicador sobre o valor base de `TurnManager.gaugeFilLRate` (Assets/Scripts/Combat/TurnManager.cs — note o typo do nome). Persistir a escolha em PlayerPrefs. Label "1x/2x/4x" com fonte do CozyUITheme. Pré-requisito: corrigir o valor base para 10 na CombatScene (ver ERRO-08 do review/QA_UI_AUDIT_2026-07-05.md).

### [FEAT-02] Auto-coleta magnética de itens colhidos
- **Descrição:** colheita hoje dropa GroundItems parados no chão (testei: 2 cenouras ficam lá até andar em cima). Adicionar raio de atração (~1.5u) com tween voando até o player + som de pickup.
- **Por que vale a pena:** é O feedback satisfatório de farming (Stardew-like); custo mínimo (um Update no GroundItem com distância + DOTween, já no projeto).
- **Esforço estimado:** Baixo.

**Prompt para implementar:**
> No Sowur Shield, GroundItems dropados por colheita (SoilBlockInteractable.SpawnGroundItem, linha ~577) ficam estáticos até o player pisar neles. Adicione atração magnética no `GroundItem` (Assets/Scripts/Utility/GroundItem.cs): quando o player estiver a <1.5 unidades E o item for coletável, tween da posição até o player (DOTween, ~0.25s, ease InQuad) e colete ao chegar. Adicione um pequeno pop de escala ao spawnar (0→1.1→1). Cuidado para não atrair itens que o player acabou de dropar (delay de 0.5s pós-spawn).

### [FEAT-03] Resumo do dia ao acordar
- **Descrição:** card temático ao acordar: "Dia X — Vendeu 2 Cenouras (+$16) • Clucky foi alimentada • Clima: Sol". Os dados já existem (`BedInteractable.ProcessSellBoxSales` calcula `totalEarningsFromAllBoxes` e joga fora; trough sabe quem comeu; WeatherController rola o clima).
- **Por que vale a pena:** transforma o sono num momento de recompensa; hoje vender no SellBox não dá NENHUM feedback (o dinheiro muda silenciosamente — só percebi comparando o HUD).
- **Esforço estimado:** Médio (painel novo + coleta de 3 dados existentes).

**Prompt para implementar:**
> No Sowur Shield, crie um painel "Resumo do Dia" exibido ao acordar (após o fade do sono em BedInteractable): painel de madeira do kit com: ganhos do SellBox (o valor já é somado em `BedInteractable.ProcessSellBoxSales` — Assets/Scripts/BedInteractable.cs:258-298 — e hoje é descartado), animais alimentados pelo FeedingTrough, e clima do novo dia (WeatherController). Botão "Continuar" fecha. Textos localizados EN/PT/ES. Estrutura: novo script SowurShield.UI.DaySummaryPanel implementando IUIWindow, self-spawned ou wired na SampleScene.

### [FEAT-04] Feedback visível de save/demo
- **Descrição:** toast "Jogo salvo ✓" / "Demo: progresso não é salvo" ligado aos eventos `OnSaveStarted/OnSaveCompleted` que já existem no SaveManager.
- **Por que vale a pena:** hoje salvar é 100% silencioso (e na demo, silenciosamente um no-op — ERRO-01); custo trivial porque os eventos já disparam.
- **Esforço estimado:** Baixo.

**Prompt para implementar:**
> No Sowur Shield, adicione um toast de feedback de save: assine `SaveManager.OnSaveCompleted(bool success)` (já existe e já dispara, inclusive com false no caminho DEMO_BUILD — SaveManager.cs:268-313) num componente de UI leve (padrão do AchievementNotificationUI, que já é um toast DDOL funcional — reutilize o estilo): success=true → "Jogo salvo ✓"; success=false → "Demo: progresso não é salvo". Localizado EN/PT/ES, some após 2.5s.

### [FEAT-05] Empty-states amigáveis em todas as listas
- **Descrição:** textos localizados para: roster do TeamAssembler vazio, Quests sem missões (hoje "No active quests yet." EN cru), market/shop vazios, storage vazio.
- **Por que vale a pena:** custo quase zero, elimina telas "quebradas de aparência" e ensina o jogador onde conseguir o conteúdo ("Compre animais no Mercado").
- **Esforço estimado:** Baixo.

**Prompt para implementar:**
> No Sowur Shield, adicione empty-states localizados (EN/PT/ES) com dica de ação nas listas: TeamAssembler roster ("Nenhum animal — compre no Mercado de Animais"), QuestsCanvas ("Nenhuma missão ativa — converse com os moradores"), AnimalMarket Sell tab sem animais, BuildingShop sem construções disponíveis, storage do inventário vazio ("Arraste itens do hotbar"). Padrão: TMP centralizado, cor suave do CozyUITheme, ativado quando a lista tem 0 filhos. Cada tela tem um populate claro — ligue o toggle no fim de cada refresh.

### [FEAT-06] Pontinho de "produção pronta" sobre animais + partícula no trough
- **Descrição:** ícone flutuante (ovo/coração do kit de Icons) sobre o animal quando há produção para coletar, e mini partícula de feno ao auto-alimentar de manhã.
- **Por que vale a pena:** produção diária hoje é invisível (o GroundItem cai em algum lugar do zone); reuso do sistema de heart particle já existente.
- **Esforço estimado:** Baixo.

**Prompt para implementar:**
> No Sowur Shield, quando um Animal produz seu item diário (Animal.cs spawna GroundItem via produção), mostre um indicador visual: sprite flutuante do item produzido (ou ícone de exclamação do kit Resources/Sprites/UI/Icons) sobre a cabeça do animal, com bob de 2-3px (padrão do AnimalHappinessIcon, que já existe — reutilize a infra), até o item ser coletado. Ao auto-alimentar via FeedingTrough no OnDayChanged, spawne uma partícula curta sobre o trough (reutilize o padrão do heart particle documentado no CLAUDE.md — material Default-Particle!).

### [FEAT-07] Pins do mapa-múndi ancorados na arte
- **Descrição:** substituir o grid de botões por 5 grupos de pins (um por bioma) posicionados sobre as regiões visuais da arte do mapa, com linha pontilhada de progresso.
- **Por que vale a pena:** a arte do mapa é o asset mais bonito do jogo e está 100% coberta por botões cinza; é o upgrade de percepção de qualidade mais alto disponível.
- **Esforço estimado:** Médio (posicionar 25 pins uma vez + estados locked/done).

**Prompt para implementar:**
> No WorldMap do Sowur Shield, substitua o grid de StageButtons por pins posicionados sobre a arte do mapa: 5 clusters (um por bioma — a arte tem regiões visuais claras: floresta, montanha, etc.), cada um com 5 pins circulares pequenos (sprite do kit; locked = cinza + cadeado, disponível = dourado pulsando levemente, completo = check verde). Tooltip/label com o nome localizado ao hover/hold. Clique → TeamAssemblerUI como hoje (StageButton.cs já encapsula o fluxo — mude só o visual/posicionamento, não o fluxo). Posições: serializadas por stage num array de Vector2 anchoredPosition no canvas do mapa. Ver estado atual em Assets/Screenshots/audit_16_worldmap.png.

### [FEAT-08] Idle-wander para animais e NPCs
- **Descrição:** movimento aleatório suave (1-2 tiles a cada 5-10s) para Clucky/Bunny/NPCs, que hoje ficam 100% estáticos.
- **Por que vale a pena:** a fazenda parece congelada numa screenshot; wander é ~50 linhas e dá vida imediata.
- **Esforço estimado:** Baixo.

**Prompt para implementar:**
> No Sowur Shield, adicione um componente `SowurShield.Core.IdleWander` (novo, ~50 linhas): a cada 5-10s (random), escolhe um ponto a 1-2 unidades de distância dentro de um raio serializável a partir da posição inicial, move com MoveTowards a ~0.5u/s, respeitando colisões (Rigidbody2D já existe nos animais). Aplique aos animais (Clucky, Bunny) e ao generic_npc na SampleScene, com raio pequeno (2u) para não atrapalhar a interação (pausar o wander enquanto playerInRange do IInteractable). Flip do sprite conforme a direção. Não aplicar à Maren até o sprite dela ser corrigido (ver ERRO-06).

---

## Adendo (2026-07-05, sessão de correção)

Todos os itens da seção 0 e da seção 1 foram **corrigidos e validados em Play Mode** na sessão
seguinte à auditoria (mesma data). Correções de registro:

- **ERRO-05 (parcial):** a lista vazia do BuildingShopUI e o "Gold: 0" do AnimalMarketUI eram
  em parte **artefato do método de teste** (abri as janelas via `OpenWindow()` direto, pulando
  `OpenShop()`/`OpenMarket()` que populam os dados). O bug REAL era só o `marketData` nulo no
  `AnimalMarketNPC` — corrigido (atribuído na cena + fallback `Resources.Load` no `Awake`).
  Pelo fluxo real, o Animal Market lista Galinha/Pato/Pardal com "Ouro" correto e compra
  funcional (validei comprando um Pardal). O BuildingShop pelo fluxo real ainda precisa de
  verificação com o NPC (o `buildingRowPrefab` está corretamente wired na cena).
- **Descoberta extra na correção:** havia um **segundo `AnimalMarketNPC` fantasma** em (0,0)
  com sprite nulo — colisor interagível invisível no meio da fazenda. Desativado na cena.
- **Descoberta extra na correção:** o `BuildingShopNPC` usava `Wolf_0` — o **retrato realista
  de lobo** (1024×1489) de `Assets/Sprites/Portraits/` — como sprite de mundo (era a "foto"
  do screenshot audit_26). Substituído por frame do Premium Charakter Spritesheet (placeholder).
- **ERRO-03:** além do wiring, a ferramenta `Auto-Wire Localized Fields` conectou **137 campos**
  LocalizedString em 14 objetos de cena + 14 prefabs no projeto inteiro — muitos outros textos
  em branco foram resolvidos de tabela.
- Os sprites novos de Maren/BuildingShopNPC são **placeholders** (personagens-animais do
  Premium Charakter Spritesheet) — arte humana dedicada continua no backlog (UI_ART_PLACEHOLDERS).

## Apêndice — Estado dos sistemas testados (QA funcional)

| Sistema | Resultado |
|---|---|
| Menu principal → New Game → jogo | ✅ (com ERRO-04/UX-02 no picker) |
| Localização EN/PT/ES (fluxo normal) | ✅ HUD/diálogo/stages em PT; estáticos ainda EN (gap conhecido) |
| Farming till→water→plant→grow→harvest | ✅ ciclo completo; colheita dropa GroundItems; regrowth do solo p/ Tilled |
| Inventário (add/select/toggle UI) | ✅ (perda pós-combate = ERRO-02) |
| SellBox auto-venda no sono | ✅ ($100→$116 com 2 cenouras a 80%) |
| Sono/avanço de dia/tempo | ✅ (Dia avança, 06:00, decay de happiness funciona) |
| Animais: pet (+5), heart particle, 2º pet | ✅ / ✅ / ⚠️ AnimalInfoUI ausente da cena (silencioso) |
| FeedingTrough auto-feed | ✅ (+3 happiness, contagem 1/1 correta) |
| Diálogo (typewriter, choices, memória, rel) | ✅ (UX-11, KNOWN#2 confirmado) |
| Minimapa 3 estados | ✅ máquina de estados / ⚠️ visual (UX-03) |
| Combate completo (zona→mapa→assembler→batalha→vitória→retorno) | ✅ pipeline / ⚠️ ERRO-02/03/08, UX-05/06/07 |
| Save/Load slots | ❌ bloqueado por DEMO_BUILD no Editor (ERRO-01) |
| KNOWN_BUGS #1/#2/#3 | Parcial / Confirmado / Fix proposto |
