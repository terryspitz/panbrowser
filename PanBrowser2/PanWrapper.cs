using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

namespace Terry
{
    class PanWrapper
    {
        //higher level f# functions use as type Func1, i.e. take a function and return a function
        static Type imageType1 = typeof(FastFunc<Pan.Point, Pan.Color>);
        static Type imageType2 = typeof(FastFunc<Pan.Point, bool>);
        static Type transformType1 = typeof(FastFunc<,>);  //can't do typeof(FastFunc<Pan.Point,>)
        static Type pointTransformType = typeof(FastFunc<Pan.Point, Pan.Point>);

        public List<string> Images { get; set; }
        public List<string> Transforms { get; set; }
        protected readonly string None = "---none---";

        public PanWrapper()
        {
            GetPanFunctions();
            Images.Sort();
            Transforms.Sort();
            Transforms.Insert(0, None);
        }

        public void GetPanFunctions()
        {
            //as of Oct09 and before refactoring i had 35 images & 18 transforms

            Images = new List<string>();
            Transforms = new List<string>();

            ///basic functions taking a Point 
            MethodInfo[] methods = typeof(Pan).GetMethods();
            foreach (MethodInfo method in methods)
            {
                Type typeFrom=null, typeTo=null;
                if(!GetMethodTypes(method, ref typeFrom, ref typeTo))
                    continue;  //not an image or transform

                //Point -> bool | Double | Color | Point
                if (typeFrom == typeof(Pan.Point))
                {
                    if (typeTo == typeof(Pan.Color))
                        Images.Add(stripGet(method.Name));
                    else if (typeTo == typeof(bool))
                        Images.Add(stripGet(method.Name));
                    else if (typeTo == typeof(Double))
                        Images.Add(stripGet(method.Name));
                    else if (typeTo == typeof(Pan.Point))
                        Transforms.Add(stripGet(method.Name));
                }
                //(Point -> 'a) -> (Point -> 'a)
                else if (FSharpType.IsFunction(typeFrom)
                    && FSharpType.IsFunction(typeTo)
                    )
                {
                    Tuple<Type, Type> types1 = FSharpType.GetFunctionElements(typeFrom);
                    Tuple<Type, Type> types2 = FSharpType.GetFunctionElements(typeTo);
                    if (types1.Item1 == typeof(Pan.Point) && types1.Item2 == typeof(Pan.Point) &&
                        types1.Item2 == types2.Item2)
                        Transforms.Add(stripGet(method.Name));
                }
            }
        }

        private static string stripGet(string name)
        {
            if (name.StartsWith("get_"))
                return name.Substring(4);
            return name;
        }

        /// <summary>
        /// Checks whether method is a suitable image or transform and return the types it converts from and to
        /// </summary>
        /// <param name="method"></param>
        /// <param name="typeFrom"></param>
        /// <param name="typeTo"></param>
        /// <returns></returns>
        private static bool GetMethodTypes(MethodInfo method, ref Type typeFrom, ref Type typeTo)
        {
            List<ParameterInfo> param = new List<ParameterInfo>();
            param.AddRange(method.GetParameters());
            while (param.Count > 0)
            {
                ParameterInfo t = param[0];
                if (t.ParameterType != typeof(bool) && t.ParameterType != typeof(double) &&
                    t.ParameterType != typeof(int) && t.ParameterType != typeof(string))
                {
                    break;
                }
                param.RemoveAt(0);
            }

            if (param.Count == 1)       //assume a basic function e.g. Point -> Point
            {
                typeFrom = param[0].ParameterType;
                typeTo = method.ReturnType;
            }
            else if (param.Count == 0
                && FSharpType.IsFunction(method.ReturnType))    //method is returning a Function object
            {
                Tuple<Type, Type> types = FSharpType.GetFunctionElements(method.ReturnType);
                typeFrom = types.Item1;
                typeTo = types.Item2;
            }
            else
                return false;

            return true;
        }

