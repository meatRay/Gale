using OpenTK;
using SharpFont;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Gale.Visuals
{
	public class TextRender : IRender
	{
		public Vector2 Position;
		public bool Visible = true;
		public float FontSize => _fontsize;
		GlyphService[] Texts;
		float _fontsize;
		Matrix4 _fontscale;

		public TextRender(Vector2 position, GlyphService[] glyphs, float font_size = 1.0f)
		{
			Position = position;
			Texts = glyphs;
			CalcFontSize(font_size);
		}

		public void CalcFontSize(float font_size)
		{
			_fontsize = font_size;
			_fontscale = Matrix4.CreateScale(font_size) * Matrix4.CreateTranslation(Position.X, Position.Y, 0.0f);
		}

		public void Render(Renderer render_context)
		{
			if (Visible)
			{
				//var offset = ;
				//render_context.ShaderProgram.View.Write(ref offset);
				//var model = _fontscale;
				render_context.ShaderProgram.Model.Push(ref _fontscale);
				RenderChar(0, render_context);
				render_context.ShaderProgram.Model.Pop();
				render_context.ShaderProgram.View.Write(Matrix4.Identity);
			}
		}

		public void RenderChar( int at, Renderer render )
		{
			if (at >= Texts.Length)
				return;
			var text = Texts[at];
			//Matrix4 model;
			if (text.Character == '\n')
				render.ShaderProgram.Model.Push(_fontscale * Matrix4.CreateTranslation(0, -0.35f, 0));
			else
			{
				var to_use = text.Offset * render.ShaderProgram.Model.Top;
				render.ShaderProgram.Model.Push(ref to_use);
				text.Image?.Render(render);
				render.ShaderProgram.Model.Pop();
				render.ShaderProgram.Model.Push(text.Next * render.ShaderProgram.Model.Top);
			}
			RenderChar(++at, render);
			render.ShaderProgram.Model.Pop();
		}
	}

	public class GlyphService : IDisposable
	{
		public char Character { get; private set; }
		public Matrix4 Next { get; private set; }
		public Matrix4 Offset { get; private set; }
		public Sprite Image { get; private set; }
		public GlyphService(char character, Matrix4 next, Matrix4 offset, Sprite image)
		{
			Character = character;
			Next = next;
			Image = image;
			Offset = offset;
		}

		public static GlyphService FromGlyph(char character, GlyphSlot glyph, Renderer render_context)
		{
			glyph.RenderGlyph(RenderMode.Normal);
			Bitmap m = null;
			using (var g = glyph.GetGlyph())
			{
				g.ToBitmap(RenderMode.Normal, new FTVector26Dot6(0, 0), false);
				var bt = g.ToBitmapGlyph();
				if (bt.Bitmap.Width > 0)
					m = bt.Bitmap.ToGdipBitmap();
			}
			// Later on write a Sprite.FromBitmap for Glyph Bitmaps..
			var sprite = m == null ? null : Sprite.FromBitmap(m, render_context.ShaderProgram, 0, 0, 0, true);
			sprite?.AcquireUse();
			return new GlyphService(character, CalcNext(glyph), CalcOffset(glyph), sprite);
		}
		public static Matrix4 CalcNext(GlyphSlot glyph)
			=> Matrix4.CreateTranslation((glyph.Advance.X.ToSingle() / Sprite.TileSize) * 0.68f,
				glyph.Advance.Y.ToSingle() / Sprite.TileSize,
				0);
		public static Matrix4 CalcOffset(GlyphSlot glyph)
			=> Matrix4.CreateTranslation(glyph.Metrics.HorizontalBearingX.ToSingle() / Sprite.TileSize,
				(glyph.Metrics.HorizontalBearingY - glyph.Metrics.Height).ToSingle() / Sprite.TileSize,
				0) * Matrix4.CreateScale(0.68f);

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Image.ReleaseUse();
					Image.Dispose();
				}
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
		#endregion
	}

	public class TextService : IDisposable
	{
		Library FontLib;
		Face Font;
		public Dictionary<char, GlyphService> LoadedCharacters { get; private set; }

		public TextService(Library font_library, Face font, Dictionary<char, GlyphService> loaded_chars)
		{
			FontLib = font_library;
			Font = font;
			LoadedCharacters = loaded_chars;
		}

		public TextRender CompileString(string input, Vector2 position, Renderer render_context)
		{
			var chars = input.Where(i => i > 0 && i < 127).ToArray();
			var glyphs = new GlyphService[chars.Length];
			for (int i = 0; i < chars.Length; ++i)
				glyphs[i] = LoadedCharacters[chars[i]];
			return new TextRender(position, glyphs);
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]

		private static extern bool SetDllDirectory(string path);
		public static TextService FromFile(string file_name, Renderer render_context)
		{
			string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

			path = Path.Combine(path, IntPtr.Size == 8 ? "x64" : "x86");

			if (!SetDllDirectory(path))
				throw new System.ComponentModel.Win32Exception();

			var lib = new Library();
			var font = new Face(lib, file_name);
			var char_map = new Dictionary<char, GlyphService>();
			font.SetCharSize(24, 24, 300, 300);
			for (int i = 0; i < 127; ++i)
			{
				font.LoadChar((char)i, LoadFlags.Default, LoadTarget.Normal);
				char_map[(char)i] = GlyphService.FromGlyph((char)i, font.Glyph, render_context);
			}
			return new TextService(lib, font, char_map);
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					foreach (var g in LoadedCharacters.Values)
						g.Dispose();
					Font.Dispose();
					FontLib.Dispose();
				}
				disposedValue = true;
			}
		}

		public void Dispose()
			=> Dispose(true);
		#endregion
	}
}
