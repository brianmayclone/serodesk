using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using SeroDesk.Services;

namespace SeroDesk.Effects
{
    /// <summary>
    /// Simulates a refractive liquid-glass material with edge brightening and soft diffusion.
    /// The shader is compiled at runtime so the effect stays self-contained in the repo.
    /// </summary>
    public sealed class LiquidGlassShaderEffect : ShaderEffect
    {
        private static readonly Lazy<PixelShader?> SharedPixelShader = new(CreatePixelShaderSafe);

        private const string ShaderSource = """
sampler2D input : register(s0);
float distortion : register(c0);
float edgeIntensity : register(c1);
float glowAmount : register(c2);
float saturationBoost : register(c3);

float luminance(float3 c)
{
    return dot(c, float3(0.299, 0.587, 0.114));
}

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float2 centered = uv * 2.0 - 1.0;
    float radius = saturate(length(centered));
    float edge = smoothstep(0.30, 1.0, radius);
    float2 normal = centered / max(radius, 0.0001);
    float2 offset = normal * distortion * edge * 0.035;

    float4 baseSample = tex2D(input, saturate(uv - offset));
    float4 blurSample =
        tex2D(input, saturate(uv + float2(0.007, 0.000))) * 0.22 +
        tex2D(input, saturate(uv + float2(-0.007, 0.000))) * 0.22 +
        tex2D(input, saturate(uv + float2(0.000, 0.009))) * 0.22 +
        tex2D(input, saturate(uv + float2(0.000, -0.009))) * 0.22 +
        tex2D(input, saturate(uv + float2(0.004, -0.004))) * 0.12;

    float3 glass = lerp(baseSample.rgb, blurSample.rgb, 0.58);
    float lum = luminance(glass);
    glass = lerp(float3(lum, lum, lum), glass, 1.0 + saturationBoost * 0.12);

    float fresnel = pow(edge, 2.0) * edgeIntensity;
    float topGlow = pow(saturate(1.0 - uv.y), 3.2) * glowAmount;
    float bottomCompression = pow(saturate(uv.y), 2.1) * distortion * 0.08;

    glass += fresnel * float3(0.36, 0.37, 0.39);
    glass += topGlow * float3(0.22, 0.23, 0.24);
    glass -= bottomCompression * float3(0.01, 0.01, 0.015);

    return float4(saturate(glass), baseSample.a);
}
""";

        public static readonly DependencyProperty InputProperty =
            RegisterPixelShaderSamplerProperty(nameof(Input), typeof(LiquidGlassShaderEffect), 0);

        public static readonly DependencyProperty DistortionProperty =
            DependencyProperty.Register(
                nameof(Distortion),
                typeof(double),
                typeof(LiquidGlassShaderEffect),
                new UIPropertyMetadata(0.42d, PixelShaderConstantCallback(0)));

        public static readonly DependencyProperty EdgeIntensityProperty =
            DependencyProperty.Register(
                nameof(EdgeIntensity),
                typeof(double),
                typeof(LiquidGlassShaderEffect),
                new UIPropertyMetadata(0.75d, PixelShaderConstantCallback(1)));

        public static readonly DependencyProperty GlowAmountProperty =
            DependencyProperty.Register(
                nameof(GlowAmount),
                typeof(double),
                typeof(LiquidGlassShaderEffect),
                new UIPropertyMetadata(0.58d, PixelShaderConstantCallback(2)));

        public static readonly DependencyProperty SaturationBoostProperty =
            DependencyProperty.Register(
                nameof(SaturationBoost),
                typeof(double),
                typeof(LiquidGlassShaderEffect),
                new UIPropertyMetadata(0.42d, PixelShaderConstantCallback(3)));

        public LiquidGlassShaderEffect()
        {
            var shader = SharedPixelShader.Value;
            if (shader != null)
            {
                PixelShader = shader;

                UpdateShaderValue(InputProperty);
                UpdateShaderValue(DistortionProperty);
                UpdateShaderValue(EdgeIntensityProperty);
                UpdateShaderValue(GlowAmountProperty);
                UpdateShaderValue(SaturationBoostProperty);
            }
        }

