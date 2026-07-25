# Plano — Arquitetura de Containers de Inventário

> Criado 2026-07-25. Sucessor natural do `03_WORKLIST.md` (14/14 concluído).
> Alvo: `SellBox.cs` (1174), `Inventory.cs` (1178), `InventorySlot.cs` (907), `FeedingTrough.cs` (509).
> Mesmo protocolo de execução do `00_README.md`: pegue a primeira etapa não marcada
> cujas dependências estejam `[x]`.

---

## 1. Diagnóstico

O problema **não é** o tamanho dos arquivos. É que três containers reimplementam as mesmas
quatro responsabilidades, e o slot de UI conhece cada um deles pelo nome.

### 1.1 O acoplamento central

`InventorySlot.OnDrop` (`InventorySlot.cs:835-905`) é uma cadeia de cinco `if` que cita
`SellBox`, `IsTroughMode` e o inventário do jogador, e chama
`FindFirstObjectByType<SellBox>()` **a cada drop**:

```
if (draggedSlot.isSellBoxMode && !isSellBoxMode && sellBox.IsOpen)  -> sellBox.HandleSellBoxToInventoryDrop
if (draggedSlot.isSellBoxMode &&  isSellBoxMode && sellBox.IsOpen)  -> sellBox.HandleSellBoxInternalMove
if (isSellBoxMode && sellBox.IsOpen)                                -> sellBox.HandleSlotDrop
if (draggedSlot.IsTroughMode && !isTroughMode)                      -> lógica inline
if (isTroughMode && troughContainer != null)                        -> lógica inline
                                                                    -> inventoryManager.HandleSlotDrop
```

Um container novo (baú, loja, bancada) exige editar `InventorySlot.OnDrop` **e** escrever a
quarta cópia de build de slots, refresh, transferência e save. É esse custo marginal que o
plano ataca — não a contagem de linhas.

### 1.2 Duplicação por container

| Responsabilidade | Inventory | SellBox | FeedingTrough |
|---|---|---|---|
| Construir slots do prefab | `SetupUI`/`CreateSlotUI` | `SetupUI`/`CreateSellBoxSlotUI` | `SetupUI` |
| Assinar `OnSlotChanged` → refresh | `UpdateSlot`/`UpdateAllSlots` | idem + valor total + sprite | `RefreshSlots` |
| Transferência entre containers | `HandleSlotDrop` | 3 métodos distintos | inline no `OnDrop` |
| Save/load slot a slot | `InventoryData` própria | `worldStrings`/`worldCounters` | `worldStrings`/`worldCounters` |
| Travar movimento do jogador | — | inline | `DisablePlayerMovement` |

### 1.3 O que já está certo (a base do plano)

- `IInventoryContainer` + `InventoryContainer` (443 linhas) — add/remove/slots/stacking, sólido.
- `InventoryContainer.GetSaveData()` / `LoadFromSaveData()` **já existem e ninguém usa**:
  SellBox e FeedingTrough persistem slot a slot na mão.
- `InventorySlot` já foi parcialmente decomposto: `SlotVisualController`, `SlotDragHandler`,
  `SlotSellBoxAdapter`. Há precedente de componentização — o plano continua esse caminho.

### 1.4 O ponto cego

`ItemStack` tem 33 testes. `InventoryContainer`, `Inventory`, `SellBox`, `FeedingTrough` e toda
a lógica de transferência têm **zero**. É simultaneamente a área com mais correções de bug
documentadas (4 no `CLAUDE.md`, todas em interação de SellBox) e a com menos rede de proteção.
**Por isso a Etapa 0 vem antes de qualquer refactor.**

---

## 2. Arquitetura alvo

```
InventorySlot.OnDrop            ← 2 linhas, não conhece nenhum container concreto
        │
        ▼
ItemTransferService             ← C# puro, sem MonoBehaviour, testável sem cena
        │
        ▼
ContainerView (MonoBehaviour)   ← slots, refresh, save; um por container na cena
        │
        ▼
IContainerPolicy                ← ponto de extensão: Sell / Player / Food / Chest / Craft
        │
        ▼
IInventoryContainer             ← INALTERADO
```

