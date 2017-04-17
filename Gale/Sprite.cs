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

namespace Gale
{
	interface IRender
	{
		void Render( Shader shader_program );
	}

	class DotSet : IRender
	{
		int _vertbuffer;
		int _objbuffer;
		public int Count { get; private set; }
		public DotSet( int vert_buffer, int count )
		{
			_vertbuffer=vert_buffer;
			Count=count;
		}

		public static DotSet FromFile( Shader shader_resource, string file_name )
		{
			List<Vector3> vecs = new List<Vector3>();
			using (var reader = File.OpenText(file_name))
			{
				string line;
				while ((line=reader.ReadLine())!=null)
				{
					var splt = line.Split(' ');
					vecs.Add(new Vector3(float.Parse(splt[0])/1000.0f, float.Parse(splt[1])/1000.0f, float.Parse(splt[2])/1000.0f));
				}
			}
			int obj = GL.GenVertexArray();
			GL.BindVertexArray(obj);
			int buf = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, buf);
			GL.BufferData(BufferTarget.ArrayBuffer, 3, vecs.ToArray(), BufferUsageHint.StaticDraw);
			GL.EnableVertexAttribArray(shader_resource.VertexLocation);
			GL.VertexAttribPointer(shader_resource.VertexLocation, 3, VertexAttribPointerType.Float, false, 0, 0);
			return new DotSet(buf, vecs.Count) { _objbuffer=obj };
		}

		public void Render( Shader shader_program )
		{
			//GL.BindBuffer(BufferTarget.ArrayBuffer, _vertbuffer);
			GL.BindVertexArray(_objbuffer);
			GL.PointSize(10.0f);
			GL.DrawArrays(PrimitiveType.Points, 0, Count);
		}
	}

	class Sprite : IDisposable, IRender
	{
		int _vertbuffer;
		int _texbuffer;
		int _objbuffer;
		int _texture;
		public const float TileSize = 64;
		public const float HTileSize = TileSize*2;

		public Vector2 UnitSize { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }

		public Sprite( int width, int height, int vert_buffer, int tex_buffer, int obj_buffer, int texture )
		{
			Width=width;
			Height=height;
			UnitSize=new Vector2(width/TileSize, height/TileSize);
			_vertbuffer=vert_buffer;
			_texbuffer=tex_buffer;
			_objbuffer=obj_buffer;
			_texture=texture;
		}

		static private int LoadHandle( string filename, out int width, out int height, bool embedded_resource = true )
		{
			int tex = GL.GenTexture();
			using (var bmp = embedded_resource
				? (Bitmap)ImageResources.ResourceManager.GetObject(filename)
				//? (Bitmap)Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"Gale.{filename}"))
				: (Bitmap)Image.FromFile(filename))
			//using (var bmp = (Bitmap)ImageResources.ResourceManager.GetObject(filename))
			{
				width=bmp.Width;
				height=bmp.Height;
				BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

				GL.BindTexture(TextureTarget.Texture2D, tex);

				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
				bmp.UnlockBits(data);
				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
				//GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
				//GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestClipmapLinearSgix);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

				//GL.GetFloat((GetPName)ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt, out maxAniso);
				//GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, maxAniso);
			}
			return tex;
		}

		public static Sprite FromFile( string filename, Shader shader_resource )
		{
			int w, h;
			int _texture = LoadHandle(filename, out w, out h, false);
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
			GL.BufferData(BufferTarget.ArrayBuffer, buf.Length*sizeof(float), buf, BufferUsageHint.StaticDraw);

			int _texbuffer = -1;// GenDefaultTexBuf();

			int _objbuffer = GL.GenVertexArray();
			GL.BindVertexArray(_objbuffer);

			GL.EnableVertexAttribArray(shader_resource.VertexLocation);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertbuffer);
			GL.VertexAttribPointer(shader_resource.VertexLocation, 2, VertexAttribPointerType.Float, false, sizeof(float)*4, 0);

			GL.EnableVertexAttribArray(shader_resource.UVLocation);
			//GL.BindBuffer(BufferTarget.TextureBuffer, _texbuffer);
			GL.VertexAttribPointer(shader_resource.UVLocation, 2, VertexAttribPointerType.Float, false, sizeof(float)*4, sizeof(float)*2);

			GL.BindVertexArray(_objbuffer);
			GL.BindTexture(TextureTarget.Texture2D, _texture);
			GL.BindVertexArray(0);

			return new Sprite(w, h, _vertbuffer, -1, _objbuffer, _texture);
		}

		static private int GenDefaultTexBuf()
		{
			if (DefaultTexBuf!=-1)
				return DefaultTexBuf;
			var tex_buf = new float[]
			{
				0.0f, 0.0f,
				0.0f, 1.0f,
				1.0f, 1.0f,
				0.0f, 0.0f,
				1.0f, 1.0f,
				1.0f, 0.0f
			};
			DefaultTexBuf=GL.GenBuffer();
			GL.BindBuffer(BufferTarget.TextureBuffer, DefaultTexBuf);
			//GL.NamedBufferStorage(_vertbuffer, sizeof(float) * buf.Length, buf, BufferStorageFlags.MapWriteBit);
			GL.BufferData(BufferTarget.TextureBuffer, sizeof(float)*tex_buf.Length, tex_buf, BufferUsageHint.StaticDraw);
			return DefaultTexBuf;

		}
		static private int DefaultTexBuf = -1;
		public void Render( Shader shader_program )
		{
			GL.BindVertexArray(_objbuffer);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, _texture);
			GL.Uniform1(shader_program.TextureLocation, 0);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
		{
			if (!disposedValue)
			{
				GL.DeleteVertexArrays(1, ref _objbuffer);
				GL.DeleteBuffer(_vertbuffer);

				disposedValue=true;
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
