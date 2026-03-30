using CG_Biblioteca;
using OpenTK.Graphics.OpenGL4;

namespace gcgcg
{
    internal class Circulo : Objeto
    {
        private readonly double raio;
        private const int NUM_PONTOS = 72;

        public Circulo(Objeto _paiRef, ref char _rotulo, double _raio)
      : this(_paiRef, ref _rotulo, _raio, new Ponto4D(0.0, 0.0))
        {
        }


        public Circulo(Objeto _paiRef, ref char _rotulo, double _raio, Ponto4D ptoDeslocamento)
     : base(_paiRef, ref _rotulo)
        {
            this.raio = _raio;

            PrimitivaTipo = PrimitiveType.Points;
            PrimitivaTamanho = 5;
            double passo = 360.0 / NUM_PONTOS;
            for (int i = 0; i < NUM_PONTOS; i++)
            {
                double angulo = i * passo;
                Ponto4D pto = Matematica.GerarPtosCirculo(angulo, raio);
                pto.X += ptoDeslocamento.X;
                pto.Y += ptoDeslocamento.Y;
                base.PontosAdicionar(pto);
            }

            Atualizar(ptoDeslocamento);
        }


        public void Atualizar(Ponto4D ptoDeslocamento)
        {
            base.PontosApagar();

            base.ObjetoAtualizar();
                

        }

        override public string ToString()
        {
            System.Console.WriteLine("____________ \n");
            string retorno;
            retorno = "__ Objeto Circulo _ Tipo: " + PrimitivaTipo + " _ Tamanho: " + PrimitivaTamanho + "\n";
            retorno += "__ Raio: " + raio + " _ NumPontos: " + NUM_PONTOS + "\n";
            retorno += base.ImprimeToString();
            return retorno;
        }
    }
}