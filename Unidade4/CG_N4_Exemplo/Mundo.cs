/*
  Atividade em Equipe - Unidade 4
  Cena com dois cubos texturizados, camera orbit com mouse e 7 modos de iluminacao.

  Controles:
    [0]         sem iluminacao
    [1]         2-BasicLighting
    [2]         4-LightingMaps
    [3]         5-LightCasters-DirectionalLights
    [4]         5-LightCasters-PointLights
    [5]         5-LightCasters-Spotlight
    [6]         6-MultipleLights
    Boto direito do mouse (arrastar)  orbitar camera em torno do cubo
    Scroll do mouse                   zoom
    [Escape]    fechar
    [G]         imprimir grafo de cena (gizmo)
*/

using CG_Biblioteca;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Diagnostics;

namespace gcgcg
{
    public class Mundo : GameWindow
    {
        // ── Grafo de cena (heranca Unidade 2/3) ──────────────────────────────
        private static Objeto mundo = null;
        private char rotuloAtual = '?';

        // ── Gizmo (eixos SRU 3D) ─────────────────────────────────────────────
#if CG_Gizmo
        private readonly float[] _sruEixos =
        [
            0f, 0f, 0f,   0.5f,  0f,   0f,
            0f, 0f, 0f,   0f,   0.5f,  0f,
            0f, 0f, 0f,   0f,   0f,   0.5f,
        ];
        private int _vboEixos, _vaoEixos;
        private Stopwatch _stopwatch = new();
        private int _frames = 0;
#endif

        // ── Shaders de cor (gizmo) ────────────────────────────────────────────
        private Shader _shaderBranca, _shaderVermelha, _shaderVerde, _shaderAzul;
        private Shader _shaderCiano,  _shaderMagenta,  _shaderAmarela;

        // ── Shaders de iluminacao (modo 0-6) ─────────────────────────────────
        private Shader[] _shadersModo = new Shader[7];

        // ── Cubos texturizados ────────────────────────────────────────────────
        private CuboTexturizado _cuboBig;
        private CuboTexturizado _cuboPequeno;
        private Texture _texEquipe;

        // ── Camera orbit ──────────────────────────────────────────────────────
        private Camera  _camera;                     // somente para GetProjectionMatrix
        private float   _camYaw    = 45f;            // graus
        private float   _camPitch  = 20f;            // graus
        private float   _camRadius = 6f;
        private Vector2 _lastMouse;
        private bool    _primeiroMovimento = true;

        // ── Animacao do cubo pequeno ──────────────────────────────────────────
        private float _orbitAngle   = 0f;            // angulo de orbita (rad)
        private float _selfRotAngle = 0f;            // auto-rotacao (rad)
        private const float OrbitRadius      = 2.5f;
        private const float OrbitSpeedDeg    = 60f;  // graus/s — sentido anti-horario
        private const float SelfRotSpeedDeg  = 90f;

        // ── Modo de iluminacao ────────────────────────────────────────────────
        private int _modoLuz = 0;

        // Posicoes das 4 luzes pontuais (modo 6)
        private static readonly Vector3[] PtLightPositions =
        {
            new Vector3( 1.5f,  0.5f,  2.0f),
            new Vector3( 2.3f, -1.5f, -2.0f),
            new Vector3(-2.0f,  1.5f, -3.0f),
            new Vector3( 0.0f,  0.5f, -2.0f),
        };

        public Mundo(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws)
        {
            mundo ??= new Objeto(null, ref rotuloAtual);
        }

        // ─────────────────────────────────────────────────────────────────────
        protected override void OnLoad()
        {
            base.OnLoad();

            Utilitario.Diretivas();
#if CG_DEBUG
            Console.WriteLine($"Janela: {FramebufferSize.X}x{FramebufferSize.Y}");
#endif
            GL.ClearColor(0.05f, 0.05f, 0.08f, 1f);
            GL.Enable(EnableCap.DepthTest);
            // CullFace desabilitado para evitar artefatos nos cubos texturizados
            GL.Disable(EnableCap.CullFace);

            // ── Shaders de cor (gizmo) ────────────────────────────────────────
            _shaderBranca  = new Shader("Shaders/shader.vert", "Shaders/shaderBranca.frag");
            _shaderVermelha= new Shader("Shaders/shader.vert", "Shaders/shaderVermelha.frag");
            _shaderVerde   = new Shader("Shaders/shader.vert", "Shaders/shaderVerde.frag");
            _shaderAzul    = new Shader("Shaders/shader.vert", "Shaders/shaderAzul.frag");
            _shaderCiano   = new Shader("Shaders/shader.vert", "Shaders/shaderCiano.frag");
            _shaderMagenta = new Shader("Shaders/shader.vert", "Shaders/shaderMagenta.frag");
            _shaderAmarela = new Shader("Shaders/shader.vert", "Shaders/shaderAmarela.frag");

            // ── Shaders de iluminacao ─────────────────────────────────────────
            for (int i = 0; i <= 6; i++)
                _shadersModo[i] = new Shader("Shaders/shader_tex.vert", $"Shaders/shader_mode{i}.frag");

            // Configurar samplers uma unica vez por shader
            foreach (var sh in _shadersModo)
            {
                sh.Use();
                sh.SetInt("texture0",    0);
                sh.SetInt("diffuseMap",  0);
                sh.SetInt("specularMap", 1);
            }

#if CG_Gizmo
            // ── Gizmo: eixos SRU ─────────────────────────────────────────────
            _vboEixos = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboEixos);
            GL.BufferData(BufferTarget.ArrayBuffer, _sruEixos.Length * sizeof(float), _sruEixos, BufferUsageHint.StaticDraw);
            _vaoEixos = GL.GenVertexArray();
            GL.BindVertexArray(_vaoEixos);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            _stopwatch.Start();
#endif

