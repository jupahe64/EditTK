#version 450

#define MAT4_ATTRIBUTE(LOC1,LOC2,LOC3,LOC4, NAME) \
    layout(location = LOC1) in vec4 NAME##_1;\
    layout(location = LOC2) in vec4 NAME##_2;\
    layout(location = LOC3) in vec4 NAME##_3;\
    layout(location = LOC4) in vec4 NAME##_4;\
    mat4 NAME = mat4(NAME##_1,NAME##_2,NAME##_3,NAME##_4);

layout(set = 0, binding = 0) uniform ub_Scene
{
    mat4 View;
    vec3 CamPlaneNormal;
    float CamPlaneOffset;
    vec2 ViewportSize;
    float ForceSolidHighlight;
    float BlendAlpha;
};

//layout(set = 1, binding = 0) uniform ub_Plane
//{
//    mat4 Transform;
//};

layout(location = 0) in vec3 pos;
layout(location = 1) in vec4 col;

MAT4_ATTRIBUTE(2,3,4,5, Transform)

layout(location = 6) in uint Id;
layout(location = 7) in vec4 HighlightColor;


layout(location = 0) out vec3 vPos;
layout(location = 1) out float depth;
layout(location = 2) out flat uint id;
layout(location = 3) out vec4 vCol;
layout(location = 4) out vec4 highlightColor;

void main() {

    vec4 worldPos = Transform * vec4(pos, 1);

    vPos = pos.xyz;

    vCol = col;
   
    gl_Position = View * vec4(worldPos.xyz, 1);

    depth = (CamPlaneOffset-dot(worldPos.xyz,CamPlaneNormal));

    id = Id;

    highlightColor = HighlightColor;
}