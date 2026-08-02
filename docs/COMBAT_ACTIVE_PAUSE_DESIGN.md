# Combate — Pausa Ativa com Modo Automático

Proposta de design para revisão. **Nada foi implementado ainda.**

---

## O problema, com números

Levantei o `TurnManager` antes de propor qualquer coisa. Duas constatações:

**1. O jogador não tem nenhuma decisão durante a batalha.**

O `Update()` enche os gauges e, quando um chega a 100, `ExecuteUnitTurn` roda na hora — para
os seus animais tanto quanto para os inimigos. Não existe nenhum ponto que espere input. A
mesma IA (`SelectTarget`) escolhe alvo para os dois lados, e `GetReadySkill` dispara a skill
do jogador sozinha assim que sai do cooldown.

O único botão em combate é o de itens consumíveis.

**2. O ritmo é ilegível.**

`turnGauge += speed * (deltaTime * 10)`, agindo aos 100:

| Velocidade | Age a cada |
|---|---|
| 8 (vaca) | 1,25 s |
| 16 (médio) | 0,63 s |
| 24–28 (rápido) | ~0,4 s |

Com 6 unidades em campo, sai **uma ação a cada ~0,1 s**. Os números de dano piscam e somem.

---

## A proposta

**Pausa ativa como padrão, com o modo escolhido antes da batalha começar.**

- **Pausa Ativa**: quando um animal *seu* enche o gauge, a batalha congela e você escolhe a
  ação dele. Os inimigos continuam automáticos.
- **Automático**: comportamento atual (a IA decide por você), mas com ritmo legível.

A escolha fica no **TeamAssembler** — a tela do grid onde você monta o time — junto da decisão
de quais animais levar. É a mesma decisão em natureza: você define *como* vai jogar a batalha
no mesmo lugar em que define *com quem*. A última escolha é lembrada para a próxima batalha.

### Por que essa e não outra

Aproveita tudo que já existe. O gauge vira o timer da sua vez em vez de ser jogado fora. As
19 skills, os cooldowns e os 5 status effects deixam de ser sorteio e viram decisão. E o modo
automático preserva o jogo cozy — ninguém quer comandar manualmente a 30ª batalha igual.

---

## Como fica na prática

### Onde o modo é escolhido (TeamAssembler)

```
   TELA DE MONTAGEM DO TIME
   ┌────────────────────────────────────────┐
   │  Animais          │      Grid          │
   │  disponíveis      │   [ ][ ][ ]        │
   │   • Vaca          │   [ ][ ][ ]        │
   │   • Galinha       │                    │
   │   • Coelho        │                    │
   ├────────────────────────────────────────┤
   │  Modo:  (•) Pausa Ativa   ( ) Auto     │  ← NOVO
   │                                        │
   │            [ Iniciar Batalha ]         │
   └────────────────────────────────────────┘
```

O modo escolhido viaja no `TeamAssemblerData` junto com o time, e o `TurnManager` o lê ao
inicializar o combate.

### Fluxo em Pausa Ativa

```
gauge do seu animal chega a 100
        ↓
  batalha congela
        ↓
  painel de comando aparece
   ┌──────────────────────────┐
   │  ▸ Atacar                │
   │  ▸ Skill (Coice)   [2t]  │  ← cinza se em cooldown
   │  ▸ Item                  │
   │  ▸ Defender              │
   └──────────────────────────┘
        ↓
  Atacar/Skill → escolher alvo (clicar no inimigo)
        ↓
  ação executa, gauge zera, batalha volta a correr
```

### O que cada opção faz

| Opção | Efeito | Observação |
|---|---|---|
| **Atacar** | Ataque básico no alvo escolhido | Hoje o alvo é escolhido pela IA |
| **Skill** | Usa a skill ativa do animal | Desabilitada em cooldown, com os turnos restantes visíveis |
| **Item** | Abre o `ConsumableBattleUI` que já existe | Não gasta o turno (a decidir — ver questões abertas) |
| **Defender** | Shield 50% por 1 turno, e o gauge reenche mais rápido | **Novo**: dá um uso a turnos onde atacar é ruim |

### Ritmo

Independente do modo, os números precisam ficar legíveis:

- `gaugeFilLRate`: **10 → 6** (~40% mais lento)
- `actionMicroDelay`: **0,05 → 0,25 s** entre unidades de um mesmo lote
- Botão de velocidade **1× / 2×** no HUD, para quem quer correr

Em automático o 2× dá aproximadamente o ritmo de hoje, então quem gosta do atual não perde nada.

---

## O que muda no código

Nada disso exige reescrever o combate. Os pontos de entrada:

| Arquivo | Mudança |
|---|---|
| `TurnManager.cs` | `ExecuteUnitTurn`: se for unidade do jogador **e** modo pausa ativa, dispara `OnPlayerTurnStarted` e aguarda em vez de executar. `ProcessActionBatch` vira `yield return` até a ação ser escolhida |
| `TurnManager.cs` | Novo `CombatMode { ActivePause, Auto }` + `SubmitPlayerAction(...)`. Lê o modo do `TeamAssemblerData` no `InitializeCombat` |
| `CombatUnit.cs` | `GetReadySkill` deixa de auto-disparar a skill do jogador em pausa ativa |
| **Novo** `BattleCommandUI.cs` | Painel de comando, auto-construído (mesmo padrão de `ConsumableBattleUI` e `RelationshipUI`) |
| **Novo** `TargetSelector.cs` | Clique no inimigo para escolher alvo, com destaque |
| `TeamAssemblerData.cs` | Novo campo `combatMode`, ao lado de `selectedStageName` |
| `TeamAssemblerUI.cs` | Toggle Pausa Ativa / Automático perto do botão "Iniciar Batalha" |
| `BattleHudOverlay.cs` | Só o botão de velocidade (1×/2×) — o modo não é mais trocável aqui |
| `CombatStatusEffect.cs` | Nada — `Shield` já existe e cobre o Defender |

