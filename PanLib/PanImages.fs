(* ------------------------------------ *)
(* ---  Terrys Pan impl. based on -- *)
(* - http://research.microsoft.com/~conal/pan - *)
(* ----------------------------------------- *)

module Pan

open System
open Terry
open System.Windows.Media

(* ----------- 2 What is an image? ------------- *)

type Point = { x: float; y:float }

type Image<'a> = Point -> 'a    //support bool, float or Color

let toPoint xx yy = { x=xx; y=yy; }
let addp p1 p2 = { x=p1.x+p2.x; y=p1.y+p2.y}
let subp p1 p2 = { x=p1.x-p2.x; y=p1.y-p2.y }

(* --- bool images --- *)

let unitbox p = (p.x > -0.0 && p.x < 1.0) && (p.y > 0.0 && p.y < 1.0)
let vstrip p = (p.x > -0.5 && p.x < 0.5)
let mapToRange x r = ((x % r) + r) % r;     //like x modulo r but get negatives in range [0,r] too
let mapToRangeI x r = ((x % r) + r) % r;     //not sure why the one above isn't generic
let mapTo01 x = mapToRange x 1.0
let even x = x%2 =0
let evenF x = (mapToRange x 2.0) <1.0
let odd x = (x % 2) <>0
let checker p = even (int (floor p.x) + int (floor p.y))
let stripes p = evenF p.x
let tileP p = { x= mapTo01 p.x; y= mapTo01 p.y }
let tile im = im << tileP

let tileSmoothP p = 
    let map x = if (even (int x)) then mapTo01 x else 1.0 - mapTo01 x
    { x= map p.x; y=map p.y }

let distO p = System.Math.Sqrt(p.x*p.x + p.y*p.y)
let altRings p = even (int  (distO p))

let fromPolar (r, th) = { x=r*sin th; y= r*cos th}
let toPolar ( p: Point ) = (distO p, atan2 p.x p.y )

//type PolarPoint = ( float * float )

//let polarChecker ( n: int) ( p: Point ) = 
let polarChecker petals p = 
    let sc pp = { x=fst pp ; y= snd pp * float petals / System.Math.PI }
    in checker ( sc ( toPolar p ))

let wavyRings p = (1.0 + cos (System.Math.PI * distO p)) / 2.0
let wavyStripes p = (1.0 + cos (System.Math.PI * p.x)) / 2.0
let wavyStripes2 p = cos (System.Math.PI * p.x)

let gasket p = 
    let x= int (p.x*50.0)
    let y= int (p.y*50.0)
    in (x ||| y) = x

(* ----------- 3 Colours ------------- *)

type Color = { r: float; g: float; b: float; a: float }
type ImageC = Point -> Color

let SysColor (c : System.Windows.Media.Color) = 
    let bf (b:byte) = (float)b/255.0 in
    { r=bf c.R; g=bf c.G; b=bf c.B; a=bf c.A }

let invisible = { r=0.0; g=0.0; b=0.0; a=0.0 }
let black = { r=0.0; g=0.0; b=0.0; a=1.0 }
let red = { r=1.0; g=0.0; b=0.0; a=1.0 }
let green = { r=0.0; g=1.0; b=0.0; a=1.0 }
let nicegreen = { r=0.5; g=0.84; b=0.21; a=1.0 }    //7ed837
let blue = { r=0.0; g=0.0; b=1.0; a=1.0 }
let yellow = { r=1.0; g=1.0; b=0.0; a=1.0 }
let magenta = { r=1.0; g=0.0; b=1.0; a=1.0 }
let cyan = { r=0.0; g=1.0; b=1.0; a=1.0 }
let white = { r=1.0; g=1.0; b=1.0; a=1.0 }

(* lerpC: float -> Color -> Color -> Color*)
let lerpC w (c1: Color) (c2: Color) = 
    let h e1 e2 = w*e1 + (1.0 - w)*e2 in
    {    r= h c1.r c2.r; 
        g= h c1.g c2.g; 
        b= h c1.b c2.b; 
        a= h c1.a c2.a }
            
let lighten x c = lerpC x c white
let darken x c = lerpC x c black

let bilerpC ll lr ul ur p = lerpC p.y (lerpC p.x ll lr) (lerpC p.x ul ur)

//cOver : Color -> Color -> Color
let overwriteC c1 c2 = 
    let h e1 e2 = e1 + (1.0 - c1.a)*e2 in
    { r= h c1.r c2.r; g= h c1.g c2.g; b= h c1.b c2.b; a= h c1.a c2.a }

(* ----------- 4 Pointwise lifting ------------- *)

let canvas a (p: Point) = a
let lift1 h f1 p = h (f1 p)
let lift2 h f1 f2 p = h (f1 p) (f2 p)
let lift3 h f1 f2 f3 p = h (f1 p) (f2 p) (f3 p)

let cond a b c = if a then b else c
let condC = lift3 cond
let lerpI = lift3 lerpC

let bwImage imgB = condC imgB (canvas black) (canvas white)
let byImage imgB  = condC imgB (canvas blue) (canvas yellow)

//try it like this
//let bwunixbox = bwImage unitbox

let bilerpBRBW = condC unitbox (bilerpC black red blue white) (canvas white)

//let ybRings = lerpI wavDist (canvas blue) (canvas yellow)
let ringChecker radiusArg = lerpI wavyRings (bwImage (polarChecker radiusArg)) (byImage checker)

(* ----------- 5 Spacial Transforms ------------- *)

type TransformP = Point -> Point
type TransformC = (Point -> Color) -> (Point -> Color) 

let identityTransform (p : Point) = p
let translateP dx dy p = { x= dx + p.x; y= dy + p.y }
let scalePXY sx sy p = { x= sx * p.x; y= sy * p.y }
let scaleP s p = { x= s * p.x; y= s * p.y }
let rotateP th p = { x= p.x * cos th - p.y * sin th;
                     y= p.y * cos th + p.x * sin th }

//sliders for the above
let dx = ("Translate x", -5.0, 5.0, 0.0)
let dy = ("Translate y", -5.0, 5.0, 0.0)
let sx = ("Scale x", -5.0, 5.0, 1.0)
let sy = ("Scale y", -5.0, 5.0, 1.0)
let s = ("Scale", -5.0, 5.0, 1.0)
let th = ("Rotation", -2.0, 2.0, 0.0)
                        
type ImageTranform<'a> = (Point -> 'a) -> (Point -> 'a)

//let identityImageTransform (image : Point -> 'a) p = image p
//let transformImage (pointTransform : Transform) (image : Image<'a>) = image << pointTransform 
let transformImage (pointTransform : Point -> Point) (image : Point -> 'a) = image << pointTransform 

//really want 
let applyPointTransform (transform : ('a -> 'a)) (image : ('a -> 'c)) a = image (transform a)
let applyImageTransform (transform : ('a -> 'b) -> ('a -> 'b)) (image : ('a -> 'b)) = transform image

let translate dx dy     image p = image (translateP -dx -dy p)
let scaleXY sx sy         image p = image (scalePXY (1.0/sx) (1.0/sy) p)
let scale s            image p = image (scaleP (1.0/s) p)
let rotate deg           image p = image (rotateP -(deg/360.0*2.0*Math.PI) p)

//try it:

let swirlP r p = rotateP (log ((distO p)+1e-6) * r / 5.0) p

let swirl swirlArg image p = image (swirlP swirlArg p)
let swirlT r = transformImage (fun p -> rotateP (log ((distO p)+1e-6) * r / 5.0) p)
let swirl2 swirlArg = applyPointTransform (fun p -> rotateP ((exp -(pown (distO p) 1)) * (swirlArg/ (float)5.0)) p)

//should be: let swirlVstrip = swirl ((float )(getSlider "swirl" -10 10 10)) vstrip )    but the compiler precaches the silder value
let swirlArg = ("Swirl",-20.0, 20.0, 1.0)
let swirlVstrip swirlArg = swirl swirlArg vstrip 

let unitCircle p = distO p < 1.0
let unitboxCentered = translate -0.5 -0.5 unitbox
let tileDisc = tile (translate 0.5 0.5 (scale 0.5 unitCircle))
let tilebox = tile (scale 0.9 unitbox)


(* ----------- 9 Some Polar Transforms ------------- *)

(* some attempts at compose operator from various sources *)
//let compose f g = fun x -> f(g(x));;
//let (<<) f g x = f (g x)
//let apply f x = f x

let polarTransform xf = toPolar >> xf >> fromPolar
let polarIdentity = polarTransform (fun (r,th) -> (r,th))

let radialInvert invradiusarg = 
    let invert (r, th) = (invradiusarg/ (r+1e-10),th ) in
    polarTransform invert

let invradiusarg = ("Invert radius", 0.01, 10.0, 1.0)

//let radialInvertT = transformImage radialInvertP
//let radialInvertChecker = radialInvert checker
//let radialInvertVStrip = radialInvert vstrip

//flowers by terry: inspired by pic @ http://www.codeproject.com/KB/WPF/WPFJoshSmith.aspx
let petals = ("Petals", 1, 30, 5)
let innerRadius = ("Inner Radius", 0, 5, 1)
let outerRadius = ("Outer Radius", 0, 5, 2)

let flowerR petals innerRadius outerRadius (r, th) =
    (r/((outerRadius-innerRadius)/2.0 * sin (th * (float)petals) + (innerRadius + outerRadius)/2.0), th)

let flowerBool petals innerRadius outerRadius p = 
    let disc (r,th) = r<1.0 in
    disc (flowerR petals innerRadius outerRadius  (toPolar p))
let flowerFloat petals innerRadius outerRadius p = 
    fst (flowerR petals innerRadius outerRadius (toPolar p))

let flowerTransform petals innerRadius outerRadius = polarTransform (flowerR petals innerRadius outerRadius )
let flowerTileTransform petals innerRadius outerRadius = tile (flowerTransform petals innerRadius outerRadius)
let flowerTileDisc petals innerRadius outerRadius = tileDisc << (flowerTransform petals innerRadius outerRadius )

let radToPoint (r, th) = { x=th; y=r; }
let pointToRad p = (p.x, p.y)
let radTransform p = radToPoint (toPolar p)
let flowerTile2 = tileDisc << radTransform

(* ----------- from the Pan Viewer demo ------------- *)  

let nestedSquares p = even (int  (max (abs p.x) (abs p.y) ))
let nestedLogSquares p = even (int  (log (max (abs p.x) (abs p.y) )))

  
(* ----------- Pictures and Text from Pan Samples -------------*)

let text textarg p = (Terry.Helpers.TextPoint(textarg, p.x, p.y) )
let textarg = ("Text", "pan rules ok...")

let tiletext textarg = 
    //scaleXY 1.0 0.5 
    (tile (scaleXY 1.0 2.0 
    //(translate 0.0 -0.2 
    (text textarg)
    //)
    ))
//let inverttext = radialInvert tiletext
let textbox textarg = condC (text textarg) (canvas white) (byImage unitbox)

let circleTranformPolar polar = 
    {x=snd polar % System.Math.PI; y= fst polar % System.Math.PI}

let circleTranform p = circleTranformPolar (toPolar p)
//let circleText = transformImage circleTranform tiletext

let spiral swirlArg p = (fst p + (((swirlArg * snd p) % System.Math.PI)/System.Math.PI), snd p)
let spiralTranform swirlArg p = circleTranformPolar (spiral swirlArg (toPolar p))
//let spiralText = transformImage spiralTranform tiletext

let swirlText swirlArg textarg = 
    swirl swirlArg (tiletext textarg) 

let textCircles textarg = 
    tile ( translate 0.5 0.5
        (condC (scale 0.4 (transformImage circleTranform (tiletext textarg)))
             (canvas black) 
             (condC unitCircle (canvas green) (canvas invisible)))
    )

(*let picture p = 
    let (c : float[]) = PanUI.Images.PictureImage("pic1", p.x, p.y)
    in { r=arr.get c 0; g=arr.get c 1; b=arr.get c 2; a=arr.get c 3 }
*)    
(* let radInvertPic = radInvert picture *)

