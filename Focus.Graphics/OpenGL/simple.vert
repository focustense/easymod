#version 330 core

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

layout(location = 0) in vec3 vPos;

void main()
{
    gl_Position = projection * view * model * vec4(vPos, 1.0);
}
