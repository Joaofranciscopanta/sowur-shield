# Guia de Solução de Problemas - Minimap

## 🔴 PROBLEMA: Mapa Preto (mais comum)

### Solução Rápida - 5 Passos

**Passo 1: Verifique a Layer "Minimap"**
1. Vá em **Edit → Project Settings → Tags and Layers**
2. Procure por uma layer chamada **"Minimap"** (exatamente com esse nome)
3. Se não existir, crie em qualquer User Layer disponível (Layer 6, 7, 8, etc.)
4. **Importante**: Anote o número da layer!

**Passo 2: Configure a MinimapCamera**
1. Na Hierarchy, selecione **MinimapCamera**
2. No Inspector, procure o componente **Camera**
3. Em **Culling Mask**, clique no dropdown
4. Clique em **"Nothing"** (desmarca tudo)
5. **Marque SOMENTE a layer "Minimap"** ✓
6. O Culling Mask deve mostrar: **"Minimap"**

**Passo 3: Adicione MinimapIcon ao Player**
1. Selecione seu **Player** na Hierarchy
2. Clique em **Add Component**
3. Digite: `MinimapIcon`
4. Aperte Enter para adicionar

5. Configure o componente:
   - **Icon Type**: `Player`
   - **Icon Color**: Verde (deixe o padrão)
   - **Icon Size**: `2` (para ficar bem visível)
   - **Always Visible**: ✓ Marque
   - **Minimap Layer Name**: `Minimap` (exatamente como criou)

**Passo 4: Verifique se o Ícone Foi Criado**
1. Com o Player selecionado, **expanda ele** na Hierarchy (clique na setinha)
2. Deve aparecer um filho chamado **"Player_MinimapIcon"**
3. Selecione esse filho
4. No Inspector, olhe no topo onde diz **Layer**
5. Deve estar marcado como **"Minimap"**
6. Se não estiver, mude manualmente para **Minimap**

**Passo 5: Entre em Play Mode e Verifique**
1. Aperte **Play**
2. Olhe o Console (Window → General → Console)
3. Procure por mensagens **[MinimapUI]**
4. Deve aparecer: **"Connected to MinimapCamera RenderTexture: MinimapRenderTexture"**

Se ainda estiver preto, vá para **Diagnóstico Avançado** abaixo.

---

## 🔧 Diagnóstico Avançado - Mapa Preto

### Teste 1: Verifique a Textura no Play Mode

**NO MODO PLAY:**
1. Selecione **MinimapImage** na Hierarchy (dentro de Canvas → MinimapPanel)
2. Olhe o componente **Raw Image** no Inspector
3. O campo **Texture** deve mostrar: **"MinimapRenderTexture"**
4. Se mostrar **"None"**, a conexão falhou

**Se a textura está None:**
1. Selecione **MinimapController** na Hierarchy
2. Procure **MinimapUI (Script)** no Inspector
3. Clique com **botão direito** no nome do script
4. Clique em **"Force Reconnect Camera"**
5. Olhe o Console para mensagens de debug

### Teste 2: Verifique se a Camera Está Renderizando

1. Entre em **Play Mode**
2. Selecione **MinimapCamera** na Hierarchy
3. Olhe a **Game View** (aba Game, não Scene)
4. Se você ver uma pequena prévia da câmera no canto, significa que ela está renderizando
5. Se não ver nada, a câmera não está configurada corretamente

### Teste 3: Verifique a Posição da Camera

1. Selecione **MinimapCamera**
2. Olhe o **Transform** no Inspector:
   - **Position**: Deve estar NA FRENTE do mundo (ex: `0, 0, -100`)
   - **Rotation**: `0, 0, 0` (olhando para FRENTE - essencial para jogos 2D no plano XY!)
   - **Scale**: `1, 1, 1`

3. Se a posição/rotação estiver errada:
   - Position X e Y: Onde seu jogador vai estar (geralmente `0, 0`)
   - Position Z: **Negativo** para ficar na frente (ex: `-100`)
   - Rotation: **DEVE SER `0, 0, 0`** para jogos 2D no plano XY!

### Teste 4: Verifique o Script MinimapCamera

1. Selecione **MinimapCamera**
2. Procure **MinimapCamera (Script)** no Inspector
3. Verifique:
   - **Player Target**: Deve ter seu Player GameObject
   - **Default Orthographic Size**: `10`
   - **Minimap Layers**: Deve mostrar **"Minimap"** na lista
   - **Follow Player**: ✓ Marcado
   - **Render Texture Size**: `1024`

4. Se **Render Texture** estiver vazio no campo:
   - Entre em Play Mode
   - Pare o Play Mode
   - Entre novamente
   - O script cria a textura no Awake()

