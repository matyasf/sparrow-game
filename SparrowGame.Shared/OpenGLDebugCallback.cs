using System;
using Sparrow.Core;
#if __WINDOWS__
using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
#elif __ANDROID__
using Android.Opengl;
#endif

namespace SparrowGame.Shared
{

#if __WINDOWS__
    class OpenGLDebugCallback
    {

        private static OpenGLDebugCallback instance;
        private DebugProc PCCallbackInstance = PCCallbackHandler; // The callback delegate must be stored to avoid GC

        public static void Init()
        {
            instance = new OpenGLDebugCallback();
        }
        
        public OpenGLDebugCallback()
        {
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            GL.DebugMessageCallback(PCCallbackInstance, IntPtr.Zero);
        }

        static void PCCallbackHandler(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            if (severity == DebugSeverity.DebugSeverityHigh || severity == DebugSeverity.DebugSeverityMedium)
            {
                string msg = Marshal.PtrToStringAnsi(message);
                Console.WriteLine("[GL] {0}; {1}; {2}; {3}; {4}", source, type, id, severity, msg);
            }
        }

#elif __ANDROID__
    class OpenGLDebugCallback : Java.Lang.Object, GLES31Ext.IDebugProcKHR
    { 
            
        private static OpenGLDebugCallback instance;
        
        public static void Init()
        {
            instance = new OpenGLDebugCallback();
        }

        public OpenGLDebugCallback()
        {
            if (Context.DeviceSupportsOpenGLExtension("GL_KHR_debug"))
            {
                try
                {
                    GLES31Ext.GlDebugMessageCallbackKHR(this);
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine("WARNING: No support for OpenGL debug callback, likely its not implemented");
                }
            }
            else
            {
                Console.Out.WriteLine("WARNING: No support for OpenGL debug callback");
            }
        }

        public void OnMessage(int source, int type, int id, int severity, string message)
        {
            Console.Out.WriteLine("OpenGL msg: " + source + " " + message);
        }
#endif
    }

}
