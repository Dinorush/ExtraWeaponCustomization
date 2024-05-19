using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public static class AutoAimReticle
    {
        public readonly static CrosshairHitIndicator Reticle;
        public readonly static GameObject ReticleHolder;

        static AutoAimReticle()
        {
            // Setup reticle
            ReticleHolder = new GameObject();
            ReticleHolder.transform.SetParent(GuiManager.CrosshairLayer.CanvasTrans);
            ReticleHolder.transform.localScale = Vector3.one;
            ReticleHolder.transform.eulerAngles = Vector3.zero;

            Reticle = GameObject.Instantiate(GuiManager.CrosshairLayer.m_hitIndicatorFriendly, ReticleHolder.transform);
            Reticle.name = "AutoAimIndicator";
            Reticle.transform.localScale = Vector3.zero;
            Reticle.transform.localEulerAngles = Vector3.zero;
            Reticle.m_hitColor = Color.black;
            Reticle.UpdateColorsWithAlphaMul(0f);
        }
    }
}