### 2.1 Decisões travadas com o Lucas (2026-07-25)

| Decisão | Escolha | Consequência no desenho |
|---|---|---|
| Escopo | Etapas 0–5 completas | — |
| Saves antigos | **Podem quebrar** | Formato novo direto; bump `CURRENT_SAVE_VERSION` 1 → 2 e migração V1→V2 que descarta as chaves antigas. Primeiro uso real do dispatch criado na TASK-004. |
| Containers futuros | Baú, Loja, Cozinha | A cozinha é a que mais restringe: entrada e saída com regras diferentes **no mesmo container** ⇒ a policy tem que valer por slot. |

### 2.2 `IContainerPolicy` — assinatura por slot

A cozinha força isto. Se a policy fosse por container, uma bancada com slots de entrada
(aceita ingredientes) e de saída (só permite retirar) precisaria de dois containers e de um
caso especial no serviço de transferência — exatamente o que estamos removendo.

```csharp
public enum SlotRole { Storage, Input, Output }

public interface IContainerPolicy
{
    SlotRole GetRole(int slotIndex);              // default: Storage
    bool CanAccept(Item item, int slotIndex);     // SellBox: item.canBeSold
    bool CanWithdraw(int slotIndex);              // Output de craft: true; Input: false
    void OnAccepted(Item item, int quantity);     // som, partícula, sprite da caixa
    void OnRejected(Item item, int slotIndex);    // feedback vermelho
}
```

`DefaultContainerPolicy` implementa tudo como "Storage, aceita e permite retirar" — o baú é
literalmente isso, zero código novo.

### 2.3 Onde as lojas entram (e onde não entram)

`ShopUI` (403), `AnimalMarketUI` (512) e `BuildingShopUI` (402) hoje vivem fora dessa
arquitetura e **compram por botão, não por arrastar**. O plano os trata assim:

- Eles **ganham** a `ContainerView` para exibir estoque (mata o build de slots duplicado).
- Eles **não** entram no `ItemTransferService` — uma compra é uma transação com preço,
  estoque e desconto por relacionamento, não um movimento de itens. Forçar isso na policy
  transformaria `CanAccept` num monstro.
- A policy da loja retorna `CanAccept == false` para todo drop direto, e a compra continua
  passando pelo caminho de botão que já funciona.

Isto é uma limitação consciente, não um esquecimento. Migrar as lojas é escopo da Etapa 6,
que **não faz parte deste plano** — fica registrada como continuação.

---

## 3. Etapas

Cada etapa compila, passa nos testes e é commitável sozinha. Perder uma sessão no meio custa
no máximo uma etapa.

---

### ETAPA 0 — Rede de proteção para `InventoryContainer`
- [x] status — feito 2026-07-25. `InventoryContainerTests.cs`, **56 testes** (plano previa ~40).
  Nenhum arquivo de produção alterado. Três achados registrados na seção 6.
- risco: nenhum (só adiciona testes)
- depende de: nada
- arquivos: novo `Assets/Tests/EditMode/InventoryContainerTests.cs`

**Por quê primeiro:** as etapas 1–5 movem lógica de lugar. Sem testes, "movi sem quebrar" é fé.

**Coberto:**
- `AddItem` em container vazio, parcialmente cheio, cheio; retorno em overflow parcial
- Stacking até `maxStackSize` e transbordo para o próximo slot livre
- `RemoveItem` atravessando múltiplos slots; remoção maior que o estoque
- `CanAdd` / `HasEmptySlot` / `GetFirstEmptySlotIndex` nas bordas
- `SetMaxSlots` **encolhendo com itens nos slots removidos** (hoje sem teste, e
  `Inventory.SetInventorySize` depende disso)
- `FindSlotWithItem` com o item ausente
- Round-trip `GetSaveData()` → `LoadFromSaveData()` preservando índices e quantidades
- Eventos `OnSlotChanged` / `OnItemAdded` / `OnItemRemoved` disparando na quantidade certa

