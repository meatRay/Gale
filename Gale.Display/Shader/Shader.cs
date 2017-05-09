using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using System.Threading.Tasks;
using OpenTK;

namespace Gale.Visuals
{
	public abstract class ShaderValue<T>
	{
		public ShaderValue(int at)
			=> At = at;
		public int At { get; private set; }
		public T Default;

		public virtual void Write()
			=> Write(ref Default);
		public virtual void Write(T value)
			=> Write(ref value);
		public abstract void Write(ref T value);
	}
	public class ShaderFloat : ShaderValue<float>
	{
		public ShaderFloat(int at) : base(at)
		{ }

		public override void Write(ref float value)
			=> GL.Uniform1(At, value);
	}
	public class ShaderMatrix : ShaderValue<Matrix4>
	{
		public Matrix4 Top
			=> _stack.Last.Value;
		private LinkedList<Matrix4> _stack;

		public ShaderMatrix(int at) : base(at)
			=> _stack = new LinkedList<Matrix4>();

		public override void Write(ref Matrix4 value)
			=> GL.UniformMatrix4(At, false, ref value);
		public override void Write()
			=> Write(_stack.Last.Value);
		public void Push(ref Matrix4 value)
			=> _stack.AddLast(value);
		public void Push(Matrix4 value)
			=> _stack.AddLast(value);
		public void Pop()
			=> _stack.RemoveLast();
		public void Clear()
			=> _stack.Clear();
	}
	public partial class Shader : IDisposable
	{
		public Sprite Highlight;
		public int ShaderID { get; private set; }
		public ShaderMatrix Model { get; private set; }
		public int TextureLocation { get; private set; }
		public int VertexLocation { get; private set; }
		public int UVLocation { get; private set; }
		public ShaderMatrix UVOffset { get; private set; }
		public ShaderMatrix Projection { get; private set; }
		public int ZLocation { get; private set; }
		public ShaderMatrix View { get; private set; }
		public ShaderFloat Music { get; private set; }
		public Shader(int shader_id,
			ShaderMatrix model_mat,
			int texture_location,
			int vertex_location,
			int uv_location,
			ShaderMatrix proj,
			int z_location,
			ShaderMatrix view,
			ShaderFloat music,
			ShaderMatrix uv_offset)
		{
			ShaderID = shader_id;
			Model = model_mat;
			TextureLocation = texture_location;
			VertexLocation = vertex_location;
			UVLocation = uv_location;
			Projection = proj;
			ZLocation = z_location;
			View = view;
			Music = music;
			UVOffset = uv_offset;
		}

		public static async Task<Shader> CompileFrom(string vert_shader_filename, string frag_shader_filename)
		{
			using (var vert = await Step.CompileFromFile(vert_shader_filename, ShaderType.VertexShader))
			using (var frag = await Step.CompileFromFile(frag_shader_filename, ShaderType.FragmentShader))
				return CompileFrom(vert, frag);
		}
		public static Shader CompileFrom(params Step[] shader_steps)
			=> CompileFromShaders(shader_steps);
		public static Shader CompileFrom(IEnumerable<Step> shader_steps)
			=> CompileFromShaders(shader_steps);
		private static Shader CompileFromShaders(IEnumerable<Step> shader_steps)
		{
			int id = GL.CreateProgram();

			foreach (var shader in shader_steps)
				GL.AttachShader(id, shader.ResourceID);

			GL.LinkProgram(id);

			foreach (var shader in shader_steps)
				GL.DetachShader(id, shader.ResourceID);

			string log = GL.GetProgramInfoLog(id);
			if (!string.IsNullOrWhiteSpace(log))
				throw new Exception(log);

			var model_id = new ShaderMatrix(GL.GetUniformLocation(id, "model_matrix"));
			int pos_id = GL.GetAttribLocation(id, "position");
			int uv_id = GL.GetAttribLocation(id, "vertexUV");
			int tex_id = GL.GetUniformLocation(id, "texture");
			var proj_id = new ShaderMatrix(GL.GetUniformLocation(id, "projection"));
			int z_id = GL.GetUniformLocation(id, "z");
			var view_id = new ShaderMatrix(GL.GetUniformLocation(id, "view"));
			var music = new ShaderFloat(GL.GetUniformLocation(id, "music"));
			var uvo = new ShaderMatrix(GL.GetUniformLocation(id, "uv_at"));
			return new Shader(id, model_id, tex_id, pos_id, uv_id, proj_id, z_id, view_id, music, uvo);
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (ShaderID != -1)
					GL.DeleteProgram(ShaderID);
				disposedValue = true;
			}
		}

		~Shader()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
