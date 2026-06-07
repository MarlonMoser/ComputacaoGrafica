#version 330 core
// Modo 3: 5-LightCasters-DirectionalLights (luz direcional estruturada)

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
out vec4 FragColor;

struct DirLight {
    vec3 direction;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

uniform sampler2D texture0;
uniform DirLight dirLight;
uniform vec3 viewPos;

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir, vec3 texColor)
{
    vec3 lightDir   = normalize(-light.direction);
    float diff      = max(dot(normal, lightDir), 0.0);
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec      = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);

    vec3 ambient  = light.ambient  * texColor;
    vec3 diffuse  = light.diffuse  * diff * texColor;
    vec3 specular = light.specular * spec;
    return ambient + diffuse + specular;
}

void main()
{
    vec3 texColor = texture(texture0, TexCoord).rgb;
    vec3 norm     = normalize(Normal);
    vec3 viewDir  = normalize(viewPos - FragPos);
    FragColor = vec4(CalcDirLight(dirLight, norm, viewDir, texColor), 1.0);
}