**Nota de implementação:** `ItemDatabase` resolve nomes via `Resources` e não tem ponto de
injeção, então `InstallTestItemDatabase()` no fim do arquivo de teste alcança os estáticos
privados por reflection. Ficou contido no teste em vez de adicionar API só-para-teste na
produção — se mais suítes precisarem, aí sim vale um seam de verdade.

**Feito quando:** ~40 testes passando; nenhum arquivo de produção alterado. ✅

---

### ETAPA 1 — `ItemTransferService`
- [x] status — feito 2026-07-25. Service + `IContainerPolicy` + `DefaultContainerPolicy`,
  **38 testes**. Nada em produção chama o serviço ainda.
- risco: baixo (código novo, ainda não plugado)
- depende de: Etapa 0
- arquivos: novo `Assets/Scripts/Inventory/ItemTransferService.cs`,
  novo `Assets/Scripts/Inventory/IContainerPolicy.cs`,
  novo `Assets/Tests/EditMode/ItemTransferServiceTests.cs`

**Ajuste em relação ao plano original:** a interface `IContainerPolicy` e o
`DefaultContainerPolicy` vieram da Etapa 2 para cá — a assinatura do serviço depende deles,
e sem um policy permissivo não dá para testar o serviço isoladamente. A Etapa 2 fica só com as
policies concretas (SellBox e comedouro).

**Decisão de desenho — `SlotRole` é metadado, não permissão.** O enum descreve o slot para a UI
(renderizar um slot de saída diferente); quem decide é `CanAccept`/`CanWithdraw`. O serviço nunca
lê `GetRole`. Assim existe uma única fonte de verdade: uma policy de bancada devolve `Output`
para o slot de resultado **e** `CanAccept == false` para ele.

**Comportamentos preservados de propósito:** soltar um stack sobre um stack cheio do mesmo item
faz swap (não rejeita), igual ao `Inventory.HandleSlotDrop` de hoje. Swap parcial não existe —
não há onde colocar o resto, então nada acontece.

Classe estática pura. Uma única entrada:

```csharp
public static TransferResult Transfer(
    IInventoryContainer from, int fromIndex,
    IInventoryContainer to,   int toIndex,
    IContainerPolicy toPolicy, int quantity = -1);   // -1 = stack inteiro

public readonly struct TransferResult
{
    public bool Moved;            // algo saiu da origem
    public int  QuantityMoved;
    public bool Rejected;         // policy recusou
    public bool Partial;          // coube parte
}
```

Absorve os quatro caminhos que hoje estão espalhados: `Inventory.HandleSlotDrop:441`,
`SellBox.HandleSlotDrop:607`, `SellBox.HandleSellBoxInternalMove:673`,
`SellBox.HandleSellBoxToInventoryDrop:705`.

Precisa cobrir os quatro cenários que a versão do `Inventory` já trata e a do `SellBox` não:
slot destino vazio, stack compatível com sobra, stack compatível sem sobra, e **swap** de
itens incompatíveis.

**Feito quando:** ~30 testes, incluindo `from == to` (movimento interno) e destino que rejeita.
Nenhum arquivo de produção ainda chama o serviço.

---

### ETAPA 2 — As policies concretas
- [x] status — feito 2026-07-25. `SellBoxPolicy` + `FeedingTroughPolicy`, **25 testes**
  (isoladas e através do `ItemTransferService`). Nada em produção usa ainda.
- risco: baixo
- depende de: Etapa 1
- arquivos: novos `Assets/Scripts/Inventory/Policies/SellBoxPolicy.cs`,
  `FeedingTroughPolicy.cs`
  (a interface e o `DefaultContainerPolicy` já vieram na Etapa 1)

`SellBoxPolicy.CanAccept` = `item.canBeSold`. Ganho colateral: hoje essa checagem está inline
no `SellBox.HandleSlotDrop`, ou seja, só vale naquele caminho — como policy, o
`ItemTransferService` a aplica em toda rota de entrada, inclusive no swap, onde um item
não-vendável poderia ser empurrado para dentro da caixa.

