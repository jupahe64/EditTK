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

layout(location = 0) in vec3 vPos;
layout(location = 1) in float depth;
layout(location = 2) in flat uint id;
layout(location = 3) in vec4 vCol;
layout(location = 4) in vec4 highlightColor;

layout(location = 0) out vec4 outColor;
layout(location = 1) out uint outPickingID;




void main()
{
    outColor = vec4(
        mix(
            mix(
                vec3(0,0,0.5),
                vec3(0,0.25,1),
                vPos.y*0.5 + 0.5

            )*0.3,
            vec3(0,0.25,1),

            pow(vCol.r,2.0)
        ), 1);

    outColor.rgb = mix(outColor.rgb,highlightColor.rgb,highlightColor.a);

    vec2 uv = gl_FragCoord.xy/ViewportSize;

    //outColor = texture(sampler2D(T_Color0,S_Color0), uv) + vec4(uv,1.0,1.0);
    //outColor = vec4(1.0);
    //outPickingID = PickingID;

    outPickingID = id;

    gl_FragDepth =  depth;
}