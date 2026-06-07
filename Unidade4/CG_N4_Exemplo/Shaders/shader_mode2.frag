#version 330 core
// Modo 2: 4-LightingMaps (Phong com mapa difuso e especular)

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D diffuseMap;
uniform sampler2D specularMap;
uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 viewPos;

void main()
{
    vec3 diffColor = texture(diffuseMap,  TexCoord).rgb;
    vec3 specColor = texture(specularMap, TexCoord).rgb;

    vec3 norm = normalize(Normal);
    vec3 ld   = normalize(-lightDir);

    vec3 ambient = 0.1 * diffColor;

    float diff   = max(dot(norm, ld), 0.0);
    vec3 diffuse = diff * lightColor * diffColor;

    vec3 viewDir    = normalize(viewPos - FragPos);
    vec3 reflectDir = reflect(-ld, norm);
    float spec      = pow(max(dot(viewDir, reflectDir), 0.0), 64.0);
    vec3 specular   = spec * lightColor * specColor;

    FragColor = vec4(ambient + diffuse + specular, 1.0);
}
