#version 450

layout(location = 0) in vec2 fragUV;

layout(location = 0) out vec4 outColor;

layout(set = 0, binding = 0) uniform ub_Scene
{
    mat4 View;
    vec3 CamPlaneNormal;
    float CamPlaneOffset;
    vec2 ViewportSize;
    float ForceSolidHighlight;
    float BlendAlpha;
};

layout(set = 2, binding = 0) uniform texture2D  Color0Texture;
layout(set = 2, binding = 1) uniform texture2D  Color1Texture;
layout(set = 2, binding = 2) uniform texture2D  Depth0Texture;
layout(set = 2, binding = 3) uniform texture2D  Depth1Texture;
layout(set = 2, binding = 4) uniform sampler    TextureSampler;
layout(set = 2, binding = 5) uniform itexture2D Picking1Texture;
layout(set = 2, binding = 6) uniform sampler    PickingSampler;

layout(set = 2, binding = 7) uniform ub_HoverColor
{
    vec4 HoverColor;
};

float smin(float a, float b, float k) {
  float res = exp(-k * a) + exp(-k * b);
  return -log(res) / k;
}

void main() {
    ivec2 texelPos = ivec2(fragUV.x*ViewportSize.x,fragUV.y*ViewportSize.y);

    outColor = texture(sampler2D(Color0Texture, TextureSampler), fragUV);
    
    
    ////----------------- selection highlight --------------------------------------


    int sampleOffsetX = 2;
    int sampleOffsetY = 2;

    ivec2 point00 = texelPos + ivec2(sampleOffsetX*0, sampleOffsetY*0);
    ivec2 point01 = texelPos + ivec2(sampleOffsetX*0, sampleOffsetY*1);
    ivec2 point10 = texelPos + ivec2(sampleOffsetX*1, sampleOffsetY*0);
    ivec2 point11 = texelPos + ivec2(sampleOffsetX*1, sampleOffsetY*1);
 
    int p1_00 = texelFetch(isampler2D(Picking1Texture, PickingSampler), point00, 0).r;
    int p1_10 = texelFetch(isampler2D(Picking1Texture, PickingSampler), point10, 0).r;
    int p1_01 = texelFetch(isampler2D(Picking1Texture, PickingSampler), point01, 0).r;
    int p1_11 = texelFetch(isampler2D(Picking1Texture, PickingSampler), point11, 0).r;
 
    float d0_00 = texelFetch(sampler2D(Depth0Texture, TextureSampler), point00, 0).r;
    float d0_10 = texelFetch(sampler2D(Depth0Texture, TextureSampler), point10, 0).r;
    float d0_01 = texelFetch(sampler2D(Depth0Texture, TextureSampler), point01, 0).r;
    float d0_11 = texelFetch(sampler2D(Depth0Texture, TextureSampler), point11, 0).r;

    float d1_00 = texelFetch(sampler2D(Depth1Texture, TextureSampler), point00, 0).r;
    float d1_10 = texelFetch(sampler2D(Depth1Texture, TextureSampler), point10, 0).r;
    float d1_01 = texelFetch(sampler2D(Depth1Texture, TextureSampler), point01, 0).r;
    float d1_11 = texelFetch(sampler2D(Depth1Texture, TextureSampler), point11, 0).r;

    vec4 c1_00 = texelFetch(sampler2D(Color1Texture, TextureSampler), point00, 0);
    vec4 c1_10 = texelFetch(sampler2D(Color1Texture, TextureSampler), point10, 0);
    vec4 c1_01 = texelFetch(sampler2D(Color1Texture, TextureSampler), point01, 0);
    vec4 c1_11 = texelFetch(sampler2D(Color1Texture, TextureSampler), point11, 0);
 
    float line = float(
        p1_00!=p1_10 || 
        p1_00!=p1_01 || 
        p1_00!=p1_11);
 
    float pr = 0;         //Pixel depth Reference
    vec4  pc = vec4(0);   //Pixel Color
    float pd = 1;         //Pixel Depth
    float m = 0;          //Mix value
    
    m = float(   d1_00<pd);
    pd =  mix(pd,d1_00,m );
    pr =  mix(pr,d0_00,m );
    pc =  mix(pc,c1_00,m );
    
    m = float(   d1_01<pd);
    pd =  mix(pd,d1_01,m );
    pr =  mix(pr,d0_01,m );
    pc =  mix(pc,c1_01,m );
    
    m = float(   d1_10<pd);
    pd =  mix(pd,d1_10,m );
    pr =  mix(pr,d0_10,m );
    pc =  mix(pc,c1_10,m );
    
    m = float(   d1_11<pd);
    pd =  mix(pd,d1_11,m );
    pr =  mix(pr,d0_11,m );
    pc =  mix(pc,c1_11,m );

    line *= (pd <= pr) ? 1 : 0.5;

    line *= float(pd > 0);
 
    outColor.rgb = mix(outColor.rgb, pc.rgb, line);


    //------------------ hover highlight ----------------------

    const float line_width = 3.0;

    float min_dist = line_width;
    
    const int sampleR = 3;
        
    for(int x = -sampleR; x < sampleR; x ++)
        for(int y = -sampleR; y < sampleR; y ++)
        {
            ivec2 _texelPos = texelPos + ivec2(x,y);

            if(_texelPos.x < 0 || _texelPos.y < 0 || _texelPos.x >= ViewportSize.x-1 || _texelPos.y >= ViewportSize.y-1)
                continue;
            
            bool pCenter = texelFetch(sampler2D(Depth1Texture, TextureSampler), _texelPos, 0).r == 0.0;

            bool pRight  = texelFetch(sampler2D(Depth1Texture, TextureSampler), _texelPos + ivec2(1, 0), 0).r == 0.0;

            bool pBottom = texelFetch(sampler2D(Depth1Texture, TextureSampler), _texelPos + ivec2(0, 1), 0).r == 0.0;
            
            if(pCenter != pRight || pCenter != pBottom)
            {
                float dist = length(vec2(x+0.5,y+0.5));

                float mind  = min(min_dist,dist);

                min_dist  = mix(min_dist, mind, 0.2);
            }
        }

    bool isHoveredPart = (texelFetch(sampler2D(Depth1Texture, TextureSampler), texelPos, 0).r == 0.0);

    float hoverOverlay = isHoveredPart ? 0.6 : 0.0;
    
    hoverOverlay = mix(hoverOverlay, 1.0, smoothstep(line_width,line_width-1.0, min_dist)); 

    
    outColor = mix(outColor, vec4(HoverColor.rgb, 1.0), min(hoverOverlay * HoverColor.a,1.0));

    outColor.a = BlendAlpha;
}