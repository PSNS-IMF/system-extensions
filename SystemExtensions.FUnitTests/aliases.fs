namespace SystemExtensions.FUnitTests

type internal ErrorState = Psns.Common.SystemExtensions.Diagnostics.ErrorLoggingStateModule.ErrorState
type internal ErrorStateTransition = Psns.Common.SystemExtensions.Diagnostics.ErrorLoggingStateModule.ErrorStateTransition
type internal Classification = Psns.Common.Analysis.Anomaly.Classification

module internal aliases =
    let normal = ErrorState.Normal
    let saturated = ErrorState.Saturated
    let normalizing = ErrorStateTransition.Normalizing
    let saturating = ErrorStateTransition.Saturating
    let noTransition = ErrorStateTransition.None
    let classNorm = Classification.Norm
    let classHigh = Classification.High

module internal helpers =
    let internal fst (a, _, _) = a
    let internal snd (_, b, _) = b
    let internal thd (_, _, c) = c