using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits
{
    public static class AutoAimReticle
    {
        public readonly static CrosshairHitIndicator Reticle;

        static AutoAimReticle()
        {
            Reticle = GameObject.Instantiate(GuiManager.CrosshairLayer.m_hitIndicatorFriendly, GuiManager.CrosshairLayer.CanvasTrans);
            Reticle.name = "AutoAimIndicator";
            Reticle.transform.localScale = Vector3.zero;
            Reticle.transform.localEulerAngles = Vector3.zero;
            Reticle.m_hitColor = Color.black;
            Reticle.UpdateColorsWithAlphaMul(0f);
        }
    }
}
