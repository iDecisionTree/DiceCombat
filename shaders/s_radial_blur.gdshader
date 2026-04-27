shader_type canvas_item;

uniform float blur_strength : hint_range(0.0, 1.0) = 0.0;
uniform vec2 blur_center = vec2(0.5, 0.5);
uniform int samples : hint_range(4, 32) = 8;
uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap, repeat_disable;

void fragment() {
	vec2 dir = UV - blur_center;
	float dist = length(dir);
	vec2 dir_n = normalize(dir);

	vec4 color = vec4(0.0);
	float total_weight = 0.0;

	for (int i = 0; i < samples; i++) {
		float t = float(i) / float(max(samples - 1, 1));
		float offset = t * blur_strength * dist;
		vec2 sample_uv = UV - dir_n * clamp(offset, 0.0, 1.0);
		float weight = 1.0 - t * 0.5;
		color += texture(screen_tex, sample_uv) * weight;
		total_weight += weight;
	}

	COLOR = color / total_weight;
}
