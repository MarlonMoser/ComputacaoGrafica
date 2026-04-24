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
using System.Diagnostics;

namespace gcgcg
{
  public class Mundo : GameWindow
  {
    private static Objeto mundo = null;

    private char rotuloAtual = '?';
    private Dictionary<char, Objeto> grafoLista = [];
    private Objeto objetoSelecionado = null;
    private Objeto objetoNovo = null;
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

    // FPS
    private int frames = 0;
    private Stopwatch stopwatch = new();
#endif

    private Shader _shaderBranca;
    private Shader _shaderVermelha;
    private Shader _shaderVerde;
    private Shader _shaderAzul;
    private Shader _shaderCiano;
    private Shader _shaderMagenta;
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
      _shaderBranca = new Shader("Shaders/shader.vert", "Shaders/shaderBranca.frag");
      _shaderVermelha = new Shader("Shaders/shader.vert", "Shaders/shaderVermelha.frag");
      _shaderVerde = new Shader("Shaders/shader.vert", "Shaders/shaderVerde.frag");
      _shaderAzul = new Shader("Shaders/shader.vert", "Shaders/shaderAzul.frag");
      _shaderCiano = new Shader("Shaders/shader.vert", "Shaders/shaderCiano.frag");
      _shaderMagenta = new Shader("Shaders/shader.vert", "Shaders/shaderMagenta.frag");
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

      stopwatch.Start();
      #endregion
#endif

    
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
      base.OnRenderFrame(e);

      GL.Clear(ClearBufferMask.ColorBufferBit);

      matrizGrafo.AtribuirIdentidade();
      mundo.Desenhar(matrizGrafo, objetoSelecionado);

#if CG_Gizmo
      Gizmo_Sru3D();

      frames++;
      if (stopwatch.ElapsedMilliseconds >= 1000)
      {
#if CG_DEBUG
        // Console.WriteLine($"FPS: {frames}");
#endif
        frames = 0; 
        stopwatch.Restart();
      }
#endif
      SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
      base.OnUpdateFrame(e);

      // ☞ 396c2670-8ce0-4aff-86da-0f58cd8dcfdc   TODO: forma otimizada para teclado.
      // TODO: FPS, rotina para calcular o FPS
      // ☞ c9cd577f-826a-4fb2-909a-e6b1fb217b82

      #region Teclado
      var estadoTeclado = KeyboardState;
      if (estadoTeclado.IsKeyPressed(Keys.Escape))
        Close();

      #region Funções de apoio para o desenvolvimento. Não é do enunciado  
      if (estadoTeclado.IsKeyPressed(Keys.Space))
      {
        objetoSelecionado = Grafocena.GrafoCenaProximo(mundo, objetoSelecionado, grafoLista);
        if (objetoSelecionado != null)
          objetoSelecionado.ObjetoAtualizar();
      }

      if (estadoTeclado.IsKeyPressed(Keys.F))
        Grafocena.GrafoCenaImprimir(mundo, grafoLista);
      if (estadoTeclado.IsKeyPressed(Keys.T))
      {
#if CG_DEBUG
        if (objetoSelecionado != null)
          Console.WriteLine(objetoSelecionado);
        else
          Console.WriteLine("objetoSelecionado: MUNDO \n__________________________________\n");
#endif
      }

      if (estadoTeclado.IsKeyPressed(Keys.M) && objetoSelecionado != null)
        objetoSelecionado.MatrizImprimir();
      if (estadoTeclado.IsKeyPressed(Keys.I) && objetoSelecionado != null)
        objetoSelecionado.MatrizAtribuirIdentidade();
      if (estadoTeclado.IsKeyPressed(Keys.N) && objetoSelecionado != null)
        objetoSelecionado = null;
      #endregion

      // ## 2. Estrutura de dados: polígono
      // Quando pressionar a tecla Enter finaliza o desenho do novo polígono.  
      if (estadoTeclado.IsKeyPressed(Keys.Enter) && objetoNovo != null)
      {
        // Finaliza o polígono em construção
        objetoNovo.ObjetoAtualizar();
        // Seleciona o polígono finalizado para exibir sua BBox
        objetoSelecionado = objetoNovo;
        objetoNovo = null;
        Console.WriteLine("Polígono finalizado e selecionado (BBox exibida).");
      }