        public IList<SliderAttribute> GetSliders(string image, string transform)
        {
            List<SliderAttribute> sliders = new List<SliderAttribute>();
            
            MethodInfo fn;
            fn = GetMethod(image);

            List<ParameterInfo> param = new List<ParameterInfo>();
            param.AddRange(fn.GetParameters());
            while (param.Count > 0)
            {
                ParameterInfo t = param[0];
                if (t.ParameterType != typeof(bool) && t.ParameterType != typeof(double) &&
                    t.ParameterType != typeof(int) && t.ParameterType != typeof(string))
                {
                    break;
                }

                //see if there's a hint for the range/default
                PropertyInfo hint = typeof(Pan).GetProperty(t.Name);
                switch (t.ParameterType.ToString())
                {
                    case "System.String":
                        if (hint != null && FSharpType.IsTuple(hint.PropertyType))
                        {
                            Tuple<string, string> stuple = hint.GetGetMethod().Invoke(null, null) as Tuple<string, string>;
                            if (stuple != null)
                            {
                                sliders.Add(new SliderText(stuple.Item1, stuple.Item2));
                                break;
                            }
                        }
                        sliders.Add(new SliderText(t.Name, "on and "));
                        break;
                    case "System.Double":
                        if (hint != null && FSharpType.IsTuple(hint.PropertyType))
                        {
                            Tuple<string, double, double, double> dtuple = hint.GetGetMethod().Invoke(null, null) as Tuple<string, double, double, double>;
                            if (dtuple != null)
                            {
                                sliders.Add(new SliderDouble(dtuple.Item1, dtuple.Item2, dtuple.Item3, dtuple.Item4));
                                break;
                            }
                        }
                        sliders.Add(new SliderDouble(t.Name, 0, -2, +2));
                        break;
                    case "System.Int32":
                        if (hint != null && FSharpType.IsTuple(hint.PropertyType))
                        {
                            Tuple<string, int, int, int> ituple = hint.GetGetMethod().Invoke(null, null) as Tuple<string, int, int, int>;
                            if (ituple != null)
                            {
                                sliders.Add(new SliderInt(ituple.Item1, ituple.Item2, ituple.Item3, ituple.Item4));
                                break;
                            }
                        }
                        sliders.Add(new SliderInt(t.Name, 1, -10, +10));
                        break;
                }
                
                param.RemoveAt(0);
            }
            return sliders;
        }

        /// <summary>
        /// creates a function delegate to implement the named image
        /// including binding values from controls/sliders into the 
        /// example function types include:
        /// Color simpleImage(Point)
        /// FastFunc<Point, Color> funcImage()
        /// 
        /// example images needing binding are:
        /// Color image(double, Point)
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="sliders"></param>
        /// <returns></returns>
        public static FastFunc<Pan.Point, Pan.Color> GetImageFunction(string image, IList<SliderAttribute> sliders)
        {
            FastFunc<Pan.Point, Pan.Color> imageFunction = null;
            Type typeFrom = null, typeTo = null;

            MethodInfo method;
            method = GetMethod(image);
            if(method==null)
                throw new Exception("image " + image + " not found");  //not an image or transform

            List<ParameterInfo> param = new List<ParameterInfo>();
            param.AddRange(method.GetParameters());
            List<object> paramValues = new List<object>();
            List<Type> paramTypes = new List<Type>();
            while (param.Count > 0)
            {
                ParameterInfo t = param[0];
                if (t.ParameterType != typeof(bool) && t.ParameterType != typeof(double) &&
                    t.ParameterType != typeof(int) && t.ParameterType != typeof(string))
                {
                    break;  //maybe one param left
                }
                paramValues.Add(sliders[0].Value);
                paramTypes.Add(t.ParameterType);
                sliders.RemoveAt(0);
                param.RemoveAt(0);
            }

            if (param.Count == 1)       //assume a basic function e.g. Point -> Color
            {
                if (param[0].ParameterType != typeof(Pan.Point))
                    throw new Exception("image " + image + " wrong type");
                paramTypes.Add(param[0].ParameterType);

                switch (method.ReturnType.ToString())
                {
                    case "System.Boolean":
                        if (paramValues.Count > 0)
                            imageFunction = DrawImage.boolToColImage(Curry<bool>(method, paramTypes.ToArray(), paramValues.ToArray()));
                        else
                            imageFunction = DrawImage.boolToColImage((Converter<Pan.Point, bool>)Delegate.CreateDelegate(typeof(Converter<Pan.Point, bool>), method));
                        break;
                    case "System.Double":
                        if (paramValues.Count > 0)
                            imageFunction = DrawImage.doubleToColImage(Curry<double>(method, paramTypes.ToArray(), paramValues.ToArray()));
                        else
                            imageFunction = DrawImage.doubleToColImage((Converter<Pan.Point, double>)Delegate.CreateDelegate(typeof(Converter<Pan.Point, double>), method));
                        break;
                    case "Pan+Color":
                        if (paramValues.Count > 0)
                            imageFunction = Curry<Pan.Color>(method, paramTypes.ToArray(), paramValues.ToArray());
                        else
                            imageFunction = (Converter<Pan.Point, Pan.Color>)Delegate.CreateDelegate(typeof(Converter<Pan.Point, Pan.Color>), method);
                        break;
                    default:
                        throw new Exception("image " + image + " wrong type");
                }
            }
            else if (param.Count == 0
                && FSharpType.IsFunction(method.ReturnType))    //method is returning a Function object
            {
                Tuple<Type, Type> types = FSharpType.GetFunctionElements(method.ReturnType);
                typeFrom = types.Item1;
                typeTo = types.Item2;

                //Point -> bool | Double | Color | Point
                if (typeFrom == typeof(Pan.Point))
                {
                    if (typeTo == typeof(bool))
                        imageFunction = DrawImage.boolToColImage((FastFunc<Pan.Point, bool>)method.Invoke(null, paramValues.ToArray()));
                    else if (typeTo == typeof(double))
                        imageFunction = DrawImage.doubleToColImage((FastFunc<Pan.Point, double>)method.Invoke(null, paramValues.ToArray()));
                    else if (typeTo == typeof(Pan.Color))
                        imageFunction = (FastFunc<Pan.Point, Pan.Color>)method.Invoke(null, paramValues.ToArray());
                    else
                        throw new Exception("image " + image + " wrong type");
                }
                else
                    throw new Exception("image " + image + " wrong type");
            }
            else
                throw new Exception("image " + image + " not found");  //not an image or transform


            return imageFunction;
        }

