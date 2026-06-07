#version 330 core
// Modo 1: 2-BasicLighting (Phong com luz direcional simples)

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D texture0;
uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 viewPos;

void main()
{
    vec3 texColor = texture(texture0, TexCoord).rgb;
    vec3 norm = normalize(Normal);
    vec3 ld   = normalize(-lightDir);

    vec3 ambient = 0.1 * lightColor * texColor;

    float diff   = max(dot(norm, ld), 0.0);
    vec3 diffuse = diff * lightColor * texColor;

    vec3 viewDir    = normalize(viewPos - FragPos);
    vec3 reflectDir = reflect(-ld, norm);
    float spec      = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
    vec3 specular   = 0.5 * spec * lightColor;

    FragColor = vec4(ambient + diffuse + specular, 1.0);
}
