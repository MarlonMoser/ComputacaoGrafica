using CG_Biblioteca;
using OpenTK.Graphics.OpenGL4;

namespace gcgcg
{
  internal class Ponto : Objeto
  {
    public Ponto(Objeto _paiRef, ref char _rotulo) : this(_paiRef, ref _rotulo, new Ponto4D())
    {

    }

    public Ponto(Objeto _paiRef, ref char _rotulo, Ponto4D pto) : base(_paiRef, ref _rotulo)
    {
      PrimitivaTipo = PrimitiveType.Points;
      PrimitivaTamanho = 20;

      base.PontosAdicionar(pto);

      Atualizar();
    }

    public void Atualizar()
    {
      base.ObjetoAtualizar();
    }

    /// <summary>
    /// Retorna a posição atual do ponto de controle.
    /// </summary>
    public Ponto4D ObterPosicao()
    {
      return base.PontosId(0);
    }

    /// <summary>
    /// Atualiza a posição do ponto de controle e recarrega o VAO/VBO.
    /// </summary>
    public void AtualizarPosicao(Ponto4D novaPosicao)
    {
      base.PontosAlterar(novaPosicao, 0);
    }

#if CG_DEBUG
    public override string ToString()
    {
      System.Console.WriteLine("__________________________________ \n");
      string retorno;
      retorno = "__ Objeto Ponto _ Tipo: " + PrimitivaTipo + " _ Tamanho: " + PrimitivaTamanho + "\n";
      retorno += base.ImprimeToString();
      return retorno;
    }
#endif

  }
}
