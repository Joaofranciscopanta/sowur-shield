"""
Deriva variantes de cor de um sprite sheet de pixel art, preservando sombra e alfa.

Por que existe: `duck` e `Sparrow` apontavam literalmente para `Chicken_Baby.png` e
`Chicken_Baby_Brown.png` -- eram pintinhos de galinha com outro nome. Sao tambem os
dois unicos AnimalData sem animatorController, entao as duas lacunas andavam juntas.

A licenca do Sprout Lands (Cup Nooble) permite modificar os assets explicitamente
("You can modify the assets"), e permite uso comercial. O que NAO permite e
redistribuir o pack em si -- por isso isto gera variantes dentro do projeto, e o
credito obrigatorio esta no CREDITS.md.

O metodo e hue-shift em HSV mantendo saturacao e valor relativos: e isso que faz a
variante continuar a parecer do mesmo pack, em vez de uma mancha de cor por cima. Os
pixels quase-cinzentos (bico, olho, patas) ficam de fora por um piso de saturacao,
senao o bico laranja seguiria a rotacao e o bicho perdia a leitura.

Uso:
    python Tools/recolor_sprites.py
"""

import colorsys
import os
import sys

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow nao instalado: pip install Pillow")

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PACK = os.path.join(
    RAIZ, "Assets", "Art", "ThirdParty",
    "Sprout Lands - Sprites - premium pack", "Animals", "Chicken_Baby")
SAIDA = os.path.join(RAIZ, "Assets", "Art", "Generated", "Animals")

# Abaixo deste piso de saturacao o pixel e tratado como neutro e nao roda de matiz:
# bico, olhos e patas mantem-se, o que preserva a leitura do bicho.
PISO_SATURACAO = 0.25

# O sheet nao e so plumagem: tem uma fila de CORACOES (o feedback de afeto, partilhado
# por todos os animais) e ovos. Rodar tudo em bloco pintou os coracoes de verde no pato
# e de cinzento no pardal -- vistos no primeiro resultado. Os coracoes vivem na banda
# magenta/vermelha do circulo, entao ficam de fora por matiz.
CORACAO_MIN, CORACAO_MAX = 0.90, 1.00   # ~324deg..360deg
CORACAO_MIN2, CORACAO_MAX2 = 0.0, 0.04  # e o que passa de 0 para o outro lado


def _e_coracao(h):
    return (CORACAO_MIN <= h <= CORACAO_MAX) or (CORACAO_MIN2 <= h <= CORACAO_MAX2)


def recolorir(origem, destino, giro, satura=1.0, clareia=1.0, escurece=0.0,
              piso_saturacao=PISO_SATURACAO, proteger_coracoes=True):
    """
    giro:     rotacao de matiz em voltas (0..1). 0.5 = oposto no circulo cromatico.
    satura:   multiplicador de saturacao (1 = igual).
    clareia:  multiplicador de valor/brilho (1 = igual).
    escurece: subtrai valor DEPOIS do resto. Castanho nao e um matiz proprio -- e
              amarelo escurecido e dessaturado; tentar chegar la so por giro produz
              verde-azeitona, que foi o que saiu na primeira tentativa do pardal.
    """
    img = Image.open(origem).convert("RGBA")
    px = img.load()
    largura, altura = img.size
    tocados = 0

    for y in range(altura):
        for x in range(largura):
            r, g, b, a = px[x, y]
            if a == 0:
                continue  # transparente fica transparente: o alfa nunca e mexido

            h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
            if s < piso_saturacao:
                continue  # neutro: bico/olho/pata
            if proteger_coracoes and _e_coracao(h):
                continue  # coracoes de afeto: sao UI partilhada, ficam rosa

            h = (h + giro) % 1.0
            s = min(1.0, s * satura)
            v = max(0.0, min(1.0, v * clareia - escurece))

            nr, ng, nb = colorsys.hsv_to_rgb(h, s, v)
            px[x, y] = (int(nr * 255), int(ng * 255), int(nb * 255), a)
            tocados += 1

    os.makedirs(os.path.dirname(destino), exist_ok=True)
    img.save(destino)
    print(f"  {os.path.basename(destino):32s} {largura}x{altura}  {tocados} pixels")


def main():
    if not os.path.isdir(PACK):
        sys.exit(f"Pack nao encontrado: {PACK}")

    base = os.path.join(PACK, "Chicken_Baby.png")

    print("Gerando variantes:")
    # Pato: verde-azulado de cabeca de pato-real, um pouco mais escuro e saturado.
    recolorir(base, os.path.join(SAIDA, "Duck_Generated.png"),
              giro=0.42, satura=1.15, clareia=0.92)

    # Pardal: castanho terroso. O amarelo do pintinho ja esta quase no matiz certo --
    # o que falta e ESCURECER e dessaturar. Girar mais leva a verde-azeitona.
    recolorir(base, os.path.join(SAIDA, "Sparrow_Generated.png"),
              giro=-0.02, satura=0.70, clareia=0.72, escurece=0.10)

    # ------------------------------------------------------------------
    # Golens: os 2 unicos inimigos dos 34 sem sprite -- renderizavam como uma
    # esfera cinza via CombatUnit.CreateSphereVisual().
    #
    # ⚠️ Isto e um PALIATIVO assumido, nao arte final. A silhueta e a mesma do
    # Frost Golem, so muda o mineral. Serve para o inimigo existir e ler no
    # combate; o Lucas ja disse que tudo aqui e placeholder e vai ser trocado.
    # ------------------------------------------------------------------
    golem = os.path.join(
        RAIZ, "Assets", "Art", "Enemies", "Mountain",
        "Enemy 23 — Frost Golem.png")

    if os.path.isfile(golem):
        saida_inimigos = os.path.join(RAIZ, "Assets", "Art", "Generated", "Enemies")

        # ⚠️ Piso de saturacao BAIXO aqui, ao contrario dos pintinhos: o halo destes
        # sheets e um azul lavado (saturacao ~0,15) e com o piso normal ficava de
        # fora -- o primeiro resultado tinha corpo de ferrugem dentro de um brilho
        # cyan de gelo, que se contradiziam. Estes sheets nao tem coracoes, entao a
        # protecao tambem sai.
        recolorir(golem, os.path.join(saida_inimigos, "IronGolem_Generated.png"),
                  giro=0.52, satura=0.78, clareia=0.88,
                  piso_saturacao=0.0, proteger_coracoes=False)

        # Obsidiana: vidro vulcanico, que e PRETO. Nao se chega la por matiz -- a
        # primeira tentativa (giro 0.60) deu azeitona. O caminho e tirar quase toda a
        # saturacao e baixar muito o valor, deixando so um resto quente nas fissuras.
        recolorir(golem, os.path.join(saida_inimigos, "ObsidianGolem_Generated.png"),
                  giro=0.52, satura=0.30, clareia=0.42, escurece=0.05,
                  piso_saturacao=0.0, proteger_coracoes=False)
    else:
        print(f"  (Frost Golem nao encontrado, golens ignorados: {golem})")

    print("\nFeito. Reimportar no Unity com o mesmo slicing do sheet original\n"
          "(Sprite Mode: Multiple, grelha de 16x16).")


if __name__ == "__main__":
    main()
