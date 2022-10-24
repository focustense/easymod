#version 330 core

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 lightPosition;

layout(location = 0) in vec3 vPos;
layout(location = 1) in vec3 vNormal;
layout(location = 2) in vec3 vColor;

out vec3 fragPosition;
out vec3 fragNormal;
out vec3 fragLightPosition;
out vec3 fragObjectColor;
out vec3 specNormal;

void main()
{
    gl_Position = projection * view * model * vec4(vPos, 1.0);
    fragPosition = vec3(view * model * vec4(vPos, 1.0));
    fragNormal = mat3(transpose(inverse(view * model))) * vNormal;
    fragLightPosition = vec3(view * vec4(lightPosition, 1.0));
    fragObjectColor = vColor;
}
