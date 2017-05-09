using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using OpenTK;
using System.Reflection;
using System.IO;
using System.Windows;

namespace Gale.Visuals
{
	public partial class Sprite : IDisposable, IRender
	{
		int _vertbuffer;
		int _objbuffer;
		protected int Texture { get; private set; }
		protected Matrix4 Transform;
		public static float TileSize = 64;
		public static float HTileSize = TileSize * 2;

		public Vector2 UnitSize { get; private set; }
		public float UnitDepth { get; private set; }
		public Vector2 Offset { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }
		public int ReferenceCount { get; private set; }

		public Sprite(int width, int height, int offset_x, int offset_y, int vert_buffer, int obj_buffer, int texture, int unit_depth)
		{
			Width = width;
			Height = height;
			UnitSize = new Vector2(width / TileSize, height / TileSize);
			Offset = new Vector2(offset_x / TileSize, offset_y / TileSize);
			Transform = Matrix4.CreateTranslation(Offset.X, Offset.Y, 0);
			_vertbuffer = vert_buffer;
			_objbuffer = obj_buffer;
			Texture = texture;
			UnitDepth = unit_depth / TileSize;
			ReferenceCount = 0;
		}

		public void AcquireUse()
			=> ++ReferenceCount;
		public void ReleaseUse()
			=> --ReferenceCount;

		public virtual void Render(Renderer render_context)
		{
			if (render_context.ShaderProgram.Highlight == this)
				render_context.ShaderProgram.Music.Write(0.0f);
			else
				render_context.ShaderProgram.Music.Write();
			render_context.ShaderProgram.Model.Push( render_context.ShaderProgram.Model.Top * Transform);
			render_context.ShaderProgram.Model.Write();
			render_context.ShaderProgram.UVOffset.Write();
			GL.BindVertexArray(_objbuffer);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, Texture);
			GL.Uniform1(render_context.ShaderProgram.TextureLocation, 0);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
			render_context.ShaderProgram.Model.Pop();
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue && ReferenceCount < 1)
			{
				GL.DeleteVertexArrays(1, ref _objbuffer);
				GL.DeleteBuffer(_vertbuffer);
				GL.DeleteTexture(Texture);
				disposedValue = true;
			}
		}

		~Sprite()
		{
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
