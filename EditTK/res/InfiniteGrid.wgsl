struct Attributes {
    @location(0) Position: vec3<f32>,
    @location(1) TexCoord: vec2<f32>,
    @location(2) Alpha: f32
};

struct Varyings {
    @builtin(position) Position: vec4<f32>,
    @location(0) TexCoord: vec2<f32>,
    @location(1) Alpha: f32
}

struct Uniforms {
    Transform: mat4x4<f32>,
    ViewProjection: mat4x4<f32>,
    TextureTransform: mat2x3<f32>,
    GridColor: vec4<f32>,
    Axis0Color: vec4<f32>,
    Axis1Color: vec4<f32>,
}

@group(0) @binding(0)
var<uniform> ub: Uniforms;

@vertex
fn vs_main(a: Attributes) -> Varyings {
    let pos = ub.ViewProjection * ub.Transform * vec4(a.Position, 1.0);
    return Varyings(pos, a.TexCoord, a.Alpha);
}

fn grid(uv: vec2<f32>, cellSize: f32) -> f32 {
    let cellUV = fract(uv/cellSize)*cellSize;
    let fwUV = fwidth(uv);
    return max(
        smoothstep(cellSize*0.5-fwUV.x*1.5, cellSize*0.5-fwUV.x*0.5, abs(cellUV.x - cellSize*0.5)),
        smoothstep(cellSize*0.5-fwUV.y*1.5, cellSize*0.5-fwUV.y*0.5, abs(cellUV.y - cellSize*0.5))
        );
}

fn AlphaBlend(dst: vec4<f32>, src: vec4<f32>) -> vec4<f32> {
    return vec4(
        mix(dst.rgb, src.rgb, src.a),
        dst.a + (1.0-dst.a) * src.a
    );
}

@fragment
fn fs_main(v: Varyings, @builtin(front_facing) is_front_facing: bool) -> @location(0) vec4<f32> {
    let uv = vec3(v.TexCoord, 1.0) * ub.TextureTransform;
    let fwyUV = abs(dpdy(uv));
    let fwUV = min(vec2(1.0), fwidth(uv));

    let _log = max(-0.5, log(20.0*max(fwyUV.x, fwyUV.y))/log(10.0));

    let logFloor = floor(_log);
    let blend    = fract(_log);

    var oColor = vec4(ub.GridColor.rgb, ub.GridColor.a *
    mix(
        grid(uv, pow(10.0, logFloor)), 
        grid(uv, pow(10.0, logFloor+1.0)), 
        pow(blend, 0.5)
    ));

    oColor = AlphaBlend(oColor, vec4(ub.Axis0Color.rgb, ub.Axis0Color.a *
        smoothstep(fwUV.y*1.5, fwUV.y*0.5, abs(uv.y))
    ));

    oColor = AlphaBlend(oColor, vec4(ub.Axis1Color.rgb, ub.Axis1Color.a *
        smoothstep(fwUV.x*1.5, fwUV.x*0.5, abs(uv.x))
    ));

    oColor.a *= smoothstep(0.0, 1.0, v.Alpha);

    oColor.a *= select(1.0, 0.5, is_front_facing);

    return oColor;
}