#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;

out vec3 FragPos;
out vec3 Normal;
out vec2 TexCoord;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec4 worldPos = vec4(aPosition, 1.0) * model;
    FragPos       = vec3(worldPos);
    Normal        = normalize(aNormal * mat3(model));
    TexCoord      = aTexCoord;
    gl_Position   = worldPos * view * projection;
}