**`FeedingTroughPolicy` NÃO muda o jogo** — decisão revista durante a implementação. O plano
original previa rejeitar não-ração no drop, mas isso é mudança de gameplay, não refactor.
A flag `RejectNonFood` existe e está testada nos dois estados, mas nasce em **`false`**, que é
exatamente o que o jogo faz hoje (aceita tudo, ignora o que não serve na hora de alimentar).
Assim a Etapa 4 pode subir sem alterar o jogo, e virar a flag depois é uma decisão isolada e
revertível.

O conjunto do que conta como ração vem por callback (`Func<IEnumerable<Item>>`), não lido do
`AnimalZone` direto: o conjunto depende de quais animais estão na zona **naquele momento**, e
o callback é o que permite testar a policy sem cena, sem `AnimalZone` e sem `ItemDatabase`
populado. Se o callback devolver `null` (sem zona ligada, ou `ItemDatabase` ainda não pronto)
a policy fica permissiva — trancar o jogador fora de um comedouro que ele não consegue encher
seria pior que aceitar demais. Lista vazia é resposta legítima e rejeita tudo.

**Feito quando:** policies testadas isoladamente + integradas ao `ItemTransferService`. ✅

---

### ETAPA 3 — `ContainerView` e migração do FeedingTrough
- [ ] status
- risco: médio (primeiro container real migrado)
- depende de: Etapa 2
- arquivos: novo `Assets/Scripts/Inventory/ContainerView.cs`, `Animals/FeedingTrough.cs`

`ContainerView` recebe container + `slotParent` + `slotPrefab` + policy, instancia os slots,
assina `OnSlotChanged` e faz refresh. Expõe `SlotCount`, `GetSlotUI(i)`, `Refresh()`.

**O trough é o cobaia de propósito:** é o menor (509 linhas), o mais recente, o menos
acoplado e o único sem histórico de bug. Se a abstração estiver errada, descobrimos aqui e
não dentro do SellBox.

**Feito quando:** comedouro abre, aceita drag do inventário, devolve item ao inventário,
persiste, alimenta no `OnDayChanged` — tudo como antes. Requer sessão no Editor.

---

### ETAPA 4 — Migrar SellBox e Inventory; limpar o `OnDrop`
- [ ] status
- risco: **alto** — é a etapa perigosa
- depende de: Etapa 3
- arquivos: `Core/SellBox.cs`, `Inventory/Inventory.cs`, `Inventory/InventorySlot.cs`,
  `Inventory/SlotSellBoxAdapter.cs`

Só aqui o `OnDrop` perde a cadeia de `if` e o `FindFirstObjectByType<SellBox>()`: o slot
pergunta à própria `ContainerView` quem é o dono e delega ao `ItemTransferService`.

Sobra em cada classe:
- **SellBox** (~350 linhas): multiplicador, `CalculateTotalValue`, sprite dinâmico da caixa,
  `SellAllItemsAutomatically` no sono, `IInteractable`/`IUIWindow`.
- **Inventory** (~500 linhas): hotbar, seleção, input actions, `UseItem`, sort/filtro,
  auto-refill.

**Atenção — `CLAUDE.md` documenta 4 correções de bug em interação de SellBox.** Não mexer em
`CursorController`, `InteractionManager` nem nos caminhos de tecla E / clique nesta etapa.
O escopo aqui é exclusivamente drag/drop e construção de slots.

**Feito quando:** roteiro manual no Editor — arrastar item→SellBox, SellBox→inventário,
mover dentro do SellBox, item não-vendável rejeitado com feedback vermelho, dormir e vender,
E e clique esquerdo abrindo a caixa, ESC.

---

### ETAPA 5 — Unificar persistência (`saveVersion` 1 → 2)
- [ ] status
- risco: médio
- depende de: Etapa 4
- arquivos: `Inventory/ContainerPersistence.cs` (novo), `Core/GameData.cs`,
  `Core/SaveManager.cs`, os três containers

Um caminho só, usando `InventoryContainer.GetSaveData()`, indexado por `ContainerID`.
Elimina as chaves `sellbox_*` e `feedingtrough_*` escritas à mão.

Como saves antigos podem quebrar (decisão travada): bumpar `CURRENT_SAVE_VERSION` para 2 e
implementar `MigrateV1ToV2` limpando as chaves órfãs, em vez de tentar convertê-las.
**É o primeiro uso real do dispatch de migração criado na TASK-004** — que até hoje nunca
rodou. Vale um teste que exercite o caminho v1→v2 de ponta a ponta.

