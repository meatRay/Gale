using Gale.Visuals;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace Gale
{
	public class UI : IRender
	{
		public Sprite Image { get; private set; }
		public float Left { get; private set; } = 0.0f;
		public float Right { get; private set; } = 0.0f;
		public float Top { get; private set; } = 0.0f;
		public float Bottom { get; private set; } = 0.0f;
		public Matrix4 ModelView => _modelview;
		private Matrix4 _modelview;

		public UI(Sprite image, float left, float bottom, float right, float top)
			: this(image)
			=> FitTo(left, bottom, right, top);
		public UI(Sprite image)
			=> Image = image;

		public void FitTo(float left, float bottom, float right, float top)
		{
			_modelview = Matrix4.Identity;
			_modelview *= Matrix4.CreateTranslation(left, bottom, 0.0f);
			_modelview *= Matrix4.CreateScale(right - left, top - bottom, 1.0f);
		}

		public void Render(Renderer render_context)
		{
			render_context.ShaderProgram.Model.Write(_modelview);
			Image.Render(render_context);
		}
	}
}
