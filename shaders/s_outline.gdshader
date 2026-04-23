shader_type spatial;
render_mode unshaded;
render_mode cull_front;

uniform vec4 color : source_color;

void vertex() {
}

void fragment() {
	ALBEDO = color.rgb;
}