**Perda consciente:** conteúdo de SellBox e comedouro em saves v1 (inclusive de quem jogou a
demo WebGL). O resto do save sobrevive. Anunciar no changelog.

---

## 4. Riscos

| Risco | Mitigação |
|---|---|
| SellBox é a superfície com mais bugs históricos | Etapa 4 isolada e por último; trough validado antes; escopo restrito a drag/drop |
| Nada é compilado fora do Unity | Cada etapa termina com um commit e uma abertura no Editor; etapas 0–2 são C# puro e cobertas por testes |
| `ContainerView` errada só aparece no SellBox | Por isso o trough vem antes — abstração validada no container barato |
| Wiring de cena por etapa | `ContainerView` reaproveita `slotParent`/`slotPrefab` já existentes; nenhuma referência nova além do componente |
| Cozinha/loja mudarem tudo de novo | A policy já nasce por slot (`SlotRole`) e as lojas ficam explicitamente fora do transfer service |

## 5. Achados da Etapa 0

Três comportamentos do `InventoryContainer` que os testes documentaram e que **mudam o desenho
das etapas seguintes**. Nenhum foi corrigido — corrigir é decisão separada, com risco próprio.

### 6.1 `AddItem` e `RemoveItem` não são atômicos
`AddItem(item, 61)` num container com capacidade 60 adiciona os 60 **e** retorna `false`.
`RemoveItem(item, 10)` com 3 em estoque remove os 3 **e** retorna `false`.

Quem tratar o `bool` como "nada aconteceu" duplica ou perde item. O `SellBox.HandleSlotDrop`
hoje escapa disso por acidente, porque chama `CanAdd` antes e calcula `GetAvailableSpace` no
caminho parcial. O `ItemTransferService` (Etapa 1) **precisa** fazer o mesmo de forma
explícita: consultar `CanAdd`, decidir a quantidade, e só então mover.

Testes: `AddItem_PartialFit_StillAddsWhatFits`,
`RemoveItem_MoreThanAvailable_StillRemovesWhatItCould`.

### 6.2 `GetSlot` devolve a referência viva, não uma cópia
Diferente de `GetAllItems` (que clona), `GetSlot` entrega o `ItemStack` interno. Dá para
mutar o container sem passar por `SetSlot` — e portanto **sem disparar `OnSlotChanged`**,
deixando a UI dessincronizada.

Isso importa direto para a Etapa 3: a `ContainerView` vai depender de `OnSlotChanged` para
todo refresh. Qualquer código que hoje mute via `GetSlot` vira um bug visual silencioso. Vale
um grep por `GetSlot(` com atribuição antes de começar a Etapa 3.

Teste: `GetSlot_ReturnsLiveReference_NotACopy`.

### 6.3 `SetMaxSlots` destrói itens ao encolher, sem aviso
O método conta os itens que vai perder numa variável local `lostItems`
(`InventoryContainer.cs:325-332`) e **nunca a usa** — restou de um `Debug.LogWarning` removido.
Sem retorno, sem aviso, sem realocação.

`Inventory.SetInventorySize` e `UpgradeInventorySize` são os chamadores. Como só crescem hoje,
não há bug em produção — mas um baú com tamanho configurável (caso de uso confirmado) pisa
nisso na primeira vez que alguém reduzir o tamanho.

Teste: `SetMaxSlots_Shrinking_SilentlyDestroysItemsInRemovedSlots`.

---

## 6. Continuação (fora deste plano)

- **Etapa 6** — migrar `ShopUI` / `AnimalMarketUI` / `BuildingShopUI` para a `ContainerView`
  (só exibição de estoque; compra continua por botão).
- Baú e bancada de craft passam a ser conteúdo, não arquitetura: uma policy + wiring de cena.
- `SellBox` ainda recarrega `GameBalance` via `Resources.Load` a cada acesso a
  `sellMultiplier` (`SellBox.cs:68`) — cache trivial, não bundle nesta sequência.
