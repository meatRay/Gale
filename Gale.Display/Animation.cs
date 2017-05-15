using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale.Visuals
{
	[Flags]
	public enum AnimationDirections
	{
		None = 0b0000, North = 0b0001, East = 0b0010, South = 0b0100, West = 0b1000,
		All = North | East | South | West
	}
	public struct AnimationStep
	{
		public Vector2 Location { get; private set; }

		public AnimationStep(Vector2 location)
		{
			Location = location;
		}
	}
	public struct AnimationSequence
	{
		public string Name { get; private set; }
		public AnimationStep[] Steps { get; private set; }
		public AnimationDirections Direction { get; private set; }
		public Vector2 Size { get; private set; }
		public Vector2 Offset { get; private set; }
		public AnimationStep CurrentStep => Steps[_stepat];
		int _stepat;
		int _vertbuffer;

		public AnimationSequence(string name, AnimationStep[] steps, Vector2 size, Vector2 offset,
			int vertex_buffer, AnimationDirections direction = AnimationDirections.All)
		{
			Name = name;
			Steps = steps;
			Direction = direction;
			_stepat = 0;
			Size = size;
			Offset = offset;
			_vertbuffer = vertex_buffer;
		}

		public void Advance()
		{
			++_stepat;
			if (_stepat >= Steps.Length)
				Reset();
		}
		public void Reset()
			=> _stepat = 0;

		public void Bind(Shader shader_resource)
		{
			if (_vertbuffer != -1)
			{
				GL.EnableVertexAttribArray(shader_resource.VertexLocation);
				GL.BindBuffer(BufferTarget.ArrayBuffer, _vertbuffer);
				GL.VertexAttribPointer(shader_resource.VertexLocation, 2, VertexAttribPointerType.Float, false, sizeof(float) * 4, 0);

				GL.EnableVertexAttribArray(shader_resource.UVLocation);
				GL.VertexAttribPointer(shader_resource.UVLocation, 2, VertexAttribPointerType.Float, false, sizeof(float) * 4, sizeof(float) * 2);
			}
		}
	}
	public class Animation : Sprite
	{
		public AnimationSequence[] Sequences { get; private set; }
		public AnimationSequence CurrentSequence
			=> Sequences[_atsequence];
		int _atsequence = 0;

		public Animation(AnimationSequence[] sequences, int texture)
			: base((int)TileSize, (int)TileSize, 0, 0, -1, -1, texture, 0)
		{
			Sequences = sequences;
		}

		public void UseSequence(string sequence_name, AnimationDirections direction)
		{
			for (int i = 0; i < Sequences.Length; ++i)
				if (Sequences[i].Direction == direction && Sequences[i].Name == sequence_name)
				{
					_atsequence = i;
					break;
				}
		}

		public override void Render(Renderer render_context)
		{
			GL.BindVertexArray(0);
			if (render_context.ShaderProgram.Highlight == this)
				render_context.ShaderProgram.Music.Write(0.0f);
			else
				render_context.ShaderProgram.Music.Write();
			Transform = Matrix4.CreateTranslation(CurrentSequence.Offset.X, CurrentSequence.Offset.Y, 0);
			render_context.ShaderProgram.Model.Default = render_context.ShaderProgram.Model.Default * Transform;
			render_context.ShaderProgram.Model.Write();
			var to = CurrentSequence.CurrentStep.Location;
			render_context.ShaderProgram.UVOffset.Write(Matrix4.CreateTranslation(to.X, to.Y, 0));
			CurrentSequence.Bind(render_context.ShaderProgram);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, Texture);
			GL.Uniform1(render_context.ShaderProgram.TextureLocation, 0);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
			render_context.ShaderProgram.UVOffset.Write();
		}
	}
}