      // ## 3. Estrutura de dados: polígono
      // Utilize a tecla D para remover o polígono selecionado.  
      if (estadoTeclado.IsKeyPressed(Keys.D) && objetoSelecionado != null)
      {
        // Remove o polígono selecionado
        objetoSelecionado.ObjetoRemover();
        objetoSelecionado = null;
        Console.WriteLine("Polígono removido.");
      }

      // ## 4. Estrutura de dados: vértices mover
      // Utilize a posição do mouse junto com a tecla V para mover vértice mais próximo do polígono selecionado.  
      if (estadoTeclado.IsKeyDown(Keys.V) && objetoSelecionado != null)
      {
        // Move o vértice mais próximo para a posição do mouse (altera os valores do vértice, não a matriz de transformação)
        if (objetoSelecionado.PontosListaTamanho > 0)
        {
          Ponto4D sruPonto = Utilitario.NDC_TelaSRU(ClientSize.X, ClientSize.Y, new Ponto4D(MousePosition.X, MousePosition.Y));
          // Converter para coordenadas locais do objeto
          sruPonto = objetoSelecionado.MatrizGlobalInversa(sruPonto);
          // Atualiza o vértice mais próximo (remover = false para não apagar)
          objetoSelecionado.PontoMaisPerto(sruPonto, false);
        }
      }

      // ## 5. Estrutura de dados: vértices remover
      // Utilize a tecla E para remover o vértice do polígono selecionado mais próximo do ponto do mouse.  
      if (estadoTeclado.IsKeyPressed(Keys.E) && objetoSelecionado != null)
      {
        // Remove o vértice do polígono selecionado mais próximo do ponto do mouse
        if (objetoSelecionado.PontosListaTamanho > 0)
        {
          Ponto4D sruPonto = Utilitario.NDC_TelaSRU(ClientSize.X, ClientSize.Y, new Ponto4D(MousePosition.X, MousePosition.Y));
          // Converter para coordenadas locais do objeto
          sruPonto = objetoSelecionado.MatrizGlobalInversa(sruPonto);
          int idx = objetoSelecionado.PontoMaisPerto(sruPonto, true); // remover = true
          if (idx == -1)
          {
            // O objeto foi removido por ter menos de 2 vértices
            objetoSelecionado = null;
            Console.WriteLine("Vértice removido e polígono descartado (menos de 2 vértices).");
          }
          else
          {
            Console.WriteLine($"Vértice removido. Índice: {idx}");
          }
        }
      }

      // ## 7. Interação: desenho
      // Utilize a tecla P para poder mudar o polígono selecionado para aberto ou fechado.  
      if (estadoTeclado.IsKeyPressed(Keys.P) && objetoSelecionado != null)
      {
        // Só faz sentido para polígonos (classe Poligono que herda de Objeto)
        if (objetoSelecionado is Poligono)
        {
          // Alterna entre LineLoop (fechado) e LineStrip (aberto)
          if (objetoSelecionado.PrimitivaTipo == PrimitiveType.LineLoop)
            objetoSelecionado.PrimitivaTipo = PrimitiveType.LineStrip;
          else
            objetoSelecionado.PrimitivaTipo = PrimitiveType.LineLoop;

          // Atualiza o objeto para que mudanças sejam refletidas
          objetoSelecionado.ObjetoAtualizar();

          Console.WriteLine($"Polígono '{objetoSelecionado.Rotulo}' agora {(objetoSelecionado.PrimitivaTipo == PrimitiveType.LineLoop ? "fechado" : "aberto")}.");
        }
        else
        {
          Console.WriteLine("Tecla P pressionada, mas o objeto selecionado não é um polígono.");
        }
      }

