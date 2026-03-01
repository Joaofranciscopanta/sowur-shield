# Sowur Shield — Development Log

> Registro cronológico de tudo que foi implementado, por que foi feito, e como funciona.
> Atualizado a cada sessão de desenvolvimento.

---

## Sessão 1 — 2026-02-28

### Contexto
Sistema de save slots havia sido implementado anteriormente mas precisava de polish.
Esta sessão focou em corrigir bugs de UX, adicionar tracking de playtime, e iniciar
a fase de refatoração de fundação do plano de desenvolvimento.

---

## Polish do Sistema de Save Slots

### Problema 1 — Bug crítico: Continue com todos os slots vazios
**Arquivo:** `Assets/Scripts/MainMenuUI.cs`

**O que estava errado:**
`GetMostRecentSlotFromDisk()` retornava `null` se todos os slots estivessem vazios.
`OnContinueClicked()` não fazia null-check e chamava `LoadGameFromSlot(null)`,
causando comportamento indefinido.

**Fix:**
```csharp
private void OnContinueClicked()
{
    PlaySound(buttonClickSound);
    string mostRecent = GetMostRecentSlotFromDisk();
    if (!string.IsNullOrEmpty(mostRecent))
        LoadGameFromSlot(mostRecent);
    // botão já deveria estar desabilitado se não há saves, mas
    // o guard aqui previne crash em qualquer estado inesperado
}
```

---

### Problema 2 — totalPlayTime nunca atualizado
**Arquivos:** `Assets/Scripts/SaveManager.cs`, `Assets/Scripts/UI Systems/SaveSlotButton.cs`

**O que estava errado:**
`GameData.totalPlayTime` existia como campo mas ficava sempre em `0`.
`WriteSlotMeta()` já lia o campo corretamente, mas ninguém o incrementava.

**Fix em SaveManager.cs — acumulação em Update():**
```csharp
private void Update()
{
    // Acumula apenas enquanto a cena do jogo está ativa
    // GameTimeController.instance == null no menu principal
    if (currentGameData != null && GameTimeController.instance != null)
        currentGameData.totalPlayTime += Time.unscaledDeltaTime;
}
```

Usamos `Time.unscaledDeltaTime` em vez de `Time.deltaTime` para que o
playtime acumule mesmo se o Time.timeScale for alterado (pause, câmera lenta, etc).

**Fix em SaveSlotButton.cs — exibição formatada:**
```csharp
if (playTimeText != null)
{
    int totalMinutes = Mathf.FloorToInt(info.totalPlayTime / 60f);
    if (totalMinutes >= 1)
    {
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        playTimeText.text = hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
    }
    else
    {
        playTimeText.text = string.Empty; // esconde se < 1 minuto
    }
}
```

Threshold de 1 minuto evita exibir "0m" logo após criar um save.

---

### Problema 3 — AutoSave visível mas inutilizável no painel Save in-game
**Arquivo:** `Assets/Scripts/GameMenuUI.cs`

**O que estava errado:**
AutoSave aparecia com cadeado no painel de Save manual — confuso para o jogador,
que já sabe que o jogo salva automaticamente ao dormir.

**Fix:**
```csharp
if (currentInGameSlotMode == InGameSlotMode.Save)
{
    // AutoSave completamente omitido da lista de save manual
    if (info.isAutoSave) continue;
    ...
}
```

---

### Problema 4 — Botão delete ausente no painel Load in-game
**Arquivo:** `Assets/Scripts/GameMenuUI.cs`

**O que estava errado:**
No modo Load do painel in-game, `onDeleteAction` era sempre `null` —
o botão de deletar nunca aparecia, sem motivo técnico.

**Fix:**
```csharp
// Modo Load: delete disponível em slots ocupados não-AutoSave
Action deleteAction = info.isEmpty || info.isAutoSave
    ? null
    : (Action)(() => DeleteSlotAndRefreshInGame(slotName));

btn.Initialize(info, loadAction, deleteAction, locked);
```

Adicionado helper `DeleteSlotAndRefreshInGame()` que deleta e repopula
a lista sem fechar o painel:
```csharp
private void DeleteSlotAndRefreshInGame(string slotName)
{
    SaveManager.Instance?.DeleteSlot(slotName);
    PopulateInGameSlotPanel();
}
```

---

### Problema 5 — Dead code em MainMenuUI
**Arquivo:** `Assets/Scripts/MainMenuUI.cs`

**O que foi removido:**
- `StartNewGame()` — substituído por `StartNewGameInSlot()`
- `LoadGame()` — substituído por `LoadGameFromSlot()`
- Referência a `StartNewGame()` em `OnConfirmationYes()` também removida

---

### SaveSlotButton — novos campos e formatação
**Arquivo:** `Assets/Scripts/UI Systems/SaveSlotButton.cs`

**Adicionado:**
- Campo `[SerializeField] private TextMeshProUGUI playTimeText`
- Formatação de nome: `"Slot1"` → `"Slot 1"` via substring
- PlayTimeText exibido dentro de um `StatsRow` (HorizontalLayoutGroup)
  com MoneyText à esquerda e PlayTimeText à direita

**Prefab SaveSlotButton — mudanças no Unity:**
- `StatsRow`: novo GameObject filho do `ContentGroup` com `HorizontalLayoutGroup`
- `MoneyText` e `PlayTimeText` movidos para dentro do `StatsRow`
- `DeleteButton` movido para dentro do `ContentGroup` com `Ignore Layout: true`,
  ancorado no canto superior direito do card
