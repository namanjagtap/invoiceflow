# InvoiceFlow: Topics Learned

A revision guide to everything learned while building InvoiceFlow. Each topic is tied to the code where it is used.

**How to use it:** read a topic, open the linked file, then answer the self-check questions out loud before you open the answers.

**Each topic has:**

- **What it is:** the idea in plain words
- **Why InvoiceFlow needs it:** the real problem it solves here
- **Where:** the files that use it
- **Controllers:** what changes in a controller-based API (only where it matters)
- **Common mistake:** what people get wrong
- **Self-check:** 2–3 questions, answers folded

## Contents

- [Week 1: Modern C# and the .NET runtime](#week-1-modern-c-and-the-net-runtime)
  - Day 1: [Solution setup](#1-solution-setup-and-build-rules) · [Records](#2-records-and-value-equality) · [Value objects](#3-value-objects-parse-dont-validate) · [Modern C# syntax](#4-modern-c-syntax-used-everywhere) · [Nullable types](#5-nullable-reference-types-and-notnullwhen) · [Regex](#6-regular-expressions-generatedregex-and-z) · [Money](#7-money-decimal-rounding-and-currency)
  - Day 2: [How async works](#8-how-asyncawait-works) · [Deadlocks](#9-sync-over-async-deadlocks-and-configureawait) · [Cancellation](#10-cancellationtoken) · [Safe file handling](#11-handling-uploaded-files-safely)
  - Day 3: [DI lifetimes](#12-dependency-injection-lifetimes) · [Captive dependencies](#13-captive-dependencies) · [Options pattern](#14-the-options-pattern-with-validation) · [Module registration](#15-module-registration-methods)
  - Day 4: [Middleware](#16-the-middleware-pipeline) · [Correlation ID](#17-correlation-id-and-request-timing-middleware) · [Modular monolith](#18-modular-monolith) · [Architecture tests](#19-architecture-tests) · [xUnit](#20-unit-testing-with-xunit)
- [Week 2: ASP.NET Core Web APIs done properly](#week-2-aspnet-core-web-apis-done-properly)
  - Day 1: [Minimal APIs](#21-minimal-apis-and-parameter-binding) · [DTOs](#22-request-and-response-dtos) · [Validation](#23-validation-with-fluentvalidation-and-an-endpoint-filter) · [Domain entities](#24-rich-domain-entities) · [Time](#25-time-timeprovider-utc-and-indian-dates) · [In-memory repositories](#26-in-memory-repositories-and-the-duplicate-rule)
  - Day 2: [ProblemDetails](#27-problemdetails-one-error-format-everywhere) · [Exception handling](#28-global-exception-handling) · [Serilog](#29-structured-logging-with-serilog)
  - Day 3: [JWT](#30-jwt-authentication) · [Secure by default](#31-secure-by-default-401-vs-403) · [Roles and policies](#32-roles-policies-and-claims) · [Ownership checks](#33-ownership-and-resource-based-authorization)
  - Day 4: [Status codes](#34-rest-conventions-and-status-codes) · [Paging](#35-pagination-and-filtering) · [Idempotency](#36-idempotency-keys) · [Rate limiting](#37-rate-limiting) · [OpenAPI](#38-openapi-typed-results-and-documented-responses)
- [Coming next](#coming-next)

---

# Week 1: Modern C# and the .NET runtime

**Goal of the week:** understand what happens inside async/await, dependency injection and the request pipeline, not just use them.

| Day | Topic | What we built | Main commits |
|---|---|---|---|
| 1 | C# 12–14 features | Solution skeleton, `Gstin`, `Money`, `InvoiceNumber` | `2eab8f7`, `1ea68fe`, `7069d8e`, `b721945`, `28f282b` |
| 2 | async/await internals | Local file store with cancellation, upload endpoint, AsyncLab deadlock experiment | `7023f74`, `fe4f916`, `0e95622`, `891b2ef` |
| 3 | Dependency injection | DI lifetime learning tests, validated options for Intake and Approval, `AddXxxModule()` methods | `c6f1daa`, `432beb8`, `369c9e4`, `a323bfe` |
| 4 | Middleware and module boundaries | Correlation ID and request timing middleware, architecture tests | `500c093`, `b9986c7`, `3b1a0ab` |

---

## Day 1: C# features and value objects

### 1. Solution setup and build rules

**What it is:** a solution is split into projects, and settings shared by every project live in one place.

- `InvoiceFlow.slnx` is the solution file in the newer XML format.
- `Directory.Build.props` sits at the repo root. MSBuild applies it to every project below it.

**Why InvoiceFlow needs it:** there are 15 projects (Host, 6 modules, SharedKernel, 6 test projects, AsyncLab). Setting the target framework and warning rules once stops them drifting apart.

| Setting | Effect |
|---|---|
| `TargetFramework net10.0` | Every project targets .NET 10 |
| `Nullable enable` | The compiler warns about possible nulls ([topic 5](#5-nullable-reference-types-and-notnullwhen)) |
| `ImplicitUsings enable` | Common `using`s such as `System` and `System.Linq` are added for you |
| `TreatWarningsAsErrors true` | A warning fails the build, so warnings can't pile up |
| `EnforceCodeStyleInBuild true` | `.editorconfig` style rules run at build time, not just in the editor |

`.editorconfig` makes file-scoped namespaces (`namespace X;`) a warning when not used. Combined with `TreatWarningsAsErrors`, that makes it an error. This rule was added later, in `188269d`.

**Where:** [Directory.Build.props](../Directory.Build.props), [.editorconfig](../.editorconfig), [InvoiceFlow.slnx](../InvoiceFlow.slnx)

**Common mistake:** turning on `TreatWarningsAsErrors` late in a project, when there are already hundreds of warnings. Turn it on at the start, as we did.

**Self-check**

1. Where do you set something every project should share, and why not copy it into each `.csproj`?
2. What does `EnforceCodeStyleInBuild` add on top of `.editorconfig`?

<details><summary>Answers</summary>

1. In `Directory.Build.props` at the repo root. MSBuild imports it automatically into every project below that folder. One file means one place to change, and no project can quietly use different settings.
2. Without it, `.editorconfig` rules only show in the IDE. With it, they run during `dotnet build`, so CI and the command line enforce them too. With `TreatWarningsAsErrors`, a style violation then fails the build.

</details>

---

### 2. Records and value equality

**What it is:** a `record` is a class where the compiler writes the equality code for you. Two records are equal when all their values are equal, not when they are the same object in memory.

- `record` generates `Equals`, `GetHashCode`, `==`, `!=` and a readable `ToString`.
- `sealed` stops anyone deriving from it. A derived record could add fields and break equality.
- `with` creates a copy with some properties changed, for example `invoice with { Status = ... }`. It works only on properties with `init` or `set`.

**Why InvoiceFlow needs it:** two `Money` values of ₹500 must be equal, and two `Gstin` objects for the same GSTIN must be equal. Records give that for free.

**Where:** [Money.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/ValueObjects/Money.cs), [Gstin.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/ValueObjects/Gstin.cs), [InvoiceNumber.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/ValueObjects/InvoiceNumber.cs). DTOs such as `InvoiceResponse` are records too.

**A detail worth remembering:** `Money.Amount` is `{ get; }` with no `init`. That means `money with { Amount = 1.234m }` **does not compile**. If it had `init`, `with` would copy the object and skip the rounding in the constructor, creating a `Money` that breaks its own rules.

**Common mistake:** making value-object properties `init` "because records usually do". `with` then bypasses your validation.

**Self-check**

1. `Money.Inr(500) == Money.Inr(500)`: true or false? What if `Money` were a plain `class`?
2. Why are the value objects `sealed`?
3. Why can't you use `with` on `Money`, and why is that good?

<details><summary>Answers</summary>

1. True: records compare values. With a plain class it would be false, because `==` would compare references (two different objects).
2. A derived type could add fields that the base `Equals` doesn't know about. `sealed` closes that door, and also tells readers the type isn't designed for inheritance.
3. Its properties are get-only, and `with` needs `init` or `set`. Every `Money` must go through the constructor, which rounds to 2 decimals. `with` would skip that.

</details>

---

### 3. Value objects: "parse, don't validate"

**What it is:** a value object is a small type with no identity that wraps a value and guarantees it is valid. Examples are a GSTIN, an amount of money or an invoice number. The pattern used here has four parts:

1. a **private constructor**, so nobody can do `new Gstin("garbage")`
2. **`TryParse`**, which returns `false` for bad input (no exception)
3. **`Parse`**, which throws `FormatException` for bad input
4. **normalisation** inside parsing: trim and upper-case, so `" 29aagcb7383j1z4 "` becomes `29AAGCB7383J1Z4`

Once you hold a `Gstin`, it is valid. Code that receives a `Gstin` never has to check it again. That is "parse, don't validate".

**Why InvoiceFlow needs it:** GST rules depend on correct identifiers.

- A wrong GSTIN means the buyer loses input tax credit.
- The first two digits are the state code. `IsSameState` decides CGST + SGST (same state) or IGST (different states). That's Week 4.
- The 15th character is a checksum. `HasValidChecksum` catches typos the regex can't.

**Two real bugs we fixed:**

- **Leading zero (`00c1995`):** `StateCode` was `int.Parse(Value[..2])`, which turned `"07"` (Delhi) into `7`. State codes are identifiers, not numbers, so it became a `string`. *Rule: if you never do maths on it, it's not a number* (phone numbers, PIN codes, state codes).
- **`$` vs `\z` (`28f282b`):** see [topic 6](#6-regular-expressions-generatedregex-and-z).

**Where:** [Gstin.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/ValueObjects/Gstin.cs), [InvoiceNumber.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/ValueObjects/InvoiceNumber.cs), [GstinTests.cs](../tests/InvoiceFlow.SharedKernel.Tests/ValueObjects/GstinTests.cs)

**Common mistake:** passing `string gstin` around and validating it in five places, so one of them eventually forgets.

**Self-check**

1. When would you call `TryParse`, and when `Parse`?
2. Why is the constructor private?
3. Why is `StateCode` a `string` and not an `int`?

<details><summary>Answers</summary>

1. Use `TryParse` when bad input is expected and you want to handle it, such as user input in a validator. Use `Parse` when the input should already be valid, and invalid input means a bug. For example, a handler after validation has passed.
2. So the only way to get an instance is through parsing, which validates and normalises. A public constructor would let invalid values in.
3. It's an identifier. `int` drops the leading zero (`"07"` becomes `7`), and you never add or multiply state codes.

</details>

---

### 4. Modern C# syntax used everywhere

**What it is:** newer C# features that make code shorter and clearer. All of these appear in the project.

| Feature | Example from InvoiceFlow | Meaning |
|---|---|---|
| Primary constructor | `class LocalInvoiceFileStore(IOptions<IntakeOptions> options, TimeProvider timeProvider)` | Constructor parameters declared on the class line; usable in the whole class |
| Collection expression | `List<string> failures = [];` and `[FileField] = [message]` | `[]` creates any collection type |
| Relational and logical patterns | `c is < 'A' or > 'Z'`, `request.Amount is null or <= 0` | Readable range checks without `&&` and `||` chains |
| Range and index | `Value[..2]` (first 2 chars), `Tiers[^1]` (last tier) | Slices and from-the-end indexing |
| File-scoped namespace | `namespace InvoiceFlow.Intake;` | One less level of indentation |
| `u8` string literal | `"%PDF-"u8` | A UTF-8 byte span made at compile time |
| Target-typed `new` | `Dictionary<string, string[]> errors = new();` | The type is written once |

**Where:** almost every file. See the primary constructors in [LocalInvoiceFileStore.cs](../src/Modules/Intake/InvoiceFlow.Intake/Files/LocalInvoiceFileStore.cs) and [CorrelationIdMiddleware.cs](../src/Host/InvoiceFlow.Api/Middleware/CorrelationIdMiddleware.cs).

**Common mistake:** primary constructor parameters are **not readonly fields**. They can be reassigned inside the class. When the value must not change, copy it into a `private readonly` field, as `LocalInvoiceFileStore` does with `_rootPath`.

**Self-check**

1. What is the difference between a primary-constructor parameter and a `readonly` field?
2. Rewrite `x >= 'A' && x <= 'Z'` as a pattern.

<details><summary>Answers</summary>

1. A primary-constructor parameter is captured for the whole class but stays mutable: you can assign to it. A `readonly` field can only be set in the constructor. For values that must not change, copy the parameter into a `readonly` field.
2. `x is >= 'A' and <= 'Z'`

</details>

---

### 5. Nullable reference types and `[NotNullWhen]`

**What it is:** with `<Nullable>enable</Nullable>`, `string` means "never null" and `string?` means "may be null". The compiler warns when you might use a null. Because warnings are errors here, that warning fails the build.

`[NotNullWhen(true)]` tells the compiler: "if this method returns `true`, this parameter is not null."

```csharp
public static bool TryParse([NotNullWhen(true)] string? value,
                            [NotNullWhen(true)] out Gstin? gstin)

if (Gstin.TryParse(input, out Gstin? gstin))
{
    gstin.StateCode;   // no warning: the compiler knows gstin isn't null here
}
```

**Why InvoiceFlow needs it:** null bugs show up at compile time instead of as a `NullReferenceException` in production. The attribute makes the `TryParse` pattern work cleanly with nullable checking.

**Where:** `TryParse` in [Gstin.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/ValueObjects/Gstin.cs) and [InvoiceNumber.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/ValueObjects/InvoiceNumber.cs), and `IsValidCorrelationId` in [CorrelationIdMiddleware.cs](../src/Host/InvoiceFlow.Api/Middleware/CorrelationIdMiddleware.cs)

**Common mistake:** silencing warnings with `!` (the null-forgiving operator) everywhere. `!` is a promise to the compiler. Use it only when something else guarantees the value. For example, `request.InvoiceNumber!` in the submit handler is safe because the validation filter has already run.

**Self-check**

1. What does `[NotNullWhen(true)]` on an `out` parameter tell the compiler?
2. When is `!` acceptable?

<details><summary>Answers</summary>

1. When the method returns `true`, the `out` value is not null, so the caller can use it without a null check inside `if (TryParse(...))`.
2. Only when something the compiler can't see guarantees non-null, such as a validator that already ran. Otherwise, fix the type or add a check.

</details>

---

### 6. Regular expressions: `[GeneratedRegex]` and `\z`

**What it is:**

- **`[GeneratedRegex]`** is a source generator. The regex is compiled into C# code at build time instead of being parsed at run time. That makes it faster, and an invalid pattern fails at build time instead of in production. The class and the method must be `partial`.
- **`$` vs `\z`:** in .NET, `$` matches at the end of the string **or just before a final `\n`**. So `^...$` accepts `"27AAPFU0939F1ZV\n"`. `\z` matches only the true end.

**Why InvoiceFlow needs it:**

- GSTIN pattern: 2 digits (state) + 10 characters (PAN) + entity code + `Z` + check character.
- Invoice number pattern: 1–16 of `A–Z`, `0–9`, `/` and `-` (the GST invoice number rule).
- Fixed in `28f282b`. Our `Trim()` happens to remove the newline first, but a pattern should be correct on its own and not rely on the caller.

**Where:** [Gstin.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/ValueObjects/Gstin.cs), [InvoiceNumber.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/ValueObjects/InvoiceNumber.cs)

**Common mistake:** writing `^...$` for "the whole string must match" in .NET. Use `\z`.

**Self-check**

1. Does `^[A-Z]{3}$` match `"ABC\n"` in .NET? And `^[A-Z]{3}\z`?
2. What does `[GeneratedRegex]` give you over `new Regex(...)`?

<details><summary>Answers</summary>

1. The first matches, because `$` allows a trailing `\n`. The second does not.
2. The regex is turned into code at build time: no parsing at start-up, faster matching, and a broken pattern is a build error instead of a runtime exception.

</details>

---

### 7. Money: `decimal`, rounding and currency

**What it is:**

- **`decimal`, not `double`:** `double` is binary floating point, so `0.1 + 0.2` is `0.30000000000000004`. `decimal` stores base-10 digits exactly, which is what money needs.
- **Banker's rounding:** .NET's default rounding is `MidpointRounding.ToEven`, so `2.345` becomes `2.34` (it rounds to the even neighbour). Invoices and tax use "round half up", so `2.345` should become `2.35`. That's `MidpointRounding.AwayFromZero`.
- **Currency safety:** adding INR to USD throws `InvalidOperationException` instead of producing nonsense.
- **Indian formatting:** the `en-IN` culture groups digits in lakhs, as in `₹1,00,000.00`.

**Why InvoiceFlow needs it:** every invoice total, tax amount and approval threshold is a `Money`. A one-paisa rounding difference between our total and the vendor's total fails GST checks.

**Where:** [Money.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/ValueObjects/Money.cs), [MoneyTests.cs](../tests/InvoiceFlow.SharedKernel.Tests/ValueObjects/MoneyTests.cs)

**Common mistakes:**

- Using `double` for money.
- Forgetting that `Math.Round(x, 2)` uses banker's rounding by default.
- **Known gap for Week 7:** small Docker images run in *globalization-invariant mode*, where `CultureInfo.GetCultureInfo("en-IN")` throws. Only `Money.ToString()` would break. It is noted for the Docker week.

**Self-check**

1. What is `decimal.Round(2.345m, 2)`? And with `MidpointRounding.AwayFromZero`?
2. Why does `Money` round in its constructor and not when displaying?
3. What happens with `Money.Inr(10) + Money.Create(10, "USD")`?

<details><summary>Answers</summary>

1. `2.34` (banker's rounding, to the even digit), then `2.35`.
2. So every `Money` that exists is already at 2 decimals. Equality, sums and comparisons all work on the same rounded value. Rounding only for display would let `100.004` and `100.001` look equal but compare unequal.
3. `InvalidOperationException`, because the currencies differ.

</details>

---

## Day 2: async/await

### 8. How async/await works

**What it is:**

- **A `Task` is a promise of a result, not a thread.** `await Task.Delay(300)` uses a timer, and no thread waits for those 300 ms.
- **The compiler rewrites an `async` method into a state machine.** Each `await` is a point where the method can pause:
  1. If the awaited task is already finished, it carries on immediately.
  2. If not, it registers "call me back when done" and **returns to its caller**, freeing the thread.
  3. When the task finishes, the rest of the method (the *continuation*) runs, usually on a thread-pool thread.
- Local variables survive across `await`s because the compiler moves them into fields of the state machine.

**Why InvoiceFlow needs it:** a web server has a limited pool of threads. While a request waits on disk I/O (saving a PDF) or, later, on the database or the AI model, async gives the thread back so it can serve other requests. That is why the file store uses `FileOptions.Asynchronous`, `CopyToAsync` and `HashDataAsync`.

**Where:** [LocalInvoiceFileStore.cs](../src/Modules/Intake/InvoiceFlow.Intake/Files/LocalInvoiceFileStore.cs), [UploadInvoiceFile.cs](../src/Modules/Intake/InvoiceFlow.Intake/Endpoints/UploadInvoiceFile.cs), [AsyncLab](../labs/AsyncLab/Program.cs)

**Common mistakes:**

- **`async void`:** exceptions can't be caught, and nobody can await it. Use it only for event handlers.
- Writing `async` plus `Task.Run` around synchronous code in ASP.NET Core. That just moves the work to another pool thread and gains nothing.
- **Thread-pool starvation:** many requests blocking on `.Result` each hold a thread. The pool grows slowly, so requests queue up and the app looks frozen.

**Self-check**

1. Does `await Task.Delay(5000)` keep a thread busy for 5 seconds?
2. What does the compiler generate for an `async` method?
3. Why is blocking with `.Result` in ASP.NET Core bad even if it doesn't deadlock?

<details><summary>Answers</summary>

1. No. A timer completes the task, and no thread is blocked meanwhile.
2. A state machine type. The method body is split at each `await`. Local variables become fields, and a `MoveNext` method resumes from the right place when the awaited task completes.
3. Each blocked request holds a thread-pool thread while doing nothing. Under load the pool runs out (thread-pool starvation), and every request slows down.

</details>

---

### 9. Sync-over-async deadlocks and `ConfigureAwait`

**What it is:** a `SynchronizationContext` decides *where* a continuation runs after `await`.

- **UI apps (WinForms, WPF) and old ASP.NET:** the context sends the continuation back to the **original thread**.
- **Console apps and ASP.NET Core:** there is **no** context. The continuation runs on any thread-pool thread.

**The deadlock:** if the original thread blocks on `.Result`, the continuation is queued for that same thread, which is blocked waiting for the continuation. Neither can move.

`ConfigureAwait(false)` says "don't come back to the original context, any thread is fine". That breaks the cycle.

**Our experiment (real output from `dotnet run --project labs/AsyncLab`):**

```text
1. .Result, no context                     → result = invoice data       (no context, no deadlock)
2. .Result, single-thread context          → DEADLOCK: still waiting after 2 s
3. .Result, context + ConfigureAwait(false) → result = invoice data      (continuation ran on thread 5)
4. await (async all the way)               → result = invoice data
```

`SingleThreadContext` in the lab imitates a UI thread. It queues continuations, and nobody runs the queue while that thread is blocked.

**Why InvoiceFlow needs it:** ASP.NET Core has no `SynchronizationContext`, so this deadlock can't happen in our API. You still need to know it for:

- interviews ("why does `.Result` deadlock?")
- legacy .NET Framework code (the .NET Framework → .NET 10 migration question)
- library code, which may run under a UI context

**Where:** [labs/AsyncLab/Program.cs](../labs/AsyncLab/Program.cs)

**Common mistake:** thinking `ConfigureAwait(false)` is the fix. **The real fix is experiment 4: async all the way, never block.** `ConfigureAwait(false)` matters mainly in library code.

**Self-check**

1. Explain in two sentences why experiment 2 deadlocks.
2. Why doesn't the same code deadlock in ASP.NET Core?
3. Should application code in InvoiceFlow add `ConfigureAwait(false)` everywhere?

<details><summary>Answers</summary>

1. The `await` captured the single-thread context, so its continuation must run on that thread. That thread is blocked on `.Result`, waiting for the continuation, so each waits for the other forever.
2. ASP.NET Core has no `SynchronizationContext`. The continuation runs on any pool thread, so it can finish and release `.Result`. It is still bad, because of thread-pool starvation.
3. No. With no context there's nothing to avoid, so it adds noise. Use it in reusable libraries that might run under a UI context.

</details>

---

### 10. CancellationToken

**What it is:** a `CancellationToken` is a signal that says "stop, nobody wants the result any more". Code checks it, or passes it to async APIs that check it, and throws `OperationCanceledException`.

In ASP.NET Core, a `CancellationToken` parameter on an endpoint is bound to `HttpContext.RequestAborted`. It fires when the client disconnects.

**Why InvoiceFlow needs it:** if a vendor closes the browser halfway through a 10 MB upload, we should stop writing and hashing the file. Later the same applies to database queries and AI calls, which cost money.

**Where:**

- [LocalInvoiceFileStore.cs](../src/Modules/Intake/InvoiceFlow.Intake/Files/LocalInvoiceFileStore.cs) passes the token to `CopyToAsync` and `HashDataAsync`.
- On *any* failure, including cancellation, it deletes the half-written file and re-throws:

  ```csharp
  catch
  {
      File.Delete(path);
      throw;      // `throw;` keeps the original stack trace; `throw ex;` would lose it
  }
  ```

- Test: `SaveAsync_CancelledToken_ThrowsAndLeavesNoFile` in [LocalInvoiceFileStoreTests.cs](../tests/InvoiceFlow.Intake.Tests/Files/LocalInvoiceFileStoreTests.cs)

**Controllers:** identical. Add a `CancellationToken` parameter to the action.

**Common mistake:** accepting a token but not passing it down. The method looks cancellable but isn't.

**Self-check**

1. Where does the endpoint's `CancellationToken` come from?
2. Why does the file store delete the file in `catch` and then `throw;`?

<details><summary>Answers</summary>

1. ASP.NET Core binds it to `HttpContext.RequestAborted`, which is cancelled when the client disconnects.
2. To leave no half-written file on disk, while still letting the caller see the original exception with its stack trace.

</details>

---

### 11. Handling uploaded files safely

**What it is:** a set of rules for accepting files from users.

| Rule | How InvoiceFlow does it |
|---|---|
| Never trust the file name | Saved as `{Guid}.pdf`. The original name goes through `Path.GetFileName`, which removes paths like `..\..\` (path traversal) |
| Never trust `Content-Type` | The client can send anything. We also check the first 5 bytes are `%PDF-` (the "magic number" or file signature) |
| Limit the size | `MaxUploadSizeBytes` (10 MB) from validated options |
| Never overwrite | `FileMode.CreateNew` throws if the file already exists |
| Stream, don't buffer | `CopyToAsync` streams to disk instead of loading the whole file into memory |
| Fingerprint it | A SHA-256 hash, useful later for spotting duplicate uploads |
| Spread files out | `yyyy/MM` folders so one folder doesn't hold millions of files |

**Why InvoiceFlow needs it:** the upload endpoint is open to every vendor, so it is the easiest way to attack the system.

**Where:** [LocalInvoiceFileStore.cs](../src/Modules/Intake/InvoiceFlow.Intake/Files/LocalInvoiceFileStore.cs), [UploadInvoiceFile.cs](../src/Modules/Intake/InvoiceFlow.Intake/Endpoints/UploadInvoiceFile.cs). The endpoint side (typed results and errors) is covered in Week 2.

**Known gaps:** files have no owner yet (issue #14, Week 3), and local disk must become Blob Storage when running in containers (Week 7).

**Common mistake:** `Path.Combine(folder, file.FileName)` with the client's file name. A name like `..\..\appsettings.json` can then overwrite files outside the upload folder.

**Self-check**

1. Why check the `%PDF-` signature when `Content-Type` is already `application/pdf`?
2. Why save as a GUID instead of the original file name?

<details><summary>Answers</summary>

1. The client sets `Content-Type`, so it proves nothing. The first bytes show what the file actually is.
2. Client file names can contain path tricks, can clash with other uploads, and can contain characters the file system rejects. A GUID is unique and safe.

</details>

---

## Day 3: Dependency injection

### 12. Dependency injection lifetimes

**What it is:** the DI container creates objects for you and decides how long each one lives.

| Lifetime | One instance per… | Typical use |
|---|---|---|
| **Singleton** | the whole app | Stateless services, caches, in-memory stores |
| **Scoped** | HTTP request (scope) | Anything per request: later, the EF Core `DbContext` |
| **Transient** | every time it's requested | Small, cheap, stateless helpers |

**Why InvoiceFlow needs it:** the in-memory repositories (`InMemoryVendorRepository` and the rest) **must** be singletons. If they were scoped, every request would get an empty store and lose all data. In Week 3 the EF Core `DbContext` will be scoped, so the lifetime rules start to matter a lot.

**Where:**

- registrations in [IntakeModule.cs](../src/Modules/Intake/InvoiceFlow.Intake/IntakeModule.cs) and [ApprovalModule.cs](../src/Modules/Approval/InvoiceFlow.Approval/ApprovalModule.cs)
- proven with tests in [DependencyInjectionLifetimeTests.cs](../tests/InvoiceFlow.LearningTests/DependencyInjectionLifetimeTests.cs)

**Controllers:** the same. Controllers themselves are created per request, like scoped services.

**Common mistake:** a singleton that holds per-user or per-request data. That leaks data between users.

**Self-check**

1. What happens if `InMemoryInvoiceRepository` is registered as scoped?
2. A transient is injected into a singleton. How long does that transient live?

<details><summary>Answers</summary>

1. Each request gets a new, empty repository, so a submitted invoice is gone on the next request.
2. As long as the singleton, which is the whole app. It was created once, when the singleton was created. The learning test `Transient_InsideSingleton_LivesAsLongAsTheSingleton` proves it.

</details>

---

### 13. Captive dependencies

**What it is:** a longer-lived service holding a shorter-lived one. The classic case is a **singleton that depends on a scoped service**. The scoped service is created once and then "captured" for the whole app life, so it is shared by every request.

The container can catch this:

- `ValidateScopes = true`: throws when a scoped service is resolved from the root.
- `ValidateOnBuild = true`: checks every registration when the container is built.

ASP.NET Core turns **both on automatically in the Development environment**.

**Why InvoiceFlow needs it:** in Week 3, if anything singleton (the file store, the idempotency store) takes the scoped `DbContext` in its constructor, that is a captive dependency. `DbContext` isn't thread-safe, so it would cause random failures under load.

**Where:** `CaptiveDependency_IsRejected` in [DependencyInjectionLifetimeTests.cs](../tests/InvoiceFlow.LearningTests/DependencyInjectionLifetimeTests.cs)

**Common mistake:** "fixing" the startup error by making the scoped service a singleton. The right fix is to make the consumer scoped, or to create a scope when needed (`IServiceScopeFactory`).

**Self-check**

1. What is a captive dependency? Give an example that could happen in InvoiceFlow.
2. How do you detect one before production?

<details><summary>Answers</summary>

1. A long-lived service holding a short-lived one, so the short-lived one effectively becomes long-lived. For example, a singleton `LocalInvoiceFileStore` taking a scoped `IntakeDbContext` in Week 3. One `DbContext` would then be shared across all requests and threads.
2. Turn on `ValidateScopes` and `ValidateOnBuild` (on by default in Development). Startup then fails with "Cannot consume scoped service … from singleton …".

</details>

---

### 14. The options pattern with validation

**What it is:** configuration (`appsettings.json`, environment variables, user secrets) is bound to a strongly typed class, then injected.

| Interface | Lifetime | Reads config | Use when |
|---|---|---|---|
| `IOptions<T>` | Singleton | Once, at first use | Values that never change at run time |
| `IOptionsSnapshot<T>` | Scoped | Once per request | Per-request fresh values (can't go in a singleton) |
| `IOptionsMonitor<T>` | Singleton | Live: `CurrentValue` follows file changes | Long-lived services that need reloads |

Validation, so bad config fails **at startup** and not on the first request:

- `.ValidateDataAnnotations()` uses `[Required]` and `[Range]` on the class. Intake uses this.
- A custom `IValidateOptions<T>` handles rules attributes can't express. Approval tiers must be ascending, and approvers must not decrease.
- `.ValidateOnStart()` runs the validation when the app starts.
- `ErrorOnUnknownConfiguration = true` makes a typo such as `"Teirs"` an error instead of being silently ignored.
- `.PostConfigure(...)` adjusts values after binding. Intake turns a relative `StoragePath` into a full path under the content root.

**Why InvoiceFlow needs it:**

- The approval thresholds (up to ₹1 lakh = 1 approver, up to ₹10 lakh = 2, above that 3) are business rules that must be changeable without a code change. A wrong config here means invoices get the wrong number of approvers.
- `ApprovalPolicy` uses `IOptionsMonitor`, so a finance admin's change to `appsettings.json` applies without a restart.

**Where:**

- [IntakeOptions.cs](../src/Modules/Intake/InvoiceFlow.Intake/IntakeOptions.cs)
- [ApprovalOptions.cs](../src/Modules/Approval/InvoiceFlow.Approval/ApprovalOptions.cs) and [ApprovalOptionsValidator.cs](../src/Modules/Approval/InvoiceFlow.Approval/ApprovalOptionsValidator.cs)
- [ApprovalPolicy.cs](../src/Modules/Approval/InvoiceFlow.Approval/ApprovalPolicy.cs)
- registration in the two `XxxModule.cs` files
- the values in [appsettings.json](../src/Host/InvoiceFlow.Api/appsettings.json)

**Common mistakes:**

- Injecting `IOptionsSnapshot<T>` into a singleton. That's a captive dependency, and it throws.
- Reading `IConfiguration["Approval:Tiers:0:UpToAmount"]` directly: stringly typed and unvalidated.

**Self-check**

1. Why does `ApprovalPolicy` use `IOptionsMonitor` instead of `IOptions`?
2. What does `ValidateOnStart()` change?
3. Name a rule that `[Range]` can't express but `ApprovalOptionsValidator` does.

<details><summary>Answers</summary>

1. It is a singleton, and approval tiers should follow config changes without a restart. `IOptions` would read them once and keep them forever. `IOptionsSnapshot` can't be used in a singleton.
2. Without it, validation runs the first time someone reads the options, which could be a user's request hours later. With it, the app refuses to start with bad config.
3. "Tiers must be strictly ascending by amount" or "required approvers must not decrease". These rules compare items to each other, which a single-property attribute can't do.

</details>

---

### 15. Module registration methods

**What it is:** each module exposes one extension method, `AddXxxModule(this IServiceCollection, IConfiguration)`, that registers everything it needs. Modules that have endpoints also expose `MapXxxEndpoints(this IEndpointRouteBuilder)`.

`Program.cs` then reads like a table of contents:

```csharp
builder.Services.AddIntakeModule(builder.Configuration);
builder.Services.AddApprovalModule(builder.Configuration);
// ...
app.MapIntakeEndpoints();
app.MapApprovalEndpoints();
```

**Why InvoiceFlow needs it:**

- The host doesn't need to know a module's internal classes. Those are `internal`, so it *can't* know them.
- In Part 2, a module becomes a microservice by moving its `AddXxxModule` call into a new host.

Extraction, Validation, Payments and Reporting already have empty `AddXxxModule` methods (`a323bfe`), so the host is wired before they have code.

**Where:** [IntakeModule.cs](../src/Modules/Intake/InvoiceFlow.Intake/IntakeModule.cs), [ApprovalModule.cs](../src/Modules/Approval/InvoiceFlow.Approval/ApprovalModule.cs), [PaymentsModule.cs](../src/Modules/Payments/InvoiceFlow.Payments/PaymentsModule.cs), [Program.cs](../src/Host/InvoiceFlow.Api/Program.cs)

**Controllers:** the same. Controllers in a class library also need `AddControllers().AddApplicationPart(...)`.

**Common mistake:** registering a module's services in `Program.cs` directly. The host then depends on the module's internals, and the boundary is gone.

**Self-check**

1. Why can't `Program.cs` register `LocalInvoiceFileStore` itself?
2. How does this pattern help the Part 2 split into microservices?

<details><summary>Answers</summary>

1. `LocalInvoiceFileStore` is `internal` to the Intake project, so the host can't see it. Only `IntakeModule` is public.
2. Each module's setup is one call. A new service host calls `AddIntakeModule` and `MapIntakeEndpoints`, and the module comes with everything it needs.

</details>

---

## Day 4: Middleware and module boundaries

### 16. The middleware pipeline

**What it is:** every HTTP request passes through a chain of middleware in the order you register them, and the response comes back through them in reverse. Each one can act before and after calling `next`, or stop the chain.

```
Request  → CorrelationId → RequestTiming → ExceptionHandler → … → Authentication → Authorization → RateLimiter → Endpoint
Response ← CorrelationId ← RequestTiming ← ExceptionHandler ← … ←                                              ←
```

| Method | Meaning |
|---|---|
| `app.Use(...)` / `UseMiddleware<T>()` | Do something, then call `next` |
| `app.Run(...)` | End of the chain; never calls `next` |
| `app.Map("/path", ...)` | Branch the pipeline for a path |

**Middleware vs endpoint filters:**

- **Middleware** runs for *every* request and knows only `HttpContext`.
- **Filters** (Week 2) run only for the endpoints they're attached to, and see the bound arguments (for example the `SubmitInvoiceRequest`).

**Why order matters in InvoiceFlow:**

1. `UseCorrelationId` comes **first**, so every later log line and error response has the ID.
2. `UseRequestTiming` comes **second**. It wraps everything after it, so it measures the whole request and sees the final status code, even when the exception handler turned an exception into a 500.
3. Authentication comes before Authorization: you must know *who* before deciding *what they may do*.

**Where:** [Program.cs](../src/Host/InvoiceFlow.Api/Program.cs)

**Controllers:** the same pipeline. Controllers are the endpoint at the end of it.

**Common mistake:** a conventional middleware class is created **once**, so it behaves like a singleton. Injecting a scoped service into its constructor is a captive dependency. Put scoped services as parameters of `InvokeAsync` instead.

**Self-check**

1. Why is `UseCorrelationId` registered before everything else?
2. What happens if `UseAuthorization` comes before `UseAuthentication`?
3. Middleware or filter: where would you check a request body's idempotency key, and why?

<details><summary>Answers</summary>

1. Everything after it, including exception handling and every log line, can then use the correlation ID. Anything before it would log without the ID.
2. Authorization runs while the user is still anonymous, so authenticated requests get 401 or 403.
3. In a filter. It applies only to some endpoints and needs the bound request object to compare bodies. That's what `IdempotencyFilter` does in Week 2.

</details>

---

### 17. Correlation ID and request timing middleware

**What it is:**

- **Correlation ID:** a unique ID per request, put in the `X-Correlation-ID` response header and on every log line for that request. When a user reports an error, that ID finds every related log entry.
- **Request timing:** logs method, path, status code and duration for each request. Requests slower than 500 ms are logged as **Warning**, so slow requests stand out.

**How ours works:**

1. If the client sends a valid `X-Correlation-ID`, reuse it. This lets a frontend or another service trace one action across systems. Otherwise, create one with `Guid.NewGuid().ToString("N")`.
2. **Validate the incoming value:** at most 64 characters, letters, digits and `-` only. It's user input that we echo into headers and logs. Without the check, someone could send a huge value or inject fake log lines.
3. Set `context.TraceIdentifier = id`. In Week 2, ProblemDetails error responses use `TraceIdentifier` as `correlationId`, so the ID a user sees in an error matches the logs.
4. `logger.BeginScope(...)` attaches the ID to every log written during the request.

Timing uses `Stopwatch.GetTimestamp()` (no allocation) and logs in a `finally`, so failed requests are timed too.

**Where:** [CorrelationIdMiddleware.cs](../src/Host/InvoiceFlow.Api/Middleware/CorrelationIdMiddleware.cs), [RequestTimingMiddleware.cs](../src/Host/InvoiceFlow.Api/Middleware/RequestTimingMiddleware.cs), [CorrelationIdMiddlewareTests.cs](../tests/InvoiceFlow.Api.Tests/Middleware/CorrelationIdMiddlewareTests.cs)

**Common mistakes:**

- Trusting the incoming header without checks.
- Logging in the `try` block only, so failed requests never get a timing line.

**Self-check**

1. Why validate the incoming correlation ID?
2. Why log the timing in `finally`?
3. How does the correlation ID reach the error responses?

<details><summary>Answers</summary>

1. It comes from the client and is written into response headers and logs. Limiting length and characters stops oversized headers and log injection.
2. So the line is written even when the request throws. Failing requests are often the ones you most need to see.
3. The middleware sets `HttpContext.TraceIdentifier` to the ID. The ProblemDetails customisation (Week 2) copies `TraceIdentifier` into the `correlationId` field.

</details>

---

### 18. Modular monolith

**What it is:** one deployable app, split inside into modules with strict boundaries. Each module owns its own code and, from Week 3, its own data. Modules talk only through public contracts, never through each other's internals or tables.

```
Host (InvoiceFlow.Api) ──references──▶ every module + SharedKernel
Each module           ──references──▶ SharedKernel only
SharedKernel          ──references──▶ nothing in InvoiceFlow, and not ASP.NET Core
```

| Project | Role |
|---|---|
| `InvoiceFlow.Api` (Host) | Starts the app, the pipeline, auth, error handling, wires modules together |
| `Intake` | Vendors, invoice submission, file upload |
| `Extraction` | AI reads invoices (Week 8) |
| `Validation` | Rules engine: duplicates, GST maths, 3-way match (Week 4) |
| `Approval` | Approval requests and limits |
| `Payments` | Payment scheduling |
| `Reporting` | Reports such as invoice aging (Week 3) |
| `SharedKernel` | Value objects (`Money`, `Gstin`, `InvoiceNumber`) and shared names (roles, claims, policies) |

Everything inside a module is `internal` except its `XxxModule` class. Test projects see internals through `<InternalsVisibleTo Include="InvoiceFlow.Intake.Tests" />` in the module's `.csproj`.

**Why InvoiceFlow needs it:**

- **Simpler than microservices now:** one deploy, one debugger, no network calls, no distributed transactions.
- **Ready for Part 2:** because boundaries are already enforced, splitting a module into a service later (Weeks 13–20) is mechanical.

**Where:** the `src/` folder layout, each module's `.csproj`, [InvoiceFlow.Intake.csproj](../src/Modules/Intake/InvoiceFlow.Intake/InvoiceFlow.Intake.csproj)

**Common mistake:** "just this once" referencing another module to reuse a class. That one reference ties the modules together and makes the later split painful. Put truly shared types in SharedKernel, or communicate through events (Week 4).

**Self-check**

1. What is a modular monolith, and why start with one instead of microservices?
2. Why is almost everything in a module `internal`?
3. What may SharedKernel contain, and what must it not?

<details><summary>Answers</summary>

1. One deployable app made of strictly separated modules. You get clean boundaries without the cost of network calls, distributed data and complex deployment. When a module really needs to scale on its own, it can be split out.
2. So other projects physically can't use a module's internals. The compiler enforces the boundary, and only the `XxxModule` entry point is public.
3. Small, stable, shared building blocks: value objects and constant names. Never business logic of one module, and never ASP.NET Core. That keeps it lightweight and reusable by future services.

</details>

---

### 19. Architecture tests

**What it is:** unit tests that check the *structure* of the code instead of its behaviour. NetArchTest inspects the compiled assemblies and fails if a rule is broken.

**Our three rules:**

| Test | Rule |
|---|---|
| `Module_DoesNotDependOnOtherModules` | No module uses a type from another module |
| `Module_OnlyExposesItsModuleClass` | The only public type in a module is the one ending in `Module` |
| `SharedKernel_DoesNotDependOnModulesOrAspNetCore` | SharedKernel stays independent of modules and of ASP.NET Core |

The module tests are `[Theory]` tests with one row per module, so 6 modules × 2 rules + 1 = 13 tests.

**Why InvoiceFlow needs it:** `internal` and project references protect the boundaries, but people forget, and code review misses things. These tests turn the architecture rules into a red build. They run in CI from Week 7.

**Where:** [ModuleBoundaryTests.cs](../tests/InvoiceFlow.ArchitectureTests/ModuleBoundaryTests.cs)

**Common mistake:** adding a public request or DTO type to a module "so the test project can see it". Use `InternalsVisibleTo` instead. The second rule would catch it.

**Self-check**

1. What would make `Module_OnlyExposesItsModuleClass` fail?
2. Why does SharedKernel forbid ASP.NET Core?

<details><summary>Answers</summary>

1. Any `public` type in a module whose name doesn't end with `Module`, for example `public sealed record InvoiceResponse`.
2. SharedKernel will be used by every module and, in Part 2, by separate services and workers that may not be web apps. Depending on ASP.NET Core would drag the whole web framework into all of them.

</details>

---

### 20. Unit testing with xUnit

**What it is:** xUnit is the test framework used in every test project.

| Feature | Use |
|---|---|
| `[Fact]` | A single test case |
| `[Theory]` + `[InlineData(...)]` | The same test with several inputs |
| `[Theory]` + `[MemberData(nameof(X))]` with `TheoryData<...>` | Inputs that can't go in an attribute, such as `byte[]`, or computed lists |
| Constructor and `IDisposable` | xUnit creates a **new test class instance for every test**: set up in the constructor, clean up in `Dispose` |
| `Assert.Throws<T>` | The code must throw `T` |
| `Assert.IsType<T>` | Checks the type and returns it cast |

**Habits used in InvoiceFlow:**

- **Naming:** `Method_Scenario_ExpectedResult`, such as `SaveAsync_CancelledToken_ThrowsAndLeavesNoFile`.
- **Arrange, Act, Assert:** in that order, separated by blank lines.
- **Isolation:** the file store tests write to a unique temp folder per test and delete it in `Dispose`.
- **"Learning tests":** tests that prove how a framework behaves (DI lifetimes) instead of testing our code.

**Where:** all of [tests/](../tests). Good examples are [MoneyTests.cs](../tests/InvoiceFlow.SharedKernel.Tests/ValueObjects/MoneyTests.cs), [LocalInvoiceFileStoreTests.cs](../tests/InvoiceFlow.Intake.Tests/Files/LocalInvoiceFileStoreTests.cs) and [DependencyInjectionLifetimeTests.cs](../tests/InvoiceFlow.LearningTests/DependencyInjectionLifetimeTests.cs).

**Common mistake:** tests sharing state, such as a static field or the same temp folder. They pass alone and fail together, or fail randomly when run in parallel.

**Self-check**

1. `[InlineData]` or `[MemberData]`: which for `byte[]` inputs, and why?
2. How many times is a test class's constructor called for a class with 5 tests?
3. What is a learning test?

<details><summary>Answers</summary>

1. `[MemberData]`. Attribute arguments must be compile-time constants, and `byte[]` built from strings isn't one.
2. Five times. xUnit creates a new instance per test, which keeps tests independent.
3. A test that pins down how a library or framework behaves, so you learn it and are warned if an upgrade changes it. For example, "a transient inside a singleton lives as long as the singleton".

</details>

---

# Week 2: ASP.NET Core Web APIs done properly

**Goal of the week:** build an API a senior reviewer would approve: validated, secured, documented, with consistent errors.

| Day | Topic | What we built | Main commits |
|---|---|---|---|
| 1 | Minimal APIs and validation | Vendor and Invoice domain, in-memory repositories, FluentValidation, register vendor, submit and read invoices, Swagger UI | `d3569c0`, `86c7cc0`, `083f638`, `c37c5d6`, `43b0ba7`, `31262d8` |
| 2 | Errors and logging | ProblemDetails, global exception handler, Serilog, business event logs | `2e3b408`, `c3a833a`, `249dcdf` |
| 3 | Authentication and authorization | JWT bearer, fallback policy, roles and policies, vendor ownership, approval requests with approval limits | `a9e19be`, `9ff07fc`, `d4fe374`, `c24d94e`, `8c22a76` |
| 4 | API design | Paged and filtered invoice list, vendor scope, idempotency keys, rate limiting | `e85c9fd`, `bc4aa51`, `4814245`, `d69d628` |
| Cleanup | Review fixes | Typed upload results, 403 documented, `TimeProvider` in the file store | `dd62bf4`, `4ff8352`, `0fa49f5`, `3d2d05e` |

---

## Day 1: Minimal APIs, validation and the domain

### 21. Minimal APIs and parameter binding

**What it is:** minimal APIs map a URL to a method with no controller class: `group.MapPost("/", SubmitInvoice)`. ASP.NET Core looks at the method's parameters and decides where each one comes from.

| Parameter in our handlers | Comes from |
|---|---|
| `Guid id` with route `/{id:guid}` | The route. `:guid` is a *constraint*: `/invoices/abc` gives 404, not a binding error |
| `SubmitInvoiceRequest request` | The JSON body (complex type on POST) |
| `[AsParameters] ListInvoicesRequest request` | Each property bound separately, here from the query string (`?page=2&pageSize=20`) |
| `IFormFile file` | The multipart form upload |
| `IVendorRepository`, `TimeProvider`, `ILoggerFactory` | Dependency injection |
| `ClaimsPrincipal user` | `HttpContext.User`, the signed-in user |
| `CancellationToken cancellationToken` | `HttpContext.RequestAborted` |

**Route groups** share a prefix and settings: `MapGroup("/api/intake")`, then `.MapGroup("/invoices").WithTags("Invoices")`. Each module maps its own groups in `MapXxxEndpoints`.

**Why InvoiceFlow uses minimal APIs:** less ceremony, and it's the direction ASP.NET Core is going. Endpoints sit inside each module next to the code they call. Handlers are `private static` methods, so they get everything through parameters and keep no hidden state.

**Where:** [InvoiceEndpoints.cs](../src/Modules/Intake/InvoiceFlow.Intake/Endpoints/InvoiceEndpoints.cs), [VendorEndpoints.cs](../src/Modules/Intake/InvoiceFlow.Intake/Endpoints/VendorEndpoints.cs), [ApprovalRequestEndpoints.cs](../src/Modules/Approval/InvoiceFlow.Approval/Endpoints/ApprovalRequestEndpoints.cs)

**Controllers:** `[ApiController]` classes with `[HttpPost]` actions and `[FromBody]`/`[FromQuery]`/`[FromRoute]`. Binding works the same way. `[ApiController]` also returns 400 automatically for invalid models; minimal APIs don't, which is why we add a validation filter ([topic 23](#23-validation-with-fluentvalidation-and-an-endpoint-filter)).

**Common mistake:** expecting `[AsParameters]` on a POST to read JSON. It binds each property separately (route, query, header). For a JSON body, use a plain complex parameter.

**Self-check**

1. Walk through what happens from a request reaching Kestrel to your handler running.
2. How does ASP.NET Core know `IVendorRepository` comes from DI and `SubmitInvoiceRequest` from the body?

<details><summary>Answers</summary>

1. Kestrel parses the HTTP request into an `HttpContext`. Routing, which `WebApplication` adds automatically at the start, matches the URL to an endpoint. Then our middleware runs in order: correlation ID, timing, exception handler, status code pages, HTTPS redirection. Authentication reads the JWT, authorization checks the endpoint's policy, and the rate limiter checks the quota. Then the endpoint runs: parameters are bound, the filters run (idempotency, then validation), and finally the handler. The result goes back out through the middleware in reverse.
2. When the app starts, each parameter type is checked: services registered in DI are injected, special types (`HttpContext`, `ClaimsPrincipal`, `CancellationToken`, `IFormFile`) are recognised, and remaining complex types on POST are read from the JSON body.

</details>

---

### 22. Request and response DTOs

**What it is:** a DTO (data transfer object) is a type that defines what goes over the wire. It is separate from the domain objects inside.

- **Request DTOs have nullable fields** (`string? InvoiceNumber`, `decimal? TotalAmount`). If `TotalAmount` were `decimal`, a missing field would silently become `0`. With `decimal?` it is `null`, and the validator can say "Total amount is required."
- **Response DTOs** (`InvoiceResponse`, `VendorResponse`, `UploadFileResponse`) are flat records with primitive values: `TotalAmount` is a `decimal`, not a `Money`.

**Why InvoiceFlow needs it:**

- The API contract and the domain change separately. A new field inside `Invoice` doesn't leak to clients until we decide.
- Week 5's TypeScript client is generated from these DTOs, so they must be stable.
- In the review we replaced the upload's `StoredFile` (a storage-layer record) with `UploadFileResponse`, for the same reason.

**Where:** the `*Request.cs` and `*Response.cs` files in [Intake/Endpoints](../src/Modules/Intake/InvoiceFlow.Intake/Endpoints) and [Approval/Endpoints](../src/Modules/Approval/InvoiceFlow.Approval/Endpoints). Mapping happens in `ToResponse` and `MapResponse` in the endpoint files.

**Common mistake:** returning the domain entity directly. You expose internals, create circular-reference problems later with EF Core, and every refactor becomes a breaking API change.

**Self-check**

1. Why is `TaxableAmount` a `decimal?` in the request, but a `decimal` in the response?
2. Why not return `Invoice` from the endpoint?

<details><summary>Answers</summary>

1. In the request, `null` means "the client didn't send it", which validation must report. In the response we always have a value, so it's not nullable.
2. The entity is internal design and will change (EF Core in Week 3, events in Week 4). The DTO is a contract with clients. Separating them means internal changes don't break clients, and nothing internal is exposed by accident.

</details>

---

### 23. Validation with FluentValidation and an endpoint filter

**What it is:**

- **FluentValidation:** validation rules written as code in a class per request: `RuleFor(r => r.Gstin).NotEmpty().Must(v => Gstin.TryParse(v, out _))`.
- **Endpoint filter:** code that runs around one endpoint, *after* binding and *before* the handler. It can see the bound arguments and can return a result instead of calling the handler.

Our generic `ValidationFilter<T>` finds the `T` argument, runs its validator, and returns `TypedResults.ValidationProblem(...)` (400 with field errors) if anything fails. Otherwise it calls `next`.

All validators are registered with one line: `AddValidatorsFromAssembly(..., includeInternalTypes: true)`. Internal types are needed because the validators are `internal`.

**Two layers of validation:**

| Layer | Example | On failure |
|---|---|---|
| Request validator | "Invoice date cannot be in the future", "GSTIN is invalid" | **400** with all field errors at once. Expected: users make mistakes |
| Domain invariant | `Invoice.Submit` throws if the total ≠ taxable + tax | **500**: should never happen if validation ran. It is a safety net that means a bug |

Rules like `Cascade(CascadeMode.Stop)` stop at the first failure for a field, so an empty GSTIN reports "required" and not also "invalid".

**Why InvoiceFlow needs it:** users see *every* problem in one response with field names, so a form can show each error next to its field. Rules that compare fields (total = taxable + tax within ₹0.01) are easy to express.

**Where:** [Validation/](../src/Modules/Intake/InvoiceFlow.Intake/Validation), [ValidationFilter.cs](../src/Modules/Intake/InvoiceFlow.Intake/Endpoints/ValidationFilter.cs), and the tests in [tests/InvoiceFlow.Intake.Tests/Validation](../tests/InvoiceFlow.Intake.Tests/Validation)

**Controllers:** with `[ApiController]`, data-annotation errors return 400 automatically. FluentValidation is usually called from an action filter or manually.

**Known inconsistency:** the Approval module validates by hand inside `CreateRequest` instead of with FluentValidation. Week 4 replaces that endpoint with an event, so it's left as is.

**Common mistake:** validating only in the domain (users get a 500 for a typo) or only in the request (other callers, such as a future event handler, can create invalid objects).

**Self-check**

1. Middleware or endpoint filter for validation, and why?
2. What is the difference between a validation error and a domain invariant failure, from the client's point of view?
3. Why is `includeInternalTypes: true` needed?

<details><summary>Answers</summary>

1. An endpoint filter. It runs only for the endpoints that need it, and it sees the already-bound request object. Middleware would have to read and parse the body itself.
2. A validation error is a 400 that lists what to fix. A domain invariant failure is a 500, which means our code has a bug, because validation should have caught it first.
3. Our validators are `internal` (module rules), and the assembly scan skips non-public types by default.

</details>

---

### 24. Rich domain entities

**What it is:** entities that protect their own rules instead of being bags of public setters.

- **Private constructor + static factory** (`Invoice.Submit(...)`, `Vendor.Register(...)`, `ApprovalRequest.Create(...)`). The factory checks every rule before creating the object.
- **`private set`:** nothing outside can change state directly. Changes go through methods like `ApprovalRequest.Approve(approverId, utcNow)`, which enforces "same approver can't approve twice" and "can't approve after fully approved".
- **Collections exposed read-only:** `Decisions => _decisions.AsReadOnly()`.
- **`Guid.CreateVersion7()`:** a GUID whose first part is a timestamp, so new IDs sort by creation time. That matters for database index performance in Week 3.
- **`FinancialYear`:** the Indian financial year runs April to March, so 15 March 2026 is in `2025-26` and 1 April 2026 is in `2026-27`. It's part of the duplicate-invoice rule.

**Why InvoiceFlow needs it:** money and approvals have rules that must hold no matter which endpoint, background job or future event handler touches the object.

**Where:** [Invoice.cs](../src/Modules/Intake/InvoiceFlow.Intake/Domain/Invoice.cs), [Vendor.cs](../src/Modules/Intake/InvoiceFlow.Intake/Domain/Vendor.cs), [FinancialYear.cs](../src/Modules/Intake/InvoiceFlow.Intake/Domain/FinancialYear.cs), [ApprovalRequest.cs](../src/Modules/Approval/InvoiceFlow.Approval/Domain/ApprovalRequest.cs)

**Week 3 note:** EF Core needs a way to create these objects when loading from the database. We'll add private parameterless constructors and map the value objects. That's planned.

**Common mistake:** `public Status { get; set; }` and setting `invoice.Status = Approved` from an endpoint, skipping every rule.

**Self-check**

1. Which financial year is an invoice dated 31 March 2026 in? And 1 April 2026?
2. Why does `ApprovalRequest` expose `IReadOnlyList<ApprovalDecision>` instead of `List<ApprovalDecision>`?
3. Why UUIDv7 instead of `Guid.NewGuid()`?

<details><summary>Answers</summary>

1. `2025-26`, then `2026-27`.
2. With a `List`, any caller could `Add` a decision and skip `Approve`'s checks (duplicate approver, already approved). Read-only access forces changes through the method that enforces the rules.
3. Version 7 GUIDs start with a timestamp, so they are roughly in order. Database indexes then insert at the end instead of at random pages, which is faster and fragments less. Random v4 GUIDs scatter inserts.

</details>

---

### 25. Time: `TimeProvider`, UTC and Indian dates

**What it is:**

- **`TimeProvider`** is the built-in .NET abstraction for "now". Production uses `TimeProvider.System` (registered once in `Program.cs`). Tests use `FakeTimeProvider` to set any date and move time forward.
- **Store times in UTC:** `ReceivedAtUtc`, `RegisteredAtUtc`. The domain even checks `DateTimeKind.Utc` and throws otherwise.
- **Business dates use the business's time zone:** "invoice date cannot be in the future" compares against **today in India** (`Asia/Kolkata`, UTC+05:30). At 1:00 AM IST it's still yesterday in UTC, so comparing against the UTC date would wrongly reject a valid invoice.
- **`DateOnly`** for dates without a time (invoice date, due date).

**Why InvoiceFlow needs it:** tests like "the idempotency entry expires after 24 hours" and "the file is stored under 2026/03" are impossible to write reliably with `DateTime.UtcNow`. With a fake clock they are exact.

**Where:** `TodayInIndia` in [SubmitInvoiceRequestValidator.cs](../src/Modules/Intake/InvoiceFlow.Intake/Validation/SubmitInvoiceRequestValidator.cs), [InMemoryIdempotencyStore.cs](../src/Modules/Intake/InvoiceFlow.Intake/Idempotency/InMemoryIdempotencyStore.cs), [LocalInvoiceFileStore.cs](../src/Modules/Intake/InvoiceFlow.Intake/Files/LocalInvoiceFileStore.cs) (fixed in the review), and `FakeTimeProvider` in the store tests

**Known gap for Week 7:** small Docker images may not include time-zone data, so `Asia/Kolkata` can fail to load. Either install tzdata or use a fixed +05:30 offset (India has no daylight saving).

**Common mistake:** `DateTime.Now` anywhere on a server. It depends on the server's time zone, which is usually UTC in the cloud and not what you think.

**Self-check**

1. Why compare the invoice date with "today in India" instead of today in UTC?
2. What does `TimeProvider` give you that `DateTime.UtcNow` doesn't?

<details><summary>Answers</summary>

1. Between 00:00 and 05:30 IST, the UTC date is still the previous day. An invoice dated "today" in India would look like it's in the future and be rejected.
2. It can be replaced. Tests inject a `FakeTimeProvider` with a fixed time and can advance it, so time-dependent logic (expiry, folders by month, "not in the future") is tested exactly.

</details>

---

### 26. In-memory repositories and the duplicate rule

**What it is:** a repository hides how data is stored behind an interface (`IInvoiceRepository`). Ours store data in memory, in `ConcurrentDictionary`, until EF Core arrives in Week 3.

- **Duplicate invoice rule:** the same vendor can't submit the same invoice number twice in one financial year. The repository keys a second dictionary by `(VendorId, Number, FinancialYear)`.
- **Thread-safe add:** `TryAdd` on the business key is atomic. If two identical requests arrive at the same time, exactly one wins and the other gets `false`, which becomes a 409.
- **Manual rollback:** the business key is added first, then the ID. If the second add fails, the first is removed so the two dictionaries agree.
- **Stable sorting for paging:** `OrderByDescending(ReceivedAtUtc).ThenByDescending(Id)`. The `ThenBy` breaks ties, so the same invoice never appears on two pages.

**Why InvoiceFlow needs it:** duplicate invoices paid twice is one of the main problems InvoiceFlow solves (see the README). The interface lets Week 3 swap in EF Core without touching the endpoints.

**Where:** [InMemoryInvoiceRepository.cs](../src/Modules/Intake/InvoiceFlow.Intake/Infrastructure/InMemoryInvoiceRepository.cs), [InMemoryVendorRepository.cs](../src/Modules/Intake/InvoiceFlow.Intake/Infrastructure/InMemoryVendorRepository.cs)

**Week 3 note:** the duplicate check becomes a **unique index** on `(VendorId, Number, FinancialYear)` in SQL Server. The database then guarantees it, even with several app instances.

**Common mistake:** "check then insert" (`if (!Exists(x)) Add(x)`). Two requests can both pass the check before either inserts. Use an atomic operation (`TryAdd`, or a unique index).

**Self-check**

1. Two identical submissions arrive at the same millisecond. What happens, and why?
2. Why does the list need `ThenByDescending(Id)`?

<details><summary>Answers</summary>

1. Both call `TryAdd` on the business-key dictionary. It is atomic, so one succeeds (201) and the other returns `false` (409 Duplicate invoice).
2. Two invoices can have the same `ReceivedAtUtc`. Without a tie-breaker their order can change between queries, so paging could show one twice and skip another.

</details>

---

## Day 2: Errors and logging

### 27. ProblemDetails: one error format everywhere

**What it is:** ProblemDetails (RFC 9457) is the standard JSON shape for HTTP errors: `type`, `title`, `status`, `detail`, plus extra fields. Validation errors use the same shape with an `errors` object per field (`HttpValidationProblemDetails`).

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "file": ["File must be a PDF."] },
  "correlationId": "4f1c..."
}
```

How InvoiceFlow makes *every* error look like this:

| Source of the error | How it becomes ProblemDetails |
|---|---|
| Our handlers | `TypedResults.Problem(...)` and `TypedResults.ValidationProblem(...)` |
| Empty error responses (401 and 403 from auth, 404 for an unknown route) | `UseStatusCodePages()` fills in a ProblemDetails body |
| Unhandled exceptions | `GlobalExceptionHandler` ([topic 28](#28-global-exception-handling)) |
| All of them | `AddProblemDetails(CustomizeProblemDetails = ...)` adds `correlationId` from `TraceIdentifier` |

**Why InvoiceFlow needs it:** the frontend (Week 5–6) needs one error parser, not one per endpoint. The `correlationId` lets support find the exact log lines for a user's error. The review fixed the upload endpoint, which returned `{ "error": "..." }`, the only endpoint breaking this rule.

**Where:** [ErrorHandlingExtensions.cs](../src/Host/InvoiceFlow.Api/ErrorHandling/ErrorHandlingExtensions.cs), [Program.cs](../src/Host/InvoiceFlow.Api/Program.cs), every endpoint file

**Controllers:** `return Problem(...)` and `ValidationProblem(ModelState)` produce the same shape.

**Common mistake:** returning custom error objects per endpoint. Every client then needs special cases.

**Self-check**

1. A request with no token gets a 401. Where does its JSON body come from?
2. How does `correlationId` get into every error?

<details><summary>Answers</summary>

1. The authentication middleware sets 401 with an empty body. `UseStatusCodePages` sees an error status with no body and writes ProblemDetails, using `IProblemDetailsService`.
2. `AddProblemDetails` registers `CustomizeProblemDetails`, which runs for every ProblemDetails the framework writes and copies `HttpContext.TraceIdentifier` into `correlationId`. The correlation middleware sets `TraceIdentifier` at the start of each request.

</details>

---

### 28. Global exception handling

**What it is:** `IExceptionHandler` is the .NET 8+ way to turn unhandled exceptions into responses in one place. `UseExceptionHandler()` catches the exception and calls our handler. The handler decides the status code and writes ProblemDetails.

Our `GlobalExceptionHandler`:

| Exception | Response | Log level |
|---|---|---|
| `BadHttpRequestException` (malformed JSON, missing form field) | Its status code (usually 400), title **"Invalid request"** | Warning: the client's fault |
| Anything else | **500**, title "An unexpected error occurred." | Error: our fault |

It **never** puts `exception.Message` or a stack trace in the response. Messages can reveal internals (SQL, file paths, class names). The full exception goes to the log only. It also re-sets the correlation header, because exception handling clears response headers.

**A detail:** in Development, minimal APIs throw `BadHttpRequestException` for binding failures, which our handler converts. In Production they set 400 directly, and `UseStatusCodePages` adds the body. Both paths give the client ProblemDetails.

**Why InvoiceFlow needs it:** safe, consistent 500s and a log entry with the correlation ID for every crash.

**Where:** [GlobalExceptionHandler.cs](../src/Host/InvoiceFlow.Api/ErrorHandling/GlobalExceptionHandler.cs), [GlobalExceptionHandlerTests.cs](../tests/InvoiceFlow.Api.Tests/ErrorHandling/GlobalExceptionHandlerTests.cs)

**Controllers:** the same handler works for both. Older code uses exception filters or custom middleware with `try/catch`.

**Common mistake:** using exceptions for expected outcomes, such as throwing `DuplicateInvoiceException` to get a 409. Expected outcomes should be return values (`TryAdd` → `false` → 409). Exceptions are for bugs and broken infrastructure.

**Self-check**

1. Why not return `exception.Message` in the 500 response?
2. Why is a `BadHttpRequestException` logged as Warning but other exceptions as Error?

<details><summary>Answers</summary>

1. It can leak internals (table names, paths, library details) that help attackers, and it's useless to normal users. The client gets a generic title plus the `correlationId`, and developers use that ID to find the full exception in the logs.
2. A bad request is the client's mistake and happens normally. An Error should mean "we have a bug to fix". Mixing them makes alerts noisy and real problems harder to see.

</details>

---

### 29. Structured logging with Serilog

**What it is:** structured logging saves each log entry as named fields, not just a string. Serilog is the logging library behind ASP.NET Core's `ILogger` in InvoiceFlow.

- **Message templates:** `logger.LogInformation("Invoice {InvoiceId} submitted by vendor {VendorId}", invoice.Id, invoice.VendorId)`. `InvoiceId` and `VendorId` become searchable fields: "show all logs where VendorId = X".
- **Two-stage setup:** a *bootstrap logger* is created first, so even startup crashes are logged (`Log.Fatal` in `catch`). Then `AddSerilog` replaces it with the full config from `appsettings.json`.
- **Enrichment:** `Enrich.FromLogContext()` adds the `CorrelationId` from the middleware's `BeginScope` to every entry.
- **Formats:** in Development, readable text with `({CorrelationId})`. Elsewhere, compact JSON (one JSON object per line) for log tools like Application Insights (Week 7).
- **Levels:** `Information` for business events (vendor registered, invoice submitted, approval given), `Warning` for rejected actions (duplicate, approval denied, slow request), `Error` for crashes. `Microsoft.AspNetCore` is raised to `Warning` to cut framework noise.

**What we never log:** tokens, passwords, bank account numbers, file contents. GSTIN and amounts are logged: they're business identifiers needed for investigations, and GSTIN is printed on every invoice.

**Why InvoiceFlow needs it:** "Why was this vendor's invoice rejected yesterday?" is a query on `VendorId`, not a text search through millions of lines.

**Where:** [Program.cs](../src/Host/InvoiceFlow.Api/Program.cs), [appsettings.json](../src/Host/InvoiceFlow.Api/appsettings.json), [appsettings.Development.json](../src/Host/InvoiceFlow.Api/appsettings.Development.json), the `logger.Log...` calls in the endpoint files

**Common mistake:** string interpolation in log calls: `logger.LogInformation($"Invoice {id}")`. The message becomes a plain string, so `id` is no longer a searchable field, and the string is built even when the level is disabled.

**Self-check**

1. What's the difference between `LogInformation("Invoice {Id}", id)` and `LogInformation($"Invoice {id}")`?
2. Why create a bootstrap logger before the host is built?
3. Which level would you use for "approval denied: amount over limit", and why?

<details><summary>Answers</summary>

1. The first keeps `Id` as a named property you can filter on, and skips formatting if the level is off. The second bakes the value into a string, so the structure is lost and the work is always done.
2. If configuration or DI setup fails, the full logger doesn't exist yet. The bootstrap logger makes sure that failure still gets written (`Log.Fatal`) instead of the app dying silently.
3. Warning. It's an expected, handled situation, not a bug, but someone may want to review denied attempts. Error should be kept for things that need fixing.

</details>

---

## Day 3: Authentication and authorization

### 30. JWT authentication

**What it is:** a JWT (JSON Web Token) is a signed token the client sends in every request: `Authorization: Bearer <token>`. It has three Base64Url parts, `header.payload.signature`.

| Part | Contains |
|---|---|
| Header | Algorithm (`alg`) and type |
| Payload (claims) | `sub` (user ID), `role`, `aud` (who it's for), `iss` (who issued it), `exp` (expiry), plus our own claims `vendor_id` and `approval_limit` |
| Signature | Proves the issuer created it and nobody changed it |

**The payload is readable by anyone. It's encoded, not encrypted.** Never put secrets in it.

**How our API validates a token** (`AddJwtBearer()`): signature with the issuer's key → issuer = `dotnet-user-jwts` → audience is our URL → not expired. Any failure gives 401.

In development, `dotnet user-jwts create --role Vendor --claim vendor_id=<guid>` creates tokens. The signing key is stored in user secrets, not in the repo. Settings come from `Authentication:Schemes:Bearer` in `appsettings.Development.json`. When real sign-in arrives (Week 6, Auth.js or Microsoft Entra ID), only the issuer settings change, not the code.

**A detail:** the JWT handler maps `sub` to `ClaimTypes.NameIdentifier` and `role` to `ClaimTypes.Role`. That's why our code reads `ClaimTypes.NameIdentifier` and `IsInRole` works.

**Why InvoiceFlow needs it:** the API is called by a separate frontend and later by services and agents. Tokens are stateless: any instance can validate them without a session store.

**Where:** [Program.cs](../src/Host/InvoiceFlow.Api/Program.cs), [appsettings.Development.json](../src/Host/InvoiceFlow.Api/appsettings.Development.json), [AppClaims.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/Security/AppClaims.cs)

**Common mistake:** trusting claims without validating the token (for example, decoding a JWT by hand). Only claims from a validated token can be trusted.

**Self-check**

1. What is inside a JWT, and how is it validated?
2. Can a vendor edit their token's payload to change `vendor_id`? What happens?

<details><summary>Answers</summary>

1. A header (algorithm), a payload of claims (user, roles, audience, issuer, expiry, custom claims) and a signature. The API checks the signature with the issuer's key, then the issuer, the audience and the expiry. Only then are the claims trusted.
2. They can edit the text, but the signature no longer matches, so validation fails and the request gets 401.

</details>

---

### 31. Secure by default: 401 vs 403

**What it is:**

- **Fallback policy:** `SetFallbackPolicy(RequireAuthenticatedUser)` applies to every endpoint that has no authorization rule of its own. A new endpoint is **locked by default**: forgetting `.RequireAuthorization` gives "signed-in users only", never "anyone".
- **Opting out is explicit:** `/health` and the OpenAPI document use `.AllowAnonymous()`.

| Code | Meaning | Example |
|---|---|---|
| **401 Unauthorized** | "Who are you?" No token, or an invalid or expired one | Calling any endpoint without a token |
| **403 Forbidden** | "I know who you are, and you can't do this" | A Vendor calling `POST /vendors` (Admin only); an approver over their limit |

**Why InvoiceFlow needs it:** an AP system holds supplier bank details and payment approvals. One forgotten attribute must not open an endpoint to the internet.

**Where:** [Program.cs](../src/Host/InvoiceFlow.Api/Program.cs)

**Controllers:** the same fallback policy covers controllers. `[AllowAnonymous]` opts out.

**Common mistake:** relying on remembering `[Authorize]` on every endpoint ("insecure by default").

**Self-check**

1. 401 vs 403 vs 422: when do you return each?
2. A teammate adds a new endpoint and forgets `.RequireAuthorization(...)`. Who can call it?

<details><summary>Answers</summary>

1. 401: the caller isn't authenticated (missing or bad token). 403: authenticated, but not allowed (wrong role, someone else's vendor, over the approval limit). 422: the request is well-formed and allowed, but refers to something that doesn't make sense, such as an unknown vendor or file in an otherwise valid submission.
2. Any authenticated user, whatever their role, because of the fallback policy. Anonymous callers get 401. Not perfect, but far safer than open to everyone.

</details>

---

### 32. Roles, policies and claims

**What it is:**

- **Role:** a group a user belongs to: `Vendor`, `ApClerk`, `Approver`, `Admin`.
- **Claim:** a fact about the user in the token: `vendor_id`, `approval_limit`.
- **Policy:** a named rule built from roles, claims or custom requirements. Endpoints ask for a **policy**, never a role directly: `.RequireAuthorization(IntakePolicies.SubmitInvoices)`.

| Policy | Allowed roles |
|---|---|
| `Intake.ManageVendors` | Admin |
| `Intake.SubmitInvoices` | Vendor, ApClerk |
| `Intake.ReadInvoices` | Vendor, ApClerk, Approver, Admin |
| `Approval.CreateRequests` | ApClerk |
| `Approval.Approve` | Approver only. **Admin can't approve** (separation of duties) |

Each module defines its own policies and registers them with `AddAuthorizationCore` inside `AddXxxModule`. A module only adds policy definitions. The host owns the full authorization stack (`AddAuthorizationBuilder` and the middleware). Role and claim **names** live in SharedKernel so every module spells them the same.

**Why InvoiceFlow needs it:** if "who can submit invoices" changes, one policy changes, not twenty endpoints. Separation of duties is an audit requirement in finance: the person who manages the system must not approve payments.

**Where:** [IntakePolicies.cs](../src/Modules/Intake/InvoiceFlow.Intake/Authorization/IntakePolicies.cs), [ApprovalPolicies.cs](../src/Modules/Approval/InvoiceFlow.Approval/Authorization/ApprovalPolicies.cs), [AppRoles.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/Security/AppRoles.cs), [IntakePoliciesTests.cs](../tests/InvoiceFlow.Intake.Tests/Authorization/IntakePoliciesTests.cs)

**Controllers:** `[Authorize(Policy = "...")]`; same policies.

**Common mistake:** `[Authorize(Roles = "Admin,ApClerk")]` scattered everywhere. Role lists drift, and you can't add a claim check later without touching every endpoint.

**Self-check**

1. How did you enforce approval limits: roles or policies, and why?
2. Why are role names in SharedKernel but policies inside each module?

<details><summary>Answers</summary>

1. Both. The `Approval.Approve` policy (role: Approver) decides who may approve at all. A resource-based policy with a custom requirement compares the user's `approval_limit` claim with the request's amount ([topic 33](#33-ownership-and-resource-based-authorization)). A role alone can't express "up to ₹1 lakh".
2. Roles and claims are system-wide facts that every module and the token issuer must agree on. Policies are module rules: what Intake allows is Intake's business, so they stay internal to it.

</details>

---

### 33. Ownership and resource-based authorization

**What it is:** some rules depend on the **data**, not just the user. You only know whether a vendor may read an invoice after loading the invoice.

**Vendor ownership (`VendorAccess`):** a user with the Vendor role may act only for the vendor in their `vendor_id` claim. Staff roles may act for any vendor.

| Situation | Response | Why |
|---|---|---|
| Vendor submits for another vendor | **403** | They're clearly trying something not allowed |
| Vendor reads another vendor's invoice by ID | **404**, not 403 | A 403 would confirm the invoice exists. 404 reveals nothing |
| Vendor lists invoices with no `vendorId` | Their own invoices only | `TryResolveVendorScope` fills the scope from the claim |
| Vendor lists with another `vendorId` | **403** | |

**Approval limit (resource-based policy):**

1. `ApprovalLimitRequirement` is an empty marker. `ApprovalLimitHandler` holds the logic: succeed if `approval_limit` claim ≥ the request's amount.
2. The handler gets the `ApprovalRequest` as the *resource*, so the endpoint calls it **after** loading the request:
   ```csharp
   await authorizationService.AuthorizeAsync(user, request, ApprovalPolicies.WithinApprovalLimit);
   ```
3. Failure gives 403 and a Warning log.

**Known gap (issue #13):** the approval amount comes from the client when the request is created, so a clerk could send a small amount. Week 4 fixes this: approval requests will be created from the `InvoiceValidated` event, which carries the real invoice total.

**Where:** [VendorAccess.cs](../src/Modules/Intake/InvoiceFlow.Intake/Authorization/VendorAccess.cs), [ApprovalLimitHandler.cs](../src/Modules/Approval/InvoiceFlow.Approval/Authorization/ApprovalLimitHandler.cs), `Approve` in [ApprovalRequestEndpoints.cs](../src/Modules/Approval/InvoiceFlow.Approval/Endpoints/ApprovalRequestEndpoints.cs), [VendorAccessTests.cs](../tests/InvoiceFlow.Intake.Tests/Authorization/VendorAccessTests.cs), [ApprovalLimitHandlerTests.cs](../tests/InvoiceFlow.Approval.Tests/Authorization/ApprovalLimitHandlerTests.cs)

**Controllers:** identical. Inject `IAuthorizationService` and call `AuthorizeAsync(User, resource, policy)` in the action.

**Common mistake:** checking only the role (IDOR, insecure direct object reference): any vendor can read any invoice by guessing or copying IDs.

**Self-check**

1. Why does reading another vendor's invoice return 404 and not 403?
2. Why can't the approval limit be checked with `.RequireAuthorization(...)` on the endpoint?
3. What is the approval-amount gap (#13), and how will Week 4 fix it?

<details><summary>Answers</summary>

1. A 403 tells the caller "this invoice exists, you just can't see it", which leaks information. A 404 looks the same as a non-existent ID.
2. Endpoint authorization runs before the handler, when the approval request hasn't been loaded. The limit check needs the request's amount, so it must run inside the handler, after loading.
3. The clerk sends the amount when creating the approval request, and nothing checks it against the invoice. In Week 4, the Validation module publishes `InvoiceValidated` with the real total, and Approval creates the request from that event, so no client-supplied amount is involved.

</details>

---

## Day 4: API design

### 34. REST conventions and status codes

**What it is:** resources are nouns in the URL (`/api/intake/invoices`), actions are HTTP methods, and the status code tells the client what happened.

| Code | When InvoiceFlow returns it |
|---|---|
| **200 OK** | Successful read; successful approve |
| **201 Created** | Vendor, invoice or approval request created. Includes a `Location` header pointing to the new resource (`CreatedAtRoute` + `.WithName(...)`) |
| **400 Bad Request** | Validation failed, malformed JSON, bad idempotency key |
| **401** / **403** | See [topic 31](#31-secure-by-default-401-vs-403) |
| **404 Not Found** | Unknown ID, or another vendor's invoice |
| **409 Conflict** | Duplicate vendor GSTIN, duplicate invoice, already approved, same idempotency key still processing |
| **422 Unprocessable Entity** | Valid request but refers to an unknown vendor or file; idempotency key reused with a different body |
| **429 Too Many Requests** | Rate limit hit, with `Retry-After` |
| **500** | A bug |

**`approve` as a sub-resource action:** `POST /approval/requests/{id}/approve` is a pragmatic exception to "nouns only", because approving is a command with rules, not just "set Status = Approved".

**Upload returns 201 with no `Location`:** there's no GET endpoint for files yet. Adding one without ownership checks (#14) would let anyone download any PDF.

**Where:** all endpoint files

**Common mistake:** returning 200 with `{ "success": false }` for errors. Clients, proxies and monitoring all rely on the status code.

**Self-check**

1. 409 vs 422: what's the difference in InvoiceFlow?
2. What does `CreatedAtRoute` need, and what does it produce?

<details><summary>Answers</summary>

1. 409: the request clashes with the current state of a resource (a duplicate already exists, or the request is already approved). 422: the request is valid in form, but its content can't be processed (it refers to a vendor or file that doesn't exist, or reuses a key for a different body).
2. A named route (`.WithName("GetInvoiceById")`) and the route values (`new { id = invoice.Id }`). It returns 201 with the body and a `Location` header built from that route, such as `/api/intake/invoices/{id}`.

</details>

---

### 35. Pagination and filtering

**What it is:** `GET /api/intake/invoices?page=2&pageSize=20&vendorId=...&fromDate=2026-04-01&toDate=2026-06-30`

- `page` defaults to 1, and `pageSize` to 20 with a **maximum of 100**, so one request can't ask for a million rows.
- Optional filters: vendor, invoice date range. The validator checks `toDate >= fromDate`.
- The response says where you are: `PagedResponse<T>(Items, Page, PageSize, TotalCount)`. The client can show "page 2 of 7".
- Stable sort (newest first, ties broken by ID), so pages don't overlap ([topic 26](#26-in-memory-repositories-and-the-duplicate-rule)).
- **Vendor scope is applied before filters**: a vendor only ever searches inside their own invoices.

**Why InvoiceFlow needs it:** the AP inbox will hold thousands of invoices. Returning everything is slow for the server, the network and the browser.

**Where:** [ListInvoicesRequest.cs](../src/Modules/Intake/InvoiceFlow.Intake/Endpoints/ListInvoicesRequest.cs), [ListInvoicesRequestValidator.cs](../src/Modules/Intake/InvoiceFlow.Intake/Validation/ListInvoicesRequestValidator.cs), [PagedResponse.cs](../src/Modules/Intake/InvoiceFlow.Intake/Endpoints/PagedResponse.cs), `ListInvoices` in [InvoiceEndpoints.cs](../src/Modules/Intake/InvoiceFlow.Intake/Endpoints/InvoiceEndpoints.cs), `Search` in [InMemoryInvoiceRepository.cs](../src/Modules/Intake/InvoiceFlow.Intake/Infrastructure/InMemoryInvoiceRepository.cs)

**Week 3 note:** with SQL Server, `Skip/Take` becomes `OFFSET/FETCH`, and the filters need indexes. Deep pages (`page=5000`) get slow with offsets. Keyset ("after this ID") paging is the fix if it ever matters.

**Common mistake:** no maximum page size, or paging without a stable order.

**Self-check**

1. Why cap `pageSize` at 100?
2. A vendor calls the list without `vendorId`. What do they get, and where is that decided?

<details><summary>Answers</summary>

1. Without a cap, one request can ask for every invoice, using lots of memory, database time and bandwidth. That is accidental or deliberate denial of service.
2. Only their own invoices. `VendorAccess.TryResolveVendorScope` sets the scope to their `vendor_id` claim before the search runs.

</details>

---

### 36. Idempotency keys

**What it is:** the client sends a unique `Idempotency-Key` header with a POST. If the same request is sent again with the same key (double click, network retry, timeout), the server **returns the first result again instead of doing the work twice**.

How `IdempotencyFilter` + `InMemoryIdempotencyStore` work:

1. **No header:** run normally. The key is optional; the duplicate-invoice rule still protects the business key.
2. **Bad header** (empty, over 64 characters, several values): **400**.
3. The key is stored **per user** (`NameIdentifier`), so two users can use the same key value safely.
4. `TryStart` returns one of four outcomes:

| Outcome | When | Response |
|---|---|---|
| `Started` | New key | Run the handler |
| `Replay` | Same key, same body, finished | The stored result again, plus `Idempotent-Replayed: true` |
| `InProgress` | Same key, same body, still running | **409**, retry later |
| `PayloadMismatch` | Same key, **different body** | **422**, use a new key |

5. "Same body" means **record equality** on `SubmitInvoiceRequest`, comparing every field, not just the invoice number.
6. **Only successes are stored.** A failure (409 duplicate, 400 validation, or an exception) **releases** the key so the client can fix the request and retry with the same key.
7. Entries last **24 hours** (`FakeTimeProvider` makes that testable).

**Filter order matters:** `IdempotencyFilter` is added before `ValidationFilter`, so it wraps validation. A replay returns immediately without validating again.

**Why InvoiceFlow needs it:** a vendor on a slow connection clicks Submit twice. Without idempotency, the second click gets a confusing "409 duplicate". With it, both clicks get the same 201 and the same invoice.

**Where:** [Idempotency/](../src/Modules/Intake/InvoiceFlow.Intake/Idempotency), the filter line in [InvoiceEndpoints.cs](../src/Modules/Intake/InvoiceFlow.Intake/Endpoints/InvoiceEndpoints.cs), [IdempotencyFilterTests.cs](../tests/InvoiceFlow.Intake.Tests/Idempotency/IdempotencyFilterTests.cs), [InMemoryIdempotencyStoreTests.cs](../tests/InvoiceFlow.Intake.Tests/Idempotency/InMemoryIdempotencyStoreTests.cs)

**Known limits:** the store is in memory, so with two app instances each has its own keys, and expired entries are only removed when the same key is used again. It moves to a shared store when the app scales out (database in Week 3, or Redis in Week 11).

**Common mistake:** storing failed results. A temporary failure would then be replayed forever for that key.

**Self-check**

1. How does an idempotency key stop duplicate invoices?
2. Same key, different invoice number: what happens and why?
3. Why release the key when the handler returns 409 Duplicate?

<details><summary>Answers</summary>

1. The first request with a key stores its successful result under (user, key). A retry with the same key and same body gets that stored result back, without running the handler again, so no second invoice is ever attempted.
2. 422 "Idempotency key reused". The stored request doesn't equal the new one (record equality fails), so the key is being misused for a different operation.
3. Only successes are worth replaying. Releasing lets the client correct the problem and try again with the same key, instead of being stuck with a stored error.

</details>

---

### 37. Rate limiting

**What it is:** a limit on how many requests a caller may make in a time window. ASP.NET Core has built-in rate limiting middleware.

Our `submissions` policy:

- **Fixed window:** 10 requests per 60 seconds (from validated options in `appsettings.json`).
- **Partitioned per user:** key `user:{id}` when signed in, otherwise `ip:{address}`. One noisy user doesn't block others.
- **`QueueLimit = 0`:** extra requests are rejected immediately, not queued.
- **Rejected requests** get **429** with a `Retry-After` header (seconds until the window resets), and `UseStatusCodePages` adds the ProblemDetails body.
- Applied to `POST /invoices` and `POST /files`. They **share one budget** per user, because they use the same policy. Reads aren't limited.

**Middleware order:** `UseRateLimiter()` comes **after** `UseAuthentication()`, so the user ID is known when choosing the partition.

**Why InvoiceFlow needs it:** protects against a buggy client in a retry loop, scripted abuse, and runaway costs once AI extraction runs per upload (Week 8).

**Where:** [RateLimitingExtensions.cs](../src/Host/InvoiceFlow.Api/RateLimiting/RateLimitingExtensions.cs), [SubmissionRateLimitOptions.cs](../src/Host/InvoiceFlow.Api/RateLimiting/SubmissionRateLimitOptions.cs), [RateLimitPolicies.cs](../src/SharedKernel/InvoiceFlow.SharedKernel/Http/RateLimitPolicies.cs), [RateLimitPartitionKeyTests.cs](../tests/InvoiceFlow.Api.Tests/RateLimiting/RateLimitPartitionKeyTests.cs)

**Known limits:** counters are in memory, per instance, so 3 instances means 3× the limit (Week 7). Behind a proxy, `RemoteIpAddress` is the proxy's address until forwarded headers are configured (Week 7).

**Controllers:** `[EnableRateLimiting("submissions")]` on the action or controller.

**Common mistake:** one global limit for everyone. A single heavy user then gets every other user rejected.

**Self-check**

1. Why partition by user ID instead of using one global counter?
2. Why must `UseRateLimiter` come after `UseAuthentication`?
3. What does `Retry-After` tell the client?

<details><summary>Answers</summary>

1. Each user gets their own budget, so one user hitting the limit doesn't block everyone else.
2. The partition key reads the user ID from `HttpContext.User`, which is only filled in by authentication. Before it, everyone would look anonymous and be partitioned by IP.
3. How many seconds to wait before trying again, so well-behaved clients back off instead of hammering the API.

</details>

---

### 38. OpenAPI: typed results and documented responses

**What it is:** OpenAPI is a machine-readable description of the API (`/openapi/v1.json`): endpoints, inputs, outputs, status codes. Swagger UI turns it into a web page where you can try requests.

How the document gets the responses right:

| Technique | Documents | Example |
|---|---|---|
| **`TypedResults` + `Results<A, B, C>` return type** | Every status and body type the handler can return | `Results<CreatedAtRoute<InvoiceResponse>, ProblemHttpResult, ForbidHttpResult>` |
| **`.ProducesProblem(code)`** | Responses the return type can't show | 429 (from the rate limiter), 409, 422; 403 (`Forbid()` adds no metadata) |
| **`.ProducesValidationProblem()`** | The 400 from the validation filter | `POST /invoices` |
| **`.WithTags(...)`** | Groups in Swagger UI | Files, Vendors, Invoices, Approval requests |
| **Document transformer** | The Bearer security scheme | Swagger UI shows an **Authorize** button for the JWT |

The OpenAPI document and Swagger UI are **Development only**, and the document is `AllowAnonymous` so the UI can load it.

**Why InvoiceFlow needs it:** in Week 5 the TypeScript client is generated from this document. A wrong or missing response type becomes a wrong type in the frontend. That's why the review changed the upload from `IResult` (documented as "200, no body") to typed results.

**Where:** endpoint files, [BearerSecuritySchemeTransformer.cs](../src/Host/InvoiceFlow.Api/OpenApi/BearerSecuritySchemeTransformer.cs), [Program.cs](../src/Host/InvoiceFlow.Api/Program.cs)

**Controllers:** `ActionResult<T>` and `[ProducesResponseType(...)]` attributes.

**Common mistake:** returning `Results.Ok(...)` (untyped `IResult`). It works at run time, but the documentation can't see what it returns.

**Self-check**

1. `Results.Ok(x)` vs `TypedResults.Ok(x)`: what's the difference for OpenAPI?
2. Why does the 429 need `.ProducesProblem(...)` when the 201 doesn't?
3. Why do the 403s need `.ProducesProblem(403)`?

<details><summary>Answers</summary>

1. `Results.Ok` returns `IResult`, so the generator can't see the type or status. `TypedResults.Ok` returns `Ok<T>`, which carries the status and body type into the return-type signature, so they're documented automatically.
2. The 201 is in the handler's return type. The 429 comes from the rate limiter middleware before the handler runs, so the return type can't show it.
3. `ForbidHttpResult` doesn't describe itself to OpenAPI, unlike `ProblemHttpResult` or `Ok<T>`, so the 403 must be declared by hand.

</details>

---

# Coming next

Things we've discussed or planned, but not built yet. Each becomes a topic when it's done.

| Week | Item | Why it matters for what's already built |
|---|---|---|
| 3 | EF Core, one `DbContext` and schema per module | Replaces the in-memory repositories. Entities need private parameterless constructors; value objects become complex types or conversions |
| 3 | Unique index on `(VendorId, Number, FinancialYear)` | The database guarantees no duplicate invoices, across instances |
| 3 | `rowversion` for approvals | Two approvers clicking at the same moment can't both "win" |
| 3 | File ownership (#14) | Record who uploaded each file, and check it on submission |
| 4 | Domain events and the outbox | `InvoiceValidated` carries the real amount to Approval, fixing #13 |
| 4 | Integration tests (`WebApplicationFactory`, Testcontainers) | The endpoints (submit, approve, upload) get end-to-end tests |
| 4 | Invoice state machine, validation rules engine | `InvoiceStatus` has only `Received` today |
| 7 | Docker and Azure Container Apps | `en-IN` formatting and `Asia/Kolkata` need globalization and tzdata; files move to Blob Storage; forwarded headers for HTTPS and client IPs; idempotency and rate limits must be shared across instances; real health checks |
