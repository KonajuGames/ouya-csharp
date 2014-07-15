// OUYA Development Kit C# bindings - Copyright (C) Konaju Games
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt' which is part of this source code package.

using Android.Graphics;
using Android.Graphics.Drawables;
using Java.Nio;
using System.IO;

namespace Ouya.Console.Api
{
    public partial class OuyaController
    {
        /// <summary>
        /// The drawable & displayable name for a specific button.
        /// </summary>
        public partial class ButtonData
        {
            /// <summary>
            /// Gets the button image as a PNG.
            /// </summary>
            /// <returns>A stream containing the PNG image.</returns>
            public Stream GetAsPng()
            {
                BitmapDrawable drawable = ButtonDrawable as BitmapDrawable;
                if (drawable == null)
                    return null;

                var stream = new MemoryStream();
                drawable.Bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
                stream.Position = 0;

                return stream;
            }
        }

        /// <summary>
        /// Checks if a button state changed within the current frame loop (as signaled by calling startOfFrame).
        /// </summary>
        /// <param name="button">The button to check.</param>
        /// <returns>True if the button state changed this frame</returns>
        public bool ButtonChangedThisFrame(Buttons button)
        {
            return ButtonChangedThisFrameInternal((int)button);
        }

        /// <summary>
        /// Checks if a button has been pressed within the current frame loop (as signaled by calling startOfFrame).
        /// </summary>
        /// <param name="button">The button to check.</param>
        /// <returns></returns>
        public bool ButtonPressedThisFrame(Buttons button)
        {
            return ButtonPressedThisFrameInternal((int)button);
        }

        /// <summary>
        /// Checks if a button has been released within the current frame loop (as signaled by calling startOfFrame).
        /// </summary>
        /// <param name="button">The button to check.</param>
        /// <returns></returns>
        public bool ButtonReleasedThisFrame(Buttons button)
        {
            return ButtonReleasedThisFrameInternal((int)button);
        }

        /// <summary>
        /// Gets the current axis value.
        /// </summary>
        /// <param name="axis">The axis to check.</param>
        /// <returns>The current axis value.</returns>
        public float GetAxisValue(AxisType axis)
        {
            return GetAxisValueInternal((int)axis);
        }

        /// <summary>
        /// Gets the current pressed state of the button.
        /// </summary>
        /// <param name="button">The button to check.</param>
        /// <returns>True if the button is pressed.</returns>
        public bool GetButton(Buttons button)
        {
            return GetButtonInternal((int)button);
        }

        /// <summary>
        /// Queries the OUYA framework to get the appropriate image/name for a specific button. The returned drawable will be 160px high.
        /// </summary>
        /// <param name="button">The button to get data for.</param>
        /// <returns>The button data.</returns>
        static public ButtonData GetButtonData(Buttons button)
        {
            return GetButtonDataInternal((int)button);
        }
    }
}