using EditTK.Core.Input;
using EditTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace EditTK.Input
{
    public enum MousePressAction
    {
        MouseDown,
        MouseUp
    }

    public class MouseInput : IMappedInput
    {
        private readonly MouseButton _mouseButton;
        private readonly Key? _key;

        private readonly ModifierKeys _modifierKeys;
        private readonly ModifierKeys _modifierMask;

        private readonly Func<MouseButton, bool> _mouseEventChecker;

        public MouseInput(MouseButton mouseButton, MousePressAction mousePressAction, Key? key = null, ModifierKeys modifierKeys = ModifierKeys.None, ModifierKeys modifierMask = KeyboardInput.DEFAULT_MODIFIER_MASK)
        {
            _mouseButton = mouseButton;
            _key = key;
            _modifierKeys = modifierKeys;
            _modifierMask = modifierMask;

            switch (mousePressAction)
            {
                case MousePressAction.MouseDown:
                    _mouseEventChecker = InputTracker.GetMouseButtonDown;
                    break;
                case MousePressAction.MouseUp:
                    _mouseEventChecker = InputTracker.GetMouseButtonUp;
                    _modifierMask = ModifierKeys.None; //Releasing the mouse should always ignore modifiers
                    break;
                default:
                    throw new ArgumentException($"{nameof(mousePressAction)} {mousePressAction} is not supported");
            }
        }

        public bool WasTriggeredThisFrame()
        {
            bool modifiersMatch = (_modifierKeys & _modifierMask) == (InputTracker.ModifierKeys & _modifierMask);

            bool keyPressedIfRequired = true;

            if (_key.HasValue)
                keyPressedIfRequired = InputTracker.GetKey(_key.Value);

            bool eventHappened = _mouseEventChecker(_mouseButton);

            if (eventHappened)
            {
                Console.WriteLine($"{nameof(modifiersMatch)}: {modifiersMatch} {nameof(keyPressedIfRequired)}: {keyPressedIfRequired} {nameof(_modifierMask)}: {_modifierMask}");

                Console.WriteLine($"{nameof(_modifierKeys)}: {_modifierKeys & _modifierMask} {nameof(InputTracker.ModifierKeys)}: {InputTracker.ModifierKeys & _modifierMask}");
            }

            return modifiersMatch && keyPressedIfRequired && eventHappened;
        }
    }
}
