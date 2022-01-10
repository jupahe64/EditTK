#version 450

#define max3(a,b,c) max(max(a,b),c);

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

layout(location = 0) out vec4 outColor;
layout(location = 1) out uint outPickingID;

float oneOverLogOfTen = 1.0/log(10.0);

//inspired by blender checker texture node
float checker(vec3 p, float checkerSize)
{
	vec3 _p = p / checkerSize - 0.5;

    vec3 w = fwidth(_p);
    vec3 i = clamp((abs(mod(_p,2.0)-1.0)-0.5)/w,-0.5,0.5)+0.5;
    return abs(abs(i.x-i.z)-i.y);
}

float blendChecker(vec3 p){
	
	vec3 gradientVec = vec3(dFdy(p.x),dFdy(p.x),dFdy(p.z));

    float max_w = max3(fwidth(p.x),fwidth(p.x),fwidth(p.z));
	
	float l_w = max(1,log(max_w*100) * oneOverLogOfTen);

	float b = abs(mod(l_w, 2.0)-1.0);

    float sizeA = pow(10.0,
        max(0.0,floor((l_w)/2.0)*2.0));

    float sizeB = pow(10.0,
        max(0.0,floor((l_w-1.0)/2.0)*2.0+1.0));
    
    float checkerA = checker(p, sizeA);
    float checkerB = checker(p, sizeB);

//    vec3 tmp = abs(fract(p/(sizeA*10))-0.5);
//
//    vec3 w = fwidth(p/10/sizeA);
//
//    float line = min(
//        smoothstep(0.5, 0.5-w.x,tmp.x),
//        smoothstep(0.5, 0.5-w.z,tmp.z)
//    );


	return 0.5+mix(checkerA,
	               checkerB,b)*0.5;
}



void main()
{
    outColor = vec4(vec3(0.5)*blendChecker(fragPos), 1);

    vec2 uv = gl_FragCoord.xy/ViewportSize;

    //outColor = texture(sampler2D(T_Color0,S_Color0), uv) + vec4(uv,1.0,1.0);
    //outColor = vec4(1.0);
    //outPickingID = PickingID;

    outPickingID = 1;

    gl_FragDepth =  fragDepth;
}