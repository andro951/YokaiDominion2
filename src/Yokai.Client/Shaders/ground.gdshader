shader_type spatial;
render_mode diffuse_burley;
uniform vec4 low_color : source_color = vec4(0.075, 0.15, 0.12, 1.0);
uniform vec4 high_color : source_color = vec4(0.19, 0.29, 0.19, 1.0);
varying vec3 world_pos;
float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1,311.7))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i=floor(p); vec2 f=fract(p); f=f*f*(3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1.,0.)),f.x),mix(hash(i+vec2(0.,1.)),hash(i+vec2(1.,1.)),f.x),f.y);
}
void vertex() { world_pos = (MODEL_MATRIX * vec4(VERTEX,1.0)).xyz; }
void fragment() {
    float n=noise(world_pos.xz*0.45)*0.7+noise(world_pos.xz*4.0)*0.3;
    ALBEDO=mix(low_color.rgb,high_color.rgb,n);
    ROUGHNESS=0.94;
}
