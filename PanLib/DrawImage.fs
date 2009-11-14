
open Pan

//------ DRAW FUNCTION INTO BITMAP ----------
#if NET20
let drawImageCol f width height step =
    let size = getSlider "Size" -10 10 0
    let xrange = (int)(width / (pown 2.0 step))
    let yrange = (int)(height / (pown 2.0 step))
    let bitmap = new System.Drawing.Bitmap(xrange, yrange, System.Drawing.Imaging.PixelFormat.Format24bppRgb) 
    let mult = System.Math.Pow(1.3,-(float)size)/float(min width height)*3.0 * (pown 2.0 step) 
    let start = System.Environment.TickCount;
    let panToRGB (c: float) = (int)(255.0 * max 0.0 (min c 1.0)) in
    for i=0 to xrange-1 do
        for j=0 to yrange-1 do
            let p = { x= (float)(i-xrange/2)*mult; y= (float)(j - yrange/2)*mult } in
            let col = f p in
            let color = System.Drawing.Color.FromArgb(panToRGB col.a, panToRGB col.r, panToRGB col.g, panToRGB col.b) in
                bitmap.SetPixel(i, j, color)
        done
    done
    (bitmap, System.Environment.TickCount - start)
(*        lock (bitmapOut) 
            ( fun () -> 
                bitmapOut <- bitmap;
                form.Invalidate();
                let pixels = bitmap.Width * bitmap.Height in
                let elapsed = watch.ElapsedMilliseconds in
                    status(System.String.Format("Drew {0} pixels in {1} ms\n{2} pixels/s, {3} frames/s",
                            pixels, elapsed.ToString("n2"),
                            (pixels / (int)elapsed).ToString("n0"), (1.0 / (float)elapsed).ToString("n2")));
            )
  *)
        
let drawImageBool f width height step =
    drawImageCol (byImage f) width height step
    
let drawImageDouble f width height step =
    drawImageCol (lerpI f (canvas blue) (canvas white)) width height step

#else

open System.Windows.Media
//open System.Windows.Media.Imaging

//------ DRAW FUNCTION INTO BITMAP ----------

let drawImageCol f width height step scale =
    let xrange = (int)(width / (pown 2.0 step))
    let yrange = (int)(height / (pown 2.0 step))
    let pixels = Array.init (xrange * yrange * 3) (fun i -> (byte)0)
    let mult = System.Math.Pow(1.3,-scale)/float(min width height)*3.0 * (pown 2.0 step) 
    let start = System.Environment.TickCount;
    let panToRGB (c: float) = (byte)(255.0 * max 0.0 (min c 1.0)) in
    for i=0 to xrange-1 do
        for j=0 to yrange-1 do
            let p = { x= (float)(i-xrange/2)*mult; y= (float)(yrange/2-j)*mult } in
            let col = f p in
            //let color = System.Windows.Media.Color.FromArgb(panToRGB col.a, panToRGB col.r, panToRGB col.g, panToRGB col.b) in
                pixels.[3*(i+j*xrange)] <- panToRGB col.r;
                pixels.[3*(i+j*xrange)+1] <- panToRGB col.g;
                pixels.[3*(i+j*xrange)+2] <- panToRGB col.b;
        done
    done

// Creates a new empty image with the pre-defined palette
    let pf = PixelFormats.Rgb24 in
    (
        //BitmapSource.Create(xrange,yrange, 96.0, 96.0, pf, null, pixels, xrange * ((pf.BitsPerPixel + 7) / 8)),
        pixels, xrange, yrange, pf, xrange * ((pf.BitsPerPixel + 7) / 8),
        System.Environment.TickCount - start)
        
let drawImageBool f width height step =
    drawImageCol (byImage f) width height step

let doubleToColImage f = lerpI f (canvas blue) (canvas white)    

let boolToColImage f = byImage f

#endif