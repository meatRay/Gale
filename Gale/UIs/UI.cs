using Gale.Visuals;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace Gale
{
	public class UI : IRender
	{
		public Sprite Image { get; private set; }
		public IRender[] Children;
		public bool Visible = true;
		public Matrix4 ModelView => _modelview;
		private Matrix4 _modelview;

		public UI(Sprite image, float left, float bottom, float right, float top)
			: this(image)
			=> FitTo(left, bottom, right, top);
		public UI(Sprite image)
		{
			Image = image;
			Children = new IRender[0];
		}

		public void FitTo(float x, float y, float scale = 1.0f, bool normalize = false)
		{
			if (normalize)
				_modelview = Matrix4.CreateScale(1 / Image.UnitSize.X, 1 / Image.UnitSize.Y, 0);
			else
				_modelview = Matrix4.Identity;
			_modelview *= Matrix4.CreateScale(scale);
			_modelview *= Matrix4.CreateTranslation(x, y, 0.0f);
		}
		public void FitTo(float left, float bottom, float right, float top)
		{
			_modelview = Matrix4.CreateScale(1 / Image.UnitSize.X, 1 / Image.UnitSize.Y, 0);
			_modelview *= Matrix4.CreateScale(right - left, top - bottom, 1.0f);
			_modelview *= Matrix4.CreateTranslation(left, bottom, 0.0f);
		}

		public void Render(Renderer render_context)
		{
			if (Visible)
			{
				var model = render_context.ShaderProgram.Model;
				model.Push(_modelview * model.Top);
				model.Write();
				Image.Render(render_context);
				foreach (var child in Children)
					child.Render(render_context);
				model.Pop();
			}
		}
	}
}
