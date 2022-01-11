#version 450

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
    //float a = abs(fragNrm.x)+abs(fragNrm.y)+abs(fragNrm.z);
    //a = smoothstep(1.1,1.6,a);
    a *= a*a;
    vec2 pos2d = vPos.xz;
   
    float s = min(boxScale.x,boxScale.z);
   
    float d = sdCapsule(pos2d, vec2(-.25,0)*s,vec2(0,.25)*s, r);
   
    d = min(d,sdCapsule(pos2d, vec2(.25,0)*s,vec2(0,.25)*s, r));
   
    float l = smoothstep(-1,1,vPos.y);

    vec3 color = vec3(0,0.25,1);
   
    outColor = vec4(color*
    mix(0.2*l+0.1,1.0, eval(d,w)+evalShd(d,r)),
    1);
    outColor = mix(outColor, vec4(color,1),a);


//    outColor = vec4(
//        mix(
//            mix(
//                vec3(0,0,0.5),
//                vec3(0,0.25,1),
//                vPos.y*0.5 + 0.5
//
//            )*0.3,
//            vec3(0,0.25,1),
//
//            pow(vCol.r,2.0)
//        ), 1);
//
//    outColor.rgb = mix(outColor.rgb,highlightColor.rgb,highlightColor.a);
//
//    vec2 uv = gl_FragCoord.xy/ViewportSize;

    //outColor = texture(sampler2D(T_Color0,S_Color0), uv) + vec4(uv,1.0,1.0);
    //outColor = vec4(1.0);
    //outPickingID = PickingID;

    outPickingID = id;

    gl_FragDepth =  depth;
}