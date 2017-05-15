using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gale.Visuals
{
	public interface IRender
	{
		void Render(Renderer render_context);
	}
}
