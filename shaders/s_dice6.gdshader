shader_type spatial;
render_mode unshaded;

uniform sampler2D dice_noise : source_color;
uniform sampler2D dice_num1 : filter_linear, repeat_disable;
uniform sampler2D dice_num2 : filter_linear, repeat_disable;
uniform sampler2D dice_num3 : filter_linear, repeat_disable;
uniform sampler2D dice_num4 : filter_linear, repeat_disable;
uniform sampler2D dice_num5 : filter_linear, repeat_disable;
uniform sampler2D dice_num6 : filter_linear, repeat_disable;

uniform float noise_strength;
uniform vec4 albedo_color : source_color;
uniform vec2 num_scale;
uniform vec2 num_offset;
uniform bool num_flip_x;
uniform bool num_flip_y;

varying vec3 v_normal;
varying vec3 v_local_pos;

vec2 scale_and_offset(vec2 uv, vec2 scale, vec2 offset) {
    uv = (uv - 0.5) * scale + 0.5 + offset;
    if (num_flip_x) {
        uv.x = 1.0 - uv.x;
    }
    if (num_flip_y) {
        uv.y = 1.0 - uv.y;
    }
    return uv;
}

vec2 uv_top(vec3 p) {
    return vec2(p.x, -p.z) + 0.5;
}

vec2 uv_bottom(vec3 p) {
	return vec2(p.x, -p.z) + 0.5;
}

vec2 uv_front(vec3 p) {
    return vec2(p.x, -p.y) + 0.5;
}

vec2 uv_back(vec3 p) {
    return vec2(-p.x, -p.y) + 0.5;
}

vec2 uv_right(vec3 p) {
    return vec2(-p.z, -p.y) + 0.5;
}

vec2 uv_left(vec3 p) {
    return vec2(p.z, -p.y) + 0.5;
}

vec3 gamma(vec3 c) {
    return pow(c, vec3(2.2));
}

void vertex() {
    v_normal = NORMAL;
    v_local_pos = VERTEX;
}

void fragment() {
    vec3 n = normalize(v_normal);
    vec3 p = v_local_pos;

    float d_top = dot(n, vec3(0.0, 1.0, 0.0));
    float d_front = dot(n, vec3(0.0, 0.0, 1.0));
    float d_right = dot(n, vec3(1.0, 0.0, 0.0));
    float d_back = dot(n, vec3(0.0, 0.0, -1.0));
    float d_left = dot(n, vec3(-1.0, 0.0, 0.0));
    float d_bottom = dot(n, vec3(0.0, -1.0, 0.0));

    vec4 noise = texture(dice_noise, UV);

    vec2 face_uv;
    vec4 num_color;

    if (d_top > d_front && d_top > d_right && d_top > d_back && d_top > d_left && d_top > d_bottom) {
        face_uv = scale_and_offset(uv_top(p), num_scale, num_offset);
        num_color = texture(dice_num1, face_uv);
    } else if (d_front > d_right && d_front > d_back && d_front > d_left && d_front > d_bottom) {
        face_uv = scale_and_offset(uv_front(p), num_scale, num_offset);
        num_color = texture(dice_num2, face_uv);
    } else if (d_right > d_back && d_right > d_left && d_right > d_bottom) {
        face_uv = scale_and_offset(uv_right(p), num_scale, num_offset);
        num_color = texture(dice_num3, face_uv);
    } else if (d_back > d_left && d_back > d_bottom) {
        face_uv = scale_and_offset(uv_back(p), num_scale, num_offset);
        num_color = texture(dice_num4, face_uv);
    } else if (d_left > d_bottom) {
        face_uv = scale_and_offset(uv_left(p), num_scale, num_offset);
        num_color = texture(dice_num5, face_uv);
    } else {
        face_uv = scale_and_offset(uv_bottom(p), num_scale, num_offset);
        num_color = texture(dice_num6, face_uv);
    }

	vec3 base_color = mix(albedo_color.rgb * noise_strength, albedo_color.rgb, noise.r);
    vec3 final_color = mix(base_color, num_color.rgb, num_color.a);
    
	ALBEDO = gamma(final_color);
}