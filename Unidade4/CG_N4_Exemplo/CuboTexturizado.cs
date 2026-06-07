// Cubo com suporte a textura, normais e coordenadas UV
// Dados de vertices baseados no padrao LearnOpenTK (Chapter2 - Lighting)

using CG_Biblioteca;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace gcgcg
{
    internal class CuboTexturizado
    {
        private int _vao;
        private int _vbo;

        private Texture _textureDiffuse;
        private Texture _textureSpecular;

        // 36 vertices: 6 faces * 2 triangulos * 3 vertices
        // Layout por vertice: pos(3) + normal(3) + uv(2) = 8 floats
        private static readonly float[] Vertices =
        {
            // Face traseira (-Z), normal (0,0,-1)
            -1f, -1f, -1f,   0f,  0f, -1f,   0f, 0f,
             1f,  1f, -1f,   0f,  0f, -1f,   1f, 1f,
             1f, -1f, -1f,   0f,  0f, -1f,   1f, 0f,
             1f,  1f, -1f,   0f,  0f, -1f,   1f, 1f,
            -1f, -1f, -1f,   0f,  0f, -1f,   0f, 0f,
            -1f,  1f, -1f,   0f,  0f, -1f,   0f, 1f,

            // Face frontal (+Z), normal (0,0,1)
            -1f, -1f,  1f,   0f,  0f,  1f,   0f, 0f,
             1f, -1f,  1f,   0f,  0f,  1f,   1f, 0f,
             1f,  1f,  1f,   0f,  0f,  1f,   1f, 1f,
             1f,  1f,  1f,   0f,  0f,  1f,   1f, 1f,
            -1f,  1f,  1f,   0f,  0f,  1f,   0f, 1f,
            -1f, -1f,  1f,   0f,  0f,  1f,   0f, 0f,

            // Face esquerda (-X), normal (-1,0,0)
            -1f,  1f,  1f,  -1f,  0f,  0f,   1f, 0f,
            -1f,  1f, -1f,  -1f,  0f,  0f,   1f, 1f,
            -1f, -1f, -1f,  -1f,  0f,  0f,   0f, 1f,
            -1f, -1f, -1f,  -1f,  0f,  0f,   0f, 1f,
            -1f, -1f,  1f,  -1f,  0f,  0f,   0f, 0f,
            -1f,  1f,  1f,  -1f,  0f,  0f,   1f, 0f,

            // Face direita (+X), normal (1,0,0)
             1f,  1f,  1f,   1f,  0f,  0f,   1f, 0f,
             1f,  1f, -1f,   1f,  0f,  0f,   1f, 1f,
             1f, -1f, -1f,   1f,  0f,  0f,   0f, 1f,
             1f, -1f, -1f,   1f,  0f,  0f,   0f, 1f,
             1f, -1f,  1f,   1f,  0f,  0f,   0f, 0f,
             1f,  1f,  1f,   1f,  0f,  0f,   1f, 0f,

            // Face inferior (-Y), normal (0,-1,0)
            -1f, -1f, -1f,   0f, -1f,  0f,   0f, 1f,
             1f, -1f, -1f,   0f, -1f,  0f,   1f, 1f,
             1f, -1f,  1f,   0f, -1f,  0f,   1f, 0f,
             1f, -1f,  1f,   0f, -1f,  0f,   1f, 0f,
            -1f, -1f,  1f,   0f, -1f,  0f,   0f, 0f,
            -1f, -1f, -1f,   0f, -1f,  0f,   0f, 1f,

            // Face superior (+Y), normal (0,1,0)
            -1f,  1f, -1f,   0f,  1f,  0f,   0f, 1f,
             1f,  1f, -1f,   0f,  1f,  0f,   1f, 1f,
             1f,  1f,  1f,   0f,  1f,  0f,   1f, 0f,
             1f,  1f,  1f,   0f,  1f,  0f,   1f, 0f,
            -1f,  1f,  1f,   0f,  1f,  0f,   0f, 0f,
            -1f,  1f, -1f,   0f,  1f,  0f,   0f, 1f,
        };

        public CuboTexturizado(Texture textureDiffuse, Texture textureSpecular = null)
        {
            _textureDiffuse  = textureDiffuse;
            _textureSpecular = textureSpecular ?? textureDiffuse;

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float), Vertices, BufferUsageHint.StaticDraw);

            int stride = 8 * sizeof(float);
            // Atributo 0: posicao
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            // Atributo 1: normal
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            // Atributo 2: coordenada de textura
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            GL.BindVertexArray(0);
        }

        public void Render(Shader shader, Matrix4 model, Matrix4 view, Matrix4 projection)
        {
            _textureDiffuse.Use(TextureUnit.Texture0);
            _textureSpecular.Use(TextureUnit.Texture1);

            shader.SetMatrix4("model",      model);
            shader.SetMatrix4("view",       view);
            shader.SetMatrix4("projection", projection);

            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            GL.BindVertexArray(0);
        }

        public void OnUnload()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
        }
    }
}
