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
		GlyphService[] Texts;

		public TextRender(Vector2 position, GlyphService[] glyphs)
		{
			Position = position;
			Texts = glyphs;
		}

		public void Render(Renderer render_context)
		{
			var model = Matrix4.CreateTranslation(Position.X, Position.Y, 0.0f);
			foreach (var text in Texts)
			{
				render_context.ShaderProgram.Model.Write(model);
				text.Image.Render(render_context);
				model *= text.Next;
			}
		}
	}

	public class GlyphService : IDisposable
	{
		public char Character { get; private set; }
		public Matrix4 Next { get; private set; }
		public Sprite Image { get; private set; }
		public GlyphService(char character, Matrix4 next, Sprite image)
		{
			Character = character;
			Next = next;
			Image = image;
		}

		public static GlyphService FromGlyph(char character, GlyphSlot glyph, Renderer render_context)
		{
			glyph.RenderGlyph(RenderMode.Normal);
			Bitmap m;
			using (var g = glyph.GetGlyph())
			{
				g.ToBitmap(RenderMode.Normal, new FTVector26Dot6(0, 0), false);
				var bt = g.ToBitmapGlyph();
				m = bt.Bitmap.ToGdipBitmap();
			}
			// Later on write a Sprite.FromBitmap for Glyph Bitmaps..
			var sprite = Sprite.FromBitmap(m, render_context.ShaderProgram, 0);
			sprite.AcquireUse();
			return new GlyphService(character, CalcNext(glyph), sprite);
		}
		public static Matrix4 CalcNext(GlyphSlot glyph)
			=> Matrix4.CreateTranslation(glyph.Advance.X.ToSingle() / Sprite.TileSize,
				glyph.Advance.Y.ToSingle() / Sprite.TileSize,
				0);

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
			var chars = input.Where(i => i > 32 && i < 127).ToArray();
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
			font.SetCharSize(12, 12, 300, 300);
			for (int i = 33; i < 127; ++i)
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
				}
				disposedValue = true;
			}
		}

		public void Dispose()
			=> Dispose(true);
		#endregion
	}
}
