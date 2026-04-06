using CG_Biblioteca;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace gcgcg
{
  internal class Poligono : Objeto
  {
    public Poligono(Objeto _paiRef, ref char _rotulo, List<Ponto4D> pontosPoligono) : base(_paiRef, ref _rotulo)
    {
      PrimitivaTipo = PrimitiveType.LineLoop;
      PrimitivaTamanho = 1;
      base.pontosLista = pontosPoligono;
      Atualizar();
    }

    private void Atualizar()
    {
      base.ObjetoAtualizar();
    }

    /// <summary>
    /// Substitui a lista de pontos do polígono e recarrega o VAO/VBO.
    /// Usado pelas splines para atualizar o poliedro de controle dinamicamente.
    /// </summary>
    public void AtualizarPontos(List<Ponto4D> novosPontos)
    {
      base.pontosLista = novosPontos;
      base.ObjetoAtualizar();
    }

#if CG_DEBUG
    public override string ToString()
    {
      System.Console.WriteLine("__________________________________ \n");
      string retorno;
      retorno = "__ Objeto Poligono _ Tipo: " + PrimitivaTipo + " _ Tamanho: " + PrimitivaTamanho + "\n";
      retorno += base.ImprimeToString();
      return retorno;
    }
#endif

  }
}
