"""
Desenha os 2 icones de skill que faltavam: FeatherShield e HerdBond.

Contexto: a documentacao dizia "todas as 19 skills sem icone". Medindo, eram **2** --
as outras 17 ja tinham. Mais um caso de doc que exagera a lacuna; ver
feedback_verify_by_running_not_reading.

Estilo dos icones existentes (medido em skill_flock_instinct.png e vizinhos):
  - ~600-900px, fundo TRANSPARENTE, sem moldura
  - uma silhueta escura legivel sobre um gesto de cor (a "energia" da skill)
  - um matiz dominante por skill

Isto e geometria, nao pintura -- e por isso e o tipo de asset que da para gerar por
codigo sem ficar pior que os vizinhos. Continua a ser placeholder.

Uso:
    python Tools/make_skill_icons.py
"""

import math
import os
import sys

try:
    from PIL import Image, ImageDraw, ImageFilter
except ImportError:
    sys.exit("Pillow nao instalado: pip install Pillow")

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SAIDA = os.path.join(RAIZ, "Assets", "Art", "UI", "Skills")

TAM = 768          # desenha grande e reduz: e o que suaviza as bordas
ESCALA = 3         # supersampling


def _nova():
    return Image.new("RGBA", (TAM * ESCALA, TAM * ESCALA), (0, 0, 0, 0))


def _guardar(img, nome):
    img = img.resize((TAM, TAM), Image.LANCZOS)
    os.makedirs(SAIDA, exist_ok=True)
    caminho = os.path.join(SAIDA, nome)
    img.save(caminho)
    print(f"  {nome:28s} {TAM}x{TAM}")
    return caminho


def _pena(d, cx, cy, comprimento, largura, angulo, cor, cor_raque):
    """Uma pena: barbas em volta de uma raque central."""
    rad = math.radians(angulo)
    dx, dy = math.cos(rad), math.sin(rad)
    px, py = -dy, dx  # perpendicular

    # Barbas assimetricas em volta da raque.
    #
    # ⚠️ Um perfil simetrico e pontiagudo nao le como PENA -- le como raio de
    # estrela; tres iteracoes ficaram com cara de starburst. O que distingue uma
    # pena e (a) ser mais gorda de um lado do que do outro, (b) ter a base
    # arredondada em vez de em bico, e (c) a ponta cair para um lado.
    passos = 90
    esq, dir_ = [], []
    for i in range(passos + 1):
        t = i / passos
        # base redonda (sobe depressa), barriga a ~35%, ponta que afina devagar
        perfil = (math.sin(min(1.0, t * 1.55) * math.pi * 0.5) ** 0.55) * ((1.0 - t) ** 0.42)
        # curvatura: a pena verga, entao o eixo desloca-se ao longo do comprimento
        verga = math.sin(t * math.pi) * largura * 0.55

        bx = cx + dx * comprimento * (t - 0.5) + px * verga
        by = cy + dy * comprimento * (t - 0.5) + py * verga

        esq.append((bx + px * largura * perfil * 1.15, by + py * largura * perfil * 1.15))
        dir_.append((bx - px * largura * perfil * 0.72, by - py * largura * perfil * 0.72))
    d.polygon(esq + dir_[::-1], fill=cor)

    # Raque: segue a MESMA curva das barbas. A reta original atravessava a pena
    # vergada na diagonal e parecia um risco por cima.
    eixo = []
    for i in range(0, passos + 1, 6):
        t = i / passos
        verga = math.sin(t * math.pi) * largura * 0.55
        eixo.append((cx + dx * comprimento * (t - 0.5) + px * verga,
                     cy + dy * comprimento * (t - 0.5) + py * verga))
    d.line(eixo, fill=cor_raque, width=max(1, int(largura * 0.14)), joint="curve")


