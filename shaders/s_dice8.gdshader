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

    vec3 ref1 = vec3(-0.5774, -0.5774,  0.5774);
    vec3 ref2 = vec3(-0.5774,  0.5774,  0.5774);
    vec3 ref3 = vec3(-0.5774, -0.5774, -0.5774);
    vec3 ref4 = vec3(-0.5774,  0.5774, -0.5774);
    vec3 ref5 = vec3( 0.5774, -0.5774,  0.5774);
    vec3 ref6 = vec3( 0.5774,  0.5774,  0.5774);
    vec3 ref7 = vec3( 0.5774, -0.5774, -0.5774);
    vec3 ref8 = vec3( 0.5774,  0.5774, -0.5774);

    float d1 = dot(n, ref1);
    float d2 = dot(n, ref2);
    float d3 = dot(n, ref3);
    float d4 = dot(n, ref4);
    float d5 = dot(n, ref5);
    float d6 = dot(n, ref6);
    float d7 = dot(n, ref7);
    float d8 = dot(n, ref8);

    vec4 noise = texture(dice_noise, UV);

    vec2 num_uv = scale_and_offset(UV, num_scale, num_offset);
    if (num_flip_x) {
        num_uv.x = 1.0 - num_uv.x;
    }

    vec4 num_color;
    if (d1 > d2 && d1 > d3 && d1 > d4 && d1 > d5 && d1 > d6 && d1 > d7 && d1 > d8) {
        num_color = texture(dice_num1, num_uv);
    } else if (d2 > d1 && d2 > d3 && d2 > d4 && d2 > d5 && d2 > d6 && d2 > d7 && d2 > d8) {
        num_color = texture(dice_num2, num_uv);
    } else if (d3 > d1 && d3 > d2 && d3 > d4 && d3 > d5 && d3 > d6 && d3 > d7 && d3 > d8) {
        num_color = texture(dice_num3, num_uv);
    } else if (d4 > d1 && d4 > d2 && d4 > d3 && d4 > d5 && d4 > d6 && d4 > d7 && d4 > d8) {
        num_color = texture(dice_num4, num_uv);
    } else if (d5 > d1 && d5 > d2 && d5 > d3 && d5 > d4 && d5 > d6 && d5 > d7 && d5 > d8) {
        num_color = texture(dice_num5, num_uv);
    } else if (d6 > d1 && d6 > d2 && d6 > d3 && d6 > d4 && d6 > d5 && d6 > d7 && d6 > d8) {
        num_color = texture(dice_num6, num_uv);
    } else if (d7 > d1 && d7 > d2 && d7 > d3 && d7 > d4 && d7 > d5 && d7 > d6 && d7 > d8) {
        num_color = texture(dice_num7, num_uv);
    } else {
        num_color = texture(dice_num8, num_uv);
    }

	vec3 base_color = mix(albedo_color.rgb * noise_strength, albedo_color.rgb, noise.r);
    vec3 final_color = mix(base_color, num_color.rgb, num_color.a);
	
    ALBEDO = gamma(final_color);
}