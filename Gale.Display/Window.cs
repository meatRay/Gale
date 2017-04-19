using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using OpenTK.Graphics;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using Box2DX.Dynamics;
using Box2DX.Collision;
using Box2DX.Common;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Gale
{
	public class Render : GameWindow
	{
		public Render()
			: base(800, // initial width
				600, // initial height
				GraphicsMode.Default,
				"dreamstatecoding",  // initial title
				GameWindowFlags.Default,
				DisplayDevice.Default,
				4, // OpenGL major version
				4, // OpenGL minor version
				GraphicsContextFlags.ForwardCompatible)
		{
			AABBBounds = new AABB() { LowerBound = new Vec2(-100.0f, -100.0f), UpperBound = new Vec2(100.0f, 100.0f) };
			PhysicsContext = new World(AABBBounds, new Vec2(0.0f, 0.0f), false);
		}
		public World PhysicsContext;
		public AABB AABBBounds;
		public Shader ShaderProgram;
		public List<IRender> Effects;
		protected override void OnLoad( EventArgs e )
		{
			base.OnLoad(e);
			GL.ClearColor(0.1f, 0.1f, 0.3f, 0f);
			//GL.Enable(EnableCap.DepthTest);
			//GL.DepthFunc(DepthFunction.Less);
			//GL.Disable(EnableCap.CullFace);
			//GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
			//GL.PatchParameter(PatchParameterInt.PatchVertices, 3);
			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.Blend);

			//GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.BlendEquation(BlendEquationMode.FuncAdd);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
		}

		float _time = 0.0f;
		public const float ViewTiles = 8.0f;
		public float AspectRatio => (float)Width / Height;
		public Vector2 Camera = new Vector2(-0.0f, -0.0f);

		protected override void OnResize( EventArgs e )
		{
			GL.Viewport(0, 0, Width, Height);
		}
		
		//public SineWaveProvider32 rdr;
		public WasapiLoopbackCapture cap;
		private float _music;
		private void Prov_Sample( object sender, WaveInEventArgs e )
		{
			Int32 sample_count = e.BytesRecorded / (cap.WaveFormat.BitsPerSample / 8);
			Single[] data = new Single[sample_count];

			for (int i = 0; i < sample_count; ++i)
			{
				data[i] = BitConverter.ToSingle(e.Buffer, i * 4);
			}

			int j = 0;
			var Audio_Samples = new double[sample_count / 2];
			for (int sample = 0; sample < data.Length; sample += 2)
			{
				Audio_Samples[j] = (Double)data[sample];
				Audio_Samples[j] += (Double)data[sample + 1];
				++j;
			}
			_music = (float)Audio_Samples[0];
		}

		protected override void Dispose( bool manual )
		{
			if (!IsDisposed)
			{
				ShaderProgram.Dispose();
				/*Backdrop?.Dispose();
				foreach (var sprite in Props)
					sprite.Image.Dispose();*/
			}
			base.Dispose(manual);
		}
	}
}
