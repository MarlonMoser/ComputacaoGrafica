#version 330 core
// Modo 5: 5-LightCasters-Spotlight (lanterna na camera)

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D texture0;
uniform vec3  lightPos;
uniform vec3  lightDir;
uniform float cutOff;
uniform float outerCutOff;
uniform vec3  lightColor;
uniform vec3  viewPos;

void main()
{
    vec3 texColor = texture(texture0, TexCoord).rgb;
    vec3 norm     = normalize(Normal);
    vec3 lDir     = normalize(lightPos - FragPos);

    float theta     = dot(lDir, normalize(-lightDir));
    float epsilon   = cutOff - outerCutOff;
    float intensity = clamp((theta - outerCutOff) / epsilon, 0.0, 1.0);

    float dist        = length(lightPos - FragPos);
    float attenuation = 1.0 / (1.0 + 0.09 * dist + 0.032 * dist * dist);

    vec3 ambient  = 0.05 * texColor;

    float diff    = max(dot(norm, lDir), 0.0);
    vec3 diffuse  = diff * lightColor * texColor * intensity * attenuation;

    vec3 viewDir  = normalize(viewPos - FragPos);
    vec3 reflDir  = reflect(-lDir, norm);
    float spec    = pow(max(dot(viewDir, reflDir), 0.0), 32.0);
    vec3 specular = 0.5 * spec * lightColor * intensity * attenuation;

    FragColor = vec4(ambient + diffuse + specular, 1.0);
}
