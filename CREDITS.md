# Créditos

## Arte

### Sprout Lands — Premium Pack
**Assets — From: Sprout Lands — By: Cup Nooble**

- Site do autor: https://cupnooble.carrd.co
- Pasta no projeto: `Assets/Art/ThirdParty/Sprout Lands - Sprites - premium pack/`

A licença do pack **exige crédito**, e permite explicitamente modificar os assets e
usá-los em projeto comercial. O que ela **não** permite é redistribuir o pack em si,
mesmo modificado — só o jogo feito com ele.

⚠️ **Isto tem consequência para este repositório.** O pack está commitado em
`Assets/Art/ThirdParty/`, e o repositório é público. Um repositório público que contém
o pack completo é, na prática, redistribuição do pack — que é justamente o que a licença
proíbe. Ver a nota em *Pendências* no fim deste arquivo.

Sprites derivados por recoloração (permitido por "You can modify the assets"):

| Gerado | Origem |
|---|---|
| `Assets/Art/Generated/Animals/Duck_Generated.png` | `Chicken_Baby.png` |
| `Assets/Art/Generated/Animals/Sparrow_Generated.png` | `Chicken_Baby.png` |

Gerados por `Tools/recolor_sprites.py` (deslocamento de matiz em HSV, preservando
sombra e alfa).

### Arte de inimigos

`Assets/Art/Enemies/` — origem não registrada no repositório. As variantes abaixo são
derivadas por recoloração do *Frost Golem*, como **paliativo assumido** até haver arte
própria: a silhueta é a mesma, muda o mineral.

| Gerado | Origem |
|---|---|
| `Assets/Art/Generated/Enemies/IronGolem_Generated.png` | `Enemy 23 — Frost Golem.png` |
| `Assets/Art/Generated/Enemies/ObsidianGolem_Generated.png` | `Enemy 23 — Frost Golem.png` |

### Ícones de habilidade

`skill_feather_shield.png` e `skill_herd_bond.png` foram **desenhados por código**
(`Tools/make_skill_icons.py`), sem origem de terceiros. São placeholders geométricos:
leem bem a 64px, mas não têm a pintura dos outros 17 ícones.

---

## Pendências de licenciamento

- [ ] **Decidir o que fazer com o pack commitado num repo público.** As opções são
  comprar/obter uma licença que permita, remover o pack do histórico e distribuí-lo só
  na build, ou tornar o repositório privado. Isto não é urgente para desenvolver, mas é
  real — e não foi criado por nenhuma alteração recente; o pack já estava aqui.
- [ ] Registrar a origem e a licença da arte de `Assets/Art/Enemies/`, que hoje não
  está documentada em lugar nenhum.
