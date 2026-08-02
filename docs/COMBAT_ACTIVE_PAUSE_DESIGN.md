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

**Pausa ativa como padrão, com botão para alternar para automático a qualquer momento.**

- **Pausa Ativa**: quando um animal *seu* enche o gauge, a batalha congela e você escolhe a
  ação dele. Os inimigos continuam automáticos.
- **Automático**: comportamento atual (a IA decide por você), mas com ritmo legível.

O jogador troca no meio da batalha, sem reiniciar nada. A escolha é lembrada entre batalhas.

### Por que essa e não outra

Aproveita tudo que já existe. O gauge vira o timer da sua vez em vez de ser jogado fora. As
19 skills, os cooldowns e os 5 status effects deixam de ser sorteio e viram decisão. E o modo
automático preserva o jogo cozy — ninguém quer comandar manualmente a 30ª batalha igual.

---

## Como fica na prática

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
| `TurnManager.cs` | Novo `CombatMode { ActivePause, Auto }` + `SubmitPlayerAction(...)` |
| `CombatUnit.cs` | `GetReadySkill` deixa de auto-disparar a skill do jogador em pausa ativa |
| **Novo** `BattleCommandUI.cs` | Painel de comando, auto-construído (mesmo padrão de `ConsumableBattleUI` e `RelationshipUI`) |
| **Novo** `TargetSelector.cs` | Clique no inimigo para escolher alvo, com destaque |
| `BattleHudOverlay.cs` | Botões de modo (Pausa/Auto) e de velocidade (1×/2×) |
| `CombatStatusEffect.cs` | Nada — `Shield` já existe e cobre o Defender |

**Persistência**: `PlayerPrefs`, que o projeto já usa em vários lugares. Chave sugerida:
`combat_mode` e `combat_speed`.

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

## Questões abertas — preciso da sua decisão

Estas mudam o desenho e eu não quero escolher por você:

**1. Item gasta o turno?**
Se sim, usar poção é uma escolha com custo. Se não, o jogador cura de graça toda hora e a
tensão some. **Minha sugestão: gasta o turno.**

**2. O combate pode ser perdido por jogar mal?**
Você mencionou o jogo ser cozy. Se derrota for possível, a pausa ativa importa de verdade. Se
não, ela é conveniência. **Minha sugestão: sim, mas com derrota barata** — perde o progresso
da stage, não os animais.

**3. Automático deve ser "burro" ou espelhar boas escolhas?**
Hoje a IA de jogador é a mesma dos inimigos (alvo letal primeiro, senão coluna da frente). Se
o automático for tão bom quanto jogar manual, ninguém usa a pausa ativa. **Minha sugestão:
manter a IA atual, sem melhorá-la — automático é conveniência, não otimização.**

**4. Onde fica a escolha do modo?**
Só no HUD da batalha, ou também nas Settings do jogo? **Minha sugestão: nos dois** — o HUD
para trocar na hora, as Settings para o padrão.

---

## Escopo estimado

| Fase | Trabalho |
|---|---|
| **1. Ritmo** | Ajustar valores + botão de velocidade. Pequeno, entrega valor sozinho |
| **2. Estrutura** | `CombatMode`, espera por input, `SubmitPlayerAction`, testes em `Auto` |
| **3. UI** | Painel de comando + seleção de alvo |
| **4. Defender** | Nova ação usando o `Shield` existente |
| **5. Polimento** | Persistência, mobile/gamepad, timeout de segurança |

A fase 1 pode ir sozinha e já melhora a leitura hoje, mesmo que você desista do resto.

---

## Próximo passo

Me responda as 4 questões abertas (ou diga "vai com as suas sugestões") e eu começo pela
fase 1, que é barata e reversível, antes de mexer na estrutura.
