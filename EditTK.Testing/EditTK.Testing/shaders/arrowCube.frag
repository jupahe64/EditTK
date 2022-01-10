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

layout(location = 0) in vec3 fragPos;
layout(location = 1) in float fragDepth;
layout(location = 2) in vec4 fragCol;

layout(location = 0) out vec4 outColor;
layout(location = 1) out uint outPickingID;




void main()
{
    outColor = vec4(
        mix(
            mix(
                vec3(0,0,0.5),
                vec3(0,0.25,1),
                fragPos.y*0.5 + 0.5

            )*0.3,
            vec3(0,0.25,1),

            pow(fragCol.r,2.0)
        ), 1);

    vec2 uv = gl_FragCoord.xy/ViewportSize;

    //outColor = texture(sampler2D(T_Color0,S_Color0), uv) + vec4(uv,1.0,1.0);
    //outColor = vec4(1.0);
    //outPickingID = PickingID;

    outPickingID = 2;

    gl_FragDepth =  fragDepth;
}