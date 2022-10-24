#version 330 core

uniform vec3 lightColor;
uniform float ambientStrength;
uniform float specularStrength;
uniform float shininess;

in vec3 fragPosition;
in vec3 fragNormal;
in vec3 fragLightPosition;
in vec3 fragObjectColor;

out vec4 fragColor;

void main()
{
    vec3 normal = normalize(fragNormal);

    // Ambient
    vec3 ambientComponent = ambientStrength * lightColor;

    // Diffuse
    vec3 lightDirection = normalize(fragLightPosition - fragPosition);
    float diffuseStrength = max(dot(normal, lightDirection), 0.0);
    vec3 diffuseComponent = diffuseStrength * lightColor;

    // Specular
    vec3 specularComponent = vec3(0);
    if (diffuseStrength > 0) {
        vec3 viewDirection = normalize(-fragPosition);
        vec3 reflectDirection = reflect(-lightDirection, normal);
        float specularMultiplier = pow(max(dot(viewDirection, reflectDirection), 0.0), shininess);
        specularComponent = specularStrength * specularMultiplier * lightColor;
    }

    fragColor = vec4((ambientComponent + diffuseComponent * 0.001 + specularComponent) * fragObjectColor, 1.0);
}