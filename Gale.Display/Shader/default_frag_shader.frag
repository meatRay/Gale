#version 400
in vec2 UV;
uniform sampler2D texture;
uniform float music;

out vec4 frag_colour;

vec4 hold;

void main() {
	//frag_colour = vec4(UV.x, UV.y, 0.0f, 1.0f);
	hold = texture( texture, UV );
	frag_colour = vec4(hold.x, hold.y - music, hold.z - music, hold.w);
}