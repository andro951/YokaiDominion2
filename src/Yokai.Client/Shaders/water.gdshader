shader_type spatial;
render_mode blend_mix, cull_disabled;
varying vec3 world_pos;
void vertex() {
    world_pos=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;
    VERTEX.y += sin(world_pos.x*2.7+TIME*1.1)*0.025 + cos(world_pos.z*2.0-TIME)*0.025;
}
void fragment() {
    float ribbon = pow(0.5+0.5*sin(world_pos.z*11.0+sin(world_pos.x*3.0+TIME)*1.4-TIME*1.7),14.0);
    ALBEDO=mix(vec3(0.025,0.16,0.18),vec3(0.23,0.51,0.48),ribbon*0.45);
    METALLIC=0.35; ROUGHNESS=0.24;
    EMISSION=vec3(0.02,0.06,0.07)*ribbon;
}
