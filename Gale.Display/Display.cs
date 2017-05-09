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

namespace Gale.Visuals
{
	public class Display : GameWindow
	{
		public Display(int width, int height, string title)
			: base(width, // initial width
				height, // initial height
				GraphicsMode.Default,
				title,  // initial title
				GameWindowFlags.FixedWindow,
				DisplayDevice.Default,
				4, // OpenGL major version
				0, // OpenGL minor version
				GraphicsContextFlags.ForwardCompatible)
		{
			RenderWorker = new Renderer(this);
		}

		public Renderer RenderWorker { get; private set; }
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			GL.ClearColor(0.1f, 0.1f, 0.1f, 0f);
			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.Blend);

			//GL.BlendEquation(BlendEquationMode.FuncSubtract);
			if (!Sprite.UseLinear)
				GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
			else
				GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
			//GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			VSync = VSyncMode.Off;
		}

		public static float ViewTiles = 16.0f;
		public float AspectRatio => (float)Width / Height;
		public Vector2 Camera = new Vector2(-0.0f, -0.0f);

		protected override void OnResize(EventArgs e)
		{
			GL.Viewport(0, 0, Width, Height);
		}

		protected override void Dispose(bool manual)
		{
			if (!IsDisposed)
				RenderWorker.Dispose();
			base.Dispose(manual);
		}
	}
}
