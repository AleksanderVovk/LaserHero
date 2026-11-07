using TMPro;
using UnityEngine;

namespace LaserHero
{
    public sealed class LaserFlightControls : MonoBehaviour
    {
        public LaserFlightMotor motor;
        public GameObject toggle, up, down;
        public TMP_Text toggleLabel;
        void Update()
        {
            bool visible = motor != null && !motor.InLobby && !motor.hero.MenuOpen;
            toggle.SetActive(visible);
            up.SetActive(visible && motor.Flying);
            down.SetActive(visible && motor.Flying);
            toggleLabel.text = motor.Flying ? "Посадка" : "Взлететь";
        }
    }
}
