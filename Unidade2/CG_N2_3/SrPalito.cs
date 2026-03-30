using CG_Biblioteca;
using OpenTK.Graphics.OpenGL4;
using System;

namespace gcgcg
{
  /// <summary>
  /// Representa o "Sr. Palito": um segmento de reta cujo pé fica na origem (translado pelo pé)
  /// e cuja cabeça é calculada com raio e ângulo.
  /// Herda de <see cref="Objeto"/>.
  /// </summary>
  internal class SrPalito : Objeto
  {
    private double angulo = 45;   // graus
    private double raio = 0.5;

    // Ponto do pé (base do palito) — posição no mundo
    private Ponto4D ptoPe;
    // Ponto da cabeça — calculado a partir de ptoPe + raio/ângulo
    private Ponto4D ptoCabeca;

    /// <summary>
    /// Cria o Sr. Palito com pé na origem, raio 0.5 e ângulo 45°.
    /// </summary>
    public SrPalito(Objeto _paiRef, ref char _rotulo)
      : base(_paiRef, ref _rotulo)
    {
      PrimitivaTipo = PrimitiveType.Lines;
      PrimitivaTamanho = 2;

      ptoPe = new Ponto4D(0.0, 0.0);
      ptoCabeca = new Ponto4D(0.0, 0.0);

      // Reserva os dois pontos da lista antes de calcular
      pontosLista.Add(ptoPe);
      pontosLista.Add(ptoCabeca);

      Atualizar();
    }

    /// <summary>
    /// Recalcula ptoCabeca com base no ptoPe, raio e ângulo atuais,
    /// e atualiza o VAO/VBO.
    /// </summary>
    public void Atualizar()
    {
      // Cabeça = pé + vetor(raio, ângulo)
      double anguloRad = angulo * Math.PI / 180.0;
      ptoCabeca = new Ponto4D(
        ptoPe.X + raio * Math.Cos(anguloRad),
        ptoPe.Y + raio * Math.Sin(anguloRad)
      );

      pontosLista[0] = ptoPe;
      pontosLista[1] = ptoCabeca;

      base.ObjetoAtualizar();
    }

    /// <summary>
    /// Move o pé (e portanto o palito inteiro) horizontalmente.
    /// </summary>
    /// <param name="peInc">incremento em X (positivo = direita, negativo = esquerda)</param>
    public void AtualizarPe(double peInc)
    {
      ptoPe = new Ponto4D(ptoPe.X + peInc, ptoPe.Y);
      Atualizar();
    }

    /// <summary>
    /// Altera o raio (tamanho) do palito.
    /// </summary>
    /// <param name="raioInc">incremento do raio (pode ser negativo)</param>
    public void AtualizarRaio(double raioInc)
    {
      raio += raioInc;
      //if (raio < 0.01) raio = 0.01; // evita raio negativo ou zero
      Atualizar();
    }

    /// <summary>
    /// Altera o ângulo de rotação do palito.
    /// </summary>
    /// <param name="anguloInc">incremento do ângulo em graus (pode ser negativo)</param>
    public void AtualizarAngulo(double anguloInc)
    {
      angulo += anguloInc;
      Atualizar();
    }

#if CG_DEBUG
    public override string ToString()
    {
      System.Console.WriteLine("__________________________________ \n");
      string retorno;
      retorno = "__ Objeto SrPalito _ Tipo: " + PrimitivaTipo + " _ Tamanho: " + PrimitivaTamanho + "\n";
      retorno += "__ Ângulo: " + angulo + "° _ Raio: " + raio + "\n";
      retorno += "__ Pé: (" + ptoPe.X + ", " + ptoPe.Y + ")\n";
      retorno += "__ Cabeça: (" + ptoCabeca.X + ", " + ptoCabeca.Y + ")\n";
      retorno += base.ImprimeToString();
      return retorno;
    }
#endif
  }
}