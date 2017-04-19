using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;

namespace Gale
{
    class Game
    {
		public Prop Player;
		public Render Display;
		public Sprite Backdrop;
		public List<Prop> Props;
		public Vector2 Camera;
		protected void OnMouseDown( MouseButtonEventArgs e )
		{
			var pos = new Vector4(
				(e.X - (Display.Width * 0.5f)) / (Display.Width * 0.5f),
				-((e.Y - (Display.Height * 0.5f)) / (Display.Height * 0.5f)),
				0.0f, 1.0f);
			Player.MoveTo((inv_proj * pos).Xy + Camera);
		}
		public void Update(double delta_time)
		{
			foreach (var prop in Props)
				prop.Update(delta_time);
			var pos = Player.GetPosition();
			var r_pos = new Vector2(pos.X, pos.Y);
			Camera += Vector2.Clamp((r_pos - Camera), new Vector2(-1), new Vector2(1)) * (float)delta_time * 2;
			PhysicsContext.Step((float)delta_time, 5, 5);
		}
		Matrix4 inv_proj;
		Stopwatch timr = new Stopwatch();
		public void Render()
		{
			timr.Start();
			var proj = Matrix4.CreateOrthographic(Gale.Render.ViewTiles * Display.AspectRatio, Gale.Render.ViewTiles, -10.0f, 10.0f);
			//var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver3 * 2.0f, AspectRatio, 0.1f, 1000f);

			inv_proj = proj.Inverted();

			var view = Matrix4.CreateTranslation(-Camera.X, -Camera.Y, 0.0f);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.UseProgram(Display.ShaderProgram.ShaderID);
			GL.UniformMatrix4(Display.ShaderProgram.ProjectionLocation, false, ref proj);
			GL.UniformMatrix4(Display.ShaderProgram.ViewLocation, false, ref view);
			/*if (Display.ShaderProgram.MusicLocation != -1)
			{
				GL.Uniform1(Display.ShaderProgram.MusicLocation, System.Math.Abs(_music));
			}*/

			var matrix = Matrix4.Identity;
			GL.UniformMatrix4(Display.ShaderProgram.ModelMatLocation, false, ref matrix);
			GL.Uniform1(Display.ShaderProgram.ZLocation, 0.0f);
			Backdrop?.Render(Display.ShaderProgram);
			// Order gets to be expensive over 10k props probably.  Find a cleaner solution!!
			foreach (var sprite in Props.OrderByDescending(p => p.GetPosition().Y + p.ZPosition))
				sprite.Render(Display.ShaderProgram);
			Display.SwapBuffers();
			//Display.Title = $"FPS: {(1.0 / e.Time).ToString("F")}";
			timr.Stop();
			if (timr.ElapsedMilliseconds < 15)
				Thread.Sleep((1000 / 60) - (int)timr.ElapsedMilliseconds);
			timr.Reset();
		}
		static void Main( string[] args )
		{
			using (var render = new Render())
			{
				using (var vert = Shader.Step.CompileFrom(ShaderResources.default_vert_shader, ShaderType.VertexShader))
				using (var frag = Shader.Step.CompileFrom(ShaderResources.default_frag_shader, ShaderType.FragmentShader))
					render.ShaderProgram = Shader.CompileFrom(vert, frag);
				var sprite = Sprite.FromFile("Data/Character/Gale_Dribble.png", render.ShaderProgram);
				var sprite2 = Sprite.FromFile("Data/Prop/office_table.png", render.ShaderProgram);
				// = Sprite.FromFile("Data/Room/Office/backdrop.png", render.ShaderProgram);
				/*render.Props = new List<Prop>() {
					new Prop(render.PhysicsContext, sprite,100.0f / Sprite.TileSize, 1.0f) { Context = render },
					new Prop(render.PhysicsContext, sprite2,0.0f / Sprite.TileSize, 10.0f) { Context = render } };*/
				render.VSync = VSyncMode.Off;
				//render.Player = render.Props[0];
				//var contxt = new OpenTK.Audio.AudioContext();
				//render.rdr = new SineWaveProvider32() { Frequency = 1000, Amplitude = 0.25f };
				//render.rdr.SetWaveFormat(16000, 1);
				//using (var rdr = new WaveFileReader("Data/Severe_Tire_Damage.wav"))
				//using (var ply = new WaveOut())
				//{
				//	render.cap = new WasapiLoopbackCapture();
				//	render.cap.DataAvailable += render.Prov_Sample;
				//	ply.Init(rdr);
				//	render.cap.StartRecording();
				//	ply.Play();
				//	render.cap.StopRecording();
				var game = new Game();
				render.UpdateFrame += ( s, e ) => game.Update(e.Time);
				render.MouseDown += ( s, e ) => game.OnMouseDown(e);
				render.Run(60);
				//}
			}
		}
	}
}
