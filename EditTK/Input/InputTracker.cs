using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;

namespace EditTK.Input
{
    /// <summary>
    /// Keeps track of all mouse and keyboard inputs
    /// </summary>
    public unsafe class InputTracker
    {
        private static readonly HashSet<Key> _currentlyPressedKeys = new();
        private static readonly HashSet<Key> _newKeysThisFrame     = new();
        private static readonly HashSet<Key> _removedKeysThisFrame = new();

        private static readonly HashSet<MouseButton> _currentlyPressedMouseButtons = new();
        private static readonly HashSet<MouseButton> _newMouseButtonsThisFrame     = new();
        private static readonly HashSet<MouseButton> _removedMouseButtonsThisFrame = new();

        public static ModifierKeys   ModifierKeys    { get; private set; } = ModifierKeys.None;
        public static bool WindowHovered { get; private set; }
        public static Vector2        MousePosition   { get; private set; }
        public static Vector2        MouseMoveDelta  { get; private set; }
        public static float          MouseWheelDelta { get; private set; }
        public static InputSnapshot? FrameSnapshot   { get; private set; }

        public static float MouseDoubleClickTime = 0.30f;
        public static float MouseDoubleClickMaxDist = 6.0f;

        internal static void Reset()
        {
            _currentlyPressedKeys.Clear();
            _newKeysThisFrame.Clear();
            _removedKeysThisFrame.Clear();

            _currentlyPressedMouseButtons.Clear();
            _newMouseButtonsThisFrame.Clear();
            _removedMouseButtonsThisFrame.Clear();

            ModifierKeys = ModifierKeys.None;
        }

        private delegate uint SDL_GetGlobalMouseState_Func(int* x, int* y);

        private static SDL_GetGlobalMouseState_Func SDL_GetGlobalMouseState = Sdl2Native.LoadFunction<SDL_GetGlobalMouseState_Func>("SDL_GetGlobalMouseState");
        private static Vector2 _globalSpaceMousePos;
        private static uint _mouseButtonBitField;

        public static bool GetKey(Key key)
        {
            return _currentlyPressedKeys.Contains(key);
        }

        public static bool GetKeyDown(Key key)
        {
            return _newKeysThisFrame.Contains(key);
        }

        public static bool GetKeyUp(Key key)
        {
            return _removedKeysThisFrame.Contains(key);
        }

        public static bool GetMouseButton(MouseButton button)
        {
            return _currentlyPressedMouseButtons.Contains(button);
        }

        public static bool GetMouseButtonDown(MouseButton button)
        {
            return _newMouseButtonsThisFrame.Contains(button);
        }

        public static bool GetMouseButtonUp(MouseButton button)
        {
            return _removedMouseButtonsThisFrame.Contains(button);
        }

        internal static void BeforeWindowFrameInputs()
        {
            int x = 0;
            int y = 0;

            SDL_GetGlobalMouseState(&x, &y);

            var globalSpaceMousePos = new Vector2(x, y);


            MouseMoveDelta = globalSpaceMousePos - _globalSpaceMousePos;

            _globalSpaceMousePos = globalSpaceMousePos;
        }

