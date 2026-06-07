#version 330 core
// Modo 4: 5-LightCasters-PointLights (luz pontual com atenuacao)

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
out vec4 FragColor;

struct PointLight {
    vec3  position;
    float constant;
    float linear;
    float quadratic;
    vec3  ambient;
    vec3  diffuse;
    vec3  specular;
};

uniform sampler2D texture0;
uniform PointLight pointLight;
uniform vec3 viewPos;

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec3 texColor)
{
    vec3  lightDir   = normalize(light.position - fragPos);
    float diff       = max(dot(normal, lightDir), 0.0);
    vec3  reflectDir = reflect(-lightDir, normal);
    float spec       = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);

    float dist        = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * dist + light.quadratic * dist * dist);

    vec3 ambient  = light.ambient  * texColor  * attenuation;
    vec3 diffuse  = light.diffuse  * diff * texColor * attenuation;
    vec3 specular = light.specular * spec * attenuation;
    return ambient + diffuse + specular;
}

void main()
{
    vec3 texColor = texture(texture0, TexCoord).rgb;
    vec3 norm     = normalize(Normal);
    vec3 viewDir  = normalize(viewPos - FragPos);
    FragColor = vec4(CalcPointLight(pointLight, norm, FragPos, viewDir, texColor), 1.0);
}