        public Brush Input
        {
            get => (Brush)GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        public double Distortion
        {
            get => (double)GetValue(DistortionProperty);
            set => SetValue(DistortionProperty, value);
        }

        public double EdgeIntensity
        {
            get => (double)GetValue(EdgeIntensityProperty);
            set => SetValue(EdgeIntensityProperty, value);
        }

        public double GlowAmount
        {
            get => (double)GetValue(GlowAmountProperty);
            set => SetValue(GlowAmountProperty, value);
        }

        public double SaturationBoost
        {
            get => (double)GetValue(SaturationBoostProperty);
            set => SetValue(SaturationBoostProperty, value);
        }

        private static PixelShader? CreatePixelShaderSafe()
        {
            if (!TryCompileShader(ShaderSource, "main", "ps_3_0", out var shaderBytes, out var failureReason))
            {
                Logger.Warn($"LiquidGlass shader disabled: {failureReason}");
                return null;
            }

            var shader = new PixelShader();
            shader.SetStreamSource(new MemoryStream(shaderBytes));
            return shader;
        }

        private static bool TryCompileShader(string source, string entryPoint, string profile, out byte[] shaderBytes, out string failureReason)
        {
            shaderBytes = Array.Empty<byte>();
            failureReason = string.Empty;

            var sourceBytes = Encoding.ASCII.GetBytes(source);
            var compileResult = D3DCompile(
                source,
                new IntPtr(sourceBytes.Length),
                null,
                IntPtr.Zero,
                IntPtr.Zero,
                entryPoint,
                profile,
                0,
                0,
                out var shaderBlob,
                out var errorBlob);

            try
            {
                if (compileResult != 0 || shaderBlob == IntPtr.Zero)
                {
                    var message = ReadBlob(errorBlob);
                    failureReason = $"compilation failed (HRESULT 0x{compileResult:X8}). {message}".Trim();
                    return false;
                }

                shaderBytes = CopyBlobBytes(shaderBlob);
                if (shaderBytes.Length == 0)
                {
                    failureReason = "compiler returned an empty or unreadable shader blob.";
                    return false;
                }

                return true;
            }
            finally
            {
                ReleaseBlob(shaderBlob);
                ReleaseBlob(errorBlob);
            }
        }

        private static string ReadBlob(IntPtr blob)
        {
            if (blob == IntPtr.Zero)
            {
                return string.Empty;
            }

            var bytes = CopyBlobBytes(blob);
            return Encoding.ASCII.GetString(bytes).TrimEnd('\0', '\r', '\n', ' ');
        }

        private static byte[] CopyBlobBytes(IntPtr blob)
        {
            try
            {
                var dataPointer = GetBlobBufferPointer(blob);
                var size = checked((int)GetBlobBufferSize(blob).ToUInt64());
                if (dataPointer == IntPtr.Zero || size <= 0)
                {
                    return Array.Empty<byte>();
                }

                var bytes = new byte[size];
                Marshal.Copy(dataPointer, bytes, 0, size);
                return bytes;
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private static IntPtr GetBlobBufferPointer(IntPtr blob)
        {
            var method = Marshal.GetDelegateForFunctionPointer<GetBufferPointerDelegate>(
                Marshal.ReadIntPtr(GetVTable(blob), IntPtr.Size * 3));
            return method(blob);
        }

        private static UIntPtr GetBlobBufferSize(IntPtr blob)
        {
            var method = Marshal.GetDelegateForFunctionPointer<GetBufferSizeDelegate>(
                Marshal.ReadIntPtr(GetVTable(blob), IntPtr.Size * 4));
            return method(blob);
        }

        private static void ReleaseBlob(IntPtr blob)
        {
            if (blob == IntPtr.Zero)
            {
                return;
            }

            var release = Marshal.GetDelegateForFunctionPointer<ReleaseDelegate>(
                Marshal.ReadIntPtr(GetVTable(blob), IntPtr.Size * 2));
            release(blob);
        }

        private static IntPtr GetVTable(IntPtr instance)
        {
            if (instance == IntPtr.Zero)
            {
                throw new InvalidOperationException("D3D blob instance was null.");
            }

            return Marshal.ReadIntPtr(instance);
        }

        [DllImport("d3dcompiler_47.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int D3DCompile(
            [MarshalAs(UnmanagedType.LPStr)] string srcData,
            IntPtr srcDataSize,
            [MarshalAs(UnmanagedType.LPStr)] string? sourceName,
            IntPtr defines,
            IntPtr include,
            [MarshalAs(UnmanagedType.LPStr)] string entryPoint,
            [MarshalAs(UnmanagedType.LPStr)] string target,
            uint flags1,
            uint flags2,
            out IntPtr code,
            out IntPtr errorMsgs);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate IntPtr GetBufferPointerDelegate(IntPtr instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate UIntPtr GetBufferSizeDelegate(IntPtr instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint ReleaseDelegate(IntPtr instance);
    }
}
