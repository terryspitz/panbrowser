
module TerryImages

open Pan

open System
open Terry
open System.Windows.Media

let rainbow = [| red; magenta; blue; cyan; green; yellow; //look! it's an array.
    darken 0.5 red; darken 0.5 magenta; darken 0.5 blue; darken 0.5 cyan; darken 0.5 green; darken 0.5 yellow  |]

let intToRainbow i = rainbow.[ i % rainbow.Length ]

let rainbowCircles = 
    let r (r, th) = intToRainbow (int(r))
    in r << toPolar

let rainbowSquares p = intToRainbow (int  (max (abs p.x) (abs p.y) ))
        

//------------------------------------------------------------
//try a brillouin 2d - by terry, jul05
// don't understand how this works any more !
//------------------------------------------------------------

let countZones p basis1 basis2 maxzones = 
    let countWholeQuadrant q1 q2 p basis1 basis2 = 
        let rec countQuadrant p i j zones =
            if distO (scaleP j basis2) > 2.0 * distO p || (abs i) > 8.0 || (abs j) > 8.0 || zones > maxzones then
                zones    //bottom out
            else if (distO (scaleP i basis1)) > (2.0 * distO p) then
                //next row in quandrant
                countQuadrant p 0.0 (j+q2) zones
            else if (distO (subp p (addp (scaleP i basis1) (scaleP j basis2)))) < (distO p) then
                countQuadrant p (i+q1) j zones+1
            else 
                countQuadrant p (i+q1) j zones
        in 
            countQuadrant p q1 0.0 0
    in countWholeQuadrant 1.0 1.0 p basis1 basis2 
        + countWholeQuadrant -1.0 1.0 p basis1 basis2 
        + countWholeQuadrant 1.0 -1.0 p basis1 basis2 
        + countWholeQuadrant -1.0 -1.0 p basis1 basis2 


let basisx = ("Basis X", 0.0, 5.0, 1.0)
let basisy = ("Basis Y", 0.0, 5.0, 0.0)
let maxzones = ("Zones", 1, 20, 10)

let brill basisx basisy maxzones p = 
    let basis1 = { x=basisx; y=basisy; } in
    let basis2 = { x=0.0; y=1.0; } in
    let brillP p = (intToRainbow (countZones p basis1 basis2 maxzones)) in
    condC (scale 2.0 unitCircle) brillP (canvas white) p

    
//------------------------------------------------------------
//try a mandelbrot - by terry, jul 05
//------------------------------------------------------------

let complex2 z = 
    { x= z.x * z.x - z.y * z.y; y= 2.0 * z.x * z.y }

let origin = { x=0.0; y=0.0 }

let iterations = ("Iterations", 1, 300, 100)

let mandlebrot xx yy iterations p =
    let rec checkMand z i =
        if i > iterations then
            black
        else if (distO z) > 2.0 then
            intToRainbow i
        else 
            checkMand (addp (complex2 z) p) (i+1) 
    in checkMand {x=xx; y=yy} 0 

//------------------------------------------------------------
//dots, terry aug-2009, inspired by painting tali's room
//------------------------------------------------------------
let tileInColour boolIm p = 
    if (boolIm (Pan.tileOrigin p)) then 
        {r=mapTo01 (floor(p.x)/3.0); g=mapTo01 (floor(p.y)/5.0); b=mapTo01 (floor(p.x)/7.0+floor(p.y)/9.0); a= 1.0 } else
        white
    
let dots = tileInColour Pan.unitCircle
        
(* ----------- other images ------------- *)

let fractalSquare p = 
    let rec fractalSquareRec depth p = 
        if depth=0 then 
            false 
        else 
            if odd (int  p.x) && odd (int  p.y) then 
                true
            //else if even (int  p.x) && even (int  p.y) then fractalSquareRec (depth-2) (uscaleP 2.0 p)
            else fractalSquareRec (depth-1) (scaleP 2.0 p)
    fractalSquareRec 8 p
        
        
//clastic functions

