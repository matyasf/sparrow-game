using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace SparrowGame
{
    class GPUInfo
    {
        public static void PrintGPUInfo()
        {
            string versionOpenGL = GL.GetString(StringName.Version);
            Console.Out.WriteLine("GL version:" + versionOpenGL);

            int[] work_grp_cnt = new int[3];
            GetIndexedPName maxWorkGroupCount = (GetIndexedPName)All.MaxComputeWorkGroupCount;
            GL.GetInteger(maxWorkGroupCount, 0, out work_grp_cnt[0]);
            GL.GetInteger(maxWorkGroupCount, 1, out work_grp_cnt[1]);
            GL.GetInteger(maxWorkGroupCount, 2, out work_grp_cnt[2]);

            Console.Out.WriteLine("max global (total) work group size " + string.Join(",", work_grp_cnt));

            int[] work_grp_size = new int[3];
            GetIndexedPName maxComputeGroupSize = (GetIndexedPName)All.MaxComputeWorkGroupSize;
            GL.GetInteger(maxComputeGroupSize, 0, out work_grp_size[0]);
            GL.GetInteger(maxComputeGroupSize, 1, out work_grp_size[1]);
            GL.GetInteger(maxComputeGroupSize, 2, out work_grp_size[2]);

            Console.Out.WriteLine("max local (in one shader) work group sizes " + string.Join(",", work_grp_size));
        }

        public static void checkShaderCompileError(int shaderId)
        {
            int rvalue;
            GL.GetShader(shaderId, ShaderParameter.CompileStatus, out rvalue);
            if (rvalue != 1)
            {
                string info = GL.GetShaderInfoLog(shaderId);
                Console.Out.WriteLine("Shader compile error: " + info + " " + rvalue);
            }
        }

        public static void checkShaderLinkError(int shaderProgramId)
        {
            int rvalue;
            GL.GetProgram(shaderProgramId, GetProgramParameterName.LinkStatus, out rvalue);
            if (rvalue != 1)
            {
                string info = GL.GetProgramInfoLog(shaderProgramId);
                Console.Out.WriteLine("Shader linker error: " + info + " " + rvalue);
            }
        }
    }
}
