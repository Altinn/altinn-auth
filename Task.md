Optimize Unit Test Performance and Infrastructure

## Summary

Our test suite execution time has grown significantly as we've added EF implementation and expanded test coverage. This issue tracks concrete performance optimizations and infrastructure improvements to reduce test execution time by 30-50% while maintaining reliability. All work will be implemented incrementally with measurement at each step.

## Current Baseline (To Be Measured)

**Test Projects to Optimize**
- `AccessMgmt.Tests` (uses `WebApplicationFixture`, `CustomWebApplicationFactory`)
- `Altinn.AccessManagement.Enduser.Api.Tests` (uses `ApiFixture`)
- `Altinn.AccessManagement.ServiceOwner.Api.Tests`
- Other test projects in solution

**Performance Pain Points**
- Multiple web application instances created per test run
- Database creation and migration happens repeatedly
- Unclear test collection organization leading to sequential execution
- No fixture reuse strategy across test classes
- Expensive mock setup repeated per test class

## Implementation Plan

### Phase 1 - Measure and Analyze Current Performance

**Concrete Actions**

1. Measure baseline performance
   - Run full test suite and capture total execution time per project
   - Identify top 20 slowest individual tests using `dotnet test --logger "console;verbosity=detailed"`
   - Identify top 10 slowest test classes
   - Measure fixture initialization time by adding stopwatch logging

2. Analyze fixture usage patterns
   - Scan all test files for `IClassFixture<>` usage patterns
   - Document which fixture is used where and why
   - Identify duplicate fixture creations (same config, different instances)
   - Map out current test collection definitions

3. Profile database operations
   - Measure time spent in `PostgresServer.NewEFDatabase()`
   - Measure time spent in EF migrations
   - Count how many database instances are created during full test run
   - Identify tests that could share database instances

**Deliverables**
- Baseline performance report with concrete numbers
- Fixture usage map showing current patterns
- Database profiling report
- List of optimization opportunities ranked by impact

---

### Phase 2 - Quick Wins and Low Hanging Fruit

**Concrete Implementations**

1. Optimize CustomWebApplicationFactory
   - Currently very lightweight but creates new instance per test class
   - Action - Implement fixture sharing for test classes with identical configuration
   - Measure - Track reduction in web app instance creation count

2. Add test categorization with traits
   - Add `[Trait("Category", "Unit")]` to fast tests (no DB, no web server)
   - Add `[Trait("Category", "Integration")]` to tests needing infrastructure
   - Add `[Trait("Category", "E2E")]` to full-stack tests
   - Enable running `dotnet test --filter "Category=Unit"` for fast feedback
   - Measure - Compare unit-only test run time vs full suite

3. Fix test collection organization
   - Identify tests currently in same collection that could run in parallel
   - Create new collection definitions for logically independent test groups
   - Document collection naming convention
   - Measure - Track parallel test execution improvement

4. Optimize mock service initialization
   - Identify mock services initialized but not used in all tests
   - Implement lazy initialization for expensive mocks
   - Move shared mock configurations to reusable methods
   - Measure - Track fixture setup time reduction

**Expected Impact** - 15-20% test time reduction

---

### Phase 3 - Database Optimization Strategy

**Concrete Implementations**

1. Implement database sharing for read-only tests
   - Identify tests that only read from database (no writes)
   - Create shared database fixture for read-only test collections
   - Use transaction rollback pattern for write tests needing isolation
   - Measure - Reduction in database creation count

2. Database snapshot and restore approach
   - Create pre-migrated database snapshot
   - Restore from snapshot instead of running migrations each time
   - Implement in `EFPostgresFactory` or `PostgresServer`
   - Measure - Database initialization time reduction

3. Evaluate in-memory database for pure unit tests
   - Identify true unit tests that don't need PostgreSQL features
   - Convert to use EF Core in-memory provider where appropriate
   - Keep PostgreSQL for tests requiring specific DB features
   - Measure - Unit test execution time improvement

**Expected Impact** - 20-25% test time reduction

---

### Phase 4 - Advanced Fixture Optimization

**Concrete Implementations**

1. Implement fixture pooling for WebApplicationFixture
   - Create fixture pool that can be shared across test classes
   - Implement reset and cleanup between test class usage
   - Use `ICollectionFixture<>` where appropriate for sharing
   - Measure - Reduction in fixture creation count

2. Optimize ApiFixture seed operations
   - Review `EnsureSeedOnce` usage and expand where applicable
   - Implement fixture warmup for commonly used data
   - Cache expensive computed values in fixture
   - Measure - Test setup time reduction

3. Create fixture selection decision tree
   - Document concrete rules for which fixture to use when
   - Create code examples for each scenario
   - Add XML documentation to fixture classes with usage guidance
   - Refactor tests using wrong fixture type

**Expected Impact** - 10-15% test time reduction

---

### Phase 5 - Parallelization Improvements

**Concrete Implementations**

1. Audit and fix collection attributes
   - Review all `[Collection("...")]` attributes
   - Identify collections that prevent unnecessary serialization
   - Create new collections for independently runnable test groups
   - Document why each collection exists