### Teste 5: Adicione um Objeto Teste Visível

Vamos criar algo garantido para aparecer no minimap:

1. **Crie um Cube simples:**
   - Hierarchy → Botão direito → **3D Object → Cube**
   - Renomeie para: `MinimapTest`

2. **Posicione próximo ao Player:**
   - Position: `5, 5, 0` (perto do player)
   - Scale: `2, 2, 1` (grande e visível)

3. **Adicione MinimapIcon:**
   - Selecione MinimapTest
   - Add Component → **MinimapIcon**
   - Icon Type: **Generic**
   - Icon Color: **Vermelho** (para contrastar)
   - Icon Size: `3` (bem grande)
   - Always Visible: ✓ Marcado

4. **Teste:**
   - Entre em Play Mode
   - Se ver um quadrado vermelho no minimap = FUNCIONOU!
   - Se ainda estiver preto = problema na camera ou textura

---

## 🐛 PROBLEMA: Minimap Volta Bugado do Fullscreen

**Sintoma**: Ao voltar do fullscreen (terceira vez que aperta M), o minimap fica no centro da tela em vez de voltar pro canto superior direito.

### ✅ SOLUÇÃO: Script Já Foi Corrigido!

Atualizei o **MinimapUI.cs** com as correções. Agora ele:
- **Reseta os anchors** para top-right ANTES de animar
- **Garante a posição correta** quando volta ao modo Normal ou Semi-Transparent

### Como Aplicar a Correção:

1. **Feche o Unity** (importante!)
2. Os arquivos já foram atualizados automaticamente
3. **Abra o Unity novamente**
4. Unity vai recompilar os scripts
5. **Teste** pressionando M três vezes

Se ainda bugar, faça isso:

1. Selecione **MinimapPanel** na Hierarchy
2. No Inspector, olhe **Rect Transform**
3. **Manualmente configure:**
   - **Anchors**: Clique no quadradinho de anchors
   - **Segure Shift + Alt** (Windows) ou **Shift + Option** (Mac)
   - **Clique no preset TOP-RIGHT** (canto superior direito do grid)
   - Isso força anchors para: `(1, 1, 1, 1)` e pivot para `(1, 1)`

---

## 🔍 Checklist Completo - Ordem de Verificação

Use esta ordem para diagnosticar qualquer problema:

### 1. Layer Setup
- [ ] Layer "Minimap" existe nas Project Settings
- [ ] MinimapCamera → Culling Mask = SOMENTE "Minimap"

### 2. Camera Configuration
- [ ] MinimapCamera tem componente Camera
- [ ] Projection = Orthographic
- [ ] Position está acima do mundo (Z negativo ou positivo alto)
- [ ] Rotation = (90, 0, 0)
- [ ] Target Texture = MinimapRenderTexture (no Play Mode)

### 3. Camera Script
- [ ] MinimapCamera tem script MinimapCamera
- [ ] Player Target está assignado
- [ ] Follow Player está marcado
- [ ] Minimap Layers mostra "Minimap"

### 4. UI Setup
- [ ] MinimapPanel existe no Canvas
- [ ] MinimapPanel tem CanvasGroup
- [ ] MinimapImage (RawImage) existe dentro de MinimapPanel
- [ ] MinimapController tem MinimapUI script

### 5. Controller Setup
- [ ] MinimapController existe na cena
- [ ] Minimap Camera reference está assignada
- [ ] Minimap UI reference está assignada
- [ ] Player reference está assignada

### 6. Icons/Objects
- [ ] Player tem MinimapIcon component
- [ ] Player tem filho "Player_MinimapIcon" na Hierarchy
- [ ] Filho está na layer "Minimap"
- [ ] Icon Size > 0 (recomendado: 1.5 a 3)

### 7. Play Mode Tests
- [ ] Console mostra "[MinimapUI] Connected to MinimapCamera RenderTexture"
- [ ] Console NÃO mostra erros em vermelho
- [ ] MinimapImage.texture != None no Inspector (durante Play Mode)
- [ ] Minimap mostra algo (não está preto)

---

## 💡 Dicas de Debug

### Ver Mensagens de Debug

1. Selecione **MinimapController** na Hierarchy
2. Procure **MinimapController (Script)**
3. Marque **Enable Debug Logs** ✓
4. Faça o mesmo em **MinimapUI (Script)**
5. Entre em Play Mode
6. Olhe o Console para mensagens detalhadas

### Usar os Comandos de Teste (MUITO ÚTIL!)

