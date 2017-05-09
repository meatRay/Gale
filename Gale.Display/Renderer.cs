using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale.Visuals
{
	public enum RendererStyle { None, Gamescene, UI }
	public class Renderer : IDisposable
	{
		public Display WindowContext { get; private set; }
		public RendererStyle Style { get; private set; }
		public Matrix4 GameProjection { get; private set; }
		public Matrix4 UIProjection { get; private set; }
		public Matrix4 GameView { get; private set; }
		public Shader ShaderProgram;

		public Renderer(Display window_context)
		{
			WindowContext = window_context;
			RebuildGameProjection();
			RebuildUIProjection();
		}

		public void RebuildGameView(Vector2 translation)
			=> GameView = Matrix4.CreateTranslation(-translation.X, -translation.Y, 0.0f);
		public void RebuildGameProjection()
			=> GameProjection = Matrix4.CreateOrthographic(Visuals.Display.ViewTiles, Visuals.Display.ViewTiles / WindowContext.AspectRatio, -10.0f, 10.0f);
		public void RebuildUIProjection()
			=> UIProjection = Matrix4.CreateOrthographicOffCenter(0, 10, 0, 10 / WindowContext.AspectRatio, -1, 1);

		public void BuildGameScene()
		{
			Style = RendererStyle.Gamescene;
			GL.UseProgram(ShaderProgram.ShaderID);
			ShaderProgram.Projection.Write(GameProjection);
			ShaderProgram.View.Write(GameView);
			ShaderProgram.UVOffset.Clear();
			ShaderProgram.UVOffset.Default = Matrix4.Identity;
			ShaderProgram.UVOffset.Push(ref ShaderProgram.UVOffset.Default);
			ShaderProgram.UVOffset.Write();
			ShaderProgram.Model.Clear();
			ShaderProgram.Model.Default = Matrix4.Identity;
			ShaderProgram.Model.Push(ref ShaderProgram.Model.Default);
			GL.Uniform1(ShaderProgram.ZLocation, 0.0f);
		}

		public void BuildUIScene()
		{
			Style = RendererStyle.UI;
			GL.UseProgram(ShaderProgram.ShaderID);
			ShaderProgram.Projection.Write(UIProjection);
			ShaderProgram.View.Write(Matrix4.Identity);
			ShaderProgram.UVOffset.Default = Matrix4.Identity;
			ShaderProgram.UVOffset.Write();
			ShaderProgram.Model.Default = Matrix4.Identity;
			ShaderProgram.Model.Push(ref ShaderProgram.Model.Default);
			GL.Uniform1(ShaderProgram.ZLocation, 0.0f);
		}

		public void Display()
			=> WindowContext.SwapBuffers();

		public void Begin()
			=> GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

		public void Dispose() => ShaderProgram.Dispose();
	}
}
