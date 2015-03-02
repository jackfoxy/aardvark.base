﻿namespace Aardvark.Base.Incremental


[<AutoOpen>]
module ``Computation Expression Builders`` =
    
    type AdaptiveBuilder() =

        member x.Bind(tup : IMod<'a> * IMod<'b>, f : 'a * 'b -> IMod<'c>) : IMod<'c> =
            Mod.bind2 (fun a b -> f(a,b)) (fst tup) (snd tup)

        member x.Bind(m : IMod<'a>, f : 'a -> IMod<'b>) =
            Mod.bind f m

        member x.Return (v : 'a) =
            Mod.initMod v :> IMod<_>

        member x.ReturnFrom(m : IMod<'a>) = 
            m

    type ASetBuilder() =
        member x.Yield (v : 'a) =
            ASet.single v

        member x.YieldFrom (set : aset<'a>) =
            set

        member x.Bind(m : IMod<'a> * IMod<'b>, f : 'a * 'b -> aset<'c>) =
            ASet.bind2 (fun a b -> f(a,b)) (fst m) (snd m)

        member x.Bind(m : IMod<'a>, f : 'a -> aset<'b>) =
            ASet.bind f m

        member x.For(s : aset<'a>, f : 'a -> aset<'b>) =
            ASet.collect f s

        member x.For(s : seq<'a>, f : 'a -> aset<'b>) =
            ASet.collect' f s

        member x.Zero() =
            ASet.empty

        member x.Delay(f : unit -> aset<'a>) =
            f()

        member x.Combine(l : aset<'a>, r : aset<'a>) =
            ASet.concat' [l;r]

    module Mod =
        let toASet (m : IMod<'a>) =
            ASet.ofMod m



    let adaptive = AdaptiveBuilder()
    let aset = ASetBuilder()