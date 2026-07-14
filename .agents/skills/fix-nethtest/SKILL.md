---
name: fix-nethtest
description: Debug and fix failing Sila Foundation tests run with Nethermind.Test.Runner. Accepts test file paths as arguments. Runs the test, analyzes traces, identifies root causes in the SAVM/spec/test harness, and proposes fixes.
allowed-tools:
  [
    Bash,
    Read,
    Grep,
    Glob,
    Edit,
    Write,
  ]
---

# Fix failing Sila Foundation tests

Diagnose and fix tests run with `Nethermind.Test.Runner` (nethtest). The test runner executes Sila Foundation state tests and blockchain tests against the Nethermind SAVM.

**Arguments:** `$ARGUMENTS` — one or more test file paths.

---

## Phase 1 — Run the test

### Detect test type

Infer the test type from the JSON structure — do **not** require the user to specify it:

- **Blockchain test** — the test object contains `"blocks"`, `"genesisBlockHeader"`, or `"network"` keys
- **State test** — the test object contains `"transaction"` and `"post"` keys

A quick check: `grep -l '"blocks"' <file>` or `grep -l '"transaction"' <file>`.

### Build and run

```bash
# Build (only if needed)
dotnet build src/Nethermind/Nethermind.Test.Runner/Nethermind.Test.Runner.csproj -c release --verbosity quiet

# Run state test
dotnet run --project src/Nethermind/Nethermind.Test.Runner/Nethermind.Test.Runner.csproj -c release -- -t -i "<test-file>"

# Run blockchain test (add -b flag)
dotnet run --project src/Nethermind/Nethermind.Test.Runner/Nethermind.Test.Runner.csproj -c release -- -b -t -i "<test-file>"
```

**Output structure:**
- **Trace lines** (one per SAVM opcode): `{"pc":N,"op":N,"gas":"0x...","gasCost":"0x...","stack":[...],"depth":N,"opName":"...","error":"..."}`
- **Execution result**: `{"output":"0x...","gasUsed":"0x...","time":N,"error":"..."}`
- **State root**: `{"stateRoot":"0x..."}`
- **Test result JSON**: `[{"name":"...","pass":bool,"fork":"...","stateRoot":"0x..."}]`

If `pass` is `true`, report success and stop. Otherwise proceed to Phase 2.

---

## Phase 2 — Classify the failure

Read the test JSON file to understand:
1. **Target fork** — from the `post` section key (e.g., `"SilaOsaka"`)
2. **Transaction type** — legacy, SIP-1559, blob, SetCode (SIP-7702 if `authorizationList` present)
3. **Expected state root** — from `post.<fork>[0].hash` (all-zeros = placeholder, not a real expected value)
4. **Environment** — which optional fields are present/absent in `env` (e.g., `currentExcessBlobGas`, `currentBeaconRoot`)

Then classify the failure from the trace:

| Trace signal | Failure class | Likely cause area |
|---|---|---|
| `"error":"BadInstruction"` on a known opcode | **Opcode not available** | Spec flag gate or instruction runtime guard |
| `"error":"BadInstruction"` on unknown opcode | **Unimplemented opcode** | Missing opcode in `SavmInstructions.cs` |
| `"error":"OutOfGas"` | **Gas accounting** | Gas cost calculation or intrinsic gas |
| `"error":"StackUnderflow"` or `"error":"StackOverflow"` | **Stack effect** | Usually expected behavior of the bytecode, not a bug |
| No SAVM error but wrong state root | **State mismatch** | Wrong storage writes, balance changes, or header field defaults |
| `"loadFailure"` in result | **Parse error** | Unsupported JSON field or missing deserialization |
| Block validation fails (blockchain tests) | **Header issue** | Missing header fields or wrong defaults for fork |

---

## Phase 3 — Root cause analysis

### For BadInstruction failures

This is the most common failure pattern for new forks. Follow these steps:

1. **Identify the opcode** from the trace entry where `error` is `"BadInstruction"`:
   - `op` field = opcode byte value
   - `opName` field = human-readable name

2. **Check opcode registration** in `src/Nethermind/Nethermind.Savm/Instructions/SavmInstructions.cs`:
   - Find the line `lookup[(int)Instruction.<OPNAME>] = ...`
   - Note the spec flag gate: `if (spec.<FlagName>)` wrapping it

3. **Check if the spec flag is true for the target fork**:
   - Find the flag in `src/Nethermind/Nethermind.Core/Specs/IReleaseSpecExtensions.cs` or `IReleaseSpec.cs`
   - Trace it through the fork hierarchy in `src/Nethermind/Nethermind.Specs/Forks/`
   - Each fork class calls `Apply(ReleaseSpec spec)` to set its flags; the chain is replayed from root

4. **If the opcode IS registered but still fails** — the instruction implementation has a runtime guard. Check the instruction handler in `src/Nethermind/Nethermind.Savm/Instructions/SavmInstructions.*.cs`:
   - Look for patterns like `if (!context.Header.SomeField.HasValue) goto BadInstruction;`
   - These guards fail when the test harness doesn't set the header field

5. **Check the test harness header construction**:
   - **State tests:** `src/Nethermind/Sila.Test.Base/GeneralStateTestBase.cs` — look at the `BlockHeader` initializer
   - **Blockchain tests:** `src/Nethermind/Sila.Test.Base/BlockchainTestBase.cs`
   - Look for conditional defaults that use **type checks** (`is SilaCancun`, `is SilaPrague`) instead of **spec flag checks** (`IsSip4844Enabled`, `IsSip7702Enabled`) — type checks only match the exact class, not subclasses or later forks
   - Look for missing defaults for new fork fields (e.g., `ExcessBlobGas`, `RequestsHash`, `BlockAccessListHash`)