let herringblock p = 
    let toXlessthan2 p = let xx = mapToRange p.x 2.0 in { x=xx; y=p.y-(p.x - xx)}
    let toYlessthan4 p = { x=p.x; y=mapToRange p.y 4.0}
    in
        let pp = toYlessthan4 (toXlessthan2 p) 
        in
        let ppp =
            if pp.x<1.0 && pp.y>3.0 then {x=pp.x+1.0; y=pp.y-3.0} else 
            if pp.x>1.0 && pp.y>1.0 then {x=pp.x-1.0; y=pp.y-1.0} else pp
        in 
            if ppp.x<1.0 && ppp.y>1.0 then {x=ppp.y-1.0; y=1.0-ppp.x} else ppp

let herringbone p =
    let block p = p.x<0.1 || p.x >1.9 || p.y<0.1 || p.y>0.9 
    in
    block (herringblock p)
    

let swirlArg = ("Swirl",-5.0, 15.0, 5.0)
let dx = ("dx",-20.0, 20.0, 1.0)
let dy = ("dy",-20.0, 20.0, 5.0)
let height = ("Height",-5.0, 20.0, 10.0)
let mean = ("swirl radius",-3.0, 3.0, 0.0)
let points = ("star points", 1, 10, 3)
let startAng = ("start angle",-3.15, 3.15, 0.0)
let var = ("width",0.1, 20.0, 1.0)

let gaussian x = exp -(x*x/2.0)
let gaussian2 mean var x = gaussian (x-mean)/var
let bump height pp = 
    let f = pp.y / (height * (gaussian2 0.0 1.0 pp.x))
    in if f>1.0 then 0.0 else 1.0/(1.0-f)

let vortexP swirlArg mean p = rotateP (swirlArg * (gaussian2 mean 1.0 (distO p))) p
let vortex swirlArg mean image p = image (vortexP swirlArg mean p)

let twopi = 2.0 * System.Math.PI

let showAngle startAng p = 
    let (r, th) = toPolar p
    in th<startAng

let starP (points:int) startAng = polarTransform (fun (r, th) -> (r, (mapToRange th (twopi/(float)points)-startAng) ) )

let star2 points startAng image = 
    lerpI (canvas 0.5)
        (transformImage (starP points startAng) image) 
        (lerpI (canvas 0.5)
                (applyPointTransform (starP points (startAng+twopi/(float)points)) image)
                (applyPointTransform (starP points (startAng-twopi/(float)points)) image)
        )


let bumpSwirl swirlArg mean dx dy height =
    (scaleXY 0.1 0.1
    (translate dx dy
    (vortex swirlArg mean
    (translate -dx -dy
    (bump height)
    ))))
    
    
let testfn = star2
    
let blur image = 
    lerpI (canvas 0.5) image (
        lerpI (canvas 0.5) (translate 0.0 0.1 image) (translate 0.1 0.0 image)
    )
    
//caustic transform
let time = ("Time", 0.0, 20.0, 0.0)
let prescale = ("Prescale", 0.0, 5.0, 1.0)
let postscale = ("Postscale", 0.0, 1.0, 0.2)
let bumpArg = ("Bump", 0.0, 0.5, 0.1)

let caustic time prescale postscale bumpArg 
    c01 c10 c11
    (p : Point) = 
    let speed = 0.02
    let wave i j x y = cos (Math.PI * 2.0 * ((x + speed * time) * (double)i + (y + speed * time) * (double)j))
    let WaveHeight x y = 
        (c01+0.5) * (wave 0 1 x y)
        + (c10+0.5) * (wave 1 0 x y)
        + (c11+0.5) * (wave 1 1 x y)
    let dx = postscale * (WaveHeight (p.x * prescale) (p.y * prescale) - WaveHeight ((p.x + bumpArg) * prescale) (p.y * prescale) )
    let dy = postscale * (WaveHeight (p.x * prescale) (p.y * prescale) - WaveHeight (p.x*prescale) ((p.y+bumpArg) * prescale))
    in
        { x=p.x+dx; y=p.y+dy }