2. Configure optimal parallelization settings
   - Add `xunit.runner.json` with appropriate `maxParallelThreads`
   - Configure assembly-level parallelization where safe
   - Document which resources prevent parallel execution
   - Measure - Actual parallel execution improvement

3. Implement resource isolation patterns
   - Use unique ports for each test server instance
   - Implement database name randomization to prevent conflicts
   - Add resource cleanup in fixture disposal
   - Measure - Reduction in test conflicts and retries

**Expected Impact** - 15-20% test time reduction

---

## Automated Verification

**Performance Benchmarks to Implement**

Create performance benchmark tests that fail if thresholds are exceeded.

Example structure for benchmark test class that verifies performance targets and fails build if test suite becomes too slow.

**CI/CD Integration**
- Add test performance reporting to build pipeline
- Track metrics over time
- Alert on performance regression
- Display performance trends in PR comments

---

## Concrete Deliverables

### Code Changes
- [ ] Add `[Trait]` attributes to all test classes
- [ ] Implement database pooling and sharing infrastructure
- [ ] Create reusable fixture base classes
- [ ] Add `xunit.runner.json` configuration files
- [ ] Implement fixture pooling mechanism
- [ ] Add performance benchmark tests

### Documentation
- [ ] Create `TESTING.md` guide in repository root
- [ ] Document fixture selection criteria with examples
- [ ] Add inline XML docs to fixture classes
- [ ] Create troubleshooting guide for common test issues
- [ ] Document test collection naming conventions

### Metrics and Monitoring
- [ ] Baseline performance report
- [ ] Performance tracking dashboard (can be markdown file updated by CI)
- [ ] Test execution time trends
- [ ] Fixture usage statistics

---

## Success Criteria (Measurable)

**Performance Targets**
- [ ] Total test suite execution time reduced by 30-50%
- [ ] No single test takes more than 5 seconds
- [ ] Fixture initialization under 2 seconds per instance
- [ ] Database operations under 1 second using optimization
- [ ] 80%+ of tests can run in parallel

**Quality Targets**
- [ ] Zero increase in test failures
- [ ] Zero new flaky tests introduced
- [ ] All tests properly categorized with traits
- [ ] Test execution is deterministic

**Developer Experience**
- [ ] Unit tests (trait=Unit) complete in under 30 seconds
- [ ] Clear error messages when tests fail
- [ ] Easy to run test subsets locally
- [ ] New developers can understand test infrastructure

---

## Implementation Phases (Rollout Plan)

**Week 1 - Measurement and Analysis**
- Establish baseline metrics
- Profile current performance
- Identify top optimization opportunities
- Create detailed implementation plan for phases 2-5

**Week 2 - Quick Wins (Phase 2)**
- Implement test traits
- Optimize fixture creation
- Fix collection organization
- Measure improvement

**Week 3 - Database Optimization (Phase 3)**
- Implement database sharing
- Add snapshot and restore
- Convert appropriate tests to in-memory
- Measure improvement

**Week 4 - Advanced Optimizations (Phases 4-5)**
- Implement fixture pooling
- Optimize parallelization
- Add performance monitoring
- Final measurement and documentation

---

## Risk Mitigation

**Potential Issues and Solutions**

1. Risk - Changes break existing tests
   - Mitigation - Implement changes incrementally, one project at a time
   - Run full test suite after each change
   - Keep ability to revert individual optimizations

2. Risk - Shared fixtures introduce test coupling
   - Mitigation - Ensure proper cleanup between test runs
   - Use transaction rollback for database isolation
   - Document shared state clearly

3. Risk - Parallelization causes flaky tests
   - Mitigation - Identify and fix resource conflicts
   - Add resource isolation mechanisms
   - Keep option to run tests sequentially for debugging

---

## Files to Modify (Concrete List)

**Fixture Files**
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Fixtures/WebApplicationFixture.cs`
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/CustomWebApplicationFactory.cs`
- `src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.TestUtils/Fixtures/ApiFixture.cs`

**Infrastructure Files**
- `src/apps/Altinn.AccessManagement/test/*/xunit.runner.json` (create if not exists)
- Database factory implementations
- Test collection definitions

**Documentation Files**
- `TESTING.md` (create)
- `docs/testing-best-practices.md` (create)
- Update README with testing section

---

## Tools and Approaches

**Measurement Tools**
- `dotnet test --logger "trx;LogFileName=testresults.trx"` for detailed timing
- Custom stopwatch logging in fixtures
- BenchmarkDotNet for micro-benchmarking if needed
- CI build time tracking

**Implementation Approaches**
- Start with one test project, verify, then expand
- Use feature flags for optimizations if needed
- Keep backward compatibility during transition
- Measure before and after each change

---

## Related Work

- Issue #2810 - EF Delegation Migration (completed - established current fixture patterns)
- Future work - Consider TestContainers for better database isolation

## Priority

**High** - Test performance directly impacts developer productivity and CI/CD efficiency. Every minute saved on test execution multiplies across all developers and pipeline runs.
