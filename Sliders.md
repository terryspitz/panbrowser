The functions need to expose variables to the GUI for users to play with, for example Windows Slider controls to represent an int or double (I use the term Slider to include a text string too).  I tried various designs for this before settling on the implementation used.  Additional requirements were to allow code to set the upper and lower limits and default value for the variables.

# Sliders Try 1

I defined a new Sliders library which the F# referred to.

{{ 
let getSlider sliderName from to default = Terry.Sliders.Getdouble(sliderName, from, to, default) 
}}

This library managed a dictionary of Sliders and their current values which were created or accessed every time the F# functions needed them, for example:

{{
let swirl image p = transformImage (swirlP (getSlider "swirl" -20.0 20.0 1.0)) image p
}}

The GUI also accessed the Sliders library in order to display and alter the slider values; when they changed from the GUI the image was redrawn picking up the new values.  This worked, and sliders get picked up from inner functions automatically which is nice, but it looked ugly in the F# and there was some performance impact from repeatedly accessing the Slider values... so onto:

# Sliders Try 2

I tried using custom .NET Attributes to markup the inputs to the functions - also not particularly pretty and they don't get inherited when functions are composed which means lots more typing.
{{
let translateS ([<SliderDouble("x", 0.0, -10.0, 10.0)>](_SliderDouble(_x_,-0.0,--10.0,-10.0)_)x) ([<SliderDouble("y", 0.0, -10.0, 10.0)>](_SliderDouble(_y_,-0.0,--10.0,-10.0)_)y) = translateP (x, y) 
}}

# Sliders Try 3

Next was using proper input arguments, with null values representing unbound various which raise exceptions to indicate the slider range and defaults; this was a bit, err, clunky?
{{
let translateE x y =    if x = None then raise (NeedsDouble("x", 0.0, -2.0, 2.0)) 
                        else if y = None then raise (NeedsDouble("y", 0.0, -2.0, 2.0))
                        else translateP (x.Value, y.Value)
}}
                        
# Slider Try 4

Last and hopefully best is passing in variables and defining slider range using a matching named global returning a tuple of lower, upper and default values.  I think i like it!

{{
let translateH dx dy p = { x= dx + p.x; y= dy + p.y }
let dx = ("translate x", -5.0, 5.0, 0.0)
let dy = ("translate y", -5.0, 5.0, 0.0)
}}

In order to bind the values from the GUI, I had some fun writing a [currying](http://en.wikipedia.org/wiki/Currying) function: in the above example starting with particular dx and dy I dynamically create a .NET function taking only a point p which returns a translation by that dx,dy.  It does this in a similar way you can see (using Reflector.NET) F# doing its currying, which is just hardcoding certain inputs before calling the underlying uncurried function. The code to do this (from PanWrapper.cs) is quite simple (once you've read half a dozen F# blogs of course):

{{
        private static void CreateCurryFunction(MethodInfo method, Type[]()() paramTypes, object[]()() paramValues, DynamicMethod curryFn)
        {
            ILGenerator il = curryFn.GetILGenerator(256);
            for (int i = 0; i < paramValues.Length; i++)
            {
                switch (paramTypes[i](i).ToString())
                {
                    case "System.String":
                        il.Emit(OpCodes.Ldstr, (string)paramValues[i](i));
                        break;
                    ...
                }
            }
            if(paramTypes.Length > paramValues.Length)  //if function takes final Point arg
                il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Call, method, null);
            il.Emit(OpCodes.Ret);
        }

}}

Let me know what you think!