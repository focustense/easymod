#version 330 core

uniform sampler2D diffuseTexture;
uniform sampler2D normalTexture;
uniform float ambientLightingStrength;
uniform vec3 lightColor;

in vec2 fUV;
in vec3 tangentFragPosition;
in vec3 tangentLightPosition;

out vec4 fColor;

void main()
{
    vec3 diffuseColor = vec3(texture(diffuseTexture, fUV));
    
    // Ambient lighting
    vec3 ambientComponent = ambientLightingStrength * lightColor;

    // Diffuse lighting
    vec4 normalSample = texture(normalTexture, fUV);
    vec3 tangentNormal = normalize(normalSample.rgb * 2.0 - 1.0);
    vec3 tangentLightDirection = normalize(tangentLightPosition - tangentFragPosition);
    float diffuseAmount = clamp(dot(tangentNormal.rgb, tangentLightDirection), 0, 1);
    vec3 diffuseComponent = diffuseAmount * lightColor.rgb;

    // Specular lighting
    float specularStrength = normalSample.a;
    float shininess = 32;
    vec3 tangentViewDirection = normalize(-tangentFragPosition);
    vec3 tangentReflectDirection = reflect(-tangentLightDirection, tangentNormal);
    float specularAmount = pow(max(dot(tangentViewDirection, tangentReflectDirection), 0.0), shininess);
    vec3 specularComponent = specularStrength * specularAmount * lightColor;

    // Apply all lighting
    if (diffuseColor.r < -1000 || diffuseComponent.r < -1000 || specularComponent.r < -1000) return;
    fColor = vec4((ambientComponent + diffuseComponent + specularComponent) * diffuseColor, 1);
    // fColor = vec4(specularComponent, 1);
}