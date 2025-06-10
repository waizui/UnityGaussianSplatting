using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GaussianSplatting.Runtime;
using Unity.Collections;
using Unity.Mathematics;

namespace StreetGS
{

    /// <summary>
    /// StreetGS-specific Gaussian Splat Renderer.
    /// </summary>
    class StreetGaussianSplatModifier : MonoBehaviour
    {

        private GaussianSplatRenderer m_SplatRenderer;

        private GSVehicleController m_VehicleController;


        void Awake()
        {
            m_SplatRenderer = GetComponent<GaussianSplatRenderer>();
            m_VehicleController = GetComponent<GSVehicleController>();
        }

        void Update()
        {
            m_SplatRenderer.ModifyDataBuffer(ConvertSHCoefficients, "sh");
        }

        private GraphicsBuffer ConvertSHCoefficients(GraphicsBuffer orgBuffer, GaussianSplatAsset asset)
        {

            var rawShData = asset.shData.GetData<uint>();
            var shData = RecoverSHFourier(m_VehicleController.currentTime, 5, asset.splatCount, rawShData);

            var shBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, shData.Length, 4) { name = "GaussianSHData" };
            shBuffer.SetData(shData);
            shData.Dispose();
            return shBuffer;
        }


        private NativeArray<uint> RecoverSHFourier(float t, int fourier_dim, int splatCount, NativeArray<uint> data)
        {
            var idftBasis = new float[fourier_dim];
            for (int i = 0; i < fourier_dim; i++)
            {
                if (i % 2 == 0)
                {
                    idftBasis[i] = Mathf.Cos(Mathf.PI * t * i);
                }
                else
                {
                    idftBasis[i] = Mathf.Sin(Mathf.PI * t * (i + 1));
                }
            }

            var max_sh_degree = 1; // test
            var shLen = (max_sh_degree + 1) * (max_sh_degree + 1);
            var recData = new NativeArray<uint>(splatCount * shLen * 3, Allocator.Temp); // 3 channels for RGB
            for (int i = 0; i < splatCount * shLen * 3; i++)
            {
                int splatIndex = i / (shLen * 3);
                int shIndex = i / 3 % shLen;
                int channel = i % 3;

                float coeff = 0.0f;

                // TODO: use the correct formula for SH coefficients

                recData[i] = math.asuint(coeff);

            }


            data.Dispose();
            return recData;

        }

        private float SortableUintToFloat(uint v)
        {
            uint mask = ((v >> 31) - 1) | 0x80000000u;
            return math.asfloat(v ^ mask);
        }

    }

}