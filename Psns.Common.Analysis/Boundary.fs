module Boundary

    type Boundary<'T> =
        struct
            val Min: 'T
            val Max: 'T
            new(minMax)   = { Min = fst minMax; Max = snd minMax }
            new(min, max) = { Min = min; Max = max }
            static member op_Explicit(minMax) = new Boundary<'T>(minMax)
            static member op_Explicit(min, max) = new Boundary<'T>(min, max)
        end

    
    let map<'T, 'R> (boundary: Boundary<'T>) f = new Boundary<'R>(f boundary.Min, f boundary.Max)
    let ofTuple<'T> minMax = new Boundary<'T>(minMax)
    let ofMinMax<'T> min max = new Boundary<'T>(min, max)