def feather_shield():
    """Ativa, defensiva, afeta o proprio: escudo de penas."""
    img = _nova()
    d = ImageDraw.Draw(img)
    c = TAM * ESCALA // 2

    AZUL_CLARO = (150, 214, 240, 255)
    AZUL = (86, 160, 205, 255)
    AZUL_FUNDO = (140, 200, 232, 70)
    ESCURO = (28, 48, 68, 255)

    # halo de energia por tras
    r = int(TAM * ESCALA * 0.40)
    d.ellipse([c - r, c - r, c + r, c + r], fill=AZUL_FUNDO)

    # Penas em leque LARGO, saindo pelos lados do escudo.
    #
    # Na primeira versao estavam quase na vertical atras do escudo: so as pontas
    # apareciam por cima, e liam-se como espigoes, nao como penas -- a skill perdia
    # o nome. Abrindo o leque e baixando o centro, a pena inteira fica visivel.
    for ang, comp in ((-152, 0.62), (-118, 0.66), (-62, 0.66), (-28, 0.62)):
        _pena(d, c, int(c + TAM * ESCALA * 0.02),
              int(TAM * ESCALA * comp), int(TAM * ESCALA * 0.075),
              ang, AZUL_CLARO, AZUL)

    # Escudo por cima: silhueta escura, que e o que da leitura a 64px.
    #
    # MENOR que o leque de proposito. Com o escudo grande, so as pontas das penas
    # saiam de tras dele e liam-se como espigoes -- duas tentativas ficaram assim.
    # Encolhendo o escudo, ve-se a pena inteira e o nome da skill volta a ler-se.
    larg = TAM * ESCALA * 0.215
    topo = c - TAM * ESCALA * 0.145
    base = c + TAM * ESCALA * 0.255
    ombro = topo + (base - topo) * 0.34

    contorno = [
        (c - larg, topo), (c + larg, topo),
        (c + larg, ombro),
        (c + larg * 0.80, ombro + (base - ombro) * 0.45),
        (c, base),
        (c - larg * 0.80, ombro + (base - ombro) * 0.45),
        (c - larg, ombro),
    ]
    d.polygon(contorno, fill=ESCURO)

    # brilho interior, deslocado para cima-esquerda (luz consistente com o pack)
    interior = [(x + (c - x) * 0.30 - TAM * ESCALA * 0.012,
                 y + (c - y) * 0.30 - TAM * ESCALA * 0.020) for (x, y) in contorno]
    d.polygon(interior, fill=AZUL)

    return _guardar(img, "skill_feather_shield.png")


def herd_bond():
    """Passiva, +15% atq/def, vem da felicidade: dois elos ligados."""
    img = _nova()
    d = ImageDraw.Draw(img)
    c = TAM * ESCALA // 2

    QUENTE = (242, 186, 92, 255)
    QUENTE_ESC = (196, 130, 48, 255)
    QUENTE_FUNDO = (245, 196, 110, 65)
    ESCURO = (62, 40, 22, 255)

    r = int(TAM * ESCALA * 0.40)
    d.ellipse([c - r, c - r, c + r, c + r], fill=QUENTE_FUNDO)

    # dois aneis entrelacados: o vinculo do rebanho
    raio = int(TAM * ESCALA * 0.185)
    esp = int(TAM * ESCALA * 0.062)
    desloc = int(raio * 0.78)

    for (cx, cy) in ((c - desloc, c), (c + desloc, c)):
        # contorno escuro primeiro, depois o miolo quente: da o mesmo
        # "outline" desenhado a mao que os icones existentes tem
        d.ellipse([cx - raio - esp // 2, cy - raio - esp // 2,
                   cx + raio + esp // 2, cy + raio + esp // 2],
                  outline=ESCURO, width=int(esp * 1.5))
        d.ellipse([cx - raio, cy - raio, cx + raio, cy + raio],
                  outline=QUENTE, width=esp)

    # O anel da ESQUERDA passa por cima no cruzamento de baixo.
    #
    # A primeira tentativa recortava a regiao e colava-a no MESMO sitio, o que nao
    # faz nada -- os aneis ficavam so sobrepostos. O jeito e redesenhar o arco
    # inferior esquerdo por cima, depois de os dois aneis ja estarem pintados.
    cx_esq = c - desloc
    caixa_esq = [cx_esq - raio - esp // 2, c - raio - esp // 2,
                 cx_esq + raio + esp // 2, c + raio + esp // 2]
    d.arc(caixa_esq, start=20, end=160, fill=ESCURO, width=int(esp * 1.5))
    caixa_in = [cx_esq - raio, c - raio, cx_esq + raio, c + raio]
    d.arc(caixa_in, start=20, end=160, fill=QUENTE, width=esp)

    # coracao pequeno no centro: a felicidade e a condicao de desbloqueio
    h = TAM * ESCALA * 0.070
    hy = c + TAM * ESCALA * 0.008
    d.ellipse([c - h * 1.05, hy - h * 0.95, c - h * 0.05, hy + h * 0.05], fill=QUENTE_ESC)
    d.ellipse([c + h * 0.05, hy - h * 0.95, c + h * 1.05, hy + h * 0.05], fill=QUENTE_ESC)
    d.polygon([(c - h * 1.02, hy - h * 0.30), (c + h * 1.02, hy - h * 0.30),
               (c, hy + h * 1.15)], fill=QUENTE_ESC)

    return _guardar(img, "skill_herd_bond.png")


def main():
    print("Gerando icones de skill:")
    feather_shield()
    herd_bond()
    print("\nFeito.")


if __name__ == "__main__":
    main()
