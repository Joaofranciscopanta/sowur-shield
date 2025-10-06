# 🔧 Fix: Minimap Não Redimensiona no Fullscreen

## 🎯 Problema

Quando aperta M para ir ao fullscreen, o minimap não redimensiona para 800x800 (fica pequeno ou não muda de tamanho).

---

## ✅ Solução Rápida (3 passos)

### Passo 1: Verifique o Valor no Inspector

1. Entre em **Play Mode**
2. Selecione **MinimapController** na Hierarchy
3. Procure **MinimapUI (Script)** no Inspector
4. Olhe a seção **Position Settings**:
   - **Fullscreen Size**: Deve estar `X: 800, Y: 800`

❌ **Se estiver diferente** (ex: 200x200 ou outro valor):
- O valor foi resetado ou não foi salvo
- Continue para o Passo 2

✅ **Se já estiver 800x800**:
- O problema pode ser na animação
- Continue para o Passo 3

### Passo 2: Force o Tamanho Correto (EM PLAY MODE)

1. **Mantenha em Play Mode**
2. Selecione **MinimapController**
3. Procure **MinimapUI (Script)**
4. Clique com **botão direito** no título do componente
5. Escolha **"Debug - Force Fullscreen Size"**
6. Olhe o Console - deve aparecer: `[MinimapUI] Forced fullscreen size to: (800, 800)`
7. O minimap deve crescer para 800x800 imediatamente

✅ **Se funcionou**:
- O problema era o valor de fullscreenSize
- Saia do Play Mode
- Vá para "Corrigir Permanentemente" abaixo

❌ **Se ainda não cresceu**:
- Pode haver restrições no RectTransform
- Vá para o Passo 3

### Passo 3: Verifique Restrições do RectTransform

1. **Ainda em Play Mode**
2. Selecione **MinimapPanel** (filho do Canvas)
3. No Inspector, olhe **Rect Transform**
4. Clique com **botão direito** em **Size Delta**
5. Escolha **"Debug - Check Size Settings"** no MinimapUI
6. Olhe o Console e veja os valores

**Exemplo de saída esperada:**
```
[MinimapUI] Normal Size: (200, 200)
[MinimapUI] Fullscreen Size: (800, 800)
[MinimapUI] Current Size Delta: (200, 200)  ← deve mudar para 800 no fullscreen
[MinimapUI] Current Anchors: Min=(0.5, 0.5), Max=(0.5, 0.5)
```

---

## 🛠️ Corrigir Permanentemente

### Opção A: Configurar no Inspector (FORA DO PLAY MODE)

1. **Saia do Play Mode** (importante!)
2. Selecione **MinimapController**
3. Procure **MinimapUI (Script)**
4. Na seção **Position Settings**:
   - **Normal Position**: `X: -100, Y: -100`
   - **Normal Size**: `X: 200, Y: 200`
   - **Fullscreen Size**: `X: 800, Y: 800` ← IMPORTANTE!

5. **Salve a cena** (Ctrl+S)
6. Entre em Play Mode e teste

### Opção B: Criar um Prefab (Recomendado)

Depois de configurar corretamente:

1. Selecione **MinimapController** na Hierarchy
2. Arraste para a pasta **Assets/Prefabs**
3. Isso salva as configurações como prefab
4. Sempre que precisar, use o prefab

---

## 🔍 Diagnóstico Avançado

### Teste 1: Verificar se o Canvas Scaler Está Afetando

1. Selecione **Canvas** (pai do MinimapPanel)
2. Procure **Canvas Scaler** no Inspector
3. Verifique:
   - **UI Scale Mode**: Scale With Screen Size
   - **Reference Resolution**: 1920 x 1080
   - **Match**: 0.5

Se estiver diferente, o tamanho pode estar sendo ajustado pelo Canvas Scaler.

### Teste 2: Verificar Layout Groups

1. Selecione **MinimapPanel**
2. Verifique se tem componentes:
   - Layout Element
   - Aspect Ratio Fitter
   - Content Size Fitter

❌ **Se tiver algum desses**:
- Eles podem estar forçando um tamanho fixo
- **Remova ou desabilite** esses componentes

### Teste 3: Verificar Hierarquia

O MinimapPanel deve estar assim:

```
Canvas
└─ MinimapPanel          ← MinimapUI script aqui
   └─ MinimapImage       ← RawImage aqui
```

❌ **Se estiver diferente**:
- O script pode estar no objeto errado
- Verifique se minimapPanel reference aponta para o RectTransform correto

---

## 🎮 Teste Final

Depois de corrigir:

1. **Entre em Play Mode**
2. **Pressione M** duas vezes para ir ao fullscreen
3. **Olhe o Console** - deve aparecer:
   ```
   [MinimapUI] Transitioning to Fullscreen mode (target size: (800, 800))
   [MinimapUI] Fullscreen transition started. Current size: (200, 200), Target: (800, 800)
   ```
4. O minimap deve **crescer suavemente** para 800x800
5. Deve ficar **centralizado** na tela

✅ **Tamanhos esperados:**
- Normal: 200x200 (canto)
- Semi-transparente: 200x200 (canto)
- Fullscreen: 800x800 (centro)

---

## 🐛 Problemas Comuns

### "O valor volta para 200x200 quando saio do Play Mode"

**Causa**: Mudanças feitas EM Play Mode não são salvas.

**Solução**:
1. **Copie** os valores enquanto está em Play Mode
2. **Saia** do Play Mode
3. **Cole** os valores com o jogo parado
4. **Salve** a cena (Ctrl+S)

### "O tamanho muda, mas muito devagar"

**Causa**: Transition Duration muito alta.

**Solução**:
1. Selecione **MinimapController**
2. Procure **MinimapController (Script)**
3. **Transition Duration**: Reduza para `0.3` ou `0.2`

### "O minimap cresce, mas fica cortado"

**Causa**: Canvas ou tela muito pequenos.

**Solução**:
1. Use tamanho menor: `600x600` em vez de `800x800`
2. Ou aumente o Canvas/Game View
3. Para 1920x1080, 800x800 funciona bem

---

## 📋 Checklist de Verificação

Fora do Play Mode:
- [ ] MinimapController tem MinimapUI script
- [ ] Fullscreen Size = (800, 800)
- [ ] Cena salva (Ctrl+S)

Em Play Mode:
- [ ] Console mostra "target size: (800, 800)"
- [ ] Minimap cresce ao pressionar M duas vezes
- [ ] Minimap fica centralizado em 800x800

Se todos ✓, está funcionando!

---

## 🆘 Ainda Não Funciona?

1. **Use o comando de debug**:
   - Play Mode
   - MinimapController → MinimapUI Script
   - Botão direito → "Debug - Check Size Settings"
   - Copie a saída do Console e reporte

2. **Verifique a hierarquia**:
   - MinimapPanel deve ter um RectTransform
   - minimapPanel reference deve apontar para ele

3. **Reset completo**:
   - Delete MinimapPanel
   - Recrie seguindo STEP_BY_STEP_SETUP.md Parte 3
   - Configure Fullscreen Size = (800, 800)

---

**Com essas correções, o fullscreen deve redimensionar corretamente para 800x800! 🎯**
