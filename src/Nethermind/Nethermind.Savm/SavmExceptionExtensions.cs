// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Savm;

public static class SavmExceptionExtensions
{
    public static string? GetSavmExceptionDescription(this SavmExceptionType savmExceptionType) =>
        savmExceptionType switch
        {
            SavmExceptionType.None => null,
            SavmExceptionType.BadInstruction => "invalid instruction",
            SavmExceptionType.StackOverflow => "max call depth exceeded",
            SavmExceptionType.StackUnderflow => "stack underflow",
            SavmExceptionType.OutOfGas => "out of gas",
            SavmExceptionType.InvalidJumpDestination => "invalid jump destination",
            SavmExceptionType.AccessViolation => "return data out of bounds",
            SavmExceptionType.StaticCallViolation => "write protection",
            SavmExceptionType.PrecompileFailure => "precompile error",
            SavmExceptionType.TransactionCollision => "contract address collision",
            SavmExceptionType.NotEnoughBalance => "insufficient balance for transfer",
            SavmExceptionType.Other => "error",
            SavmExceptionType.Revert => "execution reverted",
            SavmExceptionType.InvalidCode => "invalid code: must not begin with 0xef",
            _ => "error"
        };
}