        internal static void UpdateWindowFrameInput(InputSnapshot snapshot, Sdl2Window window, bool hovered)
        {
            var unboundMousePosition = _globalSpaceMousePos - window.Bounds.Position;

            FrameSnapshot = snapshot;
            _newKeysThisFrame.Clear();
            _removedKeysThisFrame.Clear();
            _newMouseButtonsThisFrame.Clear();
            _removedMouseButtonsThisFrame.Clear();

            MousePosition = snapshot.MousePosition;
            MouseWheelDelta = snapshot.WheelDelta;
            for (int i = 0; i < snapshot.KeyEvents.Count; i++)
            {
                KeyEvent ke = snapshot.KeyEvents[i];
                if (ke.Down)
                {
                    KeyDown(ke.Key);
                }
                else
                {
                    KeyUp(ke.Key);
                }
            }
            for (int i = 0; i < snapshot.MouseEvents.Count; i++)
            {
                MouseEvent me = snapshot.MouseEvents[i];
                if (me.Down)
                {
                    MouseDown(me.MouseButton);
                }
                else
                {
                    MouseUp(me.MouseButton);
                }
            }

            if (hovered)
            {
                int x, y;

                _mouseButtonBitField = SDL_GetGlobalMouseState(&x, &y);

                MouseButton[] buttons = Enum.GetValues<MouseButton>();

                for (int i = 0; i < buttons.Length; i++)
                {
                    int button = (int)buttons[i];
                    uint buttonBit = (uint)0x1 << button;

                    bool isButtonCurrentlyDown = _currentlyPressedMouseButtons.Contains((MouseButton)button);

                    if ((_mouseButtonBitField & buttonBit) != 0 && !isButtonCurrentlyDown)
                    {
                        MouseDown((MouseButton)button);
                    }
                }
            }



            //use global mouse position if it's beyond the window bounds
            WindowHovered = (unboundMousePosition.X >= 0 && unboundMousePosition.X < window.Bounds.Width &&
                             unboundMousePosition.Y >= 0 && unboundMousePosition.Y < window.Bounds.Height);


            if(!WindowHovered)
                MousePosition = unboundMousePosition;
        }

        internal static void AfterWindowFrameInputs()
        {
            bool atleastOneButtonDown = _currentlyPressedMouseButtons.Count > 0;

            MouseButton[] buttons = Enum.GetValues<MouseButton>();

            int x,y;

            _mouseButtonBitField = SDL_GetGlobalMouseState(&x, &y);

            for (int i = 0; i < buttons.Length; i++)
            {
                int button = (int)buttons[i];
                uint buttonBit = (uint)0x1 << button;

                bool isButtonCurrentlyDown = _currentlyPressedMouseButtons.Contains((MouseButton)button);

                if ((_mouseButtonBitField & buttonBit) == 0)
                {
                    if (isButtonCurrentlyDown)
                    {
                        MouseUp((MouseButton)button);
                    }
                }
                else if (atleastOneButtonDown && !isButtonCurrentlyDown)
                {
                    MouseDown((MouseButton)button);
                }
            }
        }

        private static void MouseUp(MouseButton mouseButton)
        {
            if (_currentlyPressedMouseButtons.Remove(mouseButton))
            {
                _newMouseButtonsThisFrame.Remove(mouseButton);
                _removedMouseButtonsThisFrame.Add(mouseButton);
            }
        }

        private static void MouseDown(MouseButton mouseButton)
        {
            if (_currentlyPressedMouseButtons.Add(mouseButton))
            {
                _newMouseButtonsThisFrame.Add(mouseButton);
                _removedMouseButtonsThisFrame.Remove(mouseButton);
            }
        }

        private static void KeyUp(Key key)
        {
            if (_currentlyPressedKeys.Remove(key))
            {
                _newKeysThisFrame.Remove(key);
                _removedKeysThisFrame.Add(key);

                switch (key)
                {
                    case Key.LShift:
                    case Key.RShift:
                        ModifierKeys &= ~ModifierKeys.Shift;
                        break;
                    case Key.LControl:
                    case Key.RControl:
                        ModifierKeys &= ~ModifierKeys.Control;
                        break;
                    case Key.LAlt:
                    case Key.RAlt:
                        ModifierKeys &= ~ModifierKeys.Alt;
                        break;
                    default:
                        break;
                }
            }
        }

        private static void KeyDown(Key key)
        {
            if (_currentlyPressedKeys.Add(key))
            {
                _newKeysThisFrame.Add(key);
                _removedKeysThisFrame.Remove(key);

                switch (key)
                {
                    case Key.LShift:
                    case Key.RShift:
                        ModifierKeys |= ModifierKeys.Shift;
                        break;
                    case Key.LControl:
                    case Key.RControl:
                        ModifierKeys |= ModifierKeys.Control;
                        break;
                    case Key.LAlt:
                    case Key.RAlt:
                        ModifierKeys |= ModifierKeys.Alt;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
