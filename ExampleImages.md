{{
let swirlP r p = rotateP (log ((distO p)+1e-6) * r / 5.0) p
let swirl r image p = image (swirlP r p)
let swirlText swirlArg textArg = 
    condC (swirl swirlArg (tile (text textArg) ) ) 
        (canvas (darken 0.8 nicegreen))
        (canvas nicegreen) 
}}