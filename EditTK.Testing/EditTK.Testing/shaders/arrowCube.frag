#version 450

#define UNIFORM_ARROW_SCALE

float eval(float signedDistance, float w){
    return smoothstep(0.0,w,-signedDistance);
}

float evalShd(float signedDistance, float r){
    return smoothstep(-r,0.0,-signedDistance)*0.2;
}

float sdCapsule( vec2 p, vec2 a, vec2 b, float r )
{
  vec2 pa = p - a, ba = b - a;
  float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
  return length( pa - ba*h ) - r;
}

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
layout(location = 5) in flat vec3 boxScale;

layout(location = 0) out vec4 outColor;
layout(location = 1) out uint outPickingID;




void main()
{

    float w = fwidth(vPos.x);
    float r = 0.05 * min(boxScale.x,boxScale.z);
    float a = vCol.r;
    
    a *= a*a;
    vec2 pos2d = vPos.xz;
   
    #ifdef UNIFORM_ARROW_SCALE
    float s = min(boxScale.x,boxScale.z);
    #else
    vec2 s = boxScale.xz;
    #endif
   
    float d = sdCapsule(pos2d, vec2(-.25,0)*s,vec2(0,.25)*s, r);
   
    d = min(d,sdCapsule(pos2d, vec2(.25,0)*s,vec2(0,.25)*s, r));
   
    float l = smoothstep(-1,1,vPos.y);

    vec3 color = vec3(0,0.25,1);
   
    outColor = vec4(color*
    mix(0.2*l+0.1,1.0, eval(d,w)+evalShd(d,r)),
    1);
    outColor = mix(outColor, vec4(color,1),a);


    outColor.rgb = mix(outColor.rgb,highlightColor.rgb,highlightColor.a);

    outPickingID = id;

    gl_FragDepth =  depth;
}