1. Selecione **MinimapController** na Hierarchy (EM PLAY MODE)
2. No Inspector, procure **MinimapUI (Script)**
3. Clique com **botão direito** no nome do componente
4. Você verá opções:
   - **Debug - Check Texture Connection** ← Use este para ver se está conectado!
   - **Force Reconnect Camera** ← Use se a textura não conectou
   - **Test - Transition to Normal**
   - **Test - Transition to Semi-Transparent**
   - **Test - Transition to Fullscreen**

### Forçar Reconexão da Textura

**Se o minimap estiver preto NO PLAY MODE:**
1. Selecione **MinimapController** (em Play Mode)
2. Procure **MinimapUI (Script)** no Inspector
3. Botão direito no nome do script
4. Clique **"Force Reconnect Camera"**
5. Olhe o Console para confirmação

---

## 📸 O Que Você Deve Ver

### Configuração Correta da MinimapCamera (JOGOS 2D - PLANO XY):
```
MinimapCamera
├─ Transform
│  ├─ Position: (0, 0, -100)  ← Z NEGATIVO (na frente)
│  ├─ Rotation: (0, 0, 0)     ← ZERO! Olha para frente
│  └─ Scale: (1, 1, 1)
├─ Camera
│  ├─ Projection: Orthographic
│  ├─ Size: 10
│  ├─ Culling Mask: Minimap
│  ├─ Depth: 10
│  └─ Target Texture: MinimapRenderTexture
└─ MinimapCamera (Script)
   ├─ Player Target: [Your Player GameObject]
   ├─ Follow Player: ✓
   ├─ Minimap Layers: Minimap
   └─ Camera Distance: 100
```

⚠️ **ATENÇÃO JOGOS 2D**: A câmera DEVE ter rotação `(0, 0, 0)` para olhar para o plano XY!

### Configuração Correta do Player com Ícone:
```
Player
├─ MinimapIcon (Script)
│  ├─ Icon Type: Player
│  ├─ Icon Size: 2
│  └─ Always Visible: ✓
└─ Player_MinimapIcon (child - auto criado)
   ├─ Layer: Minimap  ← IMPORTANTE!
   └─ SpriteRenderer
      ├─ Sprite: (pode ser None)
      ├─ Color: Green
      └─ Enabled: ✓
```

---

## 🆘 Problemas Específicos

### "Console mostra: MinimapImage is null"
**Solução:**
1. Selecione MinimapController
2. Procure MinimapUI (Script)
3. Campo "Minimap Image" deve ter o RawImage
4. Se estiver None, arraste MinimapImage da Hierarchy

### "Console mostra: RenderTexture is null"
**Solução:**
1. Entre e saia do Play Mode
2. A textura é criada no Awake() da MinimapCamera
3. Se ainda null, verifique se MinimapCamera tem o script MinimapCamera

### "Console mostra: MinimapCamera not found"
**Solução:**
1. Verifique se tem GameObject chamado "MinimapCamera" na cena
2. Verifique se tem o script MinimapCamera attachado
3. Tente renomear para exatamente "MinimapCamera"

### "Minimap mostra só uma cor sólida (cinza/preto)"
**Solução:**
1. Nenhum objeto está na layer Minimap
2. Adicione MinimapIcon ao Player (passo 3 da solução rápida)
3. Ou adicione objetos visíveis na layer Minimap

### "Player não aparece no minimap"
**Solução:**
1. Verifique se Player tem MinimapIcon component
2. Expanda Player na Hierarchy, procure filho "_MinimapIcon"
3. Selecione o filho, mude Layer para "Minimap"
4. Aumente Icon Size para 2 ou 3

---

## 🎯 Se NADA Funcionar

Faça um reset completo:

1. **Delete tudo relacionado ao minimap:**
   - MinimapCamera GameObject
   - MinimapController GameObject
   - MinimapPanel no Canvas
   - MinimapIcon do Player

2. **Siga o STEP_BY_STEP_SETUP.md do zero**
   - Comece pela Parte 1 (criar layer)
   - Siga EXATAMENTE cada passo
   - Não pule nada

3. **Ou peça ajuda com:**
   - Screenshot do Inspector da MinimapCamera
   - Screenshot do Inspector da MinimapController
   - Screenshot do Console com as mensagens
   - Screenshot do que você vê (minimap preto)

---

## ✅ Quando Tudo Funcionar

Você deve ver:
- ✅ Minimap no canto superior direito
- ✅ Um ponto verde (o player) no minimap
- ✅ Minimap seguindo o player quando você anda
- ✅ M key mudando entre os 3 estados
- ✅ Console mostrando "[MinimapUI] Connected to MinimapCamera RenderTexture"
- ✅ NENHUM erro vermelho no Console

---

**Boa sorte! Se seguir este guia passo a passo, o minimap vai funcionar! 🗺️**
