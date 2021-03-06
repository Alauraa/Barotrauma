﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Barotrauma
{
    public static class PlayerInput
    {
        static MouseState mouseState, oldMouseState;
        static MouseState latestMouseState; //the absolute latest state, do NOT use for player interaction
        static KeyboardState keyboardState, oldKeyboardState;

        static double timeSinceClick;
        static Point lastClickPosition;

        const float DoubleClickDelay = 0.4f;
        const float MaxDoubleClickDistance = 10.0f;

        static bool doubleClicked;

        static bool allowInput;
        static bool wasWindowActive;

#if LINUX || OSX
        static readonly Keys[] manuallyHandledTextInputKeys = { Keys.Left, Keys.Right, Keys.Up, Keys.Down };
        const float AutoRepeatDelay = 0.5f;
        const float AutoRepeatRate = 25;
        static Dictionary<Keys, float> autoRepeatTimer = new Dictionary<Keys, float>();
#endif

        public static Vector2 MousePosition
        {
            get { return new Vector2(mouseState.Position.X, mouseState.Position.Y); }
        }

        public static Vector2 LatestMousePosition
        {
            get { return new Vector2(latestMouseState.Position.X, latestMouseState.Position.Y); }
        }

        public static bool MouseInsideWindow
        {
            get { return new Rectangle(0, 0, GameMain.GraphicsWidth, GameMain.GraphicsHeight).Contains(MousePosition); }
        }

        public static Vector2 MouseSpeed
        {
            get
            {
                return AllowInput ? MousePosition - new Vector2(oldMouseState.X, oldMouseState.Y) : Vector2.Zero;
            }
        }

        private static bool AllowInput
        {
            get { return GameMain.WindowActive && allowInput; }
        }

        public static Vector2 MouseSpeedPerSecond { get; private set; }

        public static KeyboardState GetKeyboardState
        {
            get { return keyboardState; }
        }

        public static KeyboardState GetOldKeyboardState
        {
            get { return oldKeyboardState; }
        }

        public static int ScrollWheelSpeed
        {
            get { return AllowInput ? mouseState.ScrollWheelValue - oldMouseState.ScrollWheelValue : 0; }

        }

        public static bool LeftButtonHeld()
        {
            return AllowInput && mouseState.LeftButton == ButtonState.Pressed;
        }

        public static bool LeftButtonDown()
        {
            return AllowInput &&
                oldMouseState.LeftButton == ButtonState.Released &&
                mouseState.LeftButton == ButtonState.Pressed;
        }

        public static bool LeftButtonReleased()
        {
            return AllowInput && mouseState.LeftButton == ButtonState.Released;
        }


        public static bool LeftButtonClicked()
        {
            return (AllowInput &&
                oldMouseState.LeftButton == ButtonState.Pressed
                && mouseState.LeftButton == ButtonState.Released);
        }

        public static bool RightButtonHeld()
        {
            return AllowInput && mouseState.RightButton == ButtonState.Pressed;
        }

        public static bool RightButtonClicked()
        {
            return (AllowInput &&
                oldMouseState.RightButton == ButtonState.Pressed
                && mouseState.RightButton == ButtonState.Released);
        }

        public static bool MidButtonClicked()
        {
            return (AllowInput &&
                oldMouseState.MiddleButton == ButtonState.Pressed
                && mouseState.MiddleButton == ButtonState.Released);
        }

        public static bool MidButtonHeld()
        {
            return AllowInput && mouseState.MiddleButton == ButtonState.Pressed;
        }

        public static bool Mouse4ButtonClicked()
        {
            return (AllowInput &&
                oldMouseState.XButton1 == ButtonState.Pressed
                && mouseState.XButton1 == ButtonState.Released);
        }

        public static bool Mouse4ButtonHeld()
        {
            return AllowInput && mouseState.XButton1 == ButtonState.Pressed;
        }

        public static bool Mouse5ButtonClicked()
        {
            return (AllowInput &&
                oldMouseState.XButton2 == ButtonState.Pressed
                && mouseState.XButton2 == ButtonState.Released);
        }

        public static bool Mouse5ButtonHeld()
        {
            return AllowInput && mouseState.XButton2 == ButtonState.Pressed;
        }

        public static bool MouseWheelUpClicked()
        {
            return (AllowInput && ScrollWheelSpeed > 0);
        }

        public static bool MouseWheelDownClicked()
        {
            return (AllowInput && ScrollWheelSpeed < 0);
        }

        public static bool DoubleClicked()
        {
            return AllowInput && doubleClicked;
        }

        public static bool KeyHit(InputType inputType)
        {
            return AllowInput && GameMain.Config.KeyBind(inputType).IsHit();
        }

        public static bool KeyDown(InputType inputType)
        {
            return AllowInput && GameMain.Config.KeyBind(inputType).IsDown();
        }

        public static bool KeyUp(InputType inputType)
        {
            return AllowInput && !GameMain.Config.KeyBind(inputType).IsDown();
        }

        public static bool KeyHit(Keys button)
        {
            return (AllowInput && oldKeyboardState.IsKeyDown(button) && keyboardState.IsKeyUp(button));
        }

        public static bool KeyDown(Keys button)
        {
            return (AllowInput && keyboardState.IsKeyDown(button));
        }

        public static bool KeyUp(Keys button)
        {
            return AllowInput && keyboardState.IsKeyUp(button);
        }

        public static void Update(double deltaTime)
        {
            timeSinceClick += deltaTime;

            if (!GameMain.WindowActive)
            {
                wasWindowActive = false;
                return;
            }

            //window was not active during the previous frame -> ignore inputs from this frame
            if (!wasWindowActive)
            {
                wasWindowActive = true;
                allowInput = false;
            }
            else
            {
                allowInput = true;
            }

            oldMouseState = mouseState;
            mouseState = latestMouseState;
            UpdateVariable();

            oldKeyboardState = keyboardState;
            keyboardState = Keyboard.GetState();

            MouseSpeedPerSecond = MouseSpeed / (float)deltaTime;

            doubleClicked = false;
            if (LeftButtonClicked())
            {
                if (timeSinceClick < DoubleClickDelay &&
                    (mouseState.Position - lastClickPosition).ToVector2().Length() < MaxDoubleClickDistance)
                {
                    doubleClicked = true;
                }
                lastClickPosition = mouseState.Position;
                timeSinceClick = 0.0;
            }

#if LINUX || OSX
            //arrow keys cannot be received using window.TextInput on Linux (see https://github.com/MonoGame/MonoGame/issues/5808)
            //so lets do it manually here and pass to the KeyboardDispatcher:
            foreach (Keys key in manuallyHandledTextInputKeys)
            {
                if (!autoRepeatTimer.ContainsKey(key))
                {
                    autoRepeatTimer[key] = 0.0f;
                }
                if (KeyDown(key))
                {
                    if (autoRepeatTimer[key] <= 0.0f)
                    {
                        GUI.KeyboardDispatcher.EventInput_KeyDown(null, new EventInput.KeyEventArgs(key));
                    }
                    else if (autoRepeatTimer[key] > AutoRepeatDelay)
                    {
                        GUI.KeyboardDispatcher.EventInput_KeyDown(null, new EventInput.KeyEventArgs(key));
                        autoRepeatTimer[key] -= 1.0f / AutoRepeatRate;
                    }
                    autoRepeatTimer[key] += (float)deltaTime;
                }
                else
                {
                    autoRepeatTimer[key] = 0.0f;
                }
            }
#endif
        }

        public static void UpdateVariable()
        {
            //do NOT use this for actual interaction with the game, this is to be used for debugging and rendering ONLY

            latestMouseState = Mouse.GetState();
        }
    }
}