      // ## 8. Interação: cores
      // Utilize o teclado (teclas R=vermelho,G=verde,B=azul) para trocar as cores dos polígonos selecionado.  
      if (estadoTeclado.IsKeyPressed(Keys.R) && objetoSelecionado != null)  // R=vermelho
      {
        // Aplica shader vermelho ao objeto selecionado
        objetoSelecionado.ShaderObjeto = _shaderVermelha;
        // Não é estritamente necessário atualizar os buffers ao trocar shader,
        // mas chamar ObjetoAtualizar garante que o estado do objeto esteja consistente.
        objetoSelecionado.ObjetoAtualizar();
      }
      if (estadoTeclado.IsKeyPressed(Keys.G) && objetoSelecionado != null)  // G=verde
      {
        // Aplica shader verde ao objeto selecionado
        objetoSelecionado.ShaderObjeto = _shaderVerde;
        objetoSelecionado.ObjetoAtualizar();
      }
      if (estadoTeclado.IsKeyPressed(Keys.B) && objetoSelecionado != null)  // B=azul
      {
        // Aplica shader azul ao objeto selecionado
        objetoSelecionado.ShaderObjeto = _shaderAzul;
        objetoSelecionado.ObjetoAtualizar();
      }

      // ## 10. Transformações Geométricas: translação
      // Utilizando as teclas das setas direcionais (cima/baixo,direita,esquerda) movimente o polígono selecionado.  
      if (estadoTeclado.IsKeyPressed(Keys.Left) && objetoSelecionado != null)
        Console.WriteLine("## 10. Transformações Geométricas: translação - esquerda");
      if (estadoTeclado.IsKeyPressed(Keys.Right) && objetoSelecionado != null)
        Console.WriteLine("## 10. Transformações Geométricas: translação - direita");
      if (estadoTeclado.IsKeyPressed(Keys.Up) && objetoSelecionado != null)
        Console.WriteLine("## 10. Transformações Geométricas: translação - cima");
      if (estadoTeclado.IsKeyPressed(Keys.Down) && objetoSelecionado != null)
        Console.WriteLine("## 10. Transformações Geométricas: translação - baixo");
      // ## 11. Transformações Geométricas: escala
      // Utilizando as teclas PageUp/PageDown redimensione o polígono selecionado em relação ao SRU.  [TODO: testar]
      if (estadoTeclado.IsKeyPressed(Keys.PageUp) && objetoSelecionado != null)
        Console.WriteLine("## 11. Transformações Geométricas: escala - PageUp");
      if (estadoTeclado.IsKeyPressed(Keys.PageDown) && objetoSelecionado != null)
        Console.WriteLine("## 11. Transformações Geométricas: escala - PageDown");
      // Utilizando as teclas Home/End redimensione o polígono selecionado em relação ao centro da sua BBox.  [TODO: testar]
      if (estadoTeclado.IsKeyPressed(Keys.Home) && objetoSelecionado != null)
      {
        // Aumenta a escala do objeto selecionado em relação ao centro da sua BBox
        // Usar matriz de transformação (não alterar os vértices)
        const double escalaMaior = 1.5; // 10% maior
        objetoSelecionado.MatrizEscalaXYZBBox(escalaMaior, escalaMaior, 1.0);
      }
      if (estadoTeclado.IsKeyPressed(Keys.End) && objetoSelecionado != null)
      {
        // Diminui a escala do objeto selecionado em relação ao centro da sua BBox
        const double escalaMenor = 0.9; // 10% menor
        objetoSelecionado.MatrizEscalaXYZBBox(escalaMenor, escalaMenor, 1.0);
      }
      // ## 12. Transformações Geométricas: rotação
      // Utilizando as teclas numéricas 1 e 2 gire o polígono selecionado em relação ao SRU.
      if (estadoTeclado.IsKeyPressed(Keys.D1) && objetoSelecionado != null)
        Console.WriteLine("## 12. Transformações Geométricas: rotação - Tecla 1");
      if (estadoTeclado.IsKeyPressed(Keys.D2) && objetoSelecionado != null)
        Console.WriteLine("## 12. Transformações Geométricas: rotação - Tecla 2");
      // Utilizando as teclas numéricas 3 e 4 gire o polígono selecionado em relação ao centro da sua BBox.
      if (estadoTeclado.IsKeyPressed(Keys.D3) && objetoSelecionado != null)
        Console.WriteLine("## 12. Transformações Geométricas: rotação - Tecla 3");
      if (estadoTeclado.IsKeyPressed(Keys.D4) && objetoSelecionado != null)
        Console.WriteLine("## 12. Transformações Geométricas: rotação - Tecla 4");
      #endregion

