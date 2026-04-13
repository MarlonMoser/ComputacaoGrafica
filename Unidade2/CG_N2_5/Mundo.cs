/*
 As constantes dos pré-processors estão nos arquivos ".csproj"
 desse projeto e da CG_Biblioteca.
*/

using CG_Biblioteca;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
// using System.Diagnostics;

namespace gcgcg
{
    public class Mundo : GameWindow
    {
        private static Objeto mundo = null;

        private Ponto pontoJoyStick;
        private Circulo circuloMenor;
        private Circulo circuloMaior;
        private Retangulo retanguloMenor;

        private char rotuloAtual = '?';
        private Dictionary<char, Objeto> grafoLista = [];
        private Objeto objetoSelecionado = null;
        private Transformacao4D matrizGrafo = new();

#if CG_Gizmo
        private readonly float[] _sruEixos =
        [
           0.0f,  0.0f,  0.0f, /* X- */      0.5f,  0.0f,  0.0f, /* X+ */
       0.0f,  0.0f,  0.0f, /* Y- */      0.0f,  0.5f,  0.0f, /* Y+ */
       0.0f,  0.0f,  0.0f, /* Z- */      0.0f,  0.0f,  0.5f  /* Z+ */
        ];
        private int _vertexBufferObject_sruEixos;
        private int _vertexArrayObject_sruEixos;

        // // FPS
        // private int frames = 0;
        // private Stopwatch stopwatch = new();
#endif

        private Shader _shaderVermelha;
        private Shader _shaderVerde;
        private Shader _shaderAzul;
        private Shader _shaderCiano;
        private Shader _shaderAmarela;

        public Mundo(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
          : base(gameWindowSettings, nativeWindowSettings)
        {
            mundo ??= new Objeto(null, ref rotuloAtual); //padrão Singleton
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            Utilitario.Diretivas();
#if CG_DEBUG
            Console.WriteLine("Tamanho interno da janela de desenho: " + ClientSize.X + "x" + ClientSize.Y);
#endif

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            #region Cores
            _shaderVermelha = new Shader("Shaders/shader.vert", "Shaders/shaderVermelha.frag");
            _shaderVerde = new Shader("Shaders/shader.vert", "Shaders/shaderVerde.frag");
            _shaderAzul = new Shader("Shaders/shader.vert", "Shaders/shaderAzul.frag");
            _shaderCiano = new Shader("Shaders/shader.vert", "Shaders/shaderCiano.frag");
            _shaderAmarela = new Shader("Shaders/shader.vert", "Shaders/shaderAmarela.frag");
            #endregion

#if CG_Gizmo
            #region Eixos: SRU  
            _vertexBufferObject_sruEixos = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject_sruEixos);
            GL.BufferData(BufferTarget.ArrayBuffer, _sruEixos.Length * sizeof(float), _sruEixos, BufferUsageHint.StaticDraw);
            _vertexArrayObject_sruEixos = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject_sruEixos);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            #endregion

            // stopwatch.Start();
#endif



            this.circuloMaior = new Circulo(mundo, ref rotuloAtual, 0.3)
            {
                PrimitivaTipo = PrimitiveType.LineLoop,
            };

            this.circuloMenor = new Circulo(mundo, ref rotuloAtual, 0.1)
            {
                PrimitivaTipo = PrimitiveType.LineLoop
            };

            this.pontoJoyStick = new Ponto(mundo, ref rotuloAtual, new Ponto4D(0.3, 0.3))
            {
                PrimitivaTipo = PrimitiveType.Points,
                PrimitivaTamanho = 10
            };

            Ponto pontoDirecionamento = new Ponto(mundo, ref rotuloAtual, new Ponto4D(0.3, 0.3))
            {
                PrimitivaTipo = PrimitiveType.Points,
                PrimitivaTamanho = 10,
                ShaderObjeto = _shaderAmarela
            };

            this.retanguloMenor = new Retangulo(mundo, ref rotuloAtual)
            {
                PrimitivaTipo = PrimitiveType.LineLoop
            };

