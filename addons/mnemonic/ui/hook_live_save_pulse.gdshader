shader_type canvas_item;

uniform float pulse_min : hint_range(0.0, 1.0, 0.01) = 0.12;
uniform float pulse_max : hint_range(0.0, 1.0, 0.01) = 0.42;
uniform float pulse_speed : hint_range(0.1, 12.0, 0.1) = 2.8;

void fragment() {
	vec2 centered = UV - vec2(0.5);
	if (dot(centered, centered) > 0.25) {
		discard;
	}
	vec4 c = COLOR;
	float wave = 0.5 + 0.5 * sin(TIME * pulse_speed);
	c.a *= mix(pulse_min, pulse_max, wave);
	COLOR = c;
}
