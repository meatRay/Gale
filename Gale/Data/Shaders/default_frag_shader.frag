#version 400
in vec2 UV;
uniform sampler2D texture;
uniform float music;

out vec4 frag_colour;

vec4 hold;
float mid;
void main() {
	//frag_colour = vec4(UV.x, UV.y, 0.0f, 1.0f);
	hold = texture( texture, UV );
	mid = (hold.x + hold.y + hold.z)/3.0;
	frag_colour = vec4(hold.x+((mid-hold.x)*music), hold.y+((mid-hold.y)*music), hold.z+((mid-hold.z)*music), hold.w);
}