using CG_Biblioteca;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace gcgcg
{
  /// <summary>
  /// Spline de Bézier cúbica calculada via algoritmo de De Casteljau,
  /// usando Matematica.InterpolarRetaPonto() da CG_Biblioteca.
  /// Herda de <see cref="Objeto"/>.
  /// </summary>
  internal class SplineBezier : Objeto
  {
    private int ptoSelecionado = 0;
    private char rotuloAtual;
    private readonly int splineQtdPtoMax = 100;
    private int splineQtdPto = 10;

    // Poliedro de controle desenhado em ciano
    private Poligono poliedroControle;

    // Pontos de controle visuais (objetos Ponto filhos desta spline)
    private readonly List<Ponto> pontosControleVisuais = [];

    public SplineBezier(Objeto _paiRef, ref char _rotulo) : base(_paiRef, ref _rotulo)
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
        new Ponto4D(-0.2,  0.5),
        new Ponto4D( 0.2,  0.5),
        new Ponto4D( 0.5, -0.5),
      ];

      foreach (var pos in posicoes)
        pontosControleVisuais.Add(new Ponto(this, ref rotuloAtual, pos));

      AtualizarCorPontos();
    }

    /// <summary>
    /// Cria o poliedro de controle (linha ciano conectando os pontos de controle).
    /// </summary>
    private void PoliedroControle()
    {
      List<Ponto4D> pts = [];
      foreach (var pv in pontosControleVisuais)
        pts.Add(new Ponto4D(pv.PontosId(0)));

      poliedroControle = new Poligono(this, ref rotuloAtual, pts)
      {
        PrimitivaTipo = PrimitiveType.LineStrip,
        ShaderObjeto = new Shader("Shaders/shader.vert", "Shaders/shaderCiano.frag")
      };
    }

    /// <summary>
    /// Recalcula a curva de Bézier cúbica usando De Casteljau com
    /// Matematica.InterpolarRetaPonto() da CG_Biblioteca.
    /// </summary>
    public void Atualizar()
    {
      pontosLista.Clear();

      int n = pontosControleVisuais.Count;
      if (n < 4) return;

      // Bézier cúbica por partes: segmentos de 4 pontos, com reaproveitamento do último
      int numSegmentos = (n - 1) / 3;

      for (int seg = 0; seg < numSegmentos; seg++)
      {
        int idx = seg * 3;
        Ponto4D p0 = pontosControleVisuais[idx].PontosId(0);
        Ponto4D p1 = pontosControleVisuais[idx + 1].PontosId(0);
        Ponto4D p2 = pontosControleVisuais[idx + 2].PontosId(0);
        Ponto4D p3 = pontosControleVisuais[idx + 3].PontosId(0);

        int passos = (seg == numSegmentos - 1) ? splineQtdPto : splineQtdPto - 1;

        for (int i = 0; i <= passos; i++)
        {
          double t = (double)i / splineQtdPto;

          // De Casteljau — 3 níveis usando Matematica.InterpolarRetaPonto()
          Ponto4D q0 = Matematica.InterpolarRetaPonto(p0, p1, t);
          Ponto4D q1 = Matematica.InterpolarRetaPonto(p1, p2, t);
          Ponto4D q2 = Matematica.InterpolarRetaPonto(p2, p3, t);
          Ponto4D r0 = Matematica.InterpolarRetaPonto(q0, q1, t);
          Ponto4D r1 = Matematica.InterpolarRetaPonto(q1, q2, t);

          Ponto4D b  = Matematica.InterpolarRetaPonto(r0, r1, t);

          pontosLista.Add(b);
        }
      }

      base.ObjetoAtualizar();
      AtualizarPoliedro();
    }

    /// <summary>
    /// Sincroniza os pontos do poliedro de controle com as posições atuais dos pontos de controle.
    /// Usa PontosAlterar() de Objeto — método da CG_Biblioteca.
    /// </summary>
    private void AtualizarPoliedro()
    {
      if (poliedroControle == null) return;

      for (int i = 0; i < pontosControleVisuais.Count; i++)
        poliedroControle.PontosAlterar(new Ponto4D(pontosControleVisuais[i].PontosId(0)), i);
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
    /// Move o ponto de controle selecionado (ptoInc) ou avança para o próximo (proximo=true).
    /// Usa PontosId() e PontosAlterar() de Objeto — métodos da CG_Biblioteca.
    /// </summary>
    public void AtualizarSpline(Ponto4D ptoInc, bool proximo)
    {
      if (proximo)
      {
        ptoSelecionado = (ptoSelecionado + 1) % pontosControleVisuais.Count;
        AtualizarCorPontos();
        return;
      }

      Ponto4D posAtual = pontosControleVisuais[ptoSelecionado].PontosId(0);
      pontosControleVisuais[ptoSelecionado].PontosAlterar(
        new Ponto4D(posAtual.X + ptoInc.X, posAtual.Y + ptoInc.Y), 0
      );

      Atualizar();
    }

    /// <summary>
    /// Ponto selecionado = shader vermelho; demais = shader branco.
    /// </summary>
    private void AtualizarCorPontos()
    {
      for (int i = 0; i < pontosControleVisuais.Count; i++)
      {
        pontosControleVisuais[i].ShaderObjeto = (i == ptoSelecionado)
          ? new Shader("Shaders/shader.vert", "Shaders/shaderVermelha.frag")
          : new Shader("Shaders/shader.vert", "Shaders/shaderBranca.frag");
      }
    }

#if CG_DEBUG
    public override string ToString()
    {
      System.Console.WriteLine("__________________________________ \n");
      string retorno = "__ SplineBezier _ QtdPto: " + splineQtdPto + " _ PtoSelecionado: " + ptoSelecionado + "\n";
      retorno += base.ImprimeToString();
      return retorno;
    }
#endif
  }
}