            retanguloMenor.PontosApagar();
            int i0 = 9;
            int i1 = 27;
            int i2 = 45;
            int i3 = 63;
            retanguloMenor.PontosAdicionar(circuloMaior.PontosId(i0));
            retanguloMenor.PontosAdicionar(circuloMaior.PontosId(i1));
            retanguloMenor.PontosAdicionar(circuloMaior.PontosId(i2));
            retanguloMenor.PontosAdicionar(circuloMaior.PontosId(i3));
            retanguloMenor.ObjetoAtualizar();

            Retangulo retanguloMaior = new Retangulo(mundo, ref rotuloAtual, new Ponto4D(0.6, 0), new Ponto4D(0.0, 0.6))
            {
                PrimitivaTipo = PrimitiveType.LineLoop,
                ShaderObjeto = _shaderAmarela
            };



        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);
#if CG_DEBUG
            CheckGLError("Após GL.Clear");
#endif

            matrizGrafo.AtribuirIdentidade();
            mundo.Desenhar(matrizGrafo, objetoSelecionado);

#if CG_Gizmo
            Gizmo_Sru3D();

            // frames++;
            // if (stopwatch.ElapsedMilliseconds >= 1000)
            // {
            //   Console.WriteLine($"FPS: {frames}");
            //   frames = 0; 
            //   stopwatch.Restart();
            // }
#endif
            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            #region Teclado
            var estadoTeclado = KeyboardState;
            if (estadoTeclado.IsKeyPressed(Keys.Escape))
                Close();

            #region Funções de apoio para o desenvolvimento. Não é do enunciado  
            if (estadoTeclado.IsKeyPressed(Keys.Space))
                objetoSelecionado = Grafocena.GrafoCenaProximo(mundo, objetoSelecionado, grafoLista);


            if (pontoJoyStick != null && circuloMaior != null && circuloMenor != null)
            {
                Ponto4D centroAtual = pontoJoyStick.Bbox().ObterCentro;
                Ponto4D centroCirculo = circuloMaior.Bbox().ObterCentro;

                double raio = Matematica.Distancia(centroCirculo, circuloMaior.PontosId(0));

                if (estadoTeclado.IsKeyPressed(Keys.C))
                {
                    var novo = new Ponto4D(centroAtual.X, centroAtual.Y + 0.01);
                    if (Matematica.Distancia(novo, centroCirculo) <= raio + 1e-9)
                    {
                        pontoJoyStick.MatrizTranslacaoXYZ(0, 0.01, 0);
                        circuloMenor.MatrizTranslacaoXYZ(0, 0.01, 0);
                    }
                }

                if (estadoTeclado.IsKeyPressed(Keys.B))
                {
                    var novo = new Ponto4D(centroAtual.X, centroAtual.Y - 0.01);
                    if (Matematica.Distancia(novo, centroCirculo) <= raio + 1e-9)
                    {
                        pontoJoyStick.MatrizTranslacaoXYZ(0, -0.01, 0);
                        circuloMenor.MatrizTranslacaoXYZ(0, -0.01, 0);
                    }
                }

                if (estadoTeclado.IsKeyPressed(Keys.E))
                {
                    var novo = new Ponto4D(centroAtual.X - 0.01, centroAtual.Y);
                    if (Matematica.Distancia(novo, centroCirculo) <= raio + 1e-9)
                    {
                        pontoJoyStick.MatrizTranslacaoXYZ(-0.01, 0, 0);
                        circuloMenor.MatrizTranslacaoXYZ(-0.01, 0, 0);
                    }
                }

                if (estadoTeclado.IsKeyPressed(Keys.D))
                {
                    var novo = new Ponto4D(centroAtual.X + 0.01, centroAtual.Y);
                    if (Matematica.Distancia(novo, centroCirculo) <= raio + 1e-9)
                    {
                        pontoJoyStick.MatrizTranslacaoXYZ(0.01, 0, 0);
                        circuloMenor.MatrizTranslacaoXYZ(0.01, 0, 0);
                    }
                }
            }

            if (estadoTeclado.IsKeyPressed(Keys.F))
                Grafocena.GrafoCenaImprimir(mundo, grafoLista);
            if (estadoTeclado.IsKeyPressed(Keys.T))
            {
                if (objetoSelecionado != null)
                    Console.WriteLine(objetoSelecionado);
                else
                    Console.WriteLine("objetoSelecionado: MUNDO \n__________________________________\n");
            }

