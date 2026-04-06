using CG_Biblioteca;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace gcgcg
{
  /// <summary>
  /// Representa uma Spline de Bézier cúbica por partes.
  /// A curva é calculada analiticamente a partir dos pontos de controle,
  /// sem uso de comandos de spline do OpenGL.
  /// Herda de <see cref="Objeto"/>.
  /// </summary>
  internal class SplineBezier : Objeto
  {
    // Índice do ponto de controle atualmente selecionado (destacado em vermelho)
    private int ptoSelecionado = 0;

    // Rótulo interno usado para criar objetos filhos (Pontos e Polígono)
    private char rotuloAtual;

    // Quantidade máxima e atual de pontos calculados na curva
    private readonly int splineQtdPtoMax = 100;
    private int splineQtdPto = 10;

    // Poliedro de controle (desenhado em ciano)
    private Poligono poliedroControle;

    // Lista de objetos Ponto (pontos de controle visuais)
    private readonly List<Ponto> pontosControleVisuais = [];

    // Matriz de pesos de Bézier cúbica (Bernstein)
    private double[,] matrizBezier;

    /// <summary>
    /// Cria a SplineBezier com 4 pontos de controle padrão.
    /// </summary>
    public SplineBezier(Objeto _paiRef, ref char _rotulo) : base(_paiRef, ref _rotulo)
    {
      rotuloAtual = _rotulo;

      PrimitivaTipo = PrimitiveType.LineStrip;
      PrimitivaTamanho = 1;

      BezierMatrizPeso();
      PontosControle();
      PoliedroControle();
      Atualizar();

      _rotulo = rotuloAtual;
    }

    /// <summary>
    /// Inicializa a matriz de pesos de Bézier cúbica (base de Bernstein).
    /// Para Bézier cúbico: B = [-1  3 -3  1]
    ///                          [ 3 -6  3  0]
    ///                          [-3  3  0  0]
    ///                          [ 1  0  0  0]
    /// </summary>
    private void BezierMatrizPeso()
    {
      matrizBezier = new double[4, 4]
      {
        { -1,  3, -3,  1 },
        {  3, -6,  3,  0 },
        { -3,  3,  0,  0 },
        {  1,  0,  0,  0 }
      };
    }

    /// <summary>
    /// Cria os 4 pontos de controle iniciais como objetos Ponto filhos desta spline.
    /// </summary>
    private void PontosControle()
    {
      // 4 pontos de controle formando um arco simétrico
      List<Ponto4D> posicoes =
      [
        new Ponto4D(-0.5, -0.5),
        new Ponto4D(-0.2,  0.5),
        new Ponto4D( 0.2,  0.5),
        new Ponto4D( 0.5, -0.5),
      ];

      foreach (var pos in posicoes)
      {
        var pto = new Ponto(this, ref rotuloAtual, pos);
        pontosControleVisuais.Add(pto);
      }

      // Marca o ponto inicial como selecionado (vermelho)
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
    /// Recalcula os pontos da curva de Bézier e atualiza o VAO/VBO.
    /// </summary>
    public void Atualizar()
    {
      pontosLista.Clear();

      int n = pontosControleVisuais.Count;
      if (n < 4) return;

      // Segmentos de Bézier cúbica: a cada 3 pontos compartilha 1
      int numSegmentos = (n - 1) / 3;

      for (int seg = 0; seg < numSegmentos; seg++)
      {
        int idx = seg * 3;
        Ponto4D p0 = pontosControleVisuais[idx].ObterPosicao();
        Ponto4D p1 = pontosControleVisuais[idx + 1].ObterPosicao();
        Ponto4D p2 = pontosControleVisuais[idx + 2].ObterPosicao();
        Ponto4D p3 = pontosControleVisuais[idx + 3].ObterPosicao();

        int passosSeg = (seg == numSegmentos - 1) ? splineQtdPto : splineQtdPto - 1;

        for (int i = 0; i <= passosSeg; i++)
        {
          double t = (double)i / splineQtdPto;
          double t2 = t * t;
          double t3 = t2 * t;

          // Vetor [t³ t² t 1] × MatrizBezier × [P0 P1 P2 P3]ᵀ
          double[] tv = { t3, t2, t, 1.0 };

          double x = 0, y = 0;
          double[] px = { p0.X, p1.X, p2.X, p3.X };
          double[] py = { p0.Y, p1.Y, p2.Y, p3.Y };

          for (int j = 0; j < 4; j++)
          {
            double coef = 0;
            for (int k = 0; k < 4; k++)
              coef += tv[k] * matrizBezier[k, j];
            x += coef * px[j];
            y += coef * py[j];
          }

          pontosLista.Add(new Ponto4D(x, y));
        }
      }

      base.ObjetoAtualizar();
      AtualizarPoliedro();
    }

    /// <summary>
    /// Atualiza as posições do poliedro de controle conforme os pontos atuais.
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
    /// <param name="ptoInc">Incremento a aplicar na posição</param>
    /// <param name="proximo">Se true, avança para o próximo ponto de controle</param>
    public void AtualizarSpline(Ponto4D ptoInc, bool proximo)
    {
      if (proximo)
      {
        // Avança o ponto selecionado
        ptoSelecionado = (ptoSelecionado + 1) % pontosControleVisuais.Count;
        AtualizarCorPontos();
        return;
      }

      // Move o ponto selecionado
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
      retorno = "__ SplineBezier _ QtdPto: " + splineQtdPto + " _ PtoSelecionado: " + ptoSelecionado + "\n";
      retorno += base.ImprimeToString();
      return retorno;
    }
#endif
  }
}
