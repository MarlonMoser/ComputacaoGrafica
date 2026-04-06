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

namespace gcgcg
{
  public class Mundo : GameWindow
  {
    private static Objeto mundo = null;

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
#endif

    private Shader _shaderVermelha;
    private Shader _shaderVerde;
    private Shader _shaderAzul;
    private Shader _shaderCiano;
    private Shader _shaderAmarela;

    // As duas splines da atividade
    private SplineBezier splineBezier = null;
    private SplineInter splineInter = null;

    // Controla qual grupo está ativo: 0 = Bézier, 1 = Interpolação
    private int grupoAtivo = 0;

    // Incremento de movimento dos pontos de controle
    private const double INC_MOV = 0.05;

    public Mundo(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
      : base(gameWindowSettings, nativeWindowSettings)
    {
      mundo ??= new Objeto(null, ref rotuloAtual); // padrão Singleton
    }

    protected override void OnLoad()
    {
      base.OnLoad();

      Utilitario.Diretivas();
#if CG_DEBUG
      Console.WriteLine("Tamanho interno da janela de desenho: " + ClientSize.X + "x" + ClientSize.Y);
#endif

      GL.ClearColor(0.5f, 0.5f, 0.5f, 1.0f);

      #region Cores
      _shaderVermelha = new Shader("Shaders/shader.vert", "Shaders/shaderVermelha.frag");
      _shaderVerde    = new Shader("Shaders/shader.vert", "Shaders/shaderVerde.frag");
      _shaderAzul     = new Shader("Shaders/shader.vert", "Shaders/shaderAzul.frag");
      _shaderCiano    = new Shader("Shaders/shader.vert", "Shaders/shaderCiano.frag");
      _shaderAmarela  = new Shader("Shaders/shader.vert", "Shaders/shaderAmarela.frag");
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
#endif

      #region Objeto: SplineBezier
      splineBezier = new SplineBezier(mundo, ref rotuloAtual)
      {
        ShaderObjeto = _shaderAmarela
      };
      objetoSelecionado = splineBezier;
      #endregion

      #region Objeto: SplineInter
      splineInter = new SplineInter(mundo, ref rotuloAtual)
      {
        ShaderObjeto = _shaderAmarela
      };
      #endregion

#if CG_DEBUG
      Console.WriteLine("CG_N2_4 — Splines interativas");
      Console.WriteLine("  Q       : alternar entre SplineBezier e SplineInter");
      Console.WriteLine("  Espaco  : proximo ponto de controle (dentro do grupo ativo)");
      Console.WriteLine("  C/B/E/D : mover ponto de controle (cima/baixo/esq/dir)");
      Console.WriteLine("  + / ,   : aumentar/diminuir pontos calculados na spline");
      Console.WriteLine("  F       : imprimir grafo de cena");
      Console.WriteLine("  T       : imprimir estado do grupo ativo");
      Console.WriteLine("  ESC     : sair");
#endif
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
      base.OnRenderFrame(e);

      GL.Clear(ClearBufferMask.ColorBufferBit);
#if CG_DEBUG
      CheckGLError("Apos GL.Clear");
#endif

      matrizGrafo.AtribuirIdentidade();
      mundo.Desenhar(matrizGrafo, objetoSelecionado);

#if CG_Gizmo
      Gizmo_Sru3D();
#endif
      SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
      base.OnUpdateFrame(e);

      var estadoTeclado = KeyboardState;
      if (estadoTeclado.IsKeyPressed(Keys.Escape))
        Close();

      #region Tecla Q: alternar entre os dois grupos de spline
      if (estadoTeclado.IsKeyPressed(Keys.Q))
      {
        grupoAtivo = (grupoAtivo + 1) % 2;
#if CG_DEBUG
        Console.WriteLine("Grupo ativo: " + (grupoAtivo == 0 ? "SplineBezier" : "SplineInter"));
#endif
      }
      #endregion

      #region Tecla Espaco: proximo ponto de controle no grupo ativo
      if (estadoTeclado.IsKeyPressed(Keys.Space))
      {
        if (grupoAtivo == 0)
          splineBezier.AtualizarSpline(new Ponto4D(0, 0), proximo: true);
        else
          splineInter.AtualizarSpline(new Ponto4D(0, 0), proximo: true);
      }
      #endregion

      #region Teclas C/B/E/D: mover ponto de controle selecionado
      Ponto4D incremento = new(0, 0);

      if (estadoTeclado.IsKeyPressed(Keys.C)) incremento = new Ponto4D(0,  INC_MOV); // Cima
      if (estadoTeclado.IsKeyPressed(Keys.B)) incremento = new Ponto4D(0, -INC_MOV); // Baixo
      if (estadoTeclado.IsKeyPressed(Keys.E)) incremento = new Ponto4D(-INC_MOV, 0); // Esquerda
      if (estadoTeclado.IsKeyPressed(Keys.D)) incremento = new Ponto4D( INC_MOV, 0); // Direita

      if (incremento.X != 0 || incremento.Y != 0)
      {
        if (grupoAtivo == 0)
          splineBezier.AtualizarSpline(incremento, proximo: false);
        else
          splineInter.AtualizarSpline(incremento, proximo: false);
      }
      #endregion

      #region Teclas + e ,: aumentar/diminuir quantidade de pontos calculados
      if (estadoTeclado.IsKeyPressed(Keys.Equal) || estadoTeclado.IsKeyPressed(Keys.KeyPadAdd))
      {
        splineBezier.SplineQtdPto(+1);
        splineInter.SplineQtdPto(+1);
#if CG_DEBUG
        Console.WriteLine("Aumentou pontos da spline.");
#endif
      }
      if (estadoTeclado.IsKeyPressed(Keys.Comma))
      {
        splineBezier.SplineQtdPto(-1);
        splineInter.SplineQtdPto(-1);
#if CG_DEBUG
        Console.WriteLine("Diminuiu pontos da spline.");
#endif
      }
      #endregion

      #region Funcoes auxiliares de debug
      if (estadoTeclado.IsKeyPressed(Keys.F))
        Grafocena.GrafoCenaImprimir(mundo, grafoLista);

      if (estadoTeclado.IsKeyPressed(Keys.T))
      {
        if (grupoAtivo == 0)
          Console.WriteLine(splineBezier);
        else
          Console.WriteLine(splineInter);
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
      GL.DeleteProgram(_shaderAmarela.Handle);

      base.OnUnload();
    }

    private void Gizmo_Sru3D()
    {
#if CG_Gizmo
#if CG_OpenGL
      var transform = Matrix4.Identity;
      GL.BindVertexArray(_vertexArrayObject_sruEixos);
      // Eixo X (vermelho)
      _shaderVermelha.SetMatrix4("transform", transform);
      _shaderVermelha.Use();
      GL.DrawArrays(PrimitiveType.Lines, 0, 2);
      // Eixo Y (verde)
      _shaderVerde.SetMatrix4("transform", transform);
      _shaderVerde.Use();
      GL.DrawArrays(PrimitiveType.Lines, 2, 2);
      // Eixo Z (azul)
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
