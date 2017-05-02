using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Gale.Visuals
{
	public partial class Shader
	{
		public class Step : IDisposable
		{
			public ShaderType CompileType { get; private set; }
			public int ResourceID { get; private set; } = -1;

			public Step( int resource_id, ShaderType compile_type )
			{
				ResourceID=resource_id;
				CompileType=compile_type;
			}

			public static Step CompileFrom( string shader_step, ShaderType shader_type )
			{
				var id = GL.CreateShader(shader_type);
				GL.ShaderSource(id, shader_step);
				GL.CompileShader(id);

				var log = GL.GetShaderInfoLog(id);
				if (!string.IsNullOrWhiteSpace(log))
					throw new Exception(log);
				return new Step(id, shader_type);
			}

			public static async Task<Step> CompileFromFile( string filename, ShaderType shader_type )
				=> await CompileFromFile(File.OpenText(filename), shader_type);
			public static async Task<Step> CompileFromFile( StreamReader file_stream, ShaderType shader_type )
				=> CompileFrom(await file_stream.ReadToEndAsync(), shader_type);

			#region IDisposable Support
			private bool disposedValue = false; // To detect redundant calls

			protected virtual void Dispose( bool disposing )
			{
				if (!disposedValue)
				{
					if (ResourceID!=-1)
						GL.DeleteShader(ResourceID);
					disposedValue=true;
				}
			}
			~Step()
			{
				Dispose(false);
			}
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
			#endregion
		}
	}
}
