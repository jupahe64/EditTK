#version 450

layout(set = 0, binding = 0) uniform ub_Scene
{
    mat4 View;
    vec3 CamPlaneNormal;
    float CamPlaneOffset;
    vec2 ViewportSize;
    float ForceSolidHighlight;
    float BlendAlpha;
};

layout(set = 1, binding = 0) uniform ub_Plane
{
    mat4 Transform;
};

layout(location = 0) in vec3 pos;
layout(location = 1) in vec2 uv;

layout(location = 0) out vec3 fragPos;
layout(location = 1) out float fragDepth;

void main() {

    vec4 worldPos = Transform * vec4(pos, 1);

    fragPos = worldPos.xyz;
   
    gl_Position = View * vec4(fragPos, 1);

    fragDepth = (CamPlaneOffset-dot(worldPos.xyz,CamPlaneNormal));
}