using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace UguiExtensions
{
    /// <summary>
    /// Non-Maskable RawImage
    /// 默认NmImage不支持被Mask裁剪，有助于减少GC
    /// 另外Ugui的Mask也不推荐使用，推荐用RectMask2D来代替
    /// </summary>
    public class NmRawImage : RawImage
    {
        [SerializeField]
        public bool Maskable = false;

        protected override void Awake()
        {
            base.Awake();
            maskable = Maskable;
        }

        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            var toUse = baseMaterial;
            /*if (m_ShouldRecalculateStencil)
            {
                var rootCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
                m_StencilValue = maskable ? MaskUtilities.GetStencilDepth(transform, rootCanvas) : 0;
                m_ShouldRecalculateStencil = false;
            }

            if (m_StencilValue > 0 && GetComponent<Mask>() == null)
            {
                var maskMat = StencilMaterial.Add(toUse, (1 << m_StencilValue) - 1, StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All, (1 << m_StencilValue) - 1, 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMat;
                toUse = m_MaskMaterial;
            }*/
            return toUse;
        }
    }
}
