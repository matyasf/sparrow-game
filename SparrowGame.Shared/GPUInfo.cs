using System;

#if __WINDOWS__
using OpenTK.Graphics.OpenGL4;
#elif __ANDROID__
using OpenTK.Graphics.ES20;
using Android.Opengl;
using Java.Nio;
#endif

namespace SparrowGame
{
    class GPUInfo
    {
        public static void PrintGPUInfo()
        {
            string versionOpenGL = GL.GetString(StringName.Version);
            Console.Out.WriteLine("GL version:" + versionOpenGL);

            int[] work_grp_cnt = new int[3];
            int[] work_grp_size = new int[3];
#if __WINDOWS__
            GetIndexedPName maxWorkGroupCount = (GetIndexedPName)All.MaxComputeWorkGroupCount;
            GL.GetInteger(maxWorkGroupCount, 0, out work_grp_cnt[0]);
            GL.GetInteger(maxWorkGroupCount, 1, out work_grp_cnt[1]);
            GL.GetInteger(maxWorkGroupCount, 2, out work_grp_cnt[2]);

            GetIndexedPName maxComputeGroupSize = (GetIndexedPName)All.MaxComputeWorkGroupSize;
            GL.GetInteger(maxComputeGroupSize, 0, out work_grp_size[0]);
            GL.GetInteger(maxComputeGroupSize, 1, out work_grp_size[1]);
            GL.GetInteger(maxComputeGroupSize, 2, out work_grp_size[2]);
#else
            int maxWorkGroupCount = GLES31.GlMaxComputeWorkGroupCount;
            IntBuffer maxCount = IntBuffer.Allocate(3);
            GLES20.GlGetIntegerv(maxWorkGroupCount, maxCount);
            maxCount.Get(work_grp_cnt);

            int maxComputeGroupSize = GLES31.GlMaxComputeWorkGroupSize;
            IntBuffer maxSize = IntBuffer.Allocate(3);
            GLES20.GlGetIntegerv(maxComputeGroupSize, maxSize);
            maxSize.Get(work_grp_size);
#endif
            Console.Out.WriteLine("max global (total) work group size " + string.Join(",", work_grp_cnt));
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
#if __WINDOWS__
            GL.GetProgram(shaderProgramId, GetProgramParameterName.LinkStatus, out rvalue); 
#else
            GL.GetProgram(shaderProgramId, ProgramParameter.LinkStatus, out rvalue);
#endif
            if (rvalue != 1)
            {
                string info = GL.GetProgramInfoLog(shaderProgramId);
                Console.Out.WriteLine("Shader linker error: " + info + " " + rvalue);
            }
        }
    }
}