            // ── Textura da equipe ─────────────────────────────────────────────
            // Substitua "Textures/equipe.png" pela foto real dos integrantes
            try   { _texEquipe = Texture.LoadFromFile("Textures/equipe.png"); }
            catch { _texEquipe = CriarTexturaFallback(80, 120, 200); }

            // ── Cubos ─────────────────────────────────────────────────────────
            _cuboBig     = new CuboTexturizado(_texEquipe);   // grande (fixo na origem)
            _cuboPequeno = new CuboTexturizado(_texEquipe);   // pequeno (orbita)

            // ── Camera ────────────────────────────────────────────────────────
            _camera = new Camera(ComputeCamPos(), FramebufferSize.X / (float)FramebufferSize.Y);

            Console.WriteLine("Modo de iluminacao: [0]=sem luz, [1-6]=varios modelos");
            Console.WriteLine("Botao direito do mouse: orbitar camera | Scroll: zoom");
        }

        // ─────────────────────────────────────────────────────────────────────
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // ── Matrizes de camera ────────────────────────────────────────────
            Vector3 camPos = ComputeCamPos();
            Vector3 camFront = Vector3.Normalize(-camPos);   // sempre olha para a origem
            Matrix4 view       = Matrix4.LookAt(camPos, Vector3.Zero, Vector3.UnitY);
            Matrix4 projection = _camera.GetProjectionMatrix();

            // ── Shader atual ──────────────────────────────────────────────────
            Shader sh = _shadersModo[_modoLuz];
            sh.Use();
            sh.SetVector3("viewPos", camPos);
            ConfigurarLuz(sh, camPos, camFront);

            // ── Cubo grande (fixo na origem, scale 1.0) ───────────────────────
            Matrix4 modelBig = Matrix4.Identity;
            _cuboBig.Render(sh, modelBig, view, projection);

            // ── Cubo pequeno (orbita em torno do eixo Z) ─────────────────────
            float ox = OrbitRadius * MathF.Cos(_orbitAngle);
            float oy = OrbitRadius * MathF.Sin(_orbitAngle);
            Matrix4 modelSmall =
                Matrix4.CreateScale(0.4f)
                * Matrix4.CreateRotationZ(_selfRotAngle)
                * Matrix4.CreateTranslation(ox, oy, 0f);
            _cuboPequeno.Render(sh, modelSmall, view, projection);

#if CG_Gizmo
            GizmoSru3D(view, projection);
#if CG_DEBUG
            _frames++;
            if (_stopwatch.ElapsedMilliseconds >= 1000)
            {
                Console.WriteLine($"FPS: {_frames}  | Modo luz: {_modoLuz}");
                _frames = 0;
                _stopwatch.Restart();
            }
#endif
#endif
            SwapBuffers();
        }

        // ─────────────────────────────────────────────────────────────────────
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            float dt = (float)e.Time;

            // ── Animacao do cubo pequeno ─────────────────────────────────────
            _orbitAngle   += MathHelper.DegreesToRadians(OrbitSpeedDeg)   * dt;
            _selfRotAngle += MathHelper.DegreesToRadians(SelfRotSpeedDeg) * dt;
            if (_orbitAngle   > MathF.PI * 2f) _orbitAngle   -= MathF.PI * 2f;
            if (_selfRotAngle > MathF.PI * 2f) _selfRotAngle -= MathF.PI * 2f;

            // ── Teclado ───────────────────────────────────────────────────────
            var kb = KeyboardState;
            if (kb.IsKeyPressed(Keys.Escape)) Close();

            if (kb.IsKeyPressed(Keys.D0)) { _modoLuz = 0; MsgModo(); }
            if (kb.IsKeyPressed(Keys.D1)) { _modoLuz = 1; MsgModo(); }
            if (kb.IsKeyPressed(Keys.D2)) { _modoLuz = 2; MsgModo(); }
            if (kb.IsKeyPressed(Keys.D3)) { _modoLuz = 3; MsgModo(); }
            if (kb.IsKeyPressed(Keys.D4)) { _modoLuz = 4; MsgModo(); }
            if (kb.IsKeyPressed(Keys.D5)) { _modoLuz = 5; MsgModo(); }
            if (kb.IsKeyPressed(Keys.D6)) { _modoLuz = 6; MsgModo(); }

