using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.Scripting;

namespace HybridCLR
{
    [Preserve]
    public static class RuntimeApi
    {
        /// <summary>
        /// load supplementary metadata assembly
        /// </summary>
        /// <param name="dllBytes"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
#if UNITY_EDITOR
        public static unsafe LoadImageErrorCode LoadMetadataForAOTAssembly(byte[] dllBytes, HomologousImageMode mode)
        {
            return LoadImageErrorCode.OK;
        }
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern LoadImageErrorCode LoadMetadataForAOTAssembly(byte[] dllBytes, HomologousImageMode mode);
#endif

        /// <summary>
        /// prejit method to avoid the jit cost of first time running
        /// </summary>
        /// <param name="method"></param>
        /// <returns>return true if method is jited, return false if method can't be jited </returns>
        /// 
#if UNITY_EDITOR
        public static bool PreJitMethod(MethodInfo method)
        {
            return false;
        }
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool PreJitMethod(MethodInfo method);
#endif

        /// <summary>
        /// prejit all methods of class to avoid the jit cost of first time running
        /// </summary>
        /// <param name="type"></param>
        /// <returns>return true if class is jited, return false if class can't be jited </returns>
#if UNITY_EDITOR
        public static bool PreJitClass(Type type)
        {
            return false;
        }
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool PreJitClass(Type type);
#endif

        /// <summary>
        /// get the maximum number of StackObjects in the interpreter thread stack (size*8 represents the final memory size occupied
        /// </summary>
        /// <returns></returns>
        public static int GetInterpreterThreadObjectStackSize()
        {
            return GetRuntimeOption(RuntimeOptionId.InterpreterThreadObjectStackSize);
        }

        /// <summary>
        /// set the maximum number of StackObjects for the interpreter thread stack (size*8 represents the final memory size occupied)
        /// </summary>
        /// <param name="size"></param>
        public static void SetInterpreterThreadObjectStackSize(int size)
        {
            SetRuntimeOption(RuntimeOptionId.InterpreterThreadObjectStackSize, size);
        }


        /// <summary>
        /// get the number of interpreter thread function frames (sizeof(InterpreterFrame)*size represents the final memory size occupied)
        /// </summary>
        /// <returns></returns>
        public static int GetInterpreterThreadFrameStackSize()
        {
            return GetRuntimeOption(RuntimeOptionId.InterpreterThreadFrameStackSize);
        }

        /// <summary>
        /// set the number of interpreter thread function frames (sizeof(InterpreterFrame)*size represents the final memory size occupied)
        /// </summary>
        /// <param name="size"></param>
        public static void SetInterpreterThreadFrameStackSize(int size)
        {
            SetRuntimeOption(RuntimeOptionId.InterpreterThreadFrameStackSize, size);
        }


#if UNITY_EDITOR

        private static readonly Dictionary<RuntimeOptionId, int> s_runtimeOptions = new Dictionary<RuntimeOptionId, int>();

        /// <summary>
        /// set runtime option value
        /// </summary>
        /// <param name="optionId"></param>
        /// <param name="value"></param>
        public static void SetRuntimeOption(RuntimeOptionId optionId, int value)
        {
            s_runtimeOptions[optionId] = value;
        }
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void SetRuntimeOption(RuntimeOptionId optionId, int value);
#endif

        /// <summary>
        /// get runtime option value
        /// </summary>
        /// <param name="optionId"></param>
        /// <returns></returns>
#if UNITY_EDITOR
        public static int GetRuntimeOption(RuntimeOptionId optionId)
        {
            if (s_runtimeOptions.TryGetValue(optionId, out var value))
            {
                return value;
            }
            return 0;
        }
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetRuntimeOption(RuntimeOptionId optionId);
#endif

        // ---- load gate (agonist-ai fork, native hybridclr/LoadGate.h) ----
        // The one door for code bytes. In VerifiedOnly (the native default of this fork) Assembly.Load(byte[]) and
        // LoadMetadataForAOTAssembly accept bytes only after AllowLoad was called with their SHA-256 (one ticket, one load).
        // The app's signed-delivery module verifies signature, app build and hash, then calls AllowLoad right before
        // loading. BuiltinOnly refuses every load. The mode can only be tightened during the process lifetime.

#if UNITY_EDITOR
        public static LoadGateMode GetLoadGate() => LoadGateMode.Open;
        public static LoadGateMode SetLoadGate(LoadGateMode mode) => LoadGateMode.Open;
        public static bool AllowLoad(byte[] sha256) => sha256 != null && sha256.Length == 32;
        public static int GetLoadGateCount(bool refused) => 0;
#else
        public static LoadGateMode GetLoadGate() => (LoadGateMode)GetLoadGateNative();
        public static LoadGateMode SetLoadGate(LoadGateMode mode) => (LoadGateMode)SetLoadGateNative((int)mode);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool AllowLoad(byte[] sha256);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetLoadGateCount(bool refused);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetLoadGateNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int SetLoadGateNative(int mode);
#endif
    }

    public enum LoadGateMode
    {
        Open = 0,
        VerifiedOnly = 1,
        BuiltinOnly = 2,
    }
}
