// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;

namespace Nethermind.Savm;

public abstract class SavmException : Exception
{
    public abstract SavmExceptionType ExceptionType { get; }
}

public enum SavmExceptionType
{
    Stop = -1,
    None = 0,
    BadInstruction,
    StackOverflow,
    StackUnderflow,
    OutOfGas,
    InvalidJumpDestination,
    AccessViolation,
    StaticCallViolation,
    PrecompileFailure,
    TransactionCollision,
    NotEnoughBalance,
    Other,
    Revert,
    InvalidCode,
}

public static class SavmExceptionTypeExtensions
{
    /// <summary>
    /// Returns the member name of <paramref name="type"/>, equivalent to <see cref="object.ToString"/> but reflection-free.
    /// </summary>
    /// <remarks>
    /// The trimmed NativeAOT/zkVM runtime carries no enum metadata, so <c>Enum.ToString()</c> faults in
    /// <c>ReflectionAugments.GetEnumInfo</c>. A top-level transaction can legitimately fail
    /// (Revert/OutOfGas/...) and the receipts tracer formats the error name, so the names are mapped directly.
    /// </remarks>
    public static string FastToString(this SavmExceptionType type) => type switch
    {
        SavmExceptionType.Stop => nameof(SavmExceptionType.Stop),
        SavmExceptionType.None => nameof(SavmExceptionType.None),
        SavmExceptionType.BadInstruction => nameof(SavmExceptionType.BadInstruction),
        SavmExceptionType.StackOverflow => nameof(SavmExceptionType.StackOverflow),
        SavmExceptionType.StackUnderflow => nameof(SavmExceptionType.StackUnderflow),
        SavmExceptionType.OutOfGas => nameof(SavmExceptionType.OutOfGas),
        SavmExceptionType.InvalidJumpDestination => nameof(SavmExceptionType.InvalidJumpDestination),
        SavmExceptionType.AccessViolation => nameof(SavmExceptionType.AccessViolation),
        SavmExceptionType.StaticCallViolation => nameof(SavmExceptionType.StaticCallViolation),
        SavmExceptionType.PrecompileFailure => nameof(SavmExceptionType.PrecompileFailure),
        SavmExceptionType.TransactionCollision => nameof(SavmExceptionType.TransactionCollision),
        SavmExceptionType.NotEnoughBalance => nameof(SavmExceptionType.NotEnoughBalance),
        SavmExceptionType.Other => nameof(SavmExceptionType.Other),
        SavmExceptionType.Revert => nameof(SavmExceptionType.Revert),
        SavmExceptionType.InvalidCode => nameof(SavmExceptionType.InvalidCode),
        _ => ((int)type).ToString(),
    };
}