//other stuff
    
let timestable p = 
    let i = (int)(p.x*5.0) + Math.Sign(p.x)
    let j = (int)(p.y*5.0) + Math.Sign(p.y)
    let xx = mapTo01(p.x*5.0)
    let yy = mapTo01(p.y*5.0)
    text ((i*j).ToString()) { x=xx; y=yy; }

//escher (hyperbolic?) transform 
//from http://blogs.msdn.com/b/satnam_singh/archive/2010/01/06/an-f-functional-geometry-description-of-escher-s-fish.aspx
//where panbrowser gets a mention!

let escherSquare p =
    let xx = log (1.0 / (2.0-abs p.x*1.5)) / log 2.0
    let yy = log (1.0 / (2.0-abs p.y*1.5)) / log 2.0
    in
    { x= mapTo01 xx; y= mapTo01 yy }

let cycle p =
    if(p.x>0.0) then 
        if(p.y>0.0) then 
            p
        else
            { x = -p.y; y= p.x }  
    else
        if(p.y>0.0) then
            { x = p.y; y= -p.x }  
        else
            { x = -p.x; y= -p.y }  

type Line = { origin: Point; dir: Point }


let triangle p =
    let inhalfplane line p = (dot (subp p line.origin) (perp line.dir)) > 0.0
    let p1={x=0.25; y=0.25}
    let p2={x=0.75; y=0.25}
    let p3={x=0.5; y=0.75}
    let toline p1 p2 = { origin=p1; dir= subp p1 p2 }
    let line1 = toline p1 p2
    let line2 = toline p2 p3
    let line3 = toline p3 p1
    in
        inhalfplane line1 p &&
        inhalfplane line2 p &&
        inhalfplane line3 p 

let widtharg = ("line width", 0.0, 30.0, 15.0) 
let aerotextarg = ("aero text", "aerotali") 
///inspired by aeropostale decor: combine swirly lines crossing and not cross background text
let aero aerotextarg widtharg swirlArg mean = 
    let lines p = ((abs p.y * widtharg) + widtharg % 1.0) < 0.1 
    in lift3 cond 
        (vortex swirlArg mean lines) 
        (lift1 not (translate -1.5 -0.5 (scale 3.0 (text aerotextarg)))) 
        (vortex swirlArg mean (translate 0.0 (0.5/widtharg) lines))

///=new spiral, tighter in the middle
let spiral2 p = 
    let (r, th) = toPolar p in
    let rounds = 1.0/r + th/Math.PI*2.0
    in {x= floor(rounds) + Math.PI*2.0/th ; y = rounds % 1.0 * 4.0}

//flowers by terry: inspired by pic @ http://www.codeproject.com/KB/WPF/WPFJoshSmith.aspx
let petals = ("Petals", 1, 30, 5)
let innerRadius = ("Inner Radius", 0, 5, 1)
let outerRadius = ("Outer Radius", 0, 5, 2)


let flowerR petals innerRadius outerRadius (r, th) =
    (r/((outerRadius-innerRadius)/2.0 * sin (th * (float)petals) + (innerRadius + outerRadius)/2.0), th)

let flower petals innerRadius outerRadius p = 
    let disc (r,th) = r<1.0 in
    disc (flowerR petals innerRadius outerRadius  (toPolar p))
let flowerFloat petals innerRadius outerRadius p = 
    fst (flowerR petals innerRadius outerRadius (toPolar p))
let flowerOutline petals innerRadius outerRadius p = 
    let disc (r,th) = r<1.0 && r>0.8 in
    disc (flowerR petals innerRadius outerRadius  (toPolar p))

let flowerTransform petals innerRadius outerRadius = polarTransform (flowerR petals innerRadius outerRadius )
let flowerTileTransform petals innerRadius outerRadius = tile (flowerTransform petals innerRadius outerRadius)
let flowerTileColour petals = tileInColour (flower petals 0.2 0.4)
let flowerOTileColour petals = tileInColour (flowerOutline petals 0.2 0.4)
