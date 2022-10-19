#version 330 core

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

layout(location = 0) in vec3 vPos;
layout(location = 1) in vec3 vNormal;
layout(location = 2) in vec3 vTangent;
layout(location = 3) in vec3 vBitangent;
layout(location = 4) in vec2 vUV;

out vec2 fUV;
out mat3 TBN;

void main()
{
    mat4 mvp = projection * view * model;
    gl_Position = mvp * vec4(vPos, 1.0);

    mat3 mv = mat3(view * model);
    fUV = vec2(vUV.x, 1 - vUV.y);
    vec3 vn_mv = mv * normalize(vNormal);
    vec3 vt_mv = mv * normalize(vTangent);
    vec3 vb_mv = mv * normalize(vBitangent);
    TBN = transpose(mat3(vt_mv, vb_mv, vn_mv));
}