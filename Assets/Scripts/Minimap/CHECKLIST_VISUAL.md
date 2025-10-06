# ✅ Checklist Visual - Minimap 2D

## 🎯 Como Verificar se Está Tudo Certo

### 1️⃣ ROTAÇÃO DA CÂMERA (MAIS IMPORTANTE!)

```
Unity Inspector → MinimapCamera → Transform:

┌─────────────────────────────┐
│ Transform                   │
├─────────────────────────────┤
│ Position                    │
│   X: 0                      │
│   Y: 0                      │
│   Z: -100                   │
│                             │
│ Rotation     ← OLHE AQUI!   │
│   X: 0       ← DEVE SER 0!  │
│   Y: 0       ← DEVE SER 0!  │
│   Z: 0       ← DEVE SER 0!  │
│                             │
│ Scale                       │
│   X: 1                      │
│   Y: 1                      │
│   Z: 1                      │
└─────────────────────────────┘
```

❌ **ERRADO**: Rotation (90, 0, 0) ou qualquer outro valor
✅ **CERTO**: Rotation (0, 0, 0)

---

### 2️⃣ CULLING MASK DA CÂMERA

```
Unity Inspector → MinimapCamera → Camera:

┌─────────────────────────────┐
│ Camera                      │
├─────────────────────────────┤
│ Projection: Orthographic    │
│ Size: 10                    │
│                             │
│ Culling Mask  ← OLHE AQUI!  │
│ ☐ Everything                │
│ ☐ Default                   │
│ ☑ Minimap     ← SÓ ESTE!    │
│ ☐ UI                        │
│ ☐ ... (outros)              │
└─────────────────────────────┘
```

❌ **ERRADO**: Everything, Mixed, ou sem Minimap
✅ **CERTO**: SOMENTE "Minimap" marcado

---

### 3️⃣ SCRIPT MINIMAP CAMERA

```
Unity Inspector → MinimapCamera → MinimapCamera (Script):

┌─────────────────────────────────────┐
│ MinimapCamera (Script)              │
├─────────────────────────────────────┤
│ Camera Settings                     │
│   Minimap Cam: Camera               │
│   Default Ortho Size: 10            │
│   Minimap Layers: Minimap           │
│   Camera Distance: 100  ← DIST Z!   │
│                                     │
│ Follow Settings                     │
│   Player Target: [Player] ← ASSIGN! │
│   Follow Player: ✓       ← CHECK!   │
│   Follow Smoothness: 5              │
└─────────────────────────────────────┘
```

✅ **Player Target** deve ter seu Player GameObject
✅ **Follow Player** deve estar marcado
✅ **Camera Distance** (não "Camera Height")

---

### 4️⃣ PLAYER COM ÍCONE

```
Hierarchy:

Player                          ← Seu player
├─ PlayerSprite                 ← Sprite do player
├─ Collider                     ← Collisão
└─ Player_MinimapIcon           ← DEVE EXISTIR!
   └─ Layer: Minimap            ← DEVE SER MINIMAP!

Inspector do Player:

┌─────────────────────────────────────┐
│ MinimapIcon (Script)                │
├─────────────────────────────────────┤
│ Icon Type: Player                   │
│ Icon Color: ■ Verde                 │
│ Icon Size: 2                        │
│ Always Visible: ✓                   │
│ Minimap Layer Name: Minimap         │
└─────────────────────────────────────┘
```

✅ Player tem componente **MinimapIcon**
✅ Filho **"_MinimapIcon"** existe na Hierarchy
✅ Filho está na layer **"Minimap"**

---

### 5️⃣ CONSOLE NO PLAY MODE

```
Console (Window → General → Console):

✅ MENSAGENS CORRETAS:
[MinimapCamera] Camera initialized at position: (x, y, -100), rotation: (0, 0, 0)
[MinimapCamera] Camera setup complete for 2D (XY plane)
[MinimapUI] Connected to MinimapCamera RenderTexture: MinimapRenderTexture

❌ NÃO DEVE TER:
NullReferenceException
MinimapCamera not found
RenderTexture is null
```

✅ Mensagens verdes/brancas = OK
❌ Mensagens vermelhas = ERRO

---

### 6️⃣ MINIMAP NO JOGO

```
Tela do Jogo em Play Mode:

┌──────────────────────────────────┐
│                         ┌──────┐ │
│                         │  ▪   │ │ ← Minimap no canto
│                         │ ■    │ │    ■ = Player (verde)
│                         └──────┘ │    ▪ = Outros objetos
│                                  │
│                                  │
│          [PLAYER]                │
│                                  │
│                                  │
└──────────────────────────────────┘
```

✅ Minimap aparece no canto superior direito
✅ Quadrado **VERDE** (player) está visível
✅ Minimap **segue** o player quando você anda

---

### 7️⃣ TECLA M (3 ESTADOS)

```
Estado 1: NORMAL
┌──────────────────────────────────┐
│                         ┌──────┐ │
│                         │  ▪   │ │ ← Canto, opacidade 100%
│                         │ ■    │ │
│                         └──────┘ │
└──────────────────────────────────┘

↓ Aperta M

Estado 2: SEMI-TRANSPARENTE
┌──────────────────────────────────┐
│                         ┌──────┐ │
│                         │  ▪   │ │ ← Canto, opacidade 50%
│                         │ ■    │ │    (mais transparente)
│                         └──────┘ │
└──────────────────────────────────┘

↓ Aperta M

Estado 3: FULLSCREEN
┌──────────────────────────────────┐
│                                  │
│         ┌──────────────┐         │
│         │              │         │
│         │      ▪       │         │ ← Centro, grande
│         │     ■        │         │    com zoom/pan
│         │              │         │
│         └──────────────┘         │
│                                  │
└──────────────────────────────────┘

↓ Aperta M ou ESC

Estado 1: NORMAL (volta pro canto!)
┌──────────────────────────────────┐
│                         ┌──────┐ │
│                         │  ▪   │ │ ← Deve voltar AQUI!
│                         │ ■    │ │    Não no centro!
│                         └──────┘ │
└──────────────────────────────────┘
```

✅ M alterna entre os 3 estados
✅ Volta pro **canto superior direito** (NÃO fica no centro!)
✅ ESC fecha o fullscreen

---

## 🔧 Se Algo Estiver Errado

### ❌ Rotation não é (0, 0, 0)?
**FIX RÁPIDO:**
1. Selecione MinimapCamera
2. No Inspector → MinimapCamera (Script)
3. Botão direito no título do componente
4. Escolha **"Reset Camera for 2D (XY Plane)"**

### ❌ Minimap está preto?
**VERIFIQUE:**
1. ✅ Rotation = (0, 0, 0)
2. ✅ Culling Mask = Minimap
3. ✅ Player tem MinimapIcon
4. ✅ Filho do Player na layer Minimap

### ❌ Volta do fullscreen buga?
**JÁ CORRIGIDO!**
- Feche o Unity
- Abra novamente (recompila scripts)
- Teste

---

## ✅ Tudo Certo!

Se todos os checks acima estiverem ✅, seu minimap está **100% funcional!**

```
🎉 PARABÉNS! 🎉

    ┌──────┐
    │  ▪   │
    │ ■    │  ← Minimap funcionando!
    └──────┘

Aperte M e divirta-se! 🎮
```

---

**Precisa de mais ajuda?** Veja **TROUBLESHOOTING_GUIDE_PT.md**
