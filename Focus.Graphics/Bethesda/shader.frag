#version 330 core

uniform sampler2D diffuseTexture;
uniform sampler2D normalMap;
uniform sampler2D specularMap;
uniform samplerCube environmentTexture;
uniform sampler2D reflectionMap;

uniform vec3 ambientLightingColor;
uniform float ambientLightingStrength;
uniform vec3 diffuseLightingColor;
uniform float diffuseLightingStrength;
uniform float environmentStrength;
uniform bool hasEnvironmentTexture;
uniform bool hasNormalMap;
uniform float shininess;
uniform vec3 specularLightingColor;
uniform float specularLightingStrength;
uniform int specularSource; // 0 = none, 1 = normal alpha, 2 = specular map
uniform int normalMapSwizzle; // 0 = rgba, 1 = rbga (g/b flipped, for MS normals)

in vec2 fUV;

// "ns" = "normal space", might be view space or tangent space.
// Vertex shader decides based on normal map type.
in vec3 nsFragPosition;
in vec3 nsNormalDirection;
in vec3 nsLightPosition;
in mat3 nsNormalMapTransform;
in mat4 nsCubeMapTransform;

out vec4 fColor;

void main()
{
    vec3 materialColor = vec3(texture(diffuseTexture, fUV));
    
    // Ambient lighting
    vec3 ambientComponent = ambientLightingStrength * ambientLightingColor;

    // Diffuse lighting
    vec4 normalSample = texture(normalMap, fUV);
    switch (normalMapSwizzle) {
        case 1:
            normalSample = normalSample.rbga;
            break;
    }
    vec3 nsNormal = normalize(hasNormalMap
        ? vec3(nsNormalMapTransform * normalize(normalSample.rgb * 2.0 - 1.0))
        : nsNormalDirection);
    vec3 nsLightDirection = normalize(nsLightPosition - nsFragPosition);
    float diffuseAmount = clamp(dot(nsNormal, nsLightDirection), 0, 1);
    vec3 diffuseComponent = diffuseAmount * diffuseLightingColor * diffuseLightingStrength;

    // Specular lighting
    float specularSample = 1;
    if (specularSource == 1) {
        specularSample = normalSample.a;
    } else if (specularSource == 2) {
        vec4 specularMapSample = texture(specularMap, fUV);
        specularSample = (specularMapSample.r + specularMapSample.g + specularMapSample.b) / 3.0;
    }
    float specularStrength = specularSample * specularLightingStrength;
    vec3 nsViewDirection = normalize(-nsFragPosition);
    vec3 nsReflectDirection = reflect(-nsLightDirection, nsNormal);
    float specularAmount = pow(max(dot(nsViewDirection, nsReflectDirection), 0.0), shininess);
    vec3 specularComponent = specularStrength * specularAmount * specularLightingColor;

    // Environment mapping (reflections)
    vec3 environmentComponent = vec3(0);
    if (hasEnvironmentTexture) {
        // Reflection map relates to the object, not the cube, same as specular map.
        vec4 reflectionMapSample = texture(reflectionMap, fUV);
        float reflectionAmount =
            (reflectionMapSample.r + reflectionMapSample.g + reflectionMapSample.b) / 3.0;
        vec3 nsEnvReflection = reflect(-nsViewDirection, nsNormal);
        vec3 nsEnvDirection = vec3(nsCubeMapTransform * vec4(nsEnvReflection, 0));
        vec4 environmentSample = texture(environmentTexture, nsEnvDirection);
        environmentComponent = environmentStrength * reflectionAmount * environmentSample.rgb;
    }

    // Apply all lighting
    fColor = vec4((ambientComponent + diffuseComponent + specularComponent + environmentComponent) * materialColor, 1);
}
