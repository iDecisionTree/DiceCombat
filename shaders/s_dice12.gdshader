shader_type spatial;
render_mode unshaded;

uniform sampler2D dice_noise : source_color;

uniform sampler2D dice_num1 : filter_linear, repeat_disable;
uniform sampler2D dice_num2 : filter_linear, repeat_disable;
uniform sampler2D dice_num3 : filter_linear, repeat_disable;
uniform sampler2D dice_num4 : filter_linear, repeat_disable;
uniform sampler2D dice_num5 : filter_linear, repeat_disable;
uniform sampler2D dice_num6 : filter_linear, repeat_disable;
uniform sampler2D dice_num7 : filter_linear, repeat_disable;
uniform sampler2D dice_num8 : filter_linear, repeat_disable;
uniform sampler2D dice_num9 : filter_linear, repeat_disable;
uniform sampler2D dice_num10 : filter_linear, repeat_disable;
uniform sampler2D dice_num11 : filter_linear, repeat_disable;
uniform sampler2D dice_num12 : filter_linear, repeat_disable;

uniform float noise_strength;

uniform vec2 num_scale;
uniform vec2 num_offset;
uniform bool num_flip_x;

uniform vec4 albedo_color : source_color;

varying vec3 v_normal;

vec2 scale_and_offset(vec2 uv, vec2 scale, vec2 offset) {
    return (uv - 0.5) * scale + 0.5 + offset;
}

vec3 gamma(vec3 c) {
    return pow(c, vec3(2.2));
}

void vertex() {
    v_normal = NORMAL;
}

void fragment() {
    vec3 n = normalize(v_normal);

    vec3 ref1  = vec3( 0.0000, -1.0000,  0.0000);
    vec3 ref2  = vec3( 0.7236, -0.4472,  0.5257);
    vec3 ref3  = vec3(-0.2764, -0.4472,  0.8507);
    vec3 ref4  = vec3(-0.8944, -0.4472,  0.0000);
    vec3 ref5  = vec3(-0.2764, -0.4472, -0.8507);
    vec3 ref6  = vec3( 0.7236, -0.4472, -0.5257);
    vec3 ref7  = vec3( 0.2764,  0.4472,  0.8507);
    vec3 ref8  = vec3(-0.7236,  0.4472,  0.5257);
    vec3 ref9  = vec3(-0.7236,  0.4472, -0.5257);
    vec3 ref10 = vec3( 0.2764,  0.4472, -0.8507);
    vec3 ref11 = vec3( 0.8944,  0.4472,  0.0000);
    vec3 ref12 = vec3( 0.0000,  1.0000,  0.0000);

    float d1  = dot(n, ref1);
    float d2  = dot(n, ref2);
    float d3  = dot(n, ref3);
    float d4  = dot(n, ref4);
    float d5  = dot(n, ref5);
    float d6  = dot(n, ref6);
    float d7  = dot(n, ref7);
    float d8  = dot(n, ref8);
    float d9  = dot(n, ref9);
    float d10 = dot(n, ref10);
    float d11 = dot(n, ref11);
    float d12 = dot(n, ref12);

    vec4 noise = texture(dice_noise, UV);

    vec2 num_uv = scale_and_offset(UV, num_scale, num_offset);
    if (num_flip_x) {
        num_uv.x = 1.0 - num_uv.x;
    }

    float max_d = max(
        max(max(max(d1, d2), max(d3, d4)), max(max(d5, d6), max(d7, d8))),
        max(max(d9, d10), max(d11, d12))
    );

    vec4 num_color;
    if (max_d == d1) {
        num_color = texture(dice_num1, num_uv);
    } else if (max_d == d2) {
        num_color = texture(dice_num2, num_uv);
    } else if (max_d == d3) {
        num_color = texture(dice_num3, num_uv);
    } else if (max_d == d4) {
        num_color = texture(dice_num4, num_uv);
    } else if (max_d == d5) {
        num_color = texture(dice_num5, num_uv);
    } else if (max_d == d6) {
        num_color = texture(dice_num6, num_uv);
    } else if (max_d == d7) {
        num_color = texture(dice_num7, num_uv);
    } else if (max_d == d8) {
        num_color = texture(dice_num8, num_uv);
    } else if (max_d == d9) {
        num_color = texture(dice_num9, num_uv);
    } else if (max_d == d10) {
        num_color = texture(dice_num10, num_uv);
    } else if (max_d == d11) {
        num_color = texture(dice_num11, num_uv);
    } else {
        num_color = texture(dice_num12, num_uv);
    }

    vec3 base_color = mix(albedo_color.rgb * noise_strength, albedo_color.rgb, noise.r);
    vec3 final_color = mix(base_color, num_color.rgb, num_color.a);
    
    ALBEDO = gamma(final_color);
}