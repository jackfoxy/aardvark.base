﻿namespace Aardvark.Base.Incremental

open System
open System.Runtime.CompilerServices
open System.Collections.Concurrent
open Aardvark.Base
open Aardvark.Base.Incremental.ASetReaders


module ASet =
    type AdaptiveSet<'a>(newReader : unit -> IReader<'a>) =
        let state = ReferenceCountingSet<'a>()
        let readers = WeakSet<BufferedReader<'a>>()

        let mutable inputReader = None
        let getReader() =
            match inputReader with
                | Some r -> r
                | None ->
                    let r = newReader()
                    inputReader <- Some r
                    r

        let bringUpToDate () =
            let r = getReader()
            let delta = r.GetDelta ()
            if not <| List.isEmpty delta then
                delta |> apply state |> ignore
                readers  |> Seq.iter (fun ri ->
                    ri.Emit(state, Some delta)
                )

        interface aset<'a> with
            member x.GetReader () =
                bringUpToDate()
                let r = getReader()

                let remove ri =
                    r.RemoveOutput ri
                    readers.Remove ri |> ignore

                    if readers.IsEmpty then
                        r.Dispose()
                        inputReader <- None

                let reader = new BufferedReader<'a>(bringUpToDate, remove)
                reader.Emit (state, None)
                r.AddOutput reader
                readers.Add reader |> ignore

                reader :> _

    type ConstantSet<'a>(content : seq<'a>) =
        let content = ReferenceCountingSet content

        interface aset<'a> with
            member x.GetReader () =
                let r = new BufferedReader<'a>()
                r.Emit(content, None)
                r :> IReader<_>

    type private EmptySetImpl<'a> private() =
        static let emptySet = ConstantSet [] :> aset<'a>
        static member Instance = emptySet

    let private scoped (f : 'a -> 'b) =
        let scope = Ag.getContext()
        fun v -> Ag.useScope scope (fun () -> f v)

    /// <summary>
    /// creates an empty set instance being reference
    /// equal to all other empty sets of the same type.
    /// </summary>
    let empty<'a> : aset<'a> =
        EmptySetImpl<'a>.Instance

    /// <summary>
    /// creates a set containing one element
    /// </summary>
    let single (v : 'a) =
        ConstantSet [v] :> aset<_>

    /// <summary>
    /// creates a set containing all distinct 
    /// elements in the given sequence
    /// </summary>
    let ofSeq (s : seq<'a>) =
        ConstantSet(s) :> aset<_>
    
    /// <summary>
    /// creates a set containing all distinct 
    /// elements in the given list
    /// </summary>
    let ofList (l : list<'a>) =
        ConstantSet(l) :> aset<_>

    /// <summary>
    /// creates a set containing all distinct 
    /// elements in the given array
    /// </summary>
    let ofArray (a : 'a[]) =
        ConstantSet(a) :> aset<_>

    /// <summary>
    /// returns a list of all elements currently in the adaptive set. 
    /// NOTE: will force the evaluation of the set.
    /// </summary>
    let toList (set : aset<'a>) =
        use r = set.GetReader()
        r.GetDelta() |> ignore
        r.Content |> Seq.toList

    /// <summary>
    /// returns a sequence of all elements currently in the adaptive set. 
    /// NOTE: will force the evaluation of the set.
    /// </summary>
    let toSeq (set : aset<'a>) =
        let l = toList set
        l :> seq<_>

    /// <summary>
    /// returns an array of all elements currently in the adaptive set. 
    /// NOTE: will force the evaluation of the set.
    /// </summary>
    let toArray (set : aset<'a>) =
        use r = set.GetReader()
        r.GetDelta() |> ignore
        r.Content |> Seq.toArray

    /// <summary>
    /// creates a singleton set which will always contain
    /// the latest value of the given mod-cell.
    /// </summary>
    let ofMod (m : IMod<'a>) =
        AdaptiveSet(fun () -> ofMod m) :> aset<_>

    /// <summary>
    /// creates a mod-cell containing the set's content
    /// as IVersionedSet. Since the IVersionedSet is mutating
    /// (without changing its identity) the mod system is 
    /// prepared to track changes using the version.
    /// </summary>
    let toMod (s : aset<'a>) =
        let r = s.GetReader()
        let c = r.Content :> IVersionedSet<_>

        let m = Mod.custom (fun () ->
            r.GetDelta() |> ignore
            c
        )
        r.AddOutput m
        m

    /// <summary>
    /// adaptively checks if the set contains the given element
    /// </summary>
    let contains (elem : 'a) (set : aset<'a>) =
        set |> toMod |> Mod.map (fun s -> s.Contains elem)

    /// <summary>
    /// adaptively checks if the set contains all given elements
    /// </summary>
    let containsAll (elems : #seq<'a>) (set : aset<'a>) =
        set |> toMod |> Mod.map (fun s -> elems |> Seq.forall (fun e -> s.Contains e))

    /// <summary>
    /// adaptively checks if the set contains any of the given elements
    /// </summary>
    let containsAny (elems : #seq<'a>) (set : aset<'a>) =
        set |> toMod |> Mod.map (fun s -> elems |> Seq.exists (fun e -> s.Contains e))

    /// <summary>
    /// applies the given function to all elements in the set
    /// and returns a new set containing the respective results.
    /// NOTE: duplicates are handled correctly here which means
    ///       that the function may be non-injective
    /// </summary>
    let map (f : 'a -> 'b) (set : aset<'a>) = 
        let scope = Ag.getContext()
        AdaptiveSet(fun () -> set.GetReader() |> map scope f) :> aset<'b>

    /// <summary>
    /// applies the given function to a cell and adaptively
    /// returns the resulting set.
    /// </summary>
    let bind (f : 'a -> aset<'b>) (m : IMod<'a>) =
        let scope = Ag.getContext()
        AdaptiveSet(fun () -> m |> bind scope (fun v -> (f v).GetReader())) :> aset<'b>

    /// <summary>
    /// applies the given function to both cells and adaptively
    /// returns the resulting set.
    /// </summary>
    let bind2 (f : 'a -> 'b -> aset<'c>) (ma : IMod<'a>) (mb : IMod<'b>) =
        let scope = Ag.getContext()
        AdaptiveSet(fun () -> bind2 scope (fun a b -> (f a b).GetReader()) ma mb) :> aset<'c>

    /// <summary>
    /// applies the given function to all elements in the set
    /// and unions all output-sets.
    /// NOTE: duplicates are handled correctly here meaning that
    ///       the given function may return overlapping sets.
    /// </summary>
    let collect (f : 'a -> aset<'b>) (set : aset<'a>) = 
        let scope = Ag.getContext()
        AdaptiveSet(fun () -> set.GetReader() |> collect scope (fun v -> (f v).GetReader())) :> aset<'b>

    /// <summary>
    /// applies the given function to all elements in the set
    /// and returns a set containing all the elements that were Some(v)
    /// NOTE: duplicates are handled correctly here which means
    ///       that the function may be non-injective
    /// </summary>
    let choose (f : 'a -> Option<'b>) (set : aset<'a>) =
        let scope = Ag.getContext()
        AdaptiveSet(fun () -> set.GetReader() |> choose scope f) :> aset<'b>

    /// <summary>
    /// filters the elements in the set using the given predicate
    /// </summary>
    let filter (f : 'a -> bool) (set : aset<'a>) =
        choose (fun v -> if f v then Some v else None) set


    let union (set : aset<aset<'a>>) =
        collect id set

    let union' (set : seq<aset<'a>>) =
        union (ConstantSet set)

    let concat (set : aset<aset<'a>>) =
        collect id set

    let concat' (set : seq<aset<'a>>) =
        concat (ConstantSet set)

    let collect' (f : 'a -> aset<'b>) (set : seq<'a>) =
        set |> Seq.map f |> concat'
  
    let mapM (f : 'a -> IMod<'b>) (s : aset<'a>) =
        s |> collect (fun v ->
            v |> f |> ofMod
        )

    let filterM (f : 'a -> IMod<bool>) (s : aset<'a>) =
        s |> collect (fun v ->
            v |> f |> bind (fun b -> if b then single v else empty)
        )

    let chooseM (f : 'a -> IMod<Option<'b>>) (s : aset<'a>) =
        s |> collect (fun v ->
            v |> f |> bind (fun b -> match b with | Some v -> single v | _ -> empty)
        )

    let flattenM (s : aset<IMod<'a>>) =
        s |> collect ofMod

    let reduce (f : seq<'a> -> 'b) (s : aset<'a>) : IMod<'b> =
        s |> toMod |> Mod.map f

    let foldSemiGroup (add : 'a -> 'a -> 'a) (zero : 'a) (s : aset<'a>) : IMod<'a> =
        let r = s.GetReader()
        let sum = ref zero

        let rec processDeltas (deltas : list<Delta<'a>>) =
            match deltas with
                | Add v :: deltas ->
                    sum := add !sum v
                    processDeltas deltas
                | Rem v :: deltas ->
                    false
                | [] ->
                    true

        let res =
            Mod.custom (fun () ->
                let mutable rem = false
                let delta = r.GetDelta()

                if not <| processDeltas delta then
                    sum := r.Content |> Seq.fold add zero

                !sum
            )

        r.AddOutput res
        res

    let foldGroup (add : 'a -> 'a -> 'a) (sub : 'a -> 'a -> 'a) (zero : 'a) (s : aset<'a>) : IMod<'a> =
        let r = s.GetReader()
        let sum = ref zero

        let res =
            Mod.custom (fun () ->
                let delta = r.GetDelta()
                for d in delta do
                    match d with
                        | Add v -> sum := add !sum v
                        | Rem v -> sum := sub !sum v
                !sum
            )

        r.AddOutput res
        res


    let reduceM (f : seq<'a> -> 'b) (s : aset<IMod<'a>>) : IMod<'b> =
        reduce f (collect ofMod s)

    let foldSemiGroupM (add : 'a -> 'a -> 'a) (zero : 'a) (s : aset<IMod<'a>>) : IMod<'a> =
        let s = s |> collect ofMod
        foldSemiGroup add zero s

    let foldGroupM (add : 'a -> 'a -> 'a) (sub : 'a -> 'a -> 'a) (zero : 'a) (s : aset<IMod<'a>>) : IMod<'a> =
        let s = s |> collect ofMod
        foldGroup add sub zero s


    let inline sum (s : aset<'a>) = foldGroup (+) (-) LanguagePrimitives.GenericZero s
    let inline product (s : aset<'a>) = foldGroup (*) (/) LanguagePrimitives.GenericOne s
    let inline sumM (s : aset<IMod<'a>>) = foldGroupM (+) (-) LanguagePrimitives.GenericZero s
    let inline productM (s : aset<IMod<'a>>) = foldGroupM (*) (/) LanguagePrimitives.GenericOne s

    let private callbackTable = ConditionalWeakTable<obj, ConcurrentHashSet<IDisposable>>()
    type private CallbackSubscription(m : obj, cb : unit -> unit, live : ref<bool>, reader : IAdaptiveObject, set : ConcurrentHashSet<IDisposable>) =
        let disposable = reader |> unbox<IDisposable>

        member x.Dispose() = 
            if !live then
                live := false
                disposable.Dispose()
                reader.MarkingCallbacks.Remove cb |> ignore
                set.Remove x |> ignore
                if set.Count = 0 then
                    callbackTable.Remove(m) |> ignore

        interface IDisposable with
            member x.Dispose() = x.Dispose()

        override x.Finalize() =
            try x.Dispose()
            with _ -> ()


    /// <summary>
    /// registers a callback for execution whenever the
    /// set's value might have changed and returns a disposable
    /// subscription in order to unregister the callback.
    /// Note that the callback will be executed immediately
    /// once here.
    /// </summary>
    let registerCallback (f : list<Delta<'a>> -> unit) (set : aset<'a>) =
        let m = set.GetReader()
        let f = scoped f
        let self = ref id
        let live = ref true
        self := fun () ->
            if !live then
                try
                    m.GetDelta() |> f
                finally 
                    m.MarkingCallbacks.Add !self |> ignore
        
        lock m (fun () ->
            !self ()
        )

        let callbackSet = callbackTable.GetOrCreateValue(set)
        let s = new CallbackSubscription(set, !self, live, m, callbackSet)
        callbackSet.Add s |> ignore
        s :> IDisposable