            if (estadoTeclado.IsKeyPressed(Keys.C) && objetoSelecionado != null)
            {
                objetoSelecionado.ShaderObjeto = _shaderCiano;
            }

            if (estadoTeclado.IsKeyPressed(Keys.Right) && objetoSelecionado != null)
            {
                if (objetoSelecionado.PontosListaTamanho > 0)
                {
                    objetoSelecionado.PontosAlterar(new Ponto4D(objetoSelecionado.PontosId(0).X + 0.005, objetoSelecionado.PontosId(0).Y, 0), 0);
                    objetoSelecionado.ObjetoAtualizar();
                }
            }

            if (estadoTeclado.IsKeyPressed(Keys.R) && objetoSelecionado != null)
            {
                //FIXME: Spline limpa os pontos da Spline, mas não limpa pontos e poliedro de controle 
                objetoSelecionado.PontosApagar();
            }
            #endregion

            if (this.retanguloMenor != null && this.pontoJoyStick != null)
            {
                var posJoy = pontoJoyStick.Bbox().ObterCentro;
                if (!retanguloMenor.Bbox().Dentro(posJoy))
                {
                    if (retanguloMenor.PrimitivaTipo != PrimitiveType.Points)
                    {
                        retanguloMenor.PrimitivaTipo = PrimitiveType.Points;
                        retanguloMenor.PrimitivaTamanho = 10;
                        retanguloMenor.ObjetoAtualizar();
                    }
                }
                else
                {
                    if (retanguloMenor.PrimitivaTipo != PrimitiveType.LineLoop)
                    {
                        retanguloMenor.PrimitivaTipo = PrimitiveType.LineLoop;
                        retanguloMenor.PrimitivaTamanho = 1;
                        retanguloMenor.ObjetoAtualizar();
                    }
                }
            }

            #endregion

            #region  Mouse
            int janelaLargura = ClientSize.X;
            int janelaAltura = ClientSize.Y;
            Ponto4D mousePonto = new(MousePosition.X, MousePosition.Y);
            Ponto4D sruPonto = Utilitario.NDC_TelaSRU(janelaLargura, janelaAltura, mousePonto);

            if (estadoTeclado.IsKeyPressed(Keys.LeftShift) && objetoSelecionado != null && objetoSelecionado.PontosListaTamanho > 0)
            {
                objetoSelecionado.PontosAlterar(sruPonto, 0);
                objetoSelecionado.ObjetoAtualizar();
            }

            #endregion
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

#if CG_DEBUG
            Console.WriteLine("Tamanho interno da janela de desenho: " + ClientSize.X + "x" + ClientSize.Y);
#endif
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        }

        protected override void OnUnload()
        {
            mundo.OnUnload();

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);

#if CG_Gizmo
            GL.DeleteBuffer(_vertexBufferObject_sruEixos);
            GL.DeleteVertexArray(_vertexArrayObject_sruEixos);
#endif

            GL.DeleteProgram(_shaderVermelha.Handle);
            GL.DeleteProgram(_shaderVerde.Handle);
            GL.DeleteProgram(_shaderAzul.Handle);
            GL.DeleteProgram(_shaderCiano.Handle);

            base.OnUnload();
        }

        private void Gizmo_Sru3D()
        {
#if CG_Gizmo
#if CG_OpenGL
            var transform = Matrix4.Identity;
            GL.BindVertexArray(_vertexArrayObject_sruEixos);
            // EixoX
            _shaderVermelha.SetMatrix4("transform", transform);
            _shaderVermelha.Use();
            GL.DrawArrays(PrimitiveType.Lines, 0, 2);
            // EixoY
            _shaderVerde.SetMatrix4("transform", transform);
            _shaderVerde.Use();
            GL.DrawArrays(PrimitiveType.Lines, 2, 2);
            // EixoZ
            _shaderAzul.SetMatrix4("transform", transform);
            _shaderAzul.Use();
            GL.DrawArrays(PrimitiveType.Lines, 4, 2);
#endif
#endif
        }

#if CG_DEBUG
        public static void CheckGLError(string message = "")
        {
            var error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)
            {
                Console.WriteLine($"[OpenGL Error] {error} {message}");
            }
        }
#endif

    }
}
