#version 450

layout(location = 0) in vec2 pos;
layout(location = 1) in vec2 uv;

layout(location = 0) out vec2 fragUV;

void main() {
    fragUV = uv;
   
    gl_Position = vec4(pos, 0.5, 1.0);
}