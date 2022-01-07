using EditTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace EditTK.Testing
{
    public class AxisInfo
    {
        public static readonly AxisInfo ViewRotationAxis = new(0xFF_FF_FF_FF, "View");

        public static readonly AxisInfo Axis0 = new(0xFF_44_44_FF, "X", Key.X);
        public static readonly AxisInfo Axis1 = new(0xFF_FF_88_44, "Y", Key.Y);
        public static readonly AxisInfo Axis2 = new(0xFF_44_FF_44, "Z", Key.Z);

        public static readonly AxisInfo[] Axes = new AxisInfo[]
        {
            Axis0,
            Axis1,
            Axis2
        };

        public AxisInfo(uint color, string name, Key key = Key.Unknown)
        {
            Color = color;
            Name = name;

            ToggleAxisInput = new KeyboardInput(key, KeyPressAction.KeyDown, ModifierKeys.None);
            TogglePlaneInput = new KeyboardInput(key, KeyPressAction.KeyDown, ModifierKeys.Shift);
        }

        public uint Color { get; private set; }

        public string Name { get; private set; }

        public IMappedInput ToggleAxisInput { get; private set; }
        public IMappedInput TogglePlaneInput { get; private set; }
    }
}