#if CG_DEBUG
            if (kb.IsKeyPressed(Keys.G)) mundo.GrafocenaImprimir("");
#endif

            // ── Mouse: orbita de camera ───────────────────────────────────────
            if (MouseState.IsButtonDown(MouseButton.Right))
            {
                if (_primeiroMovimento)
                {
                    _lastMouse = MousePosition;
                    _primeiroMovimento = false;
                }
                else
                {
                    float dx = MousePosition.X - _lastMouse.X;
                    float dy = MousePosition.Y - _lastMouse.Y;
                    _camYaw   += dx * 0.3f;
                    _camPitch -= dy * 0.3f;   // invertido: subir mouse = subir camera
                    _camPitch  = Math.Clamp(_camPitch, -80f, 80f);
                }
                _lastMouse = MousePosition;
            }
            else
            {
                _primeiroMovimento = true;
            }

            // Atualiza aspect ratio se janela mudou
            _camera.AspectRatio = FramebufferSize.X / (float)FramebufferSize.Y;
        }

        // ─────────────────────────────────────────────────────────────────────
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            _camRadius -= e.OffsetY * 0.3f;
            _camRadius = Math.Clamp(_camRadius, 2f, 20f);
        }

        // ─────────────────────────────────────────────────────────────────────
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
#if CG_DEBUG
            Console.WriteLine($"Janela: {FramebufferSize.X}x{FramebufferSize.Y}");
#endif
            GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        }

        // ─────────────────────────────────────────────────────────────────────
        protected override void OnUnload()
        {
            _cuboBig.OnUnload();
            _cuboPequeno.OnUnload();

            mundo.OnUnload();

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);

#if CG_Gizmo
            GL.DeleteBuffer(_vboEixos);
            GL.DeleteVertexArray(_vaoEixos);