- `EmptyGroup.VerticalLayoutGroup`: `Control Child Size` habilitado para
  que `EmptyText` tenha tamanho e seja visível

---

### Commits desta parte
```
0fbc8c0  feat: implement multi-slot save system with UX polish
```

---

## 0.2 — GameBalance ScriptableObject

### Por que foi feito

O jogo tinha números mágicos espalhados em ~4 arquivos diferentes controlando
o mesmo sistema (felicidade dos animais, distâncias de interação, economia).
Cada ajuste de balanceamento exigia editar código e recompilar.

**Antes:**
```csharp
// Animal.cs — números sem contexto
ModifyHappiness(5f);
decay -= 0.5f;
happiness = Mathf.Max(20f, happiness + decay);
public float GetHappinessMultiplier() => 0.5f + (happiness / 100f);

// SellBox.cs
public float sellMultiplier = 0.8f;

// InteractionManager.cs
return 2.0f; // SellBox range
return 2.0f; // range padrão — mesmo número, propósito diferente
```

**Depois:** um único arquivo `Assets/Resources/GameBalance.asset` editável
no Inspector do Unity, sem tocar em código.

---

### GameBalance.cs — estrutura completa

**Arquivo:** `Assets/Scripts/GameBalance.cs`

```csharp
[CreateAssetMenu(menuName = "SowurShield/Game Balance", fileName = "GameBalance")]
public class GameBalance : ScriptableObject
{
    [Header("Economy")]
    public float sellMultiplier = 0.8f;

    [Header("Animal — Happiness")]
    public float petHappinessBonus = 5f;
    public float feedHappinessBonus = 3f;
    public float autoFeedHappinessBonusPerUnit = 3f;
    public float dailyDecayNoPet = 0.5f;
    public float dailyDecayNoFeed = 1.0f;
    public float happinessFloor = 20f;
    public float happinessCeiling = 100f;
    public float initialHappiness = 50f;

    [Header("Animal — Combat Multiplier")]
    public float happinessMultiplierMin = 0.5f;  // a 20 happiness
    public float happinessMultiplierMax = 1.5f;  // a 100 happiness

    [Header("Interaction Distances")]
    public float defaultInteractionRange = 2f;
    public float sellBoxInteractionRange = 3f;
    public float maxToolDistance = 2f;
}
```

---

### Como cada sistema carrega o GameBalance

**Padrão adotado:** campo `[SerializeField] private GameBalance balance` com
fallback automático via `Resources.Load` se não atribuído no Inspector:

```csharp
// Em Start() ou Awake():
if (balance == null)
    balance = Resources.Load<GameBalance>("GameBalance");
```

Isso significa:
- Atribuir no Inspector = explícito, tem precedência
- Não atribuir = carrega automaticamente de `Assets/Resources/GameBalance.asset`
- Se o asset não existir = fallback para os valores hardcoded originais (nunca crasha)

---

### Arquivos modificados

| Arquivo | O que mudou |
|---|---|
| `Animal.cs` | `petBonus`, `feedBonus`, `autoFeedBonus`, `decay`, `floor`, `ceiling`, `initialHappiness`, `GetHappinessMultiplier()` |
| `SellBox.cs` | `sellMultiplier` virou propriedade computada; `maxInteractionDistance` inicializado do balance |
| `InteractionManager.cs` | `GetInteractionRange()` usa `balance.defaultInteractionRange`; SellBox delega para `sellBox.GetInteractionRange()` |
| `CursorController.cs` | `maxDistance` inicializado de `balance.maxToolDistance` no `Start()` |

---

### GetHappinessMultiplier — antes e depois

**Antes** (fórmula fixa, não configurável):
```csharp
public float GetHappinessMultiplier() => 0.5f + (happiness / 100f);
// resultado: 0.5x a 0 happiness, 1.5x a 100 happiness — mas os limites eram hardcoded
```

**Depois** (interpolação configurável):
```csharp
public float GetHappinessMultiplier()
{
    float min  = balance != null ? balance.happinessMultiplierMin : 0.5f;
    float max  = balance != null ? balance.happinessMultiplierMax : 1.5f;
    float ceil = balance != null ? balance.happinessCeiling       : 100f;
    return min + (happiness / ceil) * (max - min);
}
```

Agora você pode, por exemplo, apertar o range para `0.8–1.2` para um jogo
menos punitivo, sem tocar em código.

---

### Setup no Unity (passo único)

1. No Project window, clique com botão direito em `Assets/Resources/`
2. **Create → SowurShield → Game Balance**
3. Renomeia para `GameBalance` (exato — é o nome que `Resources.Load` procura)
4. Opcional: arrastar o asset nos campos **Balance** de `InteractionManager`
   e `CursorController` no Inspector para referência explícita

---

### Commits desta parte
```
bebf9b4  feat: introduce GameBalance ScriptableObject for centralized game tuning
```

---

## Próximos passos

```
Phase 0 — Foundation:
  [x] 0.1  Save File Versioning       (já estava implementado)
  [x] 0.2  GameBalance ScriptableObject
  [ ] 0.3  Cache All FindObjectOfType Calls
  [ ] 0.4  Add InteractionRange to IInteractable
  [ ] 0.5  Add Namespaces to Core Files
```
