# Combat Pipeline — Status e Investigação

## O que foi implementado

### Novos arquivos
- `Assets/Scripts/Combat/CombatTeamSpawner.cs` — spawna o time do jogador na CombatScene
- `Assets/Scripts/Combat/EnemySpawner.cs` — spawna inimigos a partir do StageData
- `Assets/Scripts/Editor/CombatSceneSetup.cs` — setup de 1 clique para a CombatScene
- `Assets/Scripts/Editor/TeamAssemblerUIBuilder.cs` — reconstrói a TeamAssemblerUI
- `Assets/Scripts/Editor/CombatAssetCreator.cs` — cria assets de combate via Editor
- `COMBAT_SETUP_GUIDE.md` — guia de configuração manual

### Arquivos modificados
- `TeamAssemblerData.cs` — convertido de classe C# estática para MonoBehaviour com DontDestroyOnLoad
- `TeamAssemblerUI.cs` — melhorias de layout, debug, e fluxo de Start Battle
- `StageButton.cs` — integração com StageManager e TeamAssemblerUI
- `BattleResultsUI.cs` — correções de scene names
- `AnimalSelectionCard.cs`, `GridPositionSlot.cs` — fixes de avisos CS0618
- `CombatScene.unity` — correção de EditorClassIdentifier + remoção de emojis TMP
- `SampleScene.unity` — TeamAssemblerCanvas adicionado

---

## Fluxo esperado (Farm → TeamAssembler → CombatScene)

```
StageButton.OnClick()
  → StageManager.SetSelectedStage("Sunny Fields")
  → TeamAssemblerUI.OpenAssembler()
      → TeamAssemblerData.ClearTeam()
      → [usuário arrasta animais para o grid]
      → [usuário clica Feed All]
      → [usuário clica Start Battle]
  → SceneManager.LoadScene("CombatScene")
      → CombatTeamSpawner.Start() → Invoke(SpawnTeams, 0.5s)
      → EnemySpawner.Start() → Invoke(SpawnEnemies, 0.6s)
      → TurnManager.Start() → Invoke(InitializeCombat, 1.0s)
```

---

## Bug principal: galinha não aparece na CombatScene

### Sintoma
- A cena transiciona corretamente (hierarquia muda para CombatScene)
- `TeamAssemblerData` persiste via DontDestroyOnLoad com Team=1
- `CombatTeamSpawner.Awake()` e `Start()` executam normalmente
- `SpawnTeams()` (via Invoke 0.5s) **nunca é chamado**

### Causa raiz identificada
A `CombatScene.unity` tinha **24 componentes MonoBehaviour com `m_EditorClassIdentifier` vazio**,
incluindo os críticos `TurnManager` e `GridManager`.

Isso gera o erro: `"The referenced script (Unknown) on this Behaviour is missing!"`

Esse erro ocorre antes do `Invoke` de 0.5s disparar e **interrompe a execução** da cena.

### Por que os ECIs estavam vazios?
Os objetos foram criados via editor scripts (CombatSceneSetup.cs) sem que Unity
re-serializasse completamente a cena. Os ECIs são preenchidos quando o Unity
reserializa o arquivo ao abrir/salvar a cena com todos os scripts compilados.

### Correção aplicada
Todos os 24 ECIs foram preenchidos diretamente no YAML da cena:

| Script | GUID (prefixo) | ECI |
|--------|---------------|-----|
| TurnManager | d87fbe1c... | SowurShield.Runtime::SowurShield.Combat.TurnManager |
| GridManager | 3bd4cebf... | SowurShield.Runtime::SowurShield.Combat.GridManager |
| BattleResultsUI | 8eb12a36... | SowurShield.Runtime::SowurShield.Combat.BattleResultsUI |
| CombatTestSpawner | 1e5fcac5... | SowurShield.Runtime::SowurShield.Combat.CombatTestSpawner |
| TextMeshProUGUI | f4688fdb... | TMPro.TextMeshProUGUI |
| Button | 4e29b1a8... | UnityEngine.UI.Button |
| Image | fe87c0e1... | UnityEngine.UI.Image |
| CanvasScaler | dc427840... | UnityEngine.UI.CanvasScaler |
| GraphicRaycaster | 0cd44c10... | UnityEngine.UI.GraphicRaycaster |
| EventSystem | 01614664... | UnityEngine.EventSystems.EventSystem |
| StandaloneInputModule | 76c392e4... | UnityEngine.EventSystems.StandaloneInputModule |

### Status após correção
**O erro persiste** — SpawnTeams() continua não sendo chamado após a correção dos ECIs.
Investigação adicional necessária.

---

## Outros problemas conhecidos (não bloqueantes)

### DontDestroyOnLoad warnings
SaveManager, GameMusicManager, GameTimeController, WorldLoader, UIManager, GameMenuManager
jogam warning `"DontDestroyOnLoad only works for root GameObjects"`.
Causa: esses objetos são filhos de outros GameObjects no SampleScene.
Efeito: eles NÃO persistem para a CombatScene. Não é crítico para o combate.

### SellBox — scripts missing
Ao iniciar o jogo, SellBox.CreateSellBoxSlotUI() joga
`"The referenced script on this Behaviour is missing!"` para o prefab de slot.
Pré-existente, não relacionado ao pipeline de combate.

---

## Próximos passos para resolver

1. **Abrir CombatScene no Unity Editor e salvar** — deixar Unity reserializar completamente
   todos os componentes. Isso deve corrigir qualquer ECI que ainda esteja incorreto.

2. **Verificar no Inspector se TurnManager e GridManager mostram o script correto**
   (sem ícone de warning amarelo). Se ainda mostrarem "Missing Script", o problema
   é de GUID e precisa ser resolvido com `Tools > Combat > Setup Combat Scene`.

3. **Se tudo estiver ok no Inspector mas SpawnTeams() ainda não rodar:**
   Verificar se há exceção não capturada entre Start() e o disparo do Invoke().
   Adicionar try/catch temporário ao Start() para capturar erros silenciosos.

4. **Alternativa ao Invoke:** substituir `Invoke(nameof(SpawnTeams), 0.5f)` por
   `StartCoroutine(SpawnAfterDelay())` com `yield return new WaitForSeconds(0.5f)`.
   Coroutines são mais robustas que Invoke quando há erros em outros scripts.

---

## Padrão de spawn confirmado como funcional

Quando CombatScene é iniciada **diretamente** (sem TeamAssembler), o fluxo funciona:
- Fallback chicken spawna em (6,2) via `Resources.Load<AnimalData>("Animals/chicken")`
- Inimigos de fallback spawnam em (2,2) e (4,2)
- TurnManager inicia o combate corretamente

O problema é **exclusivo ao fluxo via TeamAssembler** (SceneManager.LoadScene de outra cena).