      #region  Mouse

      // ## 2. Estrutura de dados: polígono
      // Utilize o mouse para clicar na tela com botão direito e poder desenhar um novo polígono.  
      if (MouseState.IsButtonPressed(MouseButton.Right))
      {
        // Obter ponto no espaço SRU a partir da posição do mouse
        Ponto4D sruPonto = Utilitario.NDC_TelaSRU(ClientSize.X, ClientSize.Y, new Ponto4D(MousePosition.X, MousePosition.Y));

        // Se não há um polígono em construção, cria um novo e adiciona o primeiro vértice
        if (objetoNovo == null)
        {
          objetoNovo = new Poligono(mundo, ref rotuloAtual, new System.Collections.Generic.List<Ponto4D>());
          // Adiciona o primeiro ponto duas vezes para possibilitar o "rastro" enquanto o mouse se move
          objetoNovo.PontosAdicionar(sruPonto);
          objetoNovo.PontosAdicionar(sruPonto);
          // Não seleciona o objeto enquanto estiver em construção para evitar exibir a BBox
        }
        else
        {
          // Adiciona um novo vértice ao polígono em construção
          objetoNovo.PontosAdicionar(sruPonto);
        }
      }
      if (MouseState.IsButtonReleased(MouseButton.Right))
      {
        Console.WriteLine("MouseState.IsButtonReleased(MouseButton.Right)");
      }
      // ## 6. Visualização: rastro
      // Exiba o “rasto” ao desenhar os segmentos do polígono.  
      if (MouseState.IsButtonDown(MouseButton.Right) && objetoNovo != null)
      {
        // Enquanto o botão direito estiver pressionado, atualiza o último vértice do polígono para
        // exibir o rastro (pré-visualização do segmento até a posição atual do mouse).
        Ponto4D sruPonto = Utilitario.NDC_TelaSRU(ClientSize.X, ClientSize.Y, new Ponto4D(MousePosition.X, MousePosition.Y));
        // Atualiza o último ponto do objetoNovo
        int ultimo = objetoNovo.PontosListaTamanho - 1;
        if (ultimo >= 0)
          objetoNovo.PontosAlterar(sruPonto, ultimo);
      }

      // ## 9. Interação: BBox
      // Utilize o mouse para clicar na tela com botão esquerdo para selecionar o polígono testando primeiro se o ponto do mouse está dentro da BBox do polígono e depois usando o algoritmo Scan Line.  
      // Caso o polígono seja selecionado se deve exibir a sua BBbox, caso contrário a variável objetoSelecionado deve ser "null", e assim nenhum contorno de BBox deve ser exibido.  
      if (MouseState.IsButtonPressed(MouseButton.Left) && objetoNovo == null)
      {
#if CG_DEBUG
        Console.WriteLine("MouseState.IsButtonPressed(MouseButton.Left)");
#endif

        // Converter posição do mouse para o espaço SRU (mundo)
        Ponto4D sruPonto = Utilitario.NDC_TelaSRU(ClientSize.X, ClientSize.Y, new Ponto4D(MousePosition.X, MousePosition.Y));

        // Tenta selecionar o polígono sob o ponto do mouse
        Objeto objetoEncontrado = null;
        if (mundo.ScanLine(sruPonto, ref objetoEncontrado))
        {
          objetoSelecionado = objetoEncontrado;
          objetoSelecionado.ObjetoAtualizar();
        }
        else
        {
          objetoSelecionado = null;
        }
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

      GL.DeleteProgram(_shaderBranca.Handle);
      GL.DeleteProgram(_shaderVermelha.Handle);
      GL.DeleteProgram(_shaderVerde.Handle);
      GL.DeleteProgram(_shaderAzul.Handle);
      GL.DeleteProgram(_shaderCiano.Handle);
      GL.DeleteProgram(_shaderMagenta.Handle);
      GL.DeleteProgram(_shaderAmarela.Handle);

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

  }
}
