# Test Naming Convention

## Standard

All new test methods should follow this pattern:

```
MethodUnderTest_Scenario_ExpectedResult
```

### Examples

```csharp
// ✅ Good — clearly communicates intent
GetUser_WithInvalidId_ReturnsNotFound()
TryDeleteDelegationPolicyRules_PolicyAlreadyDeleted_ReturnsNoChange()
CreateXacmlJsonRequest_MissingSubject_ThrowsArgumentException()

// ❌ Avoid — opaque numbered test cases
HandleRequirementAsync_TC01Async()
PDP_Decision_AltinnApps0001()
WritePolicy_TC03()
```

### Guidelines

| Principle | Detail |
|---|---|
| **No opaque IDs** | `TC01`, `0001` etc. are meaningless without reading the test body. Use a brief description instead. |
| **Use the method name** | Start with the method or endpoint under test. For HTTP endpoints, use the action (e.g., `Post_DeleteRules_…`). |
| **Scenario before result** | Describe the input/state, then the expected outcome. |
| **Keep it concise** | Aim for readable at a glance; abbreviate common terms if unambiguous. |
| **Async suffix** | Not required on test method names (suppressed via `VSTHRD200`). |

### Scope

This convention applies to **all new tests**. Existing tests will be renamed
opportunistically (e.g., when modifying a test class for other reasons) to
avoid large noisy diffs.
