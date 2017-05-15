using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale.Visuals
{
	public partial class Sprite
	{
		public static bool UseLinear = true;
		public static int LoadHandle(string filename, out int width, out int height, bool use_linear = false, bool embedded_resource = true)
		{
			int handle;
			using (var bmp = embedded_resource
				? (Bitmap)Image.FromFile(filename)
				//? (Bitmap)Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"Gale.{filename}"))
				: (Bitmap)Image.FromFile(filename))
			{
				handle = LoadHandle(bmp, out width, out height, use_linear);
			}
			return handle;
		}
		public static int LoadHandle(Bitmap bmp, out int width, out int height, bool use_linear = false)
		{
			int tex = GL.GenTexture();
			width = bmp.Width;
			height = bmp.Height;

			BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite,
				UseLinear ? System.Drawing.Imaging.PixelFormat.Format32bppPArgb : System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.BindTexture(TextureTarget.Texture2D, tex);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
			bmp.UnlockBits(data);
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);

			if (UseLinear || use_linear)
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			}
			else
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapLinear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			}
			return tex;
		}

		public static Sprite FromFile(string filename, Shader shader_resource,
			int depth, int offset_x, int offset_y, bool use_linear = false)
		{
			int w, h;
			int _texture = LoadHandle(filename, out w, out h, use_linear, false);
			return FromHandle(_texture, shader_resource, depth, offset_x, offset_y, w, h);
		}

		public static Sprite FromBitmap(Bitmap image, Shader shader_resource,
			int depth, int offset_x, int offset_y, bool use_linear = false)
		{
			int w, h;
			int _texture = LoadHandle(image, out w, out h, use_linear);
			return FromHandle(_texture, shader_resource, depth, offset_x, offset_y, w, h);
		}

		private static Sprite FromHandle(int handle, Shader shader_resource,
			int depth, int offset_x, int offset_y, int w, int h)
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

			return new Sprite(w, h, offset_x, offset_y, _vertbuffer, _objbuffer, handle, depth);
		}

		public static AnimationSequence MakeSteps(string name, IEnumerable<(int X, int Y)> locations,
			(int W, int H) size, (int X, int Y) offset, (int W, int H) sprite_size,
			AnimationDirections direction = AnimationDirections.All)
		{
			var steps = locations
				.Select(l => new AnimationStep(new Vector2((float)l.X / sprite_size.W, (float)l.Y / sprite_size.H)))
				.ToArray();
			var unitsize = new Vector2(size.W / TileSize, size.H / TileSize);
			var n_size = new Vector2((float)size.W / sprite_size.W, (float)size.H / sprite_size.H);
			var buf = new float[]
			{
				0         , 0         ,
				0.0f      , n_size.Y,
				0         , unitsize.Y,
				0.0f      , 0.0f      ,
				unitsize.X, unitsize.Y,
				n_size.X, 0.0f      ,
				0         , 0         ,
				0.0f      , n_size.Y,
				unitsize.X, unitsize.Y,
				n_size.X, 0.0f      ,
				unitsize.X, 0         ,
				n_size.X, n_size.Y,
			};

			int _vertbuffer = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertbuffer);
			GL.BufferData(BufferTarget.ArrayBuffer, buf.Length * sizeof(float), buf, BufferUsageHint.StaticDraw);
			return new AnimationSequence(name, steps, unitsize, new Vector2(offset.X / TileSize, offset.Y / TileSize), _vertbuffer, direction);
		}
	}
}
