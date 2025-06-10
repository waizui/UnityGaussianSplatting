using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GaussianSplatting.Runtime;
using Unity.Collections;

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

            var shDataSize = (int)(asset.shData.dataSize / 4);
            var rawShData = asset.shData.GetData<uint>();

            var shData = RecoverSHFourier(m_VehicleController.currentTime, 5, rawShData);

            var shBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, shDataSize, 4) { name = "GaussianSHData" };
            shBuffer.SetData(shData);
            return shBuffer;
        }


        private NativeArray<uint> RecoverSHFourier(float t, int fourier_dim, NativeArray<uint> data)
        {
            //TODO: IDFT 
            return data;

        }




    }

}