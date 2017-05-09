#version 400
layout(location = 0) in vec2 position;
layout(location = 1) in vec2 vertexUV;

uniform float z;
uniform mat4 projection;
uniform mat4 uv_at;
uniform mat4 view;
uniform mat4 model_matrix;

out vec2 UV;

void main(void)
{
	gl_Position = projection * view * model_matrix * vec4(position, z, 1.0);
	UV = vertexUV;//(uv_at * vec4(vertexUV, 0.0, 1.0)).xy;
}