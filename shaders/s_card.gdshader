shader_type spatial;
render_mode unshaded;
render_mode cull_disabled;

uniform sampler2D card_avatar : filter_linear, repeat_disable;
uniform sampler2D card_background : filter_linear, repeat_disable;
uniform sampler2D card_border : filter_linear, repeat_disable;

uniform vec2 avatar_scale;
uniform vec2 avatar_offset;
uniform vec2 background_scale;
uniform vec2 background_offset;

uniform float avatar_parallex;
uniform float background_parallex;

uniform float background_strength;

uniform float mask_start;
uniform float mask_end;

uniform float aniso;
uniform vec2 pixel_size;
uniform float pixel_density;
uniform float hue_speed;
uniform float saturation_boost;
uniform float value_boost;

uniform float highlight_strength;
uniform float highlight_size;
uniform vec3 highlight_color;

varying mat3 v_inv_model_view;
varying vec2 v_avatar_uv;
varying vec2 v_background_uv;
varying vec2 v_uv;

vec2 scale_and_offset(vec2 uv, vec2 scale, vec2 offset) {
    return (uv - 0.5) * scale + 0.5 + offset;
}

vec2 apply_parallex(vec2 uv, vec3 view, float scale) {
    return uv - view.xz * (scale / max(view.y, 0.001));
}

vec4 sample_clip(sampler2D tex, vec2 uv) {
    vec2 inside = step(vec2(0.0), uv) * step(uv, vec2(1.0));
    return texture(tex, uv) * inside.x * inside.y;
}

float hash(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

vec3 rgb_to_hsv(vec3 c) {
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv_to_rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

vec3 gamma(vec3 c) {
    return pow(c, vec3(2.2));
}

void vertex() {
    v_inv_model_view = mat3(inverse(MODEL_MATRIX)) * mat3(INV_VIEW_MATRIX);
    v_avatar_uv = scale_and_offset(UV, avatar_scale, avatar_offset);
    v_background_uv = scale_and_offset(UV, background_scale, background_offset);
    v_uv = UV;
}

void fragment() {
    vec3 view_view = normalize(VIEW);
    vec3 normal_view = normalize(NORMAL);
    vec3 view_obj = v_inv_model_view * view_view;
    vec3 normal_obj = v_inv_model_view * normal_view;

    vec2 avatar_uv = apply_parallex(v_avatar_uv, view_obj, avatar_parallex);
    vec2 background_uv = apply_parallex(v_background_uv, view_obj, background_parallex);

    vec4 avatar = sample_clip(card_avatar, avatar_uv);
    vec4 background = sample_clip(card_background, background_uv);
	vec4 raw_background = sample_clip(card_background, v_uv);
    vec4 border = sample_clip(card_border, v_uv);

    vec3 avatar_over_border = mix(border.rgb, avatar.rgb, avatar.a);
    float mask = smoothstep(mask_start, mask_end, v_uv.y);
    vec3 masked_avatar = mix(avatar_over_border, border.rgb, mask);
    float background_mask = 1.0 - max(avatar.a, border.a);
	vec3 border_over_background = mix(raw_background.rgb, border.rgb, border.a);

    vec2 pixeled_uv = floor(v_uv / pixel_size) * pixel_size;
    float rand = hash(pixeled_uv);
    float random_mask = step(1.0 - pixel_density, rand);

    vec3 aniso_dir = normalize(cross(vec3(1.0, 1.0, -1.0), normal_obj));
    float aniso_dot = dot(aniso_dir, view_obj);
    float sin_theta = sqrt(1.0 - aniso_dot * aniso_dot);
    float highlight_raw = pow(sin_theta, highlight_size) * aniso;
    float highlight_mask = smoothstep(0.1, 0.5, highlight_raw) * highlight_raw;

    float hue_shift = (view_obj.x * 2.0 + view_obj.z * 1.5) * hue_speed;
    hue_shift += pixeled_uv.x * 5.0 + pixeled_uv.y * 3.0;
    hue_shift += dot(normal_obj, view_obj) * 2.0;

	background.rgb *= background_strength;
    vec3 hsv = rgb_to_hsv(background.rgb);
    hsv.x = fract(hsv.x + hue_shift);
    hsv.y = clamp(hsv.y * saturation_boost, 0.0, 1.0);
    hsv.z = clamp(hsv.z * value_boost, 0.0, 1.0);
    vec3 rainbow_color = hsv_to_rgb(hsv);

    float iridescence_mask = random_mask * highlight_mask;
    vec3 background_color = mix(background.rgb, rainbow_color, iridescence_mask);

    vec3 highlight_add = highlight_mask * highlight_strength * highlight_color;
    background_color += highlight_add * border.a;

    vec3 final_color = masked_avatar + background_color * background_mask;
    final_color += highlight_mask * highlight_strength * border.a;

	if (FRONT_FACING) {
    	ALBEDO = gamma(final_color);
	} else {
		ALBEDO = gamma(border_over_background);
	}
}