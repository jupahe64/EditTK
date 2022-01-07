using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace EditTK.Input
{
    public enum KeyPressAction
    {
        KeyDown,
        KeyUp
    }

    public class KeyboardInput : IMappedInput
    {
        private readonly Key _key;

        private readonly ModifierKeys _modifierKeys;
        private readonly ModifierKeys _modifierMask;

        private readonly Func<Key, bool> _keyEventChecker;

        public const ModifierKeys DEFAULT_MODIFIER_MASK = ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift;
        public const ModifierKeys IGNORE_ALL_MODIFIER_MASK = ModifierKeys.None;
        public const ModifierKeys IGNORE_SHIFT_MODIFIER_MASK = ModifierKeys.Alt | ModifierKeys.Control;

        public KeyboardInput(Key key, KeyPressAction keyPressAction, ModifierKeys modifierKeys, ModifierKeys modifierMask = DEFAULT_MODIFIER_MASK)
        {
            _key = key;
            _modifierKeys = modifierKeys;
            _modifierMask = modifierMask;

            switch (keyPressAction)
            {
                case KeyPressAction.KeyDown:
                    _keyEventChecker = InputTracker.GetKeyDown;
                    break;
                case KeyPressAction.KeyUp:
                    _keyEventChecker = InputTracker.GetKeyUp;
                    this._modifierMask = ModifierKeys.None; //Releasing a key should always ignore modifiers
                    break;
                default:
                    throw new ArgumentException($"{nameof(keyPressAction)} {keyPressAction} is not supported");
            }
        }

        public bool WasTriggeredThisFrame()
        {
            bool modifiersMatch = (_modifierKeys & _modifierMask) == (InputTracker.ModifierKeys & _modifierMask);

            return modifiersMatch && _keyEventChecker(_key);
        }
    }
}
