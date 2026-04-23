shader_type spatial;
render_mode unshaded;
render_mode cull_disabled;
render_mode blend_mix;

uniform vec4 base_color : source_color;
uniform vec4 glow_color : source_color;

uniform float scan_speed;
uniform float fresnel_strength;

vec2 scan_line(vec2 uv, float time) {
    float scan = fract(uv.y * 2.0 - time * scan_speed);
    return vec2(scan, 1.0 - scan);
}

void vertex() {

}

void fragment() {
    vec2 uv = UV;
    
    float time = TIME;
        
    vec2 scan = scan_line(uv, time);
    float scan_mask = smoothstep(0.3, 0.7, scan.x) * smoothstep(0.3, 0.7, scan.y);
    float scan_glow = sin(uv.y * 30.0 + time * 5.0) * 0.5 + 0.5;
    scan_glow *= scan_mask;
    
    vec3 normal = normalize(NORMAL);
    vec3 view_dir = normalize(VIEW);
    float fresnel = pow(1.0 - dot(normal, view_dir), 2.5);
    fresnel = clamp(fresnel * fresnel_strength, 0.0, 1.0);
    
    vec3 col = base_color.rgb;
    float alpha = base_color.a;
 
    col += vec3(0.2, 0.4, 0.8) * scan_glow * 0.6;
    col += glow_color.rgb * fresnel * 0.8;
    
    float bands = 8.0;
    col = floor(col * bands) / bands;
    
    ALBEDO = col;
    ALPHA = alpha;
}