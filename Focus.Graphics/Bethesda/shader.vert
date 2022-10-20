#version 330 core

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 lightPosition;

layout(location = 0) in vec3 vPos;
layout(location = 1) in vec3 vNormal;
layout(location = 2) in vec3 vTangent;
layout(location = 3) in vec3 vBitangent;
layout(location = 4) in vec2 vUV;

out vec2 fUV;
out vec3 tangentFragPosition;
out vec3 tangentLightPosition;

void main()
{
    mat4 mvp = projection * view * model;
    gl_Position = mvp * vec4(vPos, 1.0);
    fUV = vec2(vUV.x, 1 - vUV.y);

    // Tangent space transformations
    mat3 mv = mat3(view * model);
    vec3 vn_mv = mv * normalize(vNormal);
    vec3 vt_mv = mv * normalize(vTangent);
    vec3 vb_mv = mv * normalize(vBitangent);
    mat3 TBN = transpose(mat3(vt_mv, vb_mv, vn_mv));
    tangentFragPosition = TBN * vec3(view * model * vec4(vPos, 1.0));
    tangentLightPosition = TBN * vec3(view * vec4(lightPosition, 1.0));
}
