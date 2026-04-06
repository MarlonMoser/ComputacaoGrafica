using CG_Biblioteca;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace gcgcg
{
  /// <summary>
  /// Spline de Interpolação (Catmull-Rom) calculada usando
  /// Matematica.InterpolarRetaValor() da CG_Biblioteca.
  /// A curva passa por todos os pontos de controle intermediários.
  /// Herda de <see cref="Objeto"/>.
  /// </summary>
  internal class SplineInter : Objeto
  {
    private int ptoSelecionado = 0;
    private char rotuloAtual;
    private readonly int splineQtdPtoMax = 100;
    private int splineQtdPto = 10;

    // Poliedro de controle desenhado em ciano
    private Poligono poliedroControle;

    // Pontos de controle visuais (objetos Ponto filhos desta spline)
    private readonly List<Ponto> pontosControleVisuais = [];

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
    /// Recalcula a curva Catmull-Rom usando Matematica.InterpolarRetaValor()
    /// da CG_Biblioteca para o cálculo de cada componente X e Y.
    /// </summary>
    public void Atualizar()
    {
      pontosLista.Clear();

      int n = pontosControleVisuais.Count;
      if (n < 4) return;

      // Catmull-Rom: segmentos de p1 até p[n-2] (extremos são tangentes)
      for (int seg = 0; seg < n - 3; seg++)
      {
        Ponto4D p0 = pontosControleVisuais[seg].PontosId(0);
        Ponto4D p1 = pontosControleVisuais[seg + 1].PontosId(0);
        Ponto4D p2 = pontosControleVisuais[seg + 2].PontosId(0);
        Ponto4D p3 = pontosControleVisuais[seg + 3].PontosId(0);

        int passos = (seg == n - 4) ? splineQtdPto : splineQtdPto - 1;

        for (int i = 0; i <= passos; i++)
        {
          double t  = (double)i / splineQtdPto;
          double t2 = t * t;
          double t3 = t2 * t;

          // Coeficientes Catmull-Rom
          double c0 = -t3 + 2.0 * t2 - t;           // peso de p0
          double c1 =  3.0 * t3 - 5.0 * t2 + 2.0;   // peso de p1
          double c2 = -3.0 * t3 + 4.0 * t2 + t;      // peso de p2
          double c3 =  t3 - t2;                       // peso de p3

          // Usa Matematica.InterpolarRetaValor() para combinar cada coeficiente
          // com os pares de pontos, acumulando X e Y separadamente
          double x = 0.5 * (
            Matematica.InterpolarRetaValor(0, p0.X, c0) +
            Matematica.InterpolarRetaValor(0, p1.X, c1) +
            Matematica.InterpolarRetaValor(0, p2.X, c2) +
            Matematica.InterpolarRetaValor(0, p3.X, c3)
          );

          double y = 0.5 * (
            Matematica.InterpolarRetaValor(0, p0.Y, c0) +
            Matematica.InterpolarRetaValor(0, p1.Y, c1) +
            Matematica.InterpolarRetaValor(0, p2.Y, c2) +
            Matematica.InterpolarRetaValor(0, p3.Y, c3)
          );

          pontosLista.Add(new Ponto4D(x, y));
        }
      }

      base.ObjetoAtualizar();
      AtualizarPoliedro();
    }

    /// <summary>
    /// Sincroniza os pontos do poliedro de controle com as posições atuais.
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
      string retorno = "__ SplineInter _ QtdPto: " + splineQtdPto + " _ PtoSelecionado: " + ptoSelecionado + "\n";
      retorno += base.ImprimeToString();
      return retorno;
    }
#endif
  }
}