#endif
            GL.DeleteProgram(_shaderBranca.Handle);
            GL.DeleteProgram(_shaderVermelha.Handle);
            GL.DeleteProgram(_shaderVerde.Handle);
            GL.DeleteProgram(_shaderAzul.Handle);
            GL.DeleteProgram(_shaderCiano.Handle);
            GL.DeleteProgram(_shaderMagenta.Handle);
            GL.DeleteProgram(_shaderAmarela.Handle);
            foreach (var s in _shadersModo) GL.DeleteProgram(s.Handle);

            base.OnUnload();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Calcula posicao da camera a partir de yaw/pitch/radius
        private Vector3 ComputeCamPos()
        {
            float pitchRad = MathHelper.DegreesToRadians(_camPitch);
            float yawRad   = MathHelper.DegreesToRadians(_camYaw);
            return new Vector3(
                _camRadius * MathF.Cos(pitchRad) * MathF.Cos(yawRad),
                _camRadius * MathF.Sin(pitchRad),
                _camRadius * MathF.Cos(pitchRad) * MathF.Sin(yawRad));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Configura os uniforms de iluminacao para o modo atual
        private void ConfigurarLuz(Shader sh, Vector3 camPos, Vector3 camFront)
        {
            switch (_modoLuz)
            {
                case 0:
                    break; // sem iluminacao — shader so usa textura

                case 1: // BasicLighting
                    sh.SetVector3("lightDir",   new Vector3(-0.2f, -1.0f, -0.3f));
                    sh.SetVector3("lightColor", Vector3.One);
                    break;

                case 2: // LightingMaps
                    sh.SetVector3("lightDir",   new Vector3(-0.2f, -1.0f, -0.3f));
                    sh.SetVector3("lightColor", Vector3.One);
                    break;

                case 3: // DirectionalLight estruturada
                    sh.SetVector3("dirLight.direction", new Vector3(-0.2f, -1.0f, -0.3f));
                    sh.SetVector3("dirLight.ambient",   new Vector3(0.05f, 0.05f, 0.05f));
                    sh.SetVector3("dirLight.diffuse",   new Vector3(0.8f,  0.8f,  0.8f));
                    sh.SetVector3("dirLight.specular",  new Vector3(1f,    1f,    1f));
                    break;

                case 4: // PointLight
                    sh.SetVector3("pointLight.position",  new Vector3(1.5f, 2.0f, 2.0f));
                    sh.SetFloat  ("pointLight.constant",  1.0f);
                    sh.SetFloat  ("pointLight.linear",    0.09f);
                    sh.SetFloat  ("pointLight.quadratic", 0.032f);
                    sh.SetVector3("pointLight.ambient",   new Vector3(0.05f, 0.05f, 0.05f));
                    sh.SetVector3("pointLight.diffuse",   new Vector3(0.8f,  0.8f,  0.8f));
                    sh.SetVector3("pointLight.specular",  Vector3.One);
                    break;

                case 5: // Spotlight (lanterna na camera)
                    sh.SetVector3("lightPos",      camPos);
                    sh.SetVector3("lightDir",      camFront);
                    sh.SetFloat  ("cutOff",        MathF.Cos(MathHelper.DegreesToRadians(12.5f)));
                    sh.SetFloat  ("outerCutOff",   MathF.Cos(MathHelper.DegreesToRadians(17.5f)));
                    sh.SetVector3("lightColor",    Vector3.One);
                    break;

                case 6: // MultipleLights
                    // Luz direcional
                    sh.SetVector3("dirLight.direction", new Vector3(-0.2f, -1.0f, -0.3f));
                    sh.SetVector3("dirLight.ambient",   new Vector3(0.05f, 0.05f, 0.05f));
                    sh.SetVector3("dirLight.diffuse",   new Vector3(0.4f,  0.4f,  0.4f));
                    sh.SetVector3("dirLight.specular",  new Vector3(0.5f,  0.5f,  0.5f));
                    // 4 luzes pontuais
                    for (int i = 0; i < 4; i++)
                    {
                        sh.SetVector3($"pointLights[{i}].position",  PtLightPositions[i]);
                        sh.SetFloat  ($"pointLights[{i}].constant",  1.0f);
                        sh.SetFloat  ($"pointLights[{i}].linear",    0.09f);
                        sh.SetFloat  ($"pointLights[{i}].quadratic", 0.032f);
                        sh.SetVector3($"pointLights[{i}].ambient",   new Vector3(0.05f, 0.05f, 0.05f));
                        sh.SetVector3($"pointLights[{i}].diffuse",   new Vector3(0.8f,  0.8f,  0.8f));
                        sh.SetVector3($"pointLights[{i}].specular",  Vector3.One);
                    }
                    // Spotlight (camera)
                    sh.SetVector3("spotLight.position",   camPos);
                    sh.SetVector3("spotLight.direction",  camFront);
                    sh.SetFloat  ("spotLight.cutOff",     MathF.Cos(MathHelper.DegreesToRadians(12.5f)));
                    sh.SetFloat  ("spotLight.outerCutOff",MathF.Cos(MathHelper.DegreesToRadians(17.5f)));
                    sh.SetFloat  ("spotLight.constant",   1.0f);
                    sh.SetFloat  ("spotLight.linear",     0.09f);
                    sh.SetFloat  ("spotLight.quadratic",  0.032f);
                    sh.SetVector3("spotLight.ambient",    Vector3.Zero);
                    sh.SetVector3("spotLight.diffuse",    Vector3.One);
                    sh.SetVector3("spotLight.specular",   Vector3.One);
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Textura fallback colorida (1x1 pixel)
        private static Texture CriarTexturaFallback(byte r, byte g, byte b)
        {
            int handle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, handle);
            byte[] data = { r, g, b, 255 };
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            return new Texture(handle);
        }

        private static void MsgModo()
        {
            // mensagem impressa em OnUpdateFrame — evitar chamada dupla
        }

#if CG_Gizmo
        private void GizmoSru3D(Matrix4 view, Matrix4 projection)
        {
#if CG_OpenGL && !CG_DirectX
            var model = Matrix4.Identity;
            GL.BindVertexArray(_vaoEixos);

            _shaderVermelha.SetMatrix4("model",      model);
            _shaderVermelha.SetMatrix4("view",       view);
            _shaderVermelha.SetMatrix4("projection", projection);
            _shaderVermelha.Use();
            GL.DrawArrays(PrimitiveType.Lines, 0, 2);

            _shaderVerde.SetMatrix4("model",      model);
            _shaderVerde.SetMatrix4("view",       view);
            _shaderVerde.SetMatrix4("projection", projection);
            _shaderVerde.Use();
            GL.DrawArrays(PrimitiveType.Lines, 2, 2);

            _shaderAzul.SetMatrix4("model",      model);
            _shaderAzul.SetMatrix4("view",       view);
            _shaderAzul.SetMatrix4("projection", projection);
            _shaderAzul.Use();
            GL.DrawArrays(PrimitiveType.Lines, 4, 2);
#endif
        }
#endif
    }
}
