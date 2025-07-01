using UnityEngine;
using GaussianSplatting.Runtime;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

namespace StreetGS
{

    /// <summary>
    /// StreetGS-specific Gaussian Splat Renderer.
    /// </summary>
    class StreetGaussianSplatModifier : MonoBehaviour
    {

        [BurstCompile]
        struct ParesFourierCoefficientsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> m_FourierCoefficients;
            [ReadOnly] public NativeArray<int> m_Mapping;
            [NativeDisableParallelForRestriction] public NativeArray<float> m_ReorderedCoefficients;

            public void Execute(int mapIdx)
            {
                int dim = FOURIER_DIM;
                var chn = CHANNEL;
                int stride = chn * dim;
                int from = mapIdx * stride;
                int to = m_Mapping[mapIdx] * stride;
                for (int j = 0; j < stride; ++j)
                {
                    m_ReorderedCoefficients[from + j] = m_FourierCoefficients[to + j];
                }
            }
        }

        // TODO: compute use GPU
        [BurstCompile]
        struct SetSplatsColorJob : IJobParallelFor
        {

            [NativeDisableParallelForRestriction] public NativeArray<byte> m_TextureColor;
            [ReadOnly] public NativeArray<float> m_FourierCoeffs;

            public int splatCount;

            public float time;

            public void Execute(int index)
            {
                int chn = CHANNEL;
                int dim = FOURIER_DIM;
                int fourierSplatStride = chn * dim;

                int startIdx = index * fourierSplatStride;

                float shr = IDFT(time, m_FourierCoeffs, startIdx);
                float shg = IDFT(time, m_FourierCoeffs, startIdx + dim);
                float shb = IDFT(time, m_FourierCoeffs, startIdx + 2 * dim);

                int texIdx = SplatIndexToTextureIndex((uint)index);
                int pixIdx = texIdx * 4 * 4; // 4 bytes per channel, 4 channels (RGBA)

                var dc0 = new float3(shr, shg, shb);
                dc0 = GaussianUtils.SH0ToColor(dc0);

                var bytesR = new NativeArray<byte>(4, Allocator.Temp);
                FloatToByte(dc0.x, bytesR);
                var bytesG = new NativeArray<byte>(4, Allocator.Temp);
                FloatToByte(dc0.y, bytesG);
                var bytesB = new NativeArray<byte>(4, Allocator.Temp);
                FloatToByte(dc0.z, bytesB);

                m_TextureColor[pixIdx] = bytesR[0];
                m_TextureColor[pixIdx + 1] = bytesR[1];
                m_TextureColor[pixIdx + 2] = bytesR[2];
                m_TextureColor[pixIdx + 3] = bytesR[3];

                m_TextureColor[pixIdx + 4] = bytesG[0];
                m_TextureColor[pixIdx + 5] = bytesG[1];
                m_TextureColor[pixIdx + 6] = bytesG[2];
                m_TextureColor[pixIdx + 7] = bytesG[3];

                m_TextureColor[pixIdx + 8] = bytesB[0];
                m_TextureColor[pixIdx + 9] = bytesB[1];
                m_TextureColor[pixIdx + 10] = bytesB[2];
                m_TextureColor[pixIdx + 11] = bytesB[3];
            }
        }


        public const int FOURIER_DIM = 5;
        public const int CHANNEL = 3;
        public TextAsset fourierFile; //ndArray:[N,3,5]
        public TextAsset mappingFile;

        private GaussianSplatRenderer m_SplatRenderer;

        private NativeArray<float>? m_FourierCoeffs;

        void Awake()
        {
            m_SplatRenderer = GetComponent<GaussianSplatRenderer>();
        }

        void Start()
        {
            m_FourierCoeffs = ParesFourierCoefficients(fourierFile, mappingFile);
        }

        void Update()
        {
            m_SplatRenderer.ModifyColorData(SetSplatsColor);
        }

        private NativeArray<float>? ParesFourierCoefficients(TextAsset fourierFile, TextAsset mappingFile)
        {
            if (fourierFile == null || mappingFile == null)
            {
                return null;
            }

            var mapping = mappingFile.GetData<int>();
            var coeffs = fourierFile.GetData<float>();
            if (mapping.Length != coeffs.Length / CHANNEL / FOURIER_DIM)
            {
                Debug.LogError("Mapping length does not match coefficients length.");
                return null;
            }

            var job = new ParesFourierCoefficientsJob
            {
                m_FourierCoefficients = coeffs,
                m_Mapping = mapping,
                m_ReorderedCoefficients = new NativeArray<float>(coeffs.Length, Allocator.Persistent),
            };

            job.Schedule(mapping.Length, 0).Complete();
            return job.m_ReorderedCoefficients;
        }

        //TODO: Optimize
        private Texture2D SetSplatsColor(Texture2D tex, GaussianSplatAsset asset)
        {
            int splatCount = asset.splatCount;
            if (!m_FourierCoeffs.HasValue || m_FourierCoeffs.Value.Length != splatCount * CHANNEL * FOURIER_DIM)
            {
                return tex;
            }

            var texColor = tex.GetRawTextureData<byte>();
            var (texWidth, texHeight) = GaussianSplatAsset.CalcTextureSize(splatCount);
            if (texColor.Length < texWidth * texHeight * 4)
            {
                Debug.LogError("Texture data buffer is smaller than expected.");
                return tex;
            }

            var job = new SetSplatsColorJob
            {
                m_TextureColor = texColor,
                m_FourierCoeffs = m_FourierCoeffs.Value,
                splatCount = splatCount,
                time = 1.0f,//TODO: remove test
            };

            job.Schedule(splatCount, 0).Complete();

            tex.Apply(false, false);
            return tex;
        }

        static void FloatToByte(float value, NativeArray<byte> bytes)
        {
            unsafe
            {
                uint intVal = *(uint*)&value;
                bytes[0] = (byte)(intVal & 0xFF);
                bytes[1] = (byte)((intVal >> 8) & 0xFF);
                bytes[2] = (byte)((intVal >> 16) & 0xFF);
                bytes[3] = (byte)((intVal >> 24) & 0xFF);
            }
        }

        static int SplatIndexToTextureIndex(uint idx)
        {
            uint2 xy = GaussianUtils.DecodeMorton2D_16x16(idx);
            uint width = GaussianSplatAsset.kTextureWidth / 16;
            idx >>= 8;
            uint x = (idx % width) * 16 + xy.x;
            uint y = (idx / width) * 16 + xy.y;
            return (int)(y * GaussianSplatAsset.kTextureWidth + x);
        }

        // IDFT from paper street-gaussian
        static float IDFT(float t, NativeArray<float> coefficients, int startIndex)
        {
            int dim = FOURIER_DIM;
            float result = 0f;
            for (int i = 0; i < dim; ++i)
            {
                float basis;
                if (i % 2 == 0)
                {
                    basis = Mathf.Cos(Mathf.PI * t * i);
                }
                else
                {
                    basis = Mathf.Sin(Mathf.PI * t * (i + 1));
                }
                result += basis * coefficients[startIndex + i];
            }
            return result;

        }


        void OnDestroy()
        {
            m_FourierCoeffs?.Dispose();
        }

    }

}