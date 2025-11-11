using LanguageExt;
using LanguageExt.Traits;

using static LanguageExt.Prelude;

namespace Budget;

public static class Utilities
{
    public static Eff<Sub, Sub> askE<Sub>() => new (
        new ReaderT<Sub, IO, Sub>(IO.pure)
    );
    
    // todo this is a whole project to merge back into main as proper HKT
    public static Eff<Env2, A> CoMap<Env1, Env2, A>(this Eff<Env1, A> eff, Func<Env2, Env1> f) =>
        new (new ReaderT<Env2, IO, A>(env2 => eff.effect.Run(f(env2)) ));
    
    //todo based version where everything is monad, overlaods wrap up in Pure as needed, then can go in main too!
    public static K<M, A> cond<M, A>(Seq<(bool Pred, K<M, A> True)> seq, A Default)
        where M : Monad<M>
        => seq.Rev().Fold(M.Pure(Default), (prev, nextIf) => iff(
            nextIf.Pred,
            nextIf.True,
            prev
        ));
}