**Como o modo viaja até a batalha**: `TeamAssemblerData` já é um MonoBehaviour com
`DontDestroyOnLoad` que carrega `team`, `zoneName` e `selectedStageName` da tela de montagem
para o `CombatScene`. O `combatMode` entra pelo mesmo caminho — sem código novo de
persistência entre cenas, e sem os `static` que o projeto já documenta como pouco confiáveis
em build (domain reload zera).

**Persistência entre sessões**: `PlayerPrefs` para lembrar a última escolha e a velocidade
(`combat_mode`, `combat_speed`). O projeto já usa `PlayerPrefs` em vários lugares.

**Congelar sem `Time.timeScale = 0`**: uma flag `isWaitingForPlayerInput` no `TurnManager`
que faz o `Update()` pular `FillTurnGauges`. Usar `timeScale` quebraria animações e já causou
problema antes neste projeto (o `TeamAssemblerUI` precisou de `Time.timeScale = 1f` antes do
`LoadScene` justamente por isso).

---

## Riscos

**O que pode dar errado, dito antes de começar:**

1. **Batalha travada.** Se o painel não aparecer (bug de UI, unidade morre enquanto espera), a
   batalha congela para sempre. Mitigação: timeout de ~15 s que executa a ação automática, e
   guarda para o caso da unidade morrer aguardando input.
2. **Múltiplas unidades prontas ao mesmo tempo.** O `ProcessActionBatch` processa um lote. Com
   3 animais prontos juntos, precisa enfileirar 3 comandos, não abrir 3 painéis.
3. **Os 34 testes de combate.** Vários assumem execução imediata. O modo `Auto` precisa ser o
   padrão *nos testes* para que continuem passando sem reescrita.
4. **Mobile/gamepad.** O projeto tem suporte a toque e gamepad. Seleção de alvo por clique
   precisa funcionar nos três — provavelmente com navegação por d-pad entre inimigos.

---

## Decisões fechadas

**1. Item gasta o turno?** ✅ **SIM**
Usar poção é uma escolha com custo. De graça, o jogador cura toda hora e a tensão some.

**2. O combate pode ser perdido por jogar mal?** ✅ **SIM, com derrota barata**
Perde o progresso da stage, não os animais. Derrota possível é o que faz a pausa ativa
importar; derrota cara seria hostil demais para um jogo cozy.

**3. Automático deve ser "burro" ou espelhar boas escolhas?** ✅ **Mantém a IA atual**
Sem melhorias. Automático é conveniência, não otimização — se ele jogasse tão bem quanto você,
ninguém usaria a pausa ativa.

**4. Onde fica a escolha do modo?** ✅ **DECIDIDO**
No **TeamAssembler**, a tela do grid onde o time é montado — antes da batalha começar, não
durante. Minha sugestão original (HUD + Settings) foi descartada.

Consequência que vale registrar: **o modo não pode mais ser trocado no meio da batalha.** Isso
simplifica bastante a implementação (a UI de comando não precisa aparecer/sumir no meio de um
lote de ações), mas significa que quem escolher Pausa Ativa e se cansar precisa terminar a
batalha ou sair dela. Se isso incomodar na prática, dá para adicionar o botão no HUD depois —
a estrutura suporta.

---

## Escopo estimado

| Fase | Trabalho |
|---|---|
| **1. Ritmo** | ✅ **FEITO** — valores ajustados + botão 1×/2× no HUD |
| **2. Estrutura** | `CombatMode`, espera por input, `SubmitPlayerAction`, testes em `Auto` |
| **3. Escolha do modo** | Campo no `TeamAssemblerData` + toggle no `TeamAssemblerUI` |
| **4. UI de comando** | Painel de comando + seleção de alvo |
| **5. Defender** | Nova ação usando o `Shield` existente |
| **6. Polimento** | Persistência em `PlayerPrefs`, mobile/gamepad, timeout de segurança |

A fase 1 pode ir sozinha e já melhora a leitura hoje, mesmo que você desista do resto.

---

## Estado

Todas as 4 questões estão decididas.

### Fase 1 — concluída

- `gaugeFilLRate` **10 → 6** e `actionMicroDelay` **0,05 → 0,25 s**, tanto no default do
  código quanto nos valores serializados em `CombatScene.unity` (o valor que roda é o da
  cena, não o do código — mudar só um dos dois não teria efeito nenhum).
- Botão **1× / 2×** no canto inferior direito do `BattleHudOverlay`, com a escolha
  persistida em `PlayerPrefs` (`combat_speed`).
- O multiplicador escala o gauge **e** encurta o micro-delay, então o 2× acelera o lote
  inteiro e não só o enchimento.
- **Não usa `Time.timeScale`** — o `HitStopController` já é dono dele e o derruba por
  alguns quadros nos golpes fortes; um botão escrevendo lá brigaria com a corrotina.
- 10 testes novos em `CombatPacingTests.cs`. Suíte em **822** (788 EditMode + 34 PlayMode).

**Bug encontrado no caminho** (não previsto no design): `TurnManager`, `GridManager` e
`TeamAssemblerUI` nunca limpavam o `Instance` no `OnDestroy`. Ao sair e reentrar no
`CombatScene`, o `Awake` do novo manager via um `Instance` não-nulo apontando para o objeto
destruído e **se auto-destruía sem inicializar**. Corrigido nos três.

Próximo passo: fase 2 (estrutura — `CombatMode`, espera por input, `SubmitPlayerAction`).
