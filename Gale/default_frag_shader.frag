#version 400
in vec2 UV;
uniform sampler2D texture;

out vec4 frag_colour;

void main() {
	//frag_colour = vec4(UV.x, UV.y, 0.0f, 1.0f);
	frag_colour = texture( texture, UV );
}