using System;
using UnityEngine;

namespace Gameplay
{
    public class CursorLock : MonoBehaviour
    {
        public bool lockCursor;

        private void Update()
        {
            Cursor.lockState = lockCursor? CursorLockMode.Confined: CursorLockMode.None;
            Cursor.visible = !lockCursor;
        }
    }
}