#version 330 core
// Modo 6: 6-MultipleLights (direcional + 4 pontuais + spotlight)

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

struct PointLight {
    vec3  position;
    float constant;
    float linear;
    float quadratic;
    vec3  ambient;
    vec3  diffuse;
    vec3  specular;
};

struct SpotLight {
    vec3  position;
    vec3  direction;
    float cutOff;
    float outerCutOff;
    float constant;
    float linear;
    float quadratic;
    vec3  ambient;
    vec3  diffuse;
    vec3  specular;
};

#define NR_POINT_LIGHTS 4

uniform sampler2D diffuseMap;
uniform sampler2D specularMap;
uniform DirLight   dirLight;
uniform PointLight pointLights[NR_POINT_LIGHTS];
uniform SpotLight  spotLight;
uniform vec3 viewPos;

vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir, vec3 dC, vec3 sC)
{
    vec3  ld  = normalize(-light.direction);
    float df  = max(dot(normal, ld), 0.0);
    vec3 refl = reflect(-ld, normal);
    float sp  = pow(max(dot(viewDir, refl), 0.0), 32.0);
    return light.ambient * dC + light.diffuse * df * dC + light.specular * sp * sC;
}

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec3 dC, vec3 sC)
{
    vec3  ld  = normalize(light.position - fragPos);
    float df  = max(dot(normal, ld), 0.0);
    vec3 refl = reflect(-ld, normal);
    float sp  = pow(max(dot(viewDir, refl), 0.0), 32.0);
    float d   = length(light.position - fragPos);
    float att = 1.0 / (light.constant + light.linear * d + light.quadratic * d * d);
    return (light.ambient * dC + light.diffuse * df * dC + light.specular * sp * sC) * att;
}

vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir, vec3 dC, vec3 sC)
{
    vec3  ld    = normalize(light.position - fragPos);
    float df    = max(dot(normal, ld), 0.0);
    vec3  refl  = reflect(-ld, normal);
    float sp    = pow(max(dot(viewDir, refl), 0.0), 32.0);
    float d     = length(light.position - fragPos);
    float att   = 1.0 / (light.constant + light.linear * d + light.quadratic * d * d);
    float theta = dot(ld, normalize(-light.direction));
    float eps   = light.cutOff - light.outerCutOff;
    float inten = clamp((theta - light.outerCutOff) / eps, 0.0, 1.0);
    return (light.ambient * dC + (light.diffuse * df * dC + light.specular * sp * sC) * inten) * att;
}

void main()
{
    vec3 dC      = texture(diffuseMap,  TexCoord).rgb;
    vec3 sC      = texture(specularMap, TexCoord).rgb;
    vec3 norm    = normalize(Normal);
    vec3 viewDir = normalize(viewPos - FragPos);

    vec3 result = CalcDirLight(dirLight, norm, viewDir, dC, sC);
    for (int i = 0; i < NR_POINT_LIGHTS; i++)
        result += CalcPointLight(pointLights[i], norm, FragPos, viewDir, dC, sC);
    result += CalcSpotLight(spotLight, norm, FragPos, viewDir, dC, sC);

    FragColor = vec4(result, 1.0);
}
