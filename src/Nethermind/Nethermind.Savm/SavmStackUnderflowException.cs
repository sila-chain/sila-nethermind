// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Savm
{
    public class SavmStackUnderflowException : SavmException
    {
        public override SavmExceptionType ExceptionType => SavmExceptionType.StackUnderflow;
    }
}