### For state root mismatches (no SAVM error)

1. **Check if expected hash is all-zeros** — this is a placeholder test. The test was generated but the expected root hasn't been filled in. Report this to the user; it's not a code bug.

2. **Check conditional defaults in header construction** — a missing header field (e.g., null `ExcessBlobGas`) can change how the SAVM executes even without causing `BadInstruction`.

3. **Analyze the trace for unexpected behavior**:
   - Storage writes at unexpected values
   - Calls that revert unexpectedly
   - Gas costs that differ from expected (compare with SIP specs)

4. **Check fork spec configuration**:
   - Missing SIP flags in the fork's `Apply()` method
   - Wrong gas parameters (e.g., blob schedule not applied from test `config.blobSchedule`)

5. **Check blob schedule override** — test JSON may have a `config.blobSchedule` section with per-fork overrides for `target`, `max`, and `baseFeeUpdateFraction`. Verify these are applied via `OverridableReleaseSpec` in `JsonToSilaTest.LoadSpec()`.

### For block validation failures (blockchain tests)

1. Check if required header fields are set for the target fork
2. Look for new consensus rules that affect block validity (e.g., SIP-7928 `BlockAccessListHash`)
3. Check if genesis block processing uses the correct spec (see `genesisUsesTargetFork` logic in `BlockchainTestBase.cs`)

### For load/parse failures

1. Check if the test JSON uses fields not yet supported in the deserialization models
2. Look at `Sila.Test.Base/JsonToSilaTest.cs` and the JSON model classes
3. Check `SpecNameParser.cs` for unsupported fork names

---

## Phase 4 — Fix and verify

1. **Apply the minimal fix** — prefer spec flag checks over type checks, add missing defaults, or fix gas calculations
2. **Re-run the failing test** to verify the fix resolves the issue
3. If test still fails:
   - Re-read the trace output
   - Return to Phase 2 with the new trace
   - Repeat until pass or root cause is confirmed as upstream
4. **Add a regression test** when the root cause was a Nethermind bug (AGENTS.md requires one for every bug fix) — extend an existing test file with a `[TestCase]` where possible; create a new test file only if no suitable one exists
5. **Report the result** — include:
   - Root cause (one sentence)
   - What was fixed (file:line)
   - Verification result (pass/fail + new state root)
   - Whether the test has a real expected hash or a placeholder (all-zeros)

---

## Key files reference

| Purpose | Path (relative to `src/Nethermind/`) |
|---|---|
| Test runner CLI | `Nethermind.Test.Runner/Program.cs` |
| State test execution + header | `Sila.Test.Base/GeneralStateTestBase.cs` |
| Blockchain test execution | `Sila.Test.Base/BlockchainTestBase.cs` |
| Test JSON parsing | `Sila.Test.Base/JsonToSilaTest.cs` |
| Opcode registration + spec gates | `Nethermind.Savm/Instructions/SavmInstructions.cs` |
| Instruction implementations | `Nethermind.Savm/Instructions/SavmInstructions.*.cs` |
| Spec flag extensions | `Nethermind.Core/Specs/IReleaseSpecExtensions.cs` |
| Fork definitions | `Nethermind.Specs/Forks/*.cs` |
| Fork name parser | `Nethermind.Specs/SpecNameParser.cs` |
| SAVM exception types | `Nethermind.Savm/SavmException.cs` |
| Block execution context | `Nethermind.Savm/BlockExecutionContext.cs` |

## Common bug patterns

1. **RLP deserialization** — incorrect RLP decoding can either accept malformed data (missing validation, wrong type expectations) or reject valid data (overly strict length checks, wrong sequence handling). For blockchain tests, blocks arrive as RLP — check `Nethermind.Serialization.Rlp/` decoders for the relevant types. Common issues: not handling optional fields added by new SIPs, wrong list vs single-item decoding, incorrect length prefix validation.

2. **Gas accounting** — wrong gas costs for opcodes, missing gas charges for memory expansion, incorrect intrinsic gas calculation for new transaction types, or wrong refund logic. Compare against the SIP specification. Check `GasCostOf.cs` for static costs, instruction handlers for dynamic costs, and `IntrinsicGasCalculator` for transaction-level gas.

3. **State root mismatch** — execution completes without SAVM errors but produces the wrong state root. Causes include: incorrect storage writes (wrong slot or value), wrong balance updates (fee burning, coinbase rewards, value transfer edge cases), missing or extra account touches that affect empty-account cleanup (SIP-158), or wrong nonce increments.

4. **Missing header field default** — covered in Phase 3, "Check the test harness header construction".

5. **Instruction runtime guard** — covered in Phase 3, "For BadInstruction failures" step 4.

6. **Validation errors** — missing validation that should reject invalid data (e.g., not checking transaction type constraints, accepting out-of-range values) or overly strict validation that rejects valid inputs (e.g., rejecting new transaction fields not yet accounted for, wrong bounds on SIP-introduced parameters). For blockchain tests, check block and transaction validators. For state tests, check `TransactionProcessor` validation and `BlockValidator`.

7. **Missing blob schedule override** — covered in Phase 3, "For state root mismatches" step 5.
