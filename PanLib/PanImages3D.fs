/// Terry Spitz, 2001 - 2009
/// generalise Pan images to 3D points, use a marching cubes algorithm to map isosurface
module Pan3D

open System
open Terry
open System.Windows.Media.Media3D
open Pan

let Ball (p: Vector3D) = 1.0 / p.LengthSquared

let time = ("Time", 0, 20, 0)
let surface = ("Isosurface level", 0.0, 2.0, 0.8)
    
let Balls surface time (p: Vector3D) =
    let p1 = p + new Vector3D(0.0, -1.0, 0.0)
    let p2 = p + new Vector3D(0.0, 1.0, 0.0)
    let p3 = p + new Vector3D(5.0 * sin time, 0.0,0.0)
    surface * (1.0 / (p+p1).Length
            + 1.0 / (p+p2).Length
            + 1.0 / (p+p3).Length)

let applyPanTransformToXY trans (v : Vector3D) = 
    let p = trans (toPoint v.X v.Y) in
    new Vector3D(p.x, p.y, v.Z)

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Simple balls and transforms

let invradiusarg = ("Invert radius", 0.01, 100.0, 20.0)
let invert3D invradiusarg = applyPanTransformToXY (radialInvert invradiusarg)
let invertBall invradiusarg v = if Ball v <1.0 then  1.0/ (Ball (invert3D invradiusarg v)) else 0.0
let invertBalls invradiusarg surface time v = 1.0 / ((Balls surface time) (invert3D invradiusarg v))

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Caustic 
let Caustic3D time (v : Vector3D) =
    1.0/Caustic.Instance.Calc(v.X, v.Y, v.Z, time)


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Mandlebrots in 3D

let rec MandInner x y i i2 j j2 iter maxiter =
    if (iter > maxiter || i2 + j2 > 4.0)
    then (float)iter / (float)(maxiter-2)
    else
    let temp = (i2 - j2 + x)
    let temp2 = (2.0 * i * j + y) in
        MandInner x y temp temp2 (temp*temp) (temp2*temp2) (iter+1) maxiter

let maxiter = ("Max Iterations", 2, 200, 100)

let StaticMandlebrot maxiter (v : Vector3D) = 
    //crop to sphere
    if (v.LengthSquared > 4.0) 
    then 0.0
    else
    //mandlebrot, starting from k,0  using offsets i and j
(*if (shape == ShapeType.ZoomingMandlebrot)
{
    float zoom = (float)time / 30f;
    x = (((float)p.i / resolution) + .87591f * zoom) / (1f + zoom * 5f);
    y = (((float)p.j / resolution) - .3f * zoom) / (1f + zoom * 5f);
    z = ((float)p.k / resolution) / (1f + zoom * 2f);
}
else*)
    MandInner v.X v.Y v.Z (v.Z * v.Z) 0.0 0.0 1 maxiter
    
let invertMand invradiusarg maxiter v = 1.0/ StaticMandlebrot maxiter (applyPanTransformToXY (radialInvert invradiusarg) v)
    
(*if (shape == ShapeType.ResolvingMandlebrot)
{
    int iterlimit = 20;
    maxiter = ((int)(time*3)) % (iterlimit*2);
    if(maxiter > iterlimit) maxiter = (2*iterlimit)+1-maxiter;
    maxiter = maxiter + 4; // *maxiter;
}*)

//from http://www.skytopia.com/project/fractal/2mandelbulb.html#epilogue
let power = ("Power", 1, 10, 2)
let Mandlebulb power maxiter (p: Vector3D) =
    let rec MandInner (z: Vector3D) (c: Vector3D) iter =
        if (iter > maxiter || z.LengthSquared > 4.0)
        then ((float)iter / (float)(maxiter-2))
        else     
            let zPowerN n (p: Vector3D) = 
                let theta = atan2 (sqrt (p.X*p.X+p.Y*p.Y)) p.Z
                let phi = atan2 p.Y p.X
                in Math.Pow(p.Length, n) * new Vector3D( sin(theta*n) * cos(phi*n) , sin(theta*n) * sin(phi*n) , cos(theta*n) )
            in MandInner ((zPowerN power p) + c) c (iter+1)
    in MandInner (new Vector3D()) p 1


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Other shapes
(*
let Dots (v: Vector3D) =
{
    int size = 10;
    if ((float)(resolution * resolution) < (p * p))
        return 0;
    else
        return ((Math.Abs(x * size) % 2) < 1 ^ (Math.Abs(y * size) % 2) < 1 ^ (Math.Abs(z * size) % 2) < 1) ? 2 : 0;
}
case ShapeType.Squares:
{
    int size = 2;
    if (x * x + y * y + z * z > Math.Abs(3 * Math.Sin(time/6))) return 0;
    return ((Math.Abs(x * size) % 2) < 1 ^ (Math.Abs(y * size) % 2) < 1 ^ (Math.Abs(z * size) % 2) < 1) ? 2 : 0;
}
default:
return 0f;
*)