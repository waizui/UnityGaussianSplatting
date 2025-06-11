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
            m_SplatRenderer.ModifyDataBuffer(RecoverSHCoefficients, "sh");
        }

        private GraphicsBuffer RecoverSHCoefficients(GraphicsBuffer orgBuffer, GaussianSplatAsset asset)
        {
            // TODO: recover 1st 3 coefficients 

            return orgBuffer;
        }

    }

}