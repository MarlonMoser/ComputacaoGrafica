#version 330 core
// Modo 0: sem iluminacao (textura direta)

in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D texture0;

void main()
{
    FragColor = texture(texture0, TexCoord);
}
