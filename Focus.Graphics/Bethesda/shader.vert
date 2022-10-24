#version 330 core

uniform mat4 modelToWorld;
uniform mat4 worldToView;
uniform mat4 normalToView;
uniform mat4 projection;
uniform vec3 lightPosition;
// Keep consistent with NormalMapType enum
// 0 = tangent space (default), 1 = object/model space
uniform int normalSpace;

layout(location = 0) in vec3 vPos;
layout(location = 1) in vec3 vNormal;
layout(location = 2) in vec3 vTangent;
layout(location = 3) in vec3 vBitangent;
layout(location = 4) in vec2 vUV;

out vec2 fUV;
out vec3 nsFragPosition;
out vec3 nsLightPosition;
out vec3 nsNormalDirection;
out mat3 nsNormalMapTransform;

void main()
{
    mat4 mv = worldToView * modelToWorld;
    mat4 mvp = projection * mv;
    gl_Position = mvp * vec4(vPos, 1.0);
    fUV = vec2(vUV.x, 1 - vUV.y);

    vec3 mvFragPosition = vec3(mv * vec4(vPos, 1.0));
    mat3 mvNormalMapTransform = mat3(normalToView);
    vec3 mvNormalDirection = mvNormalMapTransform * vNormal;
    vec3 mvLightPosition = vec3(worldToView * vec4(lightPosition, 1.0));
    if (normalSpace == 1) {
        nsFragPosition = mvFragPosition;
        nsNormalDirection = mvNormalDirection;
        nsLightPosition = mvLightPosition;
        nsNormalMapTransform = mvNormalMapTransform;
        return;
    }

    // Tangent space transformations
    vec3 vn_mv = vec3(mv * vec4(normalize(vNormal), 0));
    vec3 vt_mv = vec3(mv * vec4(normalize(vTangent), 0));
    vec3 vb_mv = vec3(mv * vec4(normalize(vBitangent), 0));
    mat3 TBN = transpose(mat3(vt_mv, vb_mv, vn_mv));
    nsFragPosition = TBN * mvFragPosition;
    nsNormalDirection = TBN * mvNormalDirection;
    nsLightPosition = TBN * mvLightPosition;
    nsNormalMapTransform = mat3(1.0);
}
