#version 330 core

uniform vec3 objectColor;

out vec4 fColor;

void main()
{
    fColor = vec4(objectColor, 1);
}
