using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Windows.Media.Media3D;

namespace Terry
{
    public class PanWrapper
    {
        //FSharpFunc<Pan.Point, Pan.Color> ImageType;

        protected Dictionary<string, Type> _panTypes;

        public List<string> Images { get; set; }
        public List<string> Transforms { get; set; }
        public static readonly string None = "---none---";

        public PanWrapper()
        {
            _panTypes = new Dictionary<string, Type>();
            _panTypes[typeof(Pan).ToString()] = typeof(Pan);
            _panTypes[typeof(TerryImages).ToString()] = typeof(TerryImages);
            _panTypes[typeof(Pan3D).ToString()] = typeof(Pan3D);
            GetPanFunctions();
            Images.Sort();
            Transforms.Sort();
            Transforms.Insert(0, None);
        }

        protected static bool isPointType(Type t)
        {
            return t == typeof(Pan.Point) || t == typeof(Vector3D);
        }

        protected static bool isReturnType(Type t)
        {
            return t == typeof(Boolean) || t == typeof(Double) || t == typeof(Pan.Color);
        }

        public void GetPanFunctions()
        {
            //as of Oct09 and before refactoring i had 35 images & 18 transforms

            Images = new List<string>();
            Transforms = new List<string>();

            foreach (Type panType in _panTypes.Values)
            {
                MethodInfo[] methods = panType.GetMethods();
                foreach (MethodInfo method in methods)
                {
                    Type typeFrom = null, typeTo = null;
                    if (!GetMethodTypes(method, ref typeFrom, ref typeTo))
                        continue;  //not an image or transform

                    //Point -> bool | Double | Color | Point
                    if (isPointType(typeFrom))
                    {
                        if (isReturnType(typeTo))
                            Images.Add(stripGet(panType, method.Name));
                        else if (isPointType(typeTo))
                            Transforms.Add(stripGet(panType, method.Name));
                    }
                    //(Point -> 'a) -> (Point -> 'a)
                    else if (FSharpType.IsFunction(typeFrom)
                        && FSharpType.IsFunction(typeTo)
                        )
                    {
                        Tuple<Type, Type> types1 = FSharpType.GetFunctionElements(typeFrom);
                        Tuple<Type, Type> types2 = FSharpType.GetFunctionElements(typeTo);
                        if (isPointType(types1.Item1) && types1.Item2 == types2.Item2)
                            //e.g. tile: (Point->'a) -> (Point->'a) can't be bound at runtime
                            //Console.WriteLine("Can't deal with function " + method.Name); 
                            Transforms.Add(stripGet(panType, method.Name)); 
                            
                    }
                }
            }
        }

        private static string stripGet(Type panType, string name)
        {
            if (name.StartsWith("get_"))
                name = name.Substring(4);
            return panType.ToString()+"."+name;
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
                if (!IsSliderType(t.ParameterType))
                {
                    break;
                }
                param.RemoveAt(0);
            }

            //assume a basic function e.g. Point -> Point
            if (param.Count == 1)       
            {
                typeFrom = param[0].ParameterType;
                typeTo = method.ReturnType;
            }
            else 
                //method is returning a Function object
                if (param.Count == 0 && FSharpType.IsFunction(method.ReturnType))
            {
                Tuple<Type, Type> types = FSharpType.GetFunctionElements(method.ReturnType);
                typeFrom = types.Item1;
                typeTo = types.Item2;
            }
            else
                //e.g. (Point -> Col) -> Point -> Col
                if (param.Count == 2 && FSharpType.IsFunction(param[0].ParameterType)
                && isPointType(param[1].ParameterType)
                && isReturnType(method.ReturnType))
            {
                Tuple<Type, Type> types = FSharpType.GetFunctionElements(param[0].ParameterType);
                if (types.Item1 == param[1].ParameterType && types.Item2 == method.ReturnType)
                {
                    typeFrom = param[0].ParameterType;
                    typeTo = typeFrom;
                }
                else
                    return false;
            }
            else
            {
                Console.WriteLine("Can't deal with function " + method.ToString());
                return false;
            }

            return true;
        }

        private static bool IsSliderType(Type t)
        {
            return t == typeof(bool) || t == typeof(double) ||
                                t == typeof(int) || t == typeof(string);
        }

