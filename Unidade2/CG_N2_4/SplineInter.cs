using CG_Biblioteca;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace gcgcg
{
  /// <summary>
  /// Representa uma Spline de Interpolação (Catmull-Rom).
  /// A curva passa por todos os pontos de controle.
  /// Herda de <see cref="Objeto"/>.
  /// </summary>
  internal class SplineInter : Objeto
  {
    // Índice do ponto de controle atualmente selecionado (destacado em vermelho)
    private int ptoSelecionado = 0;

    // Rótulo interno para criar objetos filhos
    private char rotuloAtual;

    // Quantidade máxima e atual de pontos calculados na curva
    private readonly int splineQtdPtoMax = 100;
    private int splineQtdPto = 10;

    // Poliedro de controle (desenhado em ciano)
    private Poligono poliedroControle;

    // Lista de objetos Ponto visuais (pontos de controle)
    private readonly List<Ponto> pontosControleVisuais = [];

    /// <summary>
    /// Cria a SplineInter com 4 pontos de controle padrão.
    /// </summary>
    public SplineInter(Objeto _paiRef, ref char _rotulo) : base(_paiRef, ref _rotulo)
    {
      rotuloAtual = _rotulo;

      PrimitivaTipo = PrimitiveType.LineStrip;
      PrimitivaTamanho = 1;

      PontosControle();
      PoliedroControle();
      Atualizar();

      _rotulo = rotuloAtual;
    }

    /// <summary>
    /// Cria os 4 pontos de controle iniciais como objetos Ponto filhos desta spline.
    /// </summary>
    private void PontosControle()
    {
      List<Ponto4D> posicoes =
      [
        new Ponto4D(-0.5, -0.5),
        new Ponto4D(-0.1,  0.4),
        new Ponto4D( 0.1,  0.4),
        new Ponto4D( 0.5, -0.5),
      ];

      foreach (var pos in posicoes)
      {
        var pto = new Ponto(this, ref rotuloAtual, pos);
        pontosControleVisuais.Add(pto);
      }

      AtualizarCorPontos();
    }

    /// <summary>
    /// Cria o poliedro de controle (linha ciano conectando os pontos de controle).
    /// </summary>
    private void PoliedroControle()
    {
      List<Ponto4D> pts = [];
      foreach (var pv in pontosControleVisuais)
        pts.Add(new Ponto4D(pv.ObterPosicao()));

      poliedroControle = new Poligono(this, ref rotuloAtual, pts)
      {
        PrimitivaTipo = PrimitiveType.LineStrip,
        ShaderObjeto = new Shader("Shaders/shader.vert", "Shaders/shaderCiano.frag")
      };
    }

    /// <summary>
    /// Recalcula os pontos da curva usando interpolação Catmull-Rom.
    /// A curva passa por todos os pontos de controle (exceto os extremos que servem de tangente).
    /// </summary>
    public void Atualizar()
    {
      pontosLista.Clear();

      int n = pontosControleVisuais.Count;
      if (n < 4) return;

      // Catmull-Rom: segmentos de p1 até p[n-2] (os extremos são tangentes)
      for (int seg = 0; seg < n - 3; seg++)
      {
        Ponto4D p0 = pontosControleVisuais[seg].ObterPosicao();
        Ponto4D p1 = pontosControleVisuais[seg + 1].ObterPosicao();
        Ponto4D p2 = pontosControleVisuais[seg + 2].ObterPosicao();
        Ponto4D p3 = pontosControleVisuais[seg + 3].ObterPosicao();

        int passosSegmento = (seg == n - 4) ? splineQtdPto : splineQtdPto - 1;

        for (int i = 0; i <= passosSegmento; i++)
        {
          double t = (double)i / splineQtdPto;
          Ponto4D pto = CatmullRom(p0, p1, p2, p3, t);
          pontosLista.Add(pto);
        }
      }

      base.ObjetoAtualizar();
      AtualizarPoliedro();
    }

    /// <summary>
    /// Calcula um ponto na curva Catmull-Rom para um dado t em [0,1].
    /// Fórmula: 0.5 * [(2*P1) + (-P0+P2)*t + (2P0-5P1+4P2-P3)*t² + (-P0+3P1-3P2+P3)*t³]
    /// </summary>
    private static Ponto4D CatmullRom(Ponto4D p0, Ponto4D p1, Ponto4D p2, Ponto4D p3, double t)
    {
      double t2 = t * t;
      double t3 = t2 * t;

      double x = 0.5 * (
        (2.0 * p1.X) +
        (-p0.X + p2.X) * t +
        (2.0 * p0.X - 5.0 * p1.X + 4.0 * p2.X - p3.X) * t2 +
        (-p0.X + 3.0 * p1.X - 3.0 * p2.X + p3.X) * t3
      );

      double y = 0.5 * (
        (2.0 * p1.Y) +
        (-p0.Y + p2.Y) * t +
        (2.0 * p0.Y - 5.0 * p1.Y + 4.0 * p2.Y - p3.Y) * t2 +
        (-p0.Y + 3.0 * p1.Y - 3.0 * p2.Y + p3.Y) * t3
      );

      return new Ponto4D(x, y);
    }

    /// <summary>
    /// Atualiza as posições do poliedro de controle.
    /// </summary>
    private void AtualizarPoliedro()
    {
      if (poliedroControle == null) return;

      poliedroControle.AtualizarPontos(
        pontosControleVisuais.ConvertAll(p => new Ponto4D(p.ObterPosicao()))
      );
    }

    /// <summary>
    /// Altera a quantidade de pontos calculados na spline.
    /// </summary>
    public void SplineQtdPto(int inc)
    {
      splineQtdPto = Math.Clamp(splineQtdPto + inc, 2, splineQtdPtoMax);
      Atualizar();
    }

    /// <summary>
    /// Move o ponto de controle selecionado e recalcula a curva.
    /// </summary>
    public void AtualizarSpline(Ponto4D ptoInc, bool proximo)
    {
      if (proximo)
      {
        ptoSelecionado = (ptoSelecionado + 1) % pontosControleVisuais.Count;
        AtualizarCorPontos();
        return;
      }

      var posAtual = pontosControleVisuais[ptoSelecionado].ObterPosicao();
      pontosControleVisuais[ptoSelecionado].AtualizarPosicao(
        new Ponto4D(posAtual.X + ptoInc.X, posAtual.Y + ptoInc.Y)
      );

      Atualizar();
    }

    /// <summary>
    /// Atualiza a cor dos pontos de controle: selecionado = vermelho, demais = branco.
    /// </summary>
    private void AtualizarCorPontos()
    {
      for (int i = 0; i < pontosControleVisuais.Count; i++)
      {
        if (i == ptoSelecionado)
          pontosControleVisuais[i].ShaderObjeto = new Shader("Shaders/shader.vert", "Shaders/shaderVermelha.frag");
        else
          pontosControleVisuais[i].ShaderObjeto = new Shader("Shaders/shader.vert", "Shaders/shaderBranca.frag");
      }
    }

#if CG_DEBUG
    public override string ToString()
    {
      System.Console.WriteLine("__________________________________ \n");
      string retorno;
      retorno = "__ SplineInter _ QtdPto: " + splineQtdPto + " _ PtoSelecionado: " + ptoSelecionado + "\n";
      retorno += base.ImprimeToString();
      return retorno;
    }
#endif
  }
}
