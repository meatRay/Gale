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
		public Matrix4 GameProjection => _gameprojection;
		public Matrix4 UIProjection => _uiprojection;
		public Matrix4 GameView => _gameview;
		public Shader ShaderProgram;

		private Matrix4 _uiprojection;
		private Matrix4 _gameprojection;
		private Matrix4 _gameview;

		public Renderer(Display window_context)
		{
			WindowContext = window_context;
			RebuildGameProjection();
			RebuildUIProjection();
		}

		public void RebuildGameView(Vector2 translation)
			=> _gameview = Matrix4.CreateTranslation(-translation.X, -translation.Y, 0.0f);
		public void RebuildGameProjection()
			=> _gameprojection = Matrix4.CreateOrthographic(Visuals.Display.ViewTiles * WindowContext.AspectRatio, Visuals.Display.ViewTiles, -10.0f, 10.0f);
		public void RebuildUIProjection()
			=> _uiprojection = Matrix4.CreateOrthographicOffCenter(0, 1, 0, 1, -1, 1);

		public void BuildGameScene()
		{
			Style = RendererStyle.Gamescene;
			GL.UseProgram(ShaderProgram.ShaderID);
			GL.UniformMatrix4(ShaderProgram.ProjectionLocation, false, ref _gameprojection);
			GL.UniformMatrix4(ShaderProgram.ViewLocation, false, ref _gameview);
			GL.Uniform1(ShaderProgram.ZLocation, 0.0f);
		}

		public void BuildUIScene()
		{
			Style = RendererStyle.UI;
			GL.UseProgram(ShaderProgram.ShaderID);
			GL.UniformMatrix4(ShaderProgram.ProjectionLocation, false, ref _uiprojection);
			var proj = Matrix4.Identity;
			GL.UniformMatrix4(ShaderProgram.ViewLocation, false, ref proj);
			GL.Uniform1(ShaderProgram.ZLocation, 0.0f);
		}

		public void Display()
			=> WindowContext.SwapBuffers();

		public void Begin()
			=> GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

		public void Dispose() => ShaderProgram.Dispose();
	}
}
