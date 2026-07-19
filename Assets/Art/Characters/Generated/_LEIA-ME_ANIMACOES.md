# Animações geradas — Sowur Shield

Todos os `.anim` aqui foram fatiados automaticamente a partir dos spritesheets já importados,
com `.meta` próprio (guid estável). Nada dos arquivos originais/de alto conflito foi tocado.

## Conteúdo

| Pasta | Qtd | O que é |
|---|---|---|
| `./` (raiz) | 24 | **Bunny** (personagem) — idle/walk/run/hoe/axe/water × 4 direções |
| `Animals/` | 64 | Galinha, pintinho, ovo, vaca, bezerro (cor padrão) — 1 clipe por linha da grade |
| `Enemies/` | 11 | Inimigos com múltiplos frames — 1 clipe (tira única) por inimigo |
| `Scenery/` | 53 | Árvores, água, pássaro (voar/pular), gato, fumaça — 1 clipe por linha |
| `BunnyController.controller` | 1 | Animator do Bunny (blend trees + estados de ação) |

## Bunny — Animator Controller

O `BunnyController.controller` já está montado com:

**Parâmetros:** `MoveX` (float), `MoveY` (float), `isWalking` (bool), `isRunning` (bool),
`Hoe` (trigger), `Axe` (trigger), `Water` (trigger).

**Estados (cada um é um blend tree 2D por direção, dirigido por MoveX/MoveY):**
- `Idle` (default) ⇄ `Walk` — via `isWalking`
- `Walk` ⇄ `Run` — via `isRunning`
- `Hoe` / `Axe` / `Water` — entram por **Any State** quando o trigger dispara e voltam ao Idle ao terminar

Direções no blend tree: Down (0,-1), Up (0,1), Right (1,0), Left (-1,0).

## Como colocar no jogo

1. Selecione o **Bunny** na cena → componente **Animator** → campo **Controller** → arraste `BunnyController`.
2. **Idle e Walk já funcionam** sem mexer no código: `PlayerMove.cs` já seta `MoveX`, `MoveY` e `isWalking`.
3. Para ligar o **Run**, adicione uma linha no `PlayerMove.cs` (ex. no `Update`):
   ```csharp
   animator.SetBool("isRunning", isSprinting);
   ```
4. Para ligar as **ações**, chame o trigger quando a ferramenta for usada:
   ```csharp
   animator.SetTrigger("Axe");   // ou "Hoe", ou "Water"
   ```
   (ex. dentro da lógica de cortar árvore / arar solo / regar).

## Observações

- **Cores dos animais:** foi gerada só a cor padrão de cada tipo. As outras cores (blue/brown/green/red/pink/purple) usam **o mesmo layout** — dá pra duplicar o clipe e trocar só a folha de origem.
- **Clipes `_rowN` (animais/cenário):** foram cortados por linha da grade automaticamente. Alguns podem juntar/separar linhas diferente do ideal — renomeie/agrupe no Unity conforme o uso.
- **FPS:** idle 6, walk 10, run 12, ações 10, animais 8, cenário 6–10. Ajuste no clipe se quiser.
- Todos os clipes têm **loop ligado**.
