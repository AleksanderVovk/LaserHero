using UnityEngine;
using UnityEngine.EventSystems;

namespace LaserHero
{
    public sealed class LaserFlightHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public LaserFlightMotor motor;
        public bool up;
        int pointer = int.MinValue;
        void SetHeld(bool held)
        {
            if (motor == null)
                return;
            if (up)
                motor.ascendHeld = held;
            else
                motor.descendHeld = held;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue)
                return;
            pointer = e.pointerId;
            SetHeld(true);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (pointer != e.pointerId)
                return;
            pointer = int.MinValue;
            SetHeld(false);
        }

        void OnDisable()
        {
            pointer = int.MinValue;
            SetHeld(false);
        }
    }
}
