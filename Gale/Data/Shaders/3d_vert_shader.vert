#version 400
layout(location = 0) in vec3 position;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model_matrix;

out vec2 UV;

void main(void)
{
    gl_Position = projection * view * model_matrix * vec4(position, 1.0);
	UV = vec2(0.0, 0.0);
}