        private static MethodInfo GetMethod(string image)
        {
            MethodInfo fn;
            //try property getter first
            fn = typeof(Pan).GetMethod("get_" + image);
            if (fn == null && image!=null)
                fn = typeof(Pan).GetMethod(image);
            return fn;
        }

        /// <summary>
        /// Returns a function that implements the named transform applied to the input image 
        /// binding params to control/sliders if necessary
        /// 
        /// example function types include:
        /// Point simpleTransform(Point) 
        /// FastFunc<Point, Point> pointTransform()
        /// Color funcTranform(double s, FastFunc<Point, Color> image, Point p)
        /// 
        /// example function types needing param binding include:
        /// Point pointTransform1(double, Point)
        /// Point pointTransform2(double, double, Point)
        /// FastFunc<Point, Point> pointTransform3(double)
        /// FastFunc<Point, Point> pointTransform4(double, double)
        /// Color funcTranform(double s, FastFunc<Point, Color> image, Point p)

        /// </summary>
        /// <param name="transform"></param>
        /// <param name="image"></param>
        /// <param name="sliders"></param>
        /// <returns></returns>
        public FastFunc<Pan.Point, Pan.Color> GetTransformFunction(string transform, FastFunc<Pan.Point, Pan.Color> image, IList<SliderAttribute> sliders)
        {
            if (string.IsNullOrEmpty(transform) || transform == None)
                return image;

            FastFunc<Pan.Point, Pan.Color> imageFunction = null;
            Type typeFrom = null, typeTo = null;
            MethodInfo method;

            method = GetMethod(transform);
            if (method == null)
                throw new Exception("transform " + transform + " not found");  //not an image or transform

            List<ParameterInfo> param = new List<ParameterInfo>();
            param.AddRange(method.GetParameters());

            if (FSharpType.IsFunction(method.ReturnType))    //method is returning a Function object
            {
                Tuple<Type, Type> types = FSharpType.GetFunctionElements(method.ReturnType);
                if(types.Item1!=typeof(Pan.Point) || types.Item2!=typeof(Pan.Point))
                    throw new Exception("transform " + transform + " wrong type");  //not an image or transform
                imageFunction = Pan.transformImage<Pan.Color>(
                        (FastFunc<Pan.Point, Pan.Point>)Delegate.CreateDelegate(typeof(FastFunc<Pan.Point, Pan.Point>), typeof(Pan), transform)
                    ,imageFunction);
            }

            Dictionary<string, object> sliderValues = new Dictionary<string, object>();
            foreach(SliderAttribute s in sliders)
                sliderValues[s.Name] = s.Value;

            while (param.Count > 0)
            {
                ParameterInfo t = param[param.Count-1];
                if (t.ParameterType != typeof(bool) && t.ParameterType != typeof(double) &&
                    t.ParameterType != typeof(int) && t.ParameterType != typeof(string))
                {
                    break;
                }

                switch (t.ParameterType.ToString())
                {
                    case "System.String":
                        break;
                    case "System.Double":
                        double d = (double)sliderValues[t.Name];
                        //imageFunction = Curry(
                        //        (FastFunc<Pan.Point, Pan.Point>)Delegate.CreateDelegate(typeof(FastFunc<Pan.Point, Pan.Point>), typeof(Pan), transform)
                        //, imageFunction)
                            ;
                        break;
                    case "System.Int32":
                        break;
                }
                param.RemoveAt(0);
            }

            if (param.Count == 1)       //assume a basic function e.g. Point -> Point
            {
                typeFrom = param[0].ParameterType;
                typeTo = method.ReturnType;
            }
            else
                throw new Exception("transform " + transform + " not found");  //not an image or transform

            if (method != null)
            {
                if (method .ReturnType == typeof(Pan.Point))    //transform
                {
                    //imageFunction = transformImage(
                    //    (Converter<Pan.Point, Pan.Point>)Delegate.CreateDelegate(typeof(Converter<Pan.Point, Pan.Point>), typeof(Pan), transform),
                    //    imageFunction);
                    imageFunction = Pan.transformImage<Pan.Color>(
                        FuncConvert.ToFastFunc(
                            (Converter<Pan.Point, Pan.Point>)Delegate.CreateDelegate(typeof(Converter<Pan.Point, Pan.Point>), typeof(Pan), transform)
                        ),
                        imageFunction);
                }
                else
                {
                    try
                    {
                        MethodInfo genericMethodInfo = method .MakeGenericMethod(new Type[] { typeof(Pan.Color) });
                        object newImageFn = genericMethodInfo.Invoke(null, new object[] { imageFunction });
                        imageFunction = newImageFn as FastFunc<Pan.Point, Pan.Color>;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return imageFunction;
        }

        /// <summary>
        /// Curries a function 
        /// <example>
        /// given a function 
        /// Color textH(string, Point)
        /// then
        /// Curry(textH, { String }, { "hello "))
        /// returns an F# FastFunc&lt;Point, Color&gt; which calls testH with first arg "hello "
        /// </example>
        /// </summary>
        /// <param name="method"></param>
        /// <param name="paramTypes"></param>
        /// <param name="paramValues"></param>
        /// <returns></returns>
        protected static FastFunc<Pan.Point, T> Curry<T>(MethodInfo method, Type[] paramTypes, object[] paramValues)
        {
            DynamicMethod curryFn = new DynamicMethod("curryFn", typeof(T), new Type[] { typeof(Pan.Point) });
            CreateCurryFunction(method, paramTypes, paramValues, curryFn);
            return FuncConvert.ToFastFunc(
                    (Converter<Pan.Point, T>)curryFn.CreateDelegate(typeof(Converter<Pan.Point, T>))
                );
        }

        private static void CreateCurryFunction(MethodInfo method, Type[] paramTypes, object[] paramValues, DynamicMethod curryFn)
        {
            ILGenerator il = curryFn.GetILGenerator(256);
            for (int i = 0; i < paramValues.Length; i++)
            {
                switch (paramTypes[i].ToString())
                {
                    case "System.String":
                        il.Emit(OpCodes.Ldstr, (string)paramValues[i]);
                        break;
                    case "System.Double":
                        il.Emit(OpCodes.Ldc_R8, (double)paramValues[i]);
                        break;
                    case "System.Int32":
                        il.Emit(OpCodes.Ldc_I4, (int)paramValues[i]);
                        break;
                    default:
                        throw new Exception("wrong param type");
                }
            }
            if(paramTypes.Length > paramValues.Length)  //if function takes final Point arg
                il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Call, method, null);
            il.Emit(OpCodes.Ret);
        }

    }
}
