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
	public interface IRender
	{
		void Render(Renderer render_context);
	}
	public class Sprite : IDisposable, IRender
	{
		int _vertbuffer;
		int _objbuffer;
		int _texture;
		public const float TileSize = 64;
		public const float HTileSize = TileSize * 2;

		public Vector2 UnitSize { get; private set; }
		public float UnitDepth { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }
		public int ReferenceCount { get; private set; }

		public Sprite(int width, int height, int vert_buffer, int obj_buffer, int texture, int unit_depth)
		{
			Width = width;
			Height = height;
			UnitSize = new Vector2(width / TileSize, height / TileSize);
			_vertbuffer = vert_buffer;
			_objbuffer = obj_buffer;
			_texture = texture;
			UnitDepth = unit_depth / TileSize;
			ReferenceCount = 0;
		}

		public void AcquireUse()
			=> ++ReferenceCount;
		public void ReleaseUse()
			=> --ReferenceCount;

		protected static int LoadHandle(string filename, out int width, out int height, bool embedded_resource = true)
		{
			int handle;
			using (var bmp = embedded_resource
				? (Bitmap)Image.FromFile(filename)
				//? (Bitmap)Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"Gale.{filename}"))
				: (Bitmap)Image.FromFile(filename))
			{
				handle = LoadHandle(bmp, out width, out height);
			}
			return handle;
		}
		protected static int LoadHandle(Bitmap bmp, out int width, out int height)
		{
			int tex = GL.GenTexture();
			width = bmp.Width;
			height = bmp.Height;
			BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			GL.BindTexture(TextureTarget.Texture2D, tex);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
			bmp.UnlockBits(data);
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestClipmapLinearSgix);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			return tex;
		}

		public static Sprite FromFile(string filename, Shader shader_resource, int depth)
		{
			int w, h;
			int _texture = LoadHandle(filename, out w, out h, false);
			return FromHandle(_texture, shader_resource, depth, w, h);
		}

		public static Sprite FromBitmap(Bitmap image, Shader shader_resource, int depth)
		{
			int w, h;
			int _texture = LoadHandle(image, out w, out h);
			return FromHandle(_texture, shader_resource, depth, w, h);
		}

		private static Sprite FromHandle(int handle, Shader shader_resource, int depth, int w, int h)
		{
			var buf = new float[]
			{
				0, 0,
				0.0f, 1.0f,
				0, h/TileSize,
				0.0f, 0.0f,
				w/TileSize, h/TileSize,
				1.0f, 0.0f,
				0, 0,
				0.0f, 1.0f,
				w/TileSize, h/TileSize,
				1.0f, 0.0f,
				w/TileSize, 0,
				1.0f, 1.0f,
			};

			int _vertbuffer = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertbuffer);
			GL.BufferData(BufferTarget.ArrayBuffer, buf.Length * sizeof(float), buf, BufferUsageHint.StaticDraw);

			int _objbuffer = GL.GenVertexArray();
			GL.BindVertexArray(_objbuffer);

			GL.EnableVertexAttribArray(shader_resource.VertexLocation);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertbuffer);
			GL.VertexAttribPointer(shader_resource.VertexLocation, 2, VertexAttribPointerType.Float, false, sizeof(float) * 4, 0);

			GL.EnableVertexAttribArray(shader_resource.UVLocation);
			//GL.BindBuffer(BufferTarget.TextureBuffer, _texbuffer);
			GL.VertexAttribPointer(shader_resource.UVLocation, 2, VertexAttribPointerType.Float, false, sizeof(float) * 4, sizeof(float) * 2);

			GL.BindVertexArray(_objbuffer);
			GL.BindTexture(TextureTarget.Texture2D, handle);
			GL.BindVertexArray(0);

			return new Sprite(w, h, _vertbuffer, _objbuffer, handle, depth);
		}

		public void Render(Renderer render_context)
		{
			if (render_context.ShaderProgram.Highlight == this)
				render_context.ShaderProgram.Music.Write(0.0f);
			else
				render_context.ShaderProgram.Music.Write();
			GL.BindVertexArray(_objbuffer);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, _texture);
			GL.Uniform1(render_context.ShaderProgram.TextureLocation, 0);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue && ReferenceCount < 1)
			{
				GL.DeleteVertexArrays(1, ref _objbuffer);
				GL.DeleteBuffer(_vertbuffer);
				GL.DeleteTexture(_texture);
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