        public IList<SliderAttribute> GetSliders(string image)
        {
            List<SliderAttribute> sliders = new List<SliderAttribute>();

            var ret = GetMethod(image);
            Type panType = ret.Item1;
            MethodInfo fn = ret.Item2;

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
                PropertyInfo hint = panType.GetProperty(t.Name);
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
                            Tuple<string, int, int, int> ituple = hint.GetGetMethod().Invoke(null, null) as Tuple<string, int, int, int>;
                            if (ituple != null)
                            {
                                sliders.Add(new SliderDouble(ituple.Item1, ituple.Item2, ituple.Item3, ituple.Item4));
                                break;
                            }

                        }
                        sliders.Add(new SliderDouble(t.Name, -2, +2, 0));
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
                            Tuple<string, double, double, double> dtuple = hint.GetGetMethod().Invoke(null, null) as Tuple<string, double, double, double>;
                            if (dtuple != null)
                            {
                                sliders.Add(new SliderInt(dtuple.Item1, (int)dtuple.Item2, (int)dtuple.Item3, (int)dtuple.Item4));
                                break;
                            }
                        }
                        sliders.Add(new SliderInt(t.Name, -10, +10, 1));
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
        /// FSharpFunc<Point, Color> funcImage()
        /// 
        /// example images needing binding are:
        /// Color image(double, Point)
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="sliders"></param>
        /// <returns></returns>
        //public FSharpFunc<PointType, ReturnType> GetImageFunction<PointType, ReturnType>(string image, IList<SliderAttribute> sliders)
        public object GetImageFunction(string image, IList<SliderAttribute> sliders)
        {
            //FSharpFunc<PointType, ReturnType> imageFunction = null;
            Type typeFrom = null, typeTo = null;

            var ret = GetMethod(image);
            Type panType = ret.Item1;
            MethodInfo method = ret.Item2;
            if(method==null)
                throw new Exception("image " + image + " not found");  //not an image or transform

            List<ParameterInfo> param;
            List<object> paramValues;
            List<Type> paramTypes;
            GetParameters(sliders, method.GetParameters(), out param, out paramValues, out paramTypes);

            if (param.Count == 1)       //assume a basic function e.g. Point -> Color
            {
                if (!isPointType(param[0].ParameterType))
                    throw new Exception("image " + image + " wrong input type");
                paramTypes.Add(param[0].ParameterType);

                if (param[0].ParameterType == typeof(Pan.Point) && method.ReturnType == typeof(Boolean))
                    return DrawImage.boolToColImage(Curry<Pan.Point, bool>(method, paramTypes.ToArray(), paramValues.ToArray()));
                else if (param[0].ParameterType == typeof(Vector3D) && method.ReturnType == typeof(Boolean))
                    return Curry<Vector3D, bool>(method, paramTypes.ToArray(), paramValues.ToArray());
                else if (param[0].ParameterType == typeof(Pan.Point) && method.ReturnType == typeof(Double))
                    return DrawImage.doubleToColImage(Curry<Pan.Point, double>(method, paramTypes.ToArray(), paramValues.ToArray()));
                else if (param[0].ParameterType == typeof(Vector3D) && method.ReturnType == typeof(Double))
                    return Curry<Vector3D, double>(method, paramTypes.ToArray(), paramValues.ToArray());
                else if (param[0].ParameterType == typeof(Pan.Point) && method.ReturnType == typeof(Pan.Color))
                    return Curry<Pan.Point, Pan.Color>(method, paramTypes.ToArray(), paramValues.ToArray());
                else 
                    throw new Exception("image " + image + " wrong type");
            }
            else if (param.Count == 0
                && FSharpType.IsFunction(method.ReturnType))    //method is returning a Function object
            {
                Tuple<Type, Type> types = FSharpType.GetFunctionElements(method.ReturnType);
                typeFrom = types.Item1;
                typeTo = types.Item2;

                //Point -> bool | Double | Color | Point
                if (typeFrom == typeof(Pan.Point) && typeTo == typeof(bool))
                    return DrawImage.boolToColImage((FSharpFunc<Pan.Point, bool>)method.Invoke(null, paramValues.ToArray()));
                else if (typeFrom == typeof(Pan.Point) && typeTo == typeof(double))
                    return DrawImage.doubleToColImage((FSharpFunc<Pan.Point, double>)method.Invoke(null, paramValues.ToArray()));
                else if (typeFrom == typeof(Pan.Point) && typeTo == typeof(Pan.Color))
                    return (FSharpFunc<Pan.Point, Pan.Color>)method.Invoke(null, paramValues.ToArray());
                else
                    throw new Exception("image " + image + " wrong type");
            }
            throw new Exception("image " + image + " not found");  //not an image or transform
        }

        private static void GetParameters(IList<SliderAttribute> sliders, ParameterInfo[] paramArray, out List<ParameterInfo> param, out List<object> paramValues, out List<Type> paramTypes)
        {
            param = new List<ParameterInfo>();
            param.AddRange(paramArray);
            paramValues = new List<object>();
            paramTypes = new List<Type>();
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
        }

        private Tuple<Type, MethodInfo> GetMethod(string name)
        {
            Type panType = _panTypes[name.Substring(0, name.IndexOf('.'))];
            MethodInfo fn;
            //try property getter first
            fn = panType.GetMethod("get_" + name.Substring(name.IndexOf('.')+1));
            if (fn == null && name != null)
                fn = panType.GetMethod(name.Substring(name.IndexOf('.')+1));
            return new Tuple<Type, MethodInfo>(panType, fn);
        }

        /// <summary>
        /// Returns a function that implements the named transform applied to the input image 
        /// binding params to control/sliders if necessary
        /// 
        /// example function types include:
        /// Point simpleTransform(Point) 
        /// FSharpFunc<Point, Point> pointTransform()
        /// Color funcTranform(double s, FSharpFunc<Point, Color> image, Point p)
        /// 
        /// example function types needing param binding include:
        /// Point pointTransform1(double, Point)
        /// Point pointTransform2(double, double, Point)
        /// FSharpFunc<Point, Point> pointTransform3(double)
        /// FSharpFunc<Point, Point> pointTransform4(double, double)
        /// Color funcTranform(double s, FSharpFunc<Point, Color> image, Point p)

        /// </summary>
        /// <param name="transform"></param>
        /// <param name="image"></param>
        /// <param name="sliders"></param>
        /// <returns></returns>
        public FSharpFunc<Pan.Point, Pan.Color> GetTransformFunction(string transform, FSharpFunc<Pan.Point, Pan.Color> image, IList<SliderAttribute> sliders)
        {
            if (string.IsNullOrEmpty(transform) || transform == None)
                return image;

            FSharpFunc<Pan.Point, Pan.Color> imageFunction = null;

            var ret = GetMethod(transform);
            Type panType = ret.Item1;
            MethodInfo method = ret.Item2;
            if (method == null)
                throw new Exception("transform " + transform + " not found");  //not an image or transform

            List<ParameterInfo> param;
            List<object> paramValues;
            List<Type> paramTypes;
            GetParameters(sliders, method.GetParameters(), out param, out paramValues, out paramTypes);

            //a basic function e.g. Point -> Point
            if (param.Count == 1 &&
                param[0].ParameterType == typeof(Pan.Point) && 
                method.ReturnType == typeof(Pan.Point)
                )
            {
                paramTypes.Add(param[0].ParameterType);
                imageFunction = Pan.transformImage(Curry<Pan.Point, Pan.Point>(method, paramTypes.ToArray(), paramValues.ToArray()), image);
            }
            else 
                //method is returning a Function object (Point->Point)
                if (param.Count == 0 &&
                    FSharpType.IsFunction(method.ReturnType)
                    )
            {
                Tuple<Type, Type> types = FSharpType.GetFunctionElements(method.ReturnType);
                if (types.Item1 != typeof(Pan.Point) || types.Item2 != typeof(Pan.Point))
                    throw new Exception("transform " + transform + " wrong type");  //not an image or transform
                imageFunction = Pan.transformImage((FSharpFunc<Pan.Point, Pan.Point>)method.Invoke(null, paramValues.ToArray()), image);
            }
            else
                //'Real' higher-order transform function: (Point -> Col) -> Point -> Col
                if (param.Count == 2 && FSharpType.IsFunction(param[0].ParameterType)
                && isPointType(param[1].ParameterType)
                && isReturnType(method.ReturnType) || method.ReturnType.IsGenericParameter
                    )
                {
                    Tuple<Type, Type> types = FSharpType.GetFunctionElements(param[0].ParameterType);
                    Debug.Assert(types.Item1 == param[1].ParameterType && types.Item2 == method.ReturnType);
                    //paramTypes.Add(param[0].ParameterType);
                    paramValues.Add(image);
                    if (param[1].ParameterType == typeof(Pan.Point) && method.ReturnType == typeof(Boolean))
                        imageFunction = DrawImage.boolToColImage(new BadCurry<Pan.Point, bool>(method, paramValues.ToArray()));
                    else if (param[1].ParameterType == typeof(Pan.Point) && method.ReturnType == typeof(Double))
                        imageFunction = DrawImage.doubleToColImage(new BadCurry<Pan.Point, Double>(method, paramValues.ToArray()));
                    else if (param[1].ParameterType == typeof(Pan.Point) && method.ReturnType == typeof(Pan.Color))
                        imageFunction = new BadCurry<Pan.Point, Pan.Color>(method, paramValues.ToArray());
                    else if (param[1].ParameterType == typeof(Pan.Point) && method.ReturnType.IsGenericParameter)
                        imageFunction = new BadCurry<Pan.Point, Pan.Color>(method.MakeGenericMethod(typeof(Pan.Color)), paramValues.ToArray());
                }
                else
                    //'Real' higher-order transform function returning Function (Point -> Col) -> (Point -> Col)
                    if (param.Count == 1 && 
                        FSharpType.IsFunction(param[0].ParameterType) &&
                        FSharpType.IsFunction(method.ReturnType)
                        )
                    {
                        Tuple<Type, Type> typesIn = FSharpType.GetFunctionElements(param[0].ParameterType);
                        Debug.Assert(isPointType(typesIn.Item1) && isReturnType(typesIn.Item2) || typesIn.Item2.IsGenericParameter);
                        Tuple<Type, Type> typesRet = FSharpType.GetFunctionElements(param[0].ParameterType);
                        Debug.Assert(isPointType(typesRet.Item1) && isReturnType(typesRet.Item2) || typesIn.Item2.IsGenericParameter);
                        paramValues.Add(image);
                        if (typesIn.Item1 == typeof(Pan.Point) && typesIn.Item2 == typeof(Boolean))
                            imageFunction = DrawImage.boolToColImage((FSharpFunc<Pan.Point, bool>)method.Invoke(null, paramValues.ToArray()));
                        else if (typesIn.Item1 == typeof(Pan.Point) && typesIn.Item2 == typeof(Double))
                            imageFunction = DrawImage.doubleToColImage((FSharpFunc<Pan.Point, Double>)method.Invoke(null, paramValues.ToArray()));
                        else if (typesIn.Item1 == typeof(Pan.Point) && typesIn.Item2 == typeof(Pan.Color))
                            imageFunction = (FSharpFunc<Pan.Point, Pan.Color>)method.Invoke(null, paramValues.ToArray());
                        else if (typesIn.Item1 == typeof(Pan.Point) && typesIn.Item2.IsGenericParameter)
                            imageFunction = (FSharpFunc<Pan.Point, Pan.Color>)method.MakeGenericMethod(typeof(Pan.Color)).Invoke(null, paramValues.ToArray());
                    }
            /*
            try
                {
                    MethodInfo genericMethodInfo = method .MakeGenericMethod(new Type[] { typeof(Pan.Color) });
                    object newImageFn = genericMethodInfo.Invoke(null, new object[] { imageFunction });
                    imageFunction = newImageFn as FSharpFunc<PointType, Pan.Color>;
                }
                catch (Exception)
                {
                }
            }*/
            return imageFunction;
        }

        /// <summary>
        /// Curries a function with known arguments
        /// <example>
        /// given a function 
        /// Color textH(string, Point)
        /// then
        /// Curry(textH, { String }, { "hello "))
        /// returns an F# FSharpFunc&lt;Point, Color&gt; which calls testH with first arg "hello "
        /// </example>
        /// </summary>
        /// <param name="method"></param>
        /// <param name="paramTypes"></param>
        /// <param name="paramValues"></param>
        /// <returns></returns>
        protected static FSharpFunc<FromType, ToType> Curry<FromType, ToType>(MethodInfo method, Type[] paramTypes, object[] paramValues)
        {
            if(paramTypes.Length==1)
                return (Converter<FromType, ToType>)Delegate.CreateDelegate(typeof(Converter<FromType, ToType>), method);

            DynamicMethod curryFn = new DynamicMethod("curryFn", typeof(ToType), new Type[] { typeof(FromType) });
            CreateCurryFunction(method, paramTypes, paramValues, curryFn);
            return FuncConvert.ToFSharpFunc(
                    (Converter<FromType, ToType>)curryFn.CreateDelegate(typeof(Converter<FromType, ToType>))
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
                        //il.Emit(OpCodes.Ldobj, paramValues[i]);
                        throw new Exception("wrong param type");
                }
            }
            if(paramTypes.Length > paramValues.Length)  //if function takes final Point arg
                il.Emit(OpCodes.Ldarg_0);
            il.EmitCall(OpCodes.Call, method, null);
            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// (Slightly slower?) curry method
        /// worth checking timings versus slick function above, it might be pretty good!
        /// please tell me F# OptimizedClosures don't do this already.
        /// </summary>
        /// <typeparam name="FromType"></typeparam>
        /// <typeparam name="ToType"></typeparam>
        class BadCurry<FromType, ToType> : FSharpFunc<FromType, ToType>
        {
            public BadCurry(MethodInfo m, object[] paramValues)
            {
                myMethod = m;
                //create bigger params list with space for final value
                myParams = new object[paramValues.Length + 1];
                paramValues.CopyTo(myParams, 0);
            }

            public override ToType Invoke(FromType from)
            {
                myParams.SetValue(from, myParams.Length-1);
                return (ToType)myMethod.Invoke(null, myParams);
            }

            private MethodInfo myMethod;
            private object[] myParams;
        }

        internal FSharpFunc<Pan.Point, Pan.Color> StandardTransforms(FSharpFunc<Pan.Point, Pan.Color> image, double size, double rotation, double x, double y)
        {
            //image = Pan.transformImage(Pan.translateP(x / 2, y / 2), image);
            image = DrawImage.scale(size, image);
            image = DrawImage.rotate(rotation, image);
            image = DrawImage.translate(x / 2, y / 2, image);
            return image;
        }
    }
}
