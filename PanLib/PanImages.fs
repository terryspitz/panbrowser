(* ------------------------------------ *)
(* ---  Terrys Pan impl. based on -- *)
(* - http://research.microsoft.com/~conal/pan - *)
(* ----------------------------------------- *)

module Pan

open System.Windows.Media

(* ----- UI ---- *)
//open Terry

//wrap query to User Interface (slider)

let getSlider s f t def = Terry.Sliders.Getdouble(s, f, t, def)
let getSlider2 s f t def p = Terry.Sliders.Getdouble(s, f, t, def)     //takes point to make sure compiler doesn't cache result at startup

//or stub it out like this:
//let getSlider s f t d = 1
//let getSlider2 s f t d p = 1


(* ----------- 2 What is an image? ------------- *)

type Point = { x: float; y:float }

type Image<'a> = Point -> 'a    //support bool, float or Color


let addp p1 p2 = { x=p1.x+p2.x; y=p1.y+p2.y}
let subp p1 p2 = { x=p1.x-p2.x; y=p1.y-p2.y }
let multp n p = { x=p.x*n; y=p.y*n }


(* --- bool images --- *)

let unitbox p = (p.x > -0.0 && p.x < 1.0) && (p.y > 0.0 && p.y < 1.0)
let vstrip p = (p.x > -0.5 && p.x < 0.5)
let even x = (x % 2) =0
let odd x = (x % 2) <>0
let checker p = even (int p.x + int p.y) 
let tileP p = { x= ((p.x % 1.0) + 1.0) % 1.0; y= ((p.y % 1.0)+1.0) % 1.0 }
let tile im = im << tileP

let distO p = System.Math.Sqrt(p.x*p.x + p.y*p.y)
let altRings p = even (int  (distO p))

let fromPolar (r, th) = { x=r*cos th; y= r*sin th}
let toPolar ( p: Point ) = (distO p, atan2 p.y p.x )

//type PolarPoint = ( float * float )

//let polarChecker ( n: int) ( p: Point ) = 
let polarChecker n p = 
    let sc pp = { x=fst pp ; y= snd pp * float  n / System.Math.PI }
    in checker ( sc ( toPolar p ))

let polarChecker10 = polarChecker 10
let polarCheckerN p = polarChecker (getSlider2 "radius" -20 20 10 p) p

let wavDist p = (1.0 + cos (System.Math.PI * distO p)) / 2.0

let gasket p = ((int  (p.x * 100.0)) ||| (int  (p.y * 100.0))) = int p.x

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

(*cOver : Color -> Color -> Color*)
let overC c1 c2 = 
    let h e1 e2 = e1 + (1.0 - c1.a)*e2 in
    { r= h c1.r c2.r; g= h c1.g c2.g; b= h c1.b c2.b; a= h c1.a c2.a }

(* ----------- 4 Pointwise lifting ------------- *)

let canvas a (p: Point) = a
let lift1 h f1 p = h (f1 p)
let lift2 h f1 f2 p = h (f1 p) (f2 p)
let lift3 h f1 f2 f3 p = h (f1 p) (f2 p) (f3 p)

let over = lift2 overC

let cond a b c = if a then b else c
let condC = lift3 cond
let lerpI = lift3 lerpC

let bwImage imgB = condC imgB (canvas black) (canvas white)
let byImage imgB  = condC imgB (canvas blue) (canvas yellow)

//try it
let polarChecker20 = byImage (polarChecker 20)
let bwunixbox = bwImage unitbox

let bilerpBRBW = condC unitbox (bilerpC black red blue white) (canvas white)

let ybRings = lerpI wavDist (canvas blue) (canvas yellow)
let ringChecker = lerpI wavDist (bwImage (polarCheckerN)) (byImage checker)

(* ----------- 5 Spacial Transforms ------------- *)

type Transform = Point -> Point

let identityTransform (p : Point) = p
let translateP (dx,dy) p = { x= dx + p.x; y= dy + p.y }
let translateP2 = translateP (((float)(getSlider "x" -10 10 0))/2.0, ((float)(getSlider "y" -10 10 0)/2.0) )
let scaleP (x,y) p = { x= x * p.x; y= y * p.y }
let uscaleP s = scaleP (s,s)
let rotateP th p = { x= p.x * cos th - p.y * sin th;
                     y= p.y * cos th + p.x * sin th }

type Filter<'a> = (Point -> 'a) -> (Point -> 'a)

let identityFilter (image : Point -> 'a) p = image p
let transformImage transformPoint (image : Point -> 'a) = image << transformPoint

let translate (dx,dy)   image p = image (translateP (0.0-dx,0.0-dy) p)
let translate2          image p = image (translateP2 p)
let scale (sx, sy)      image p = image (scaleP (1.0/sx, 1.0/sy) p)
let uscale s            image p = image (uscaleP (1.0/s) p)
let rotate th           image p = image (rotateP (0.0-th) p)

//try it:

let swirlP r p = rotateP (log ((distO p)+1e-6) * r / 5.0) p
let swirl r image p = image (swirlP r p)

//should be: let swirlVstrip = swirl ((float )(getSlider "swirl" -10 10 10)) vstrip )    but the compiler precaches the silder value
let swirlVstrip p = swirl ((float)(getSlider2 "swirl" -20 20 1 p)) vstrip p

let udisc p = distO p < 1.0
let tileDisc = tile (translate (0.5,0.5) (uscale 0.5 udisc))
let tilebox = tile (uscale 0.9 unitbox)


(* ----------- 9 Some Polar Transforms ------------- *)

(* some attempts at compose operator from various sources *)
//let compose f g = fun x -> f(g(x));;
//let (<<) f g x = f (g x)
//let apply f x = f x

let polarTransform xf p = (fromPolar << xf << toPolar) p

let radialInvert im = 
    let invert (r, th) = (1.0/ (r+1e-10),th ) in
    im << polarTransform invert

let radialInvertChecker = radialInvert checker
let radialInvertVStrip = radialInvert vstrip
//let radInvertChecker2 = apply radInvert checker   //alternate syntax

//flowers by terry: inspired by pic @ http://www.codeproject.com/KB/WPF/WPFJoshSmith.aspx
let flowerR (r, th) =
    let petals = (float) (getSlider2 "Petals" 1 30 5 r)
    let innerRadius = (float) (getSlider2 "Inner Radius" 0 5 1 r)
    let outerRadius = (float) (getSlider2 "Outer Radius" 0 5 2 r)
    in (r/((outerRadius-innerRadius)/2.0 * sin (th * petals) + (innerRadius + outerRadius)/2.0), th)

let flowerBool p = 
    let disc (r,th) = r<1.0
    disc (flowerR (toPolar p))
let flowerFloat p = 
    fst (flowerR (toPolar p))

let flowerTransform = polarTransform flowerR 
let flowerTileTransform = tile flowerTransform
let flowerTileDisc = tileDisc << flowerTransform 

let radToPoint (r, th) = { x=th; y=r; }
let pointToRad p = (p.x, p.y)
let radTransform p = radToPoint (toPolar p)
let flowerTile2 = tileDisc << radTransform

(* ----------- Terry Transforms ------------- *)

let rec fractalSquareRec depth p = 
    if depth=0 then false 
    else 
        if odd (int  p.x) && odd (int  p.y) then true
        //else if even (int  p.x) && even (int  p.y) then fractalSquareRec (depth-2) (uscaleP 2.0 p)
        else fractalSquareRec (depth-1) (uscaleP 2.0 p)
        
let fractalSquare p = fractalSquareRec 8 p

(* ----------- from the Pan Viewer demo ------------- *)  

let nestedSquares p = even (int  (max (abs p.x) (abs p.y) ))

  
(* ----------- Pictures and Text from Pan Samples -------------*)

let text p = (Terry.Helpers.TextPoint("tali ", p.x, p.y) )
//let text = unitbox    
let tiletext = scale (1.0, 0.5) (tile (scale (1.0, 2.0) (translate (0.0, -0.2) text)))
let inverttext = radialInvert tiletext
let textbox = condC text (canvas white) (byImage unitbox)

let circleTranformPolar polar = 
    {x=snd polar % System.Math.PI; y= - fst polar % System.Math.PI}

let circleTranform p = circleTranformPolar (toPolar p)
let circleText = transformImage circleTranform tiletext

let spiral p = (fst p + ((snd p % System.Math.PI)/System.Math.PI), snd p)
let spiralTranform p = circleTranformPolar (spiral (toPolar p))
let spiralText = transformImage spiralTranform tiletext

let swirlText p = 
    condC (swirl ((float) (getSlider2 "swirl" -10 10 10 p)) tiletext) 
        (canvas (SysColor Colors.BlueViolet)) 
        (canvas (SysColor Colors.Turquoise))
        p

let talitext = 
    tile ( translate (0.5, 0.5 )
        (condC (uscale 0.4 (transformImage circleTranform tiletext))
             (canvas black) 
             (condC udisc (canvas green) (canvas invisible)))
    )

(*let picture p = 
    let (c : float[]) = PanUI.Images.PictureImage("pic1", p.x, p.y)
    in { r=arr.get c 0; g=arr.get c 1; b=arr.get c 2; a=arr.get c 3 }
*)    
(* let radInvertPic = radInvert picture *)

(* --------------------------------------- *)
//try a brillouin 2d - by terry, jul05
(* --------------------------------------- *)

let countZones p basis1 basis2 = 
    let countWholeQuadrant q1 q2 p basis1 basis2 = 
        let rec countQuadrant p i j zones =
            if distO (multp j basis2) > 2.0 * distO p || (abs i) > 8.0 || (abs j) > 8.0 then
                zones    //bottom out
            else if (distO (multp i basis1)) > (2.0 * distO p) then
                //next row in quandrant
                countQuadrant p 0.0 (j+q2) zones
            else if (distO (subp p (addp (multp i basis1) (multp j basis2)))) < (distO p) then
                countQuadrant p (i+q1) j zones+1
            else 
                countQuadrant p (i+q1) j zones
        in 
            countQuadrant p q1 0.0 0
    in countWholeQuadrant 1.0 1.0 p basis1 basis2
        + countWholeQuadrant -1.0 1.0 p basis1 basis2
        + countWholeQuadrant 1.0 -1.0 p basis1 basis2
        + countWholeQuadrant -1.0 -1.0 p basis1 basis2

let rainbow = [| red; magenta; blue; cyan; green; yellow; //look! it's an array.
    darken 0.5 red; darken 0.5 magenta; darken 0.5 blue; darken 0.5 cyan; darken 0.5 green; darken 0.5 yellow  |]

let intToColor i = rainbow.[ i % rainbow.Length ]

let rainbowRad = 
    let r (r, th) = intToColor (int(r))
    in r << toPolar

//image
let brill p = 
    //let basis1 = { x=0.1 * float(getSlider "b1.x" 0 10); y=0.1 * float(getSlider "b1.y" 0 10); } in 
    let basis1 = { x=1.0; y=0.2; } in
    let basis2 = { x=0.0; y=1.0; } in
    let brillP p = (intToColor (countZones p basis1 basis2)) in
    condC (uscale 2.0 udisc) brillP (canvas white) p
    
(* --------------------------------------- *)
//try a mandelbrot - by terry, jul 05
(* --------------------------------------- *)

let complex2 z = 
    { x= z.x * z.x - z.y * z.y; y= 2.0 * z.x * z.y }

let origin = { x=0.0; y=0.0 }

let mandlebrot p =
    let rec checkMand z i maxiter =
        if i > maxiter then
            black
        else if (distO z) > 2.0 then
            intToColor i
        else 
            checkMand (addp (complex2 z) p) (i+1) maxiter
    in checkMand origin 0 (getSlider "Iterations" 1 200 100)
    


//dots, terry aug-2009, inspired by painting tali's room;
let dots = 
    let translateScaleColourDiscOver x y s col under = over (translate (x,y) (uscale s (condC udisc col (canvas invisible)))) under in
    tile
        (translateScaleColourDiscOver 0.2 0.2 0.10 (canvas (intToColor 1))
        (translateScaleColourDiscOver 0.75 0.3 0.20 (canvas (intToColor 2))
        (translateScaleColourDiscOver 0.1 0.8 0.05 (canvas (intToColor 3))
        (translateScaleColourDiscOver 0.2 0.3 0.05 (canvas (intToColor 4))
        (translateScaleColourDiscOver 0.35 0.2 0.20 (canvas (intToColor 5))
        (translateScaleColourDiscOver 0.6 0.6 0.10 (canvas (intToColor 6))
            (canvas white))
        )))))
        
        
