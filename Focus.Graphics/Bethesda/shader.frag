#version 330 core

uniform sampler2D diffuseTexture;
uniform sampler2D normalTexture;
uniform vec3 lightColor;
uniform vec3 lightDirection;

in vec2 fUV;
in mat3 TBN;

out vec4 fColor;

void main()
{
    vec4 diffuseColor = texture(diffuseTexture, fUV);
    vec3 tangentNormal = normalize(texture(normalTexture, fUV).rgb * 2.0 - 1.0);
    float diffuseAmount = clamp(dot(tangentNormal.rgb, TBN * lightDirection), 0, 1);
    vec3 diffuseLightingComponent = diffuseAmount * lightColor.rgb;
    fColor = vec4(diffuseColor.rgb + diffuseLightingComponent, 